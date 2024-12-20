using UnityEngine;
using System;


public enum PlayerControlType
{
    NONE, //default
    COMBAT_MOVEMENT, //In combat during roguelike sections
    CAMP_MOVEMENT, //Players movement in the camp
    BUILDING, //Placing a building from the build menu
    IN_CONVERSATION, //Talking to an NPC
    IN_MENU, // In any menu
    TURRET
}

public class PlayerInput : MonoBehaviour
{
    private static PlayerInput _instance;

    public static PlayerInput Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<PlayerInput>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("PlayerInput instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    public PlayerControlType currentControlType = PlayerControlType.COMBAT_MOVEMENT;

    public event Action<PlayerControlType> OnUpdatePlayerControls;

    public event Action OnYPressed;
    public event Action OnXPressed;
    public event Action OnAPressed;
    public event Action OnBPressed;

    public event Action<Vector2> OnLeftJoystick;
    public event Action<Vector2> OnRightJoystick;

    public event Action OnRBPressed;
    public event Action OnLBPressed;

    public event Action<float> OnRTPressed; // Right Trigger (returns a float value for trigger intensity)
    public event Action<float> OnLTPressed; // Left Trigger (returns a float value for trigger intensity)

    public event Action OnStartPressed;
    public event Action OnSelectPressed;

    public event Action OnLeftStickPressed;
    public event Action OnRightStickPressed;

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

        GameManager.Instance.OnGameModeChanged += UpdatePlayerControls;
    }

    private void OnDestroy()
    {
        UnsubscribeAll();

        GameManager.Instance.OnGameModeChanged -= UpdatePlayerControls;
    }

    public void Start()
    {
        UpdatePlayerControls(currentControlType);        
    }

    private void Update()
    {
        // Movement input (Left joystick)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        OnLeftJoystick?.Invoke(new Vector2(horizontal, vertical));

        // Right joystick
        float rightHorizontal = Input.GetAxis("RightStickHorizontal");
        float rightVertical = Input.GetAxis("RightStickVertical");
        OnRightJoystick?.Invoke(new Vector2(rightHorizontal, rightVertical));

        // Face buttons
        if (Input.GetButtonDown("Y")) OnYPressed?.Invoke();
        if (Input.GetButtonDown("A")) OnAPressed?.Invoke();
        if (Input.GetButtonDown("B")) OnBPressed?.Invoke();
        if (Input.GetButtonDown("X")) OnXPressed?.Invoke();

        // Bumpers
        if (Input.GetButtonDown("RB"))
        {
            OnRBPressed?.Invoke();
        }

        if (Input.GetButtonDown("LB"))
        {
            OnLBPressed?.Invoke();
        }

        //TODO setup these

        //// Triggers
        //float rtValue = Input.GetAxis("RT");
        //if (rtValue > 0.1f) OnRTPressed?.Invoke(rtValue);

        //float ltValue = Input.GetAxis("LT");
        //if (ltValue > 0.1f) OnLTPressed?.Invoke(ltValue);

        //// Joystick presses
        //if (Input.GetButtonDown("LeftStickPress")) OnLeftStickPressed?.Invoke();
        //if (Input.GetButtonDown("RightStickPress")) OnRightStickPressed?.Invoke();

        //// Start and Select buttons
        //if (Input.GetButtonDown("Start")) OnStartPressed?.Invoke();
        if (Input.GetButtonDown("Select")) OnSelectPressed?.Invoke();
    }

    public void UpdatePlayerControls(PlayerControlType playerControlType)
    {
        currentControlType = playerControlType;

        UnsubscribeAll();

        OnUpdatePlayerControls?.Invoke(currentControlType);
    }

    // This method will be called when the game mode changes
    public void UpdatePlayerControls(CurrentGameMode gameMode)
    {
        // Logic to update the control type based on game mode
        switch (gameMode)
        {
            case CurrentGameMode.ROGUE_LITE:
                currentControlType = PlayerControlType.COMBAT_MOVEMENT;
                break;
            case CurrentGameMode.CAMP:
                currentControlType = PlayerControlType.CAMP_MOVEMENT;
                break;
            case CurrentGameMode.TURRET:
                currentControlType = PlayerControlType.TURRET;
                break;
            default:
                currentControlType = PlayerControlType.NONE;
                break;
        }

        // Notify any listeners of the updated control type
        OnUpdatePlayerControls?.Invoke(currentControlType);
    }


    /// <summary>
    /// Unsubscribes all listeners from all actions.
    /// </summary>
    public void UnsubscribeAll()
    {
        OnYPressed = null;
        OnXPressed = null;
        OnAPressed = null;
        OnBPressed = null;

        OnLeftJoystick = null;
        OnRightJoystick = null;

        OnRBPressed = null;
        OnLBPressed = null;

        OnRTPressed = null;
        OnLTPressed = null;

        OnStartPressed = null;
        OnSelectPressed = null;

        OnLeftStickPressed = null;
        OnRightStickPressed = null;
    }
}
