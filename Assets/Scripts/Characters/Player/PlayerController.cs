using System;
using System.Collections;
using Managers;
using UnityEngine;
using UnityEngine.Windows;

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

    public void PossessNPC(IPossessable npc)
    {
        // Unpossess current NPC if applicable
        _possessedNPC?.OnUnpossess();

        // Assign new NPC and possess it
        _possessedNPC = npc;
        _possessedNPC?.OnPossess();
        
        // Invoke the event when an NPC is possessed
        OnNPCPossessed?.Invoke(_possessedNPC);
    }

    /// <summary>
    /// Updates player controls based on the given PlayerControlType.
    /// </summary>
    /// <param name="controlType">The desired control type.</param>
    public void SetPlayerControlType(PlayerControlType controlType)
    {
        // Subscribe to events based on the new control type
        switch (controlType)
        {
            case PlayerControlType.COMBAT_NPC_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                PlayerInput.Instance.OnYPressed += HandleYInput;
                PlayerInput.Instance.OnAPressed += HandleAInput;
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu(PlayerControlType.COMBAT_NPC_MOVEMENT);
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu(PlayerControlType.COMBAT_NPC_MOVEMENT);
                break;

            case PlayerControlType.CAMP_NPC_MOVEMENT:
            case PlayerControlType.CAMP_CAMERA_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu(PlayerControlType.CAMP_NPC_MOVEMENT);
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu(PlayerControlType.CAMP_NPC_MOVEMENT);
                break;

            case PlayerControlType.TURRET_CAMERA_MOVEMENT:
                PlayerInput.Instance.OnSelectPressed += () => OpenUtilityMenu(PlayerControlType.TURRET_CAMERA_MOVEMENT);
                PlayerInput.Instance.OnStartPressed += () => OpenPauseMenu(PlayerControlType.TURRET_CAMERA_MOVEMENT);
                break;

            default:
                break;
        }

        // Camera setup
        playerCamera.gameObject.SetActive(controlType != PlayerControlType.MAIN_MENU);
    }

    #region private

    private void OpenUtilityMenu(PlayerControlType controlType)
    {
        PlayerUIManager.Instance.utilityMenu.OpenMenu(controlType);
    }

    private void OpenPauseMenu(PlayerControlType controlType)
    {
        PlayerUIManager.Instance.pauseMenu.OpenMenu(controlType);
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

    #endregion
}