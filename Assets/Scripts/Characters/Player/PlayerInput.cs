using UnityEngine;
using System;
using UnityEngine.InputSystem.LowLevel;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using System.Collections;
using Managers;
using UnityEngine.EventSystems;

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
    #endregion

    #region Properties
    public PlayerControlType CurrentControlType { get; private set; } = PlayerControlType.COMBAT_NPC_MOVEMENT;
    private bool _playerInputDisabled = false;
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
        ProcessInput();
    }
    #endregion

    #region Public Methods
    public void DisablePlayerInput(bool disable)
    {
        _playerInputDisabled = disable;
    }

    public void UpdatePlayerControls(PlayerControlType playerControlType)
    {
        Debug.Log($"Update Controls playerControlType : <color=cyan> {playerControlType} </color>");

        // Clear any lingering menu input
        EventSystem.current.SetSelectedGameObject(null);

        if(playerControlType == PlayerControlType.IN_MENU)
        {
            UpdatePlayerControlsWithDelay(playerControlType, 1);
        }
        else
        {
            UpdateControlType(playerControlType);
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
            GameMode.TURRET => PlayerControlType.TURRET_CAMERA_MOVEMENT,
            _ => PlayerControlType.NONE
        };
    }
    #endregion
}
