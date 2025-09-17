using UnityEngine;
using System;
using UnityEngine.InputSystem.LowLevel;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using System.Collections;
using Managers;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles all player input events and control type management.
/// Implements the Singleton pattern to ensure only one instance exists.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    #region Singleton Implementation
    private static PlayerInput _instance;
    public static PlayerInput Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerInput>();
                if (_instance == null)
                {
                    Debug.LogWarning("PlayerInput instance not found in the scene!");
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Events
    public event Action<PlayerControlType> OnUpdatePlayerControls;
    
    // Input Device Events
    public event Action<InputDeviceType> OnInputDeviceChanged;
    public event Action OnControllerConnected;
    public event Action OnControllerDisconnected;
    
    // Face Buttons
    public event Action OnYPressed;
    public event Action OnXPressed;
    public event Action OnAPressed;
    public event Action OnBPressed;

    // Joysticks
    public event Action<Vector2> OnLeftJoystick;
    public event Action<Vector2> OnRightJoystick;

    // Shoulder Buttons
    public event Action OnRBPressed;
    public event Action OnLBPressed;
    public event Action<float> OnRTPressed;
    public event Action<float> OnLTPressed;

    // System Buttons
    public event Action OnStartPressed;
    public event Action OnSelectPressed;
    public event Action OnLeftStickPressed;
    public event Action OnRightStickPressed;
    
    // Quick Access Buttons
    public event Action OnQuickSavePressed;
    public event Action OnQuickLoadPressed;
    #endregion

    #region Input Settings
    [Header("Quick Access Keys")]
    [SerializeField] private KeyCode quickSaveKey = KeyCode.F5;
    [SerializeField] private KeyCode quickLoadKey = KeyCode.F9;
    #endregion

    #region Properties
    public PlayerControlType CurrentControlType { get; private set; } = PlayerControlType.COMBAT_NPC_MOVEMENT;
    public InputDeviceType CurrentInputDevice { get; private set; } = InputDeviceType.MOUSE_KEYBOARD;
    private bool _playerInputDisabled = false;
    private bool _controllerWasConnected = false;
    private float _lastInputTime = 0f;
    private const float INPUT_DEBOUNCE_TIME = 0.1f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        SubscribeToGameManager();
    }

    private void OnDestroy()
    {
        UnsubscribeAll();
        if(GameManager.Instance)
            GameManager.Instance.OnGameModeChanged -= UpdatePlayerControls;
    }

    private void Update()
    {
        if (_playerInputDisabled) return;
        DetectInputDevice();
        ProcessInput();
    }
    #endregion

    #region Public Methods
    public void DisablePlayerInput(bool disable)
    {
        _playerInputDisabled = disable;
    }

    /// <summary>
    /// Manually switch to a specific input device type
    /// </summary>
    /// <param name="deviceType">The input device type to switch to</param>
    public void SwitchToInputDevice(InputDeviceType deviceType)
    {
        if (deviceType != CurrentInputDevice)
        {
            SwitchInputDevice(deviceType);
        }
    }

    /// <summary>
    /// Get the current input device type
    /// </summary>
    /// <returns>The current input device type</returns>
    public InputDeviceType GetCurrentInputDevice()
    {
        return CurrentInputDevice;
    }

    public void UpdatePlayerControls(PlayerControlType playerControlType)
    {
        Debug.Log($"Update Controls playerControlType : <color=cyan> {playerControlType} </color>");

        if(playerControlType == PlayerControlType.IN_MENU || playerControlType == PlayerControlType.GENETIC_MUTATION_MOVEMENT || playerControlType == PlayerControlType.MAIN_MENU)
        {
            UpdatePlayerControlsWithDelay(playerControlType, 1);
        }
        else
        {
            // Clear any lingering menu input
            PlayerUIManager.Instance.SetSelectedGameObject(null);
            UpdatePlayerControlsWithDelay(playerControlType, 1);
        }
    }

    public void UpdatePlayerControls(GameMode gameMode)
    {
        Debug.Log($"Update Controls gameMode : <color=green> {gameMode} </color>");
        UpdateControlType(GetControlTypeFromGameMode(gameMode));
    }

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
        OnQuickSavePressed = null;
        OnQuickLoadPressed = null;
        OnInputDeviceChanged = null;
        OnControllerConnected = null;
        OnControllerDisconnected = null;
    }
    #endregion

    #region Private Methods
    private void InitializeSingleton()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void DetectInputDevice()
    {
        // Check for controller input
        bool controllerInput = IsControllerInputDetected();
        bool mouseKeyboardInput = IsMouseKeyboardInputDetected();
        
        // Check for controller connection changes
        bool controllerConnected = IsControllerConnected();
        if (controllerConnected != _controllerWasConnected)
        {
            _controllerWasConnected = controllerConnected;
            if (controllerConnected)
            {
                OnControllerConnected?.Invoke();
                Debug.Log("[PlayerInput] Controller connected");
            }
            else
            {
                OnControllerDisconnected?.Invoke();
                Debug.Log("[PlayerInput] Controller disconnected");
            }
        }
        
        // Determine current input device based on recent input
        InputDeviceType newInputDevice = CurrentInputDevice;
        
        if (controllerInput && !mouseKeyboardInput)
        {
            newInputDevice = InputDeviceType.CONTROLLER;
        }
        else if (mouseKeyboardInput && !controllerInput)
        {
            newInputDevice = InputDeviceType.MOUSE_KEYBOARD;
        }
        
        // Only switch if we have clear input from one device and not the other
        if (newInputDevice != CurrentInputDevice && (controllerInput || mouseKeyboardInput))
        {
            // Debounce input to prevent rapid switching
            if (Time.time - _lastInputTime > INPUT_DEBOUNCE_TIME)
            {
                SwitchInputDevice(newInputDevice);
                _lastInputTime = Time.time;
            }
        }
    }

    private bool IsControllerInputDetected()
    {
        // Check for controller button presses
        if (Input.GetButtonDown("A") || Input.GetButtonDown("B") || Input.GetButtonDown("X") || Input.GetButtonDown("Y") ||
            Input.GetButtonDown("Start") || Input.GetButtonDown("Select") || Input.GetButtonDown("LeftStickPress") || Input.GetButtonDown("RightStickPress") ||
            Input.GetButtonDown("LB") || Input.GetButtonDown("RB") ||
            Input.GetAxis("RT") > 0.1f || Input.GetAxis("LT") > 0.1f)
        {
            return true;
        }
        
        // Check for joystick movement
        Vector2 leftStick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector2 rightStick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));
        
        if (leftStick.magnitude > 0.1f || rightStick.magnitude > 0.1f)
        {
            return true;
        }
        
        return false;
    }

    private bool IsMouseKeyboardInputDetected()
    {
        // Check for mouse input
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) ||
            Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) ||
            Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            return true;
        }
        
        // Check for keyboard input (excluding controller keys)
        if (Input.anyKeyDown)
        {
            // Exclude controller-specific keys to avoid conflicts
            if (!Input.GetKeyDown(KeyCode.JoystickButton0) && !Input.GetKeyDown(KeyCode.JoystickButton1) &&
                !Input.GetKeyDown(KeyCode.JoystickButton2) && !Input.GetKeyDown(KeyCode.JoystickButton3) &&
                !Input.GetKeyDown(KeyCode.JoystickButton4) && !Input.GetKeyDown(KeyCode.JoystickButton5) &&
                !Input.GetKeyDown(KeyCode.JoystickButton6) && !Input.GetKeyDown(KeyCode.JoystickButton7) &&
                !Input.GetKeyDown(KeyCode.JoystickButton8) && !Input.GetKeyDown(KeyCode.JoystickButton9) &&
                !Input.GetKeyDown(KeyCode.JoystickButton10) && !Input.GetKeyDown(KeyCode.JoystickButton11) &&
                !Input.GetKeyDown(KeyCode.JoystickButton12) && !Input.GetKeyDown(KeyCode.JoystickButton13) &&
                !Input.GetKeyDown(KeyCode.JoystickButton14) && !Input.GetKeyDown(KeyCode.JoystickButton15) &&
                !Input.GetKeyDown(KeyCode.JoystickButton16) && !Input.GetKeyDown(KeyCode.JoystickButton17) &&
                !Input.GetKeyDown(KeyCode.JoystickButton18) && !Input.GetKeyDown(KeyCode.JoystickButton19))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsControllerConnected()
    {
        // Check if any joystick is connected
        string[] joystickNames = Input.GetJoystickNames();
        return joystickNames.Length > 0 && !string.IsNullOrEmpty(joystickNames[0]);
    }

    private void SwitchInputDevice(InputDeviceType newDevice)
    {
        if (newDevice == CurrentInputDevice) return;
        
        InputDeviceType previousDevice = CurrentInputDevice;
        CurrentInputDevice = newDevice;
        
        Debug.Log($"[PlayerInput] Input device switched from {previousDevice} to {CurrentInputDevice}");
        
        // Notify listeners about the input device change
        OnInputDeviceChanged?.Invoke(CurrentInputDevice);
        
        // Handle menu navigation based on input device
        if (CurrentInputDevice == InputDeviceType.CONTROLLER && 
            (CurrentControlType == PlayerControlType.IN_MENU || CurrentControlType == PlayerControlType.MAIN_MENU))
        {
            HandleControllerMenuNavigation();
        }
        else if (CurrentInputDevice == InputDeviceType.MOUSE_KEYBOARD)
        {
            HandleMouseKeyboardNavigation();
        }
    }

    private void HandleControllerMenuNavigation()
    {
        // Ensure a button is selected when switching to controller in a menu
        if (PlayerUIManager.Instance != null)
        {
            // Only select a button if none is currently selected
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                // Find the first selectable button in the current menu
                GameObject firstSelectable = FindFirstSelectableButton();
                if (firstSelectable != null)
                {
                    PlayerUIManager.Instance.SetSelectedGameObject(firstSelectable);
                    Debug.Log($"[PlayerInput] Selected first button for controller navigation: {firstSelectable.name}");
                }
            }
        }
    }

    private void HandleMouseKeyboardNavigation()
    {
        // Clear the selected object when switching to mouse/keyboard to allow proper mouse interaction
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            Debug.Log("[PlayerInput] Cleared selected object for mouse/keyboard navigation");
        }
    }

    private GameObject FindFirstSelectableButton()
    {
        // Try to find a button in the current menu
        if (PlayerUIManager.Instance?.currentMenu != null)
        {
            Button[] buttons = PlayerUIManager.Instance.currentMenu.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                if (button.gameObject.activeInHierarchy && button.interactable)
                {
                    return button.gameObject;
                }
            }
        }
        
        // Fallback: find any active selectable UI element
        UnityEngine.UI.Selectable[] selectables = FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsSortMode.None);
        foreach (UnityEngine.UI.Selectable selectable in selectables)
        {
            if (selectable.gameObject.activeInHierarchy && selectable.interactable)
            {
                return selectable.gameObject;
            }
        }
        
        return null;
    }

    private void SubscribeToGameManager()
    {
        GameManager.Instance.OnGameModeChanged += UpdatePlayerControls;
    }

    private void ProcessInput()
    {
        ProcessJoystickInput();
        ProcessFaceButtons();
        ProcessShoulderButtons();
        ProcessSystemButtons();
        ProcessQuickAccessButtons();
    }

    private void ProcessJoystickInput()
    {
        Vector2 leftJoystick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector2 rightJoystick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));
        
        OnLeftJoystick?.Invoke(leftJoystick);
        OnRightJoystick?.Invoke(rightJoystick);
    }

    private void ProcessFaceButtons()
    {
        if (Input.GetButtonDown("Y")) OnYPressed?.Invoke();
        if (Input.GetButtonDown("X")) OnXPressed?.Invoke();
        if (Input.GetButtonDown("A")) OnAPressed?.Invoke();
        if (Input.GetButtonDown("B")) OnBPressed?.Invoke();
    }

    private void ProcessShoulderButtons()
    {
        if (Input.GetButtonDown("RB")) OnRBPressed?.Invoke();
        if (Input.GetButtonDown("LB")) OnLBPressed?.Invoke();

        float rtValue = Input.GetAxis("RT");
        float ltValue = Input.GetAxis("LT");
        
        if (rtValue > 0.1f) OnRTPressed?.Invoke(rtValue);
        if (ltValue > 0.1f) OnLTPressed?.Invoke(ltValue);
    }

    private void ProcessSystemButtons()
    {
        if (Input.GetButtonDown("LeftStickPress")) OnLeftStickPressed?.Invoke();
        if (Input.GetButtonDown("RightStickPress")) OnRightStickPressed?.Invoke();
        if (Input.GetButtonDown("Start")) OnStartPressed?.Invoke();
        if (Input.GetButtonDown("Select")) OnSelectPressed?.Invoke();
    }

    private void ProcessQuickAccessButtons()
    {
        if (Input.GetKeyDown(quickSaveKey)) OnQuickSavePressed?.Invoke();
        if (Input.GetKeyDown(quickLoadKey)) OnQuickLoadPressed?.Invoke();
    }

    private void UpdatePlayerControlsWithDelay(PlayerControlType playerControlType, int frameDelay)
    {
        StartCoroutine(DelayedUpdatePlayerControls(playerControlType, frameDelay));
    }

    private IEnumerator DelayedUpdatePlayerControls(PlayerControlType playerControlType, int frameDelay)
    {
        for (int i = 0; i < frameDelay; i++)
        {
            yield return null;
        }
        UpdateControlType(playerControlType);
    }

    private void UpdateControlType(PlayerControlType playerControlType)
    {
        CurrentControlType = playerControlType;
        ResetControlPositions();
        UnsubscribeAll();
        OnUpdatePlayerControls?.Invoke(CurrentControlType);
        
        // Handle menu navigation based on current input device when entering menu states
        if (playerControlType == PlayerControlType.IN_MENU || playerControlType == PlayerControlType.MAIN_MENU)
        {
            if (CurrentInputDevice == InputDeviceType.CONTROLLER)
            {
                HandleControllerMenuNavigation();
            }
            else if (CurrentInputDevice == InputDeviceType.MOUSE_KEYBOARD)
            {
                HandleMouseKeyboardNavigation();
            }
        }
    }

    private void ResetControlPositions()
    {
        OnLeftJoystick?.Invoke(Vector2.zero);
        OnRightJoystick?.Invoke(Vector2.zero);
    }

    private PlayerControlType GetControlTypeFromGameMode(GameMode gameMode)
    {
        return gameMode switch
        {
            GameMode.MAIN_MENU => PlayerControlType.MAIN_MENU,
            GameMode.ROGUE_LITE => PlayerControlType.COMBAT_NPC_MOVEMENT,
            GameMode.CAMP => PlayerControlType.CAMP_CAMERA_MOVEMENT,
            GameMode.CAMP_ATTACK => PlayerControlType.CAMP_ATTACK_CAMERA_MOVEMENT,
            _ => PlayerControlType.NONE
        };
    }
    #endregion
}
