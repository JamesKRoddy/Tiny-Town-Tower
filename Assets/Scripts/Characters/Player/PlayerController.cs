using System;
using System.Collections;
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

    [Header("Camera")]
    public PlayerCamera playerCamera;

    

    private Collider playerCollider;
    private PlayerCombat playerCombat;

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

        playerCombat = GetComponent<PlayerCombat>();
    }

    private IEnumerator Start()
    {
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;

        yield return new WaitForEndOfFrame(); //This is just to take over the npc after their setup has happened

        _possessedNPC = GetComponentInChildren<IPossessable>();
        PossessNPC(_possessedNPC);
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
            case PlayerControlType.NONE:
                // No controls are active
                break;
            case PlayerControlType.COMBAT_NPC_MOVEMENT:
                if (PlayerInput.Instance != null)
                {
                    PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                    PlayerInput.Instance.OnYPressed += HandleYInput;
                    PlayerInput.Instance.OnAPressed += HandleAInput;
                    PlayerInput.Instance.OnSelectPressed += OpenCombatUtilityMenu;
                }
                break;
            case PlayerControlType.CAMP_NPC_MOVEMENT:
                if (PlayerInput.Instance != null)
                {
                    PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                    PlayerInput.Instance.OnSelectPressed += OpenCampUtilityMenu;
                }
                break;
            case PlayerControlType.TURRET_CAMERA_MOVEMENT:
                {
                    PlayerInput.Instance.OnSelectPressed += OpenTurretUtilityMenu;
                }
                break;
            case PlayerControlType.CAMP_CAMERA_MOVEMENT:
                if (PlayerInput.Instance != null)
                {
                    PlayerInput.Instance.OnSelectPressed += OpenCampUtilityMenu;
                }
                break;
            default:
                break;
        }
    }

    #region private

    private void OpenCampUtilityMenu()
    {
        UtilityMenu.Instance.OpenMenu(PlayerControlType.CAMP_NPC_MOVEMENT);
    }

    private void OpenCombatUtilityMenu()
    {
        UtilityMenu.Instance.OpenMenu(PlayerControlType.COMBAT_NPC_MOVEMENT);
    }

    private void OpenTurretUtilityMenu()
    {
        UtilityMenu.Instance.OpenMenu(PlayerControlType.TURRET_CAMERA_MOVEMENT);
    }

    private void HandleLeftJoystick(Vector2 input)
    {
        if(_possessedNPC != null && PlayerInput.Instance.currentControlType != PlayerControlType.IN_CONVERSATION)
        {
            _possessedNPC.Movement(new Vector3(input.x, 0, input.y));
        }
    }

    private void HandleAInput()
    {
        if (_possessedNPC != null && PlayerInput.Instance.currentControlType == PlayerControlType.COMBAT_NPC_MOVEMENT)
        {
            _possessedNPC.Attack();
        }
    }

    private void HandleYInput()
    {
        if (_possessedNPC != null && PlayerInput.Instance.currentControlType == PlayerControlType.COMBAT_NPC_MOVEMENT)
        {
            _possessedNPC.Dash();
        }
    }

    #endregion
}