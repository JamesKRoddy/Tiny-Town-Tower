using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class SpeedDebugMenu : BaseDebugMenu
{
    [Header("Speed Debug Menu UI")]
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TMP_Text speedValueText;
    [SerializeField] private TMP_Text statusText;
    
    [Header("Speed Control Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button normalSpeedButton;
    [SerializeField] private Button fastSpeedButton;
    [SerializeField] private Button ultraFastSpeedButton;
    [SerializeField] private Button resetButton;
    
    [Header("Speed Settings")]
    [SerializeField] private float minSpeed = 0.1f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float fastSpeed = 2f;
    [SerializeField] private float ultraFastSpeed = 5f;
    
    private float lastTimeScale = 1f;
    private bool isPaused = false;
    private StringBuilder stringBuilder = new StringBuilder();
    
    protected void Awake()
    {
        // Set menu properties
        menuName = "Speed Debug Menu";
        toggleKey = KeyCode.F3;
        
        // Find components if not assigned
        if (speedSlider == null)
            speedSlider = GetComponentInChildren<Slider>();
        if (speedValueText == null)
            speedValueText = GetComponentInChildren<TMP_Text>();
    }
    
    public override void RegisterMenu()
    {
        base.RegisterMenu();
        SetupUI();
        UpdateDisplay();
    }
    
    private void SetupUI()
    {
        // Setup speed slider
        if (speedSlider != null)
        {
            speedSlider.minValue = minSpeed;
            speedSlider.maxValue = maxSpeed;
            speedSlider.value = Time.timeScale;
            speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
        }
        
        // Setup buttons
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
        
        if (normalSpeedButton != null)
            normalSpeedButton.onClick.AddListener(() => SetGameSpeed(normalSpeed));
        
        if (fastSpeedButton != null)
            fastSpeedButton.onClick.AddListener(() => SetGameSpeed(fastSpeed));
        
        if (ultraFastSpeedButton != null)
            ultraFastSpeedButton.onClick.AddListener(() => SetGameSpeed(ultraFastSpeed));
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSpeed);
    }
    
    private void Update()
    {
        // Update display when menu is active
        if (gameObject.activeInHierarchy)
        {
            UpdateDisplay();
        }
        
        // Handle keyboard shortcuts even when menu is closed
        HandleKeyboardShortcuts();
    }
    
    private void HandleKeyboardShortcuts()
    {
        // Only process shortcuts in editor or development builds
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
        {
            TogglePause();
        }
        else if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetGameSpeed(normalSpeed);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetGameSpeed(fastSpeed);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetGameSpeed(ultraFastSpeed);
        }
        #endif
    }
    
    private void OnSpeedSliderChanged(float value)
    {
        SetGameSpeed(value);
    }
    
    public void SetGameSpeed(float speed)
    {
        speed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        
        if (speed > 0)
        {
            isPaused = false;
            lastTimeScale = speed;
        }
        
        Time.timeScale = speed;
        
        // Update slider if it exists and value is different
        if (speedSlider != null && Mathf.Abs(speedSlider.value - speed) > 0.01f)
        {
            speedSlider.value = speed;
        }
        
        UpdateDisplay();
        Debug.Log($"[SpeedDebugMenu] Game speed set to {speed:F2}x");
    }
    
    public void TogglePause()
    {
        if (isPaused)
        {
            // Unpause - restore last speed
            SetGameSpeed(lastTimeScale);
            isPaused = false;
        }
        else
        {
            // Pause - save current speed and set to 0
            if (Time.timeScale > 0)
            {
                lastTimeScale = Time.timeScale;
            }
            Time.timeScale = 0f;
            isPaused = true;
        }
        
        UpdateDisplay();
        Debug.Log($"[SpeedDebugMenu] Game {(isPaused ? "paused" : "unpaused")}");
    }
    
    public void ResetSpeed()
    {
        SetGameSpeed(normalSpeed);
        isPaused = false;
        Debug.Log("[SpeedDebugMenu] Speed reset to normal");
    }
    
    private void UpdateDisplay()
    {
        // Update speed value text
        if (speedValueText != null)
        {
            if (isPaused || Time.timeScale == 0f)
            {
                speedValueText.text = "PAUSED";
                speedValueText.color = Color.red;
            }
            else
            {
                speedValueText.text = $"{Time.timeScale:F2}x";
                speedValueText.color = Time.timeScale == normalSpeed ? Color.white : Color.yellow;
            }
        }
        
        // Update status text with detailed info
        if (statusText != null)
        {
            stringBuilder.Clear();
            stringBuilder.AppendLine("=== SPEED DEBUG MENU ===");
            stringBuilder.AppendLine();
            
            if (isPaused || Time.timeScale == 0f)
            {
                stringBuilder.AppendLine("<color=red>GAME PAUSED</color>");
                stringBuilder.AppendLine($"Last Speed: {lastTimeScale:F2}x");
            }
            else
            {
                stringBuilder.AppendLine($"Current Speed: <color=yellow>{Time.timeScale:F2}x</color>");
                
                if (Time.timeScale == normalSpeed)
                {
                    stringBuilder.AppendLine("<color=green>Normal Speed</color>");
                }
                else if (Time.timeScale < normalSpeed)
                {
                    stringBuilder.AppendLine("<color=cyan>Slow Motion</color>");
                }
                else
                {
                    stringBuilder.AppendLine("<color=orange>Fast Forward</color>");
                }
            }
            
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--- KEYBOARD SHORTCUTS ---");
            stringBuilder.AppendLine("0/Numpad0: Toggle Pause");
            stringBuilder.AppendLine($"1/Numpad1: Normal ({normalSpeed}x)");
            stringBuilder.AppendLine($"2/Numpad2: Fast ({fastSpeed}x)");
            stringBuilder.AppendLine($"3/Numpad3: Ultra Fast ({ultraFastSpeed}x)");
            stringBuilder.AppendLine($"F3: Toggle This Menu");
            
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--- GAME STATE ---");
            stringBuilder.AppendLine($"Frame Rate: {(1f / Time.unscaledDeltaTime):F0} FPS");
            stringBuilder.AppendLine($"Scaled Delta Time: {Time.deltaTime:F4}s");
            stringBuilder.AppendLine($"Unscaled Delta Time: {Time.unscaledDeltaTime:F4}s");
            stringBuilder.AppendLine($"Fixed Delta Time: {Time.fixedDeltaTime:F4}s");
            
            statusText.text = stringBuilder.ToString();
        }
    }
    
    public override void ToggleMenu()
    {
        base.ToggleMenu();
        
        // Update immediately when menu is shown
        if (gameObject.activeInHierarchy)
        {
            UpdateDisplay();
        }
    }
    
    public override void ShowMenu()
    {
        base.ShowMenu();
        UpdateDisplay();
    }
    
    // Public methods for external access
    public float GetCurrentSpeed() => Time.timeScale;
    public bool IsPaused() => isPaused || Time.timeScale == 0f;
    public void SetPaused(bool paused) => isPaused = paused;
    
    // Called when the object is destroyed
    private void OnDestroy()
    {
        // Reset time scale when debug menu is destroyed
        if (Time.timeScale != normalSpeed)
        {
            Time.timeScale = normalSpeed;
            Debug.Log("[SpeedDebugMenu] Reset time scale to normal on destroy");
        }
    }
    
    // Reset speed when entering/exiting play mode in editor
    #if UNITY_EDITOR
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && Time.timeScale != normalSpeed)
        {
            Debug.Log("[SpeedDebugMenu] Application lost focus, resetting speed to normal");
            ResetSpeed();
        }
    }
    #endif
}
