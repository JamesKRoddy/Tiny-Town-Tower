using System;
using System.Collections;
using Managers;
using UnityEngine;
using UnityEngine.Windows;
using System.Collections.Generic;
using CampBuilding;

public class PlayerController : MonoBehaviour, IControllerInput
{
    // Static instance of the PlayerUIManager class
    private static PlayerController _instance;

    // Public property to access the instance
    public static PlayerController Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<PlayerController>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("PlayerController instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("NPC Possesion")]
    public IPossessable _possessedNPC;
    public event Action<IPossessable> OnNPCPossessed;
    private CharacterInventory _cachedInventory;

    [Header("Camera")]
    public PlayerCamera playerCamera;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            _instance = this; // Set the instance
            DontDestroyOnLoad(gameObject); // Optionally persist across scenes
        }

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    private IEnumerator Start()
    {      
        yield return new WaitForEndOfFrame(); //This is just to take over the npc after their setup has happened

        _possessedNPC = GetComponentInChildren<IPossessable>();
        if(_possessedNPC != null)
        {
            PossessNPC(_possessedNPC);
            PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
        }
    }

    void Update()
    {
        if (_possessedNPC != null)
        {
            _possessedNPC.PossessedUpdate();
        }
    }

    private void OnDestroy()
    {
        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    /// <summary>
    /// Possess a new NPC, pass null if just unpossessing.
    /// </summary>
    /// <param name="npc">The NPC to possess.</param>
    public void PossessNPC(IPossessable npc = null)
    {
        // Unpossess current NPC if applicable
        _possessedNPC?.OnUnpossess();

        // Assign new NPC and possess it
        _possessedNPC = npc;
        _possessedNPC?.OnPossess();
        
        // Update cached inventory
        _cachedInventory = _possessedNPC?.GetTransform().GetComponent<CharacterInventory>();
        
        // Invoke the event when an NPC is possessed
        OnNPCPossessed?.Invoke(_possessedNPC);
    }

    /// <summary>
    /// Gets the character inventory from the currently possessed NPC.
    /// </summary>
    /// <returns>The CharacterInventory component if an NPC is possessed, null otherwise.</returns>
    public CharacterInventory GetCharacterInventory()
    {
        return _cachedInventory;
    }

    /// <summary>
    /// Updates the NPC's position after a scene transition.
    /// If no position is provided, it will default to the PlayerSpawnPoint.
    /// If a Transform is provided, it will use the position of the Transform.
    /// If a Vector3 is provided, it will use the Vector3 as the position. Has to be a vector3 because the scene transition manager will clear the transform.
    /// </summary>
    /// <param name="position"></param>
    public void UpdateNPCPosition(object position)
    {   
        MonoBehaviour npc = _possessedNPC as MonoBehaviour;
        if (npc != null)
        {
            if (position == null)
            {
                // Default to finding PlayerSpawnPoint if no position provided
                Transform spawnPoint = GameObject.Find("PlayerSpawnPoint")?.transform;
                if (spawnPoint == null)
                {
                    Debug.LogError("No player spawn point found");
                    npc.transform.position = Vector3.zero;
                }
                else
                {
                    npc.transform.position = spawnPoint.position;
                }
            }
            else if (position is Transform transform)
            {
                npc.transform.position = transform.position;
            }
            else if (position is Vector3 vector)
            {
                npc.transform.position = vector;
            }
        }
    }

    /// <summary>
    /// Updates player controls based on the given PlayerControlType.
    /// </summary>
    /// <param name="controlType">The desired control type.</param>
    public void SetPlayerControlType(PlayerControlType controlType)
    {
        //Disabling the collider so the npc doesnt hit any colliders during transitions
        var npcBehaviour = _possessedNPC as MonoBehaviour;
        Collider collider = null;
        if (npcBehaviour != null)
        {
            collider = npcBehaviour.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        // Subscribe to events based on the new control type
        switch (controlType)
        {
            case PlayerControlType.COMBAT_NPC_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                PlayerInput.Instance.OnYPressed += HandleYInput;
                PlayerInput.Instance.OnAPressed += HandleAInput;
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu();
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu();
                playerCamera.UpdateTarget((_possessedNPC as MonoBehaviour)?.transform);
                break;

            case PlayerControlType.ROBOT_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu();
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu();
                playerCamera.UpdateTarget((_possessedNPC as MonoBehaviour)?.transform);
                break;

            case PlayerControlType.ROBOT_WORKING:
                PlayerInput.Instance.OnBPressed += HandleRobotStopWork;
                
                if(PlayerUIManager.Instance.currentMenu != null) //Only swap back to the game if we are not in a menu
                {
                    PlayerInput.Instance.OnBPressed += () => PlayerUIManager.Instance.BackPressed();
                } else{
                    PlayerInput.Instance.OnBPressed += () => PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.ROBOT_MOVEMENT);
                }
                break;

            case PlayerControlType.CAMP_NPC_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu();
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu();
                playerCamera.UpdateTarget((_possessedNPC as MonoBehaviour)?.transform);
                break;
            case PlayerControlType.CAMP_CAMERA_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu();
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu();
                PlayerInput.Instance.OnAPressed += HandleWorkAssignment;
                break;

            case PlayerControlType.CAMP_WORK_ASSIGNMENT:
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                PlayerInput.Instance.OnAPressed += HandleWorkAssignment;
                PlayerInput.Instance.OnBPressed += () => ReturnToSettlerMenu();
                PlayerInput.Instance.OnBPressed += () => CloseSelectionPopup();
                break;

            case PlayerControlType.CAMP_ATTACK_CAMERA_MOVEMENT:
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu();
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu();
                break;
            case PlayerControlType.TRANSITION:
                if(collider != null)
                {
                    collider.enabled = false;
                }
                break;
            default:
                break;
        }

        // Camera setup
        playerCamera.gameObject.SetActive(controlType != PlayerControlType.MAIN_MENU);
    }

    #region private

    private void OpenUtilityMenu()
    {
        PlayerUIManager.Instance.utilityMenu.OpenMenu();
    }

    private void OpenPauseMenu()
    {
        PlayerUIManager.Instance.pauseMenu.OpenMenu();
    }

    private void HandleLeftJoystick(Vector2 input)
    {
        if(_possessedNPC != null && PlayerInput.Instance.CurrentControlType != PlayerControlType.IN_CONVERSATION)
        {
            _possessedNPC.Movement(new Vector3(input.x, 0, input.y));
        }
    }

    private void HandleAInput()
    {
        if (_possessedNPC != null && PlayerInput.Instance.CurrentControlType == PlayerControlType.COMBAT_NPC_MOVEMENT)
        {
            _possessedNPC.Attack();
        }
    }

    private void HandleYInput()
    {
        if (_possessedNPC != null && PlayerInput.Instance.CurrentControlType == PlayerControlType.COMBAT_NPC_MOVEMENT)
        {
            _possessedNPC.Dash();
        }
    }

    private void HandleWorkAssignment()
    {
        // Check for work tasks at the camera's detection point
        WorkTask workTask = playerCamera.GetWorkTaskAtDetectionPoint();

        if (workTask != null)
        {
            // Check if it's a building first
            Building building = workTask.GetComponent<Building>();
            if (building != null)
            {
                if(CampManager.Instance.WorkManager.IsNPCForAssignmentSet()){
                    CreateWorkTaskOptions(building, (task) => {
                        CampManager.Instance.WorkManager.AssignWorkToBuilding(task);
                    });
                } else{
                    CampManager.Instance.BuildManager.BuildingSelectionOptions(building);
                }
                return;
            }

            // Check if it's a turret
            BaseTurret turret = workTask.GetComponent<BaseTurret>();
            if (turret != null)
            {
                Debug.Log($"Found turret: {turret.name}");
                if(CampManager.Instance.WorkManager.IsNPCForAssignmentSet()){
                    Debug.Log("NPC for assignment is set, creating work task options");
                    CreateWorkTaskOptions(turret, (task) => {
                        CampManager.Instance.WorkManager.AssignWorkToBuilding(task);
                    });
                } else{
                    Debug.Log("No NPC for assignment, showing turret selection options");
                    CampManager.Instance.BuildManager.TurretSelectionOptions(turret);
                }
                return;
            }
        }
    }

    private void HandleRobotStopWork()
    {
        if (_possessedNPC is RobotCharacterController robot)
        {
            robot.StopWork();
        }
    }

    private void ReturnToSettlerMenu()
    {
        PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(true, 0.05f);
    }

    private void CloseSelectionPopup()
    {
        CampManager.Instance.WorkManager.CloseSelectionPopup();
    }

    private void CreateWorkTaskOptions(Building building, Action<WorkTask> onTaskSelected)
    {
        var workTasks = building.GetComponents<WorkTask>();
        CampManager.Instance.WorkManager.ShowWorkTaskOptions(workTasks, null, (task) => {
            onTaskSelected(task);
            CampManager.Instance.WorkManager.CloseSelectionPopup();
        });
    }

    private void CreateWorkTaskOptions(BaseTurret turret, Action<WorkTask> onTaskSelected)
    {
        var workTasks = turret.GetComponents<WorkTask>();
        CampManager.Instance.WorkManager.ShowWorkTaskOptions(workTasks, null, (task) => {
            onTaskSelected(task);
            CampManager.Instance.WorkManager.CloseSelectionPopup();
        });
    }

    #endregion
}