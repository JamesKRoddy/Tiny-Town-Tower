using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;

/// <summary>
/// UI component to display current time of day and progress
/// </summary>
public class TimeDisplayUI : MonoBehaviour
{
    #region UI References
    
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI timeOfDayText;
    [SerializeField] private TextMeshProUGUI timeRemainingText;
    [SerializeField] private Scrollbar timeProgressBar;
    [SerializeField] private Image timeOfDayIcon;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Time of Day Icons")]
    [SerializeField] private Sprite dawnIcon;
    [SerializeField] private Sprite dayIcon;
    [SerializeField] private Sprite duskIcon;
    [SerializeField] private Sprite nightIcon;
    
    [Header("Display Settings")]
    [SerializeField] private bool showTimeRemaining = true;
    [SerializeField] private bool showProgressBar = true;
    
    #endregion
    
    #region Private Fields
    
    private TimeManager timeManager;
    private bool isVisible = true;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        // Initial setup will be done in Setup() method called by PlayerUIManager
    }

    public void Setup()
    {
        // Subscribe to game mode changes
        GameManager.Instance.OnGameModeChanged += OnGameModeChanged;
        
        // Find the TimeManager
        if (GameManager.Instance != null)
        {
            timeManager = GameManager.Instance.TimeManager;
        }
        
        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }
        
        if (timeManager == null)
        {
            Debug.LogWarning("[TimeDisplayUI] No TimeManager found in scene!");
            gameObject.SetActive(false);
            return;
        }
        
        // Subscribe to time events
        TimeManager.OnTimeOfDayChanged += OnTimeOfDayChanged;
        TimeManager.OnTimeProgressChanged += OnTimeProgressChanged;
        
        // Initialize display
        UpdateDisplay();
        
        // Set initial visibility based on current game mode
        bool shouldShow = GameManager.Instance.CurrentGameMode == GameMode.CAMP || 
                         GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE;
        gameObject.SetActive(shouldShow);
        
        // Set initial visibility state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
        }
    }

    private void OnGameModeChanged(GameMode newGameMode)
    {
        // Show the UI in CAMP and ROGUE_LITE modes
        bool shouldShow = newGameMode == GameMode.CAMP || newGameMode == GameMode.ROGUE_LITE;
        gameObject.SetActive(shouldShow);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from game mode changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameModeChanged -= OnGameModeChanged;
        }
        
        // Unsubscribe from time events
        TimeManager.OnTimeOfDayChanged -= OnTimeOfDayChanged;
        TimeManager.OnTimeProgressChanged -= OnTimeProgressChanged;
    }
    
    private void Update()
    {
        // Update time remaining text every frame for smooth countdown
        if (showTimeRemaining && timeRemainingText != null && timeManager != null)
        {
            UpdateTimeRemainingText();
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    /// <summary>
    /// Handle time of day changes
    /// </summary>
    private void OnTimeOfDayChanged(TimeOfDay newTimeOfDay)
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// Handle time progress updates
    /// </summary>
    private void OnTimeProgressChanged(float progress)
    {
        UpdateProgressBar(progress);
    }
    
    #endregion
    
    #region Display Updates
    
    /// <summary>
    /// Update the entire display
    /// </summary>
    private void UpdateDisplay()
    {
        if (timeManager == null) return;
        
        UpdateTimeOfDayText();
        UpdateTimeOfDayIcon();
        UpdateProgressBar(timeManager.CurrentTimeProgress);
        UpdateTimeRemainingText();
    }
    
    /// <summary>
    /// Update the time of day text
    /// </summary>
    private void UpdateTimeOfDayText()
    {
        if (timeOfDayText == null || timeManager == null) return;
        
        string timeString = timeManager.GetTimeOfDayString();
        timeOfDayText.text = timeString;
        
        // Change text color based on time of day
        Color textColor = timeManager.CurrentTimeOfDay switch
        {
            TimeOfDay.Dawn => new Color(1f, 0.8f, 0.6f), // Light orange
            TimeOfDay.Day => Color.yellow,
            TimeOfDay.Dusk => new Color(1f, 0.5f, 0.2f), // Orange
            TimeOfDay.Night => new Color(0.6f, 0.6f, 1f), // Light blue
            _ => Color.white
        };
        
        timeOfDayText.color = textColor;
    }
    
    /// <summary>
    /// Update the time remaining text
    /// </summary>
    private void UpdateTimeRemainingText()
    {
        if (timeRemainingText == null || timeManager == null) return;
        
        if (showTimeRemaining)
        {
            timeRemainingText.text = timeManager.GetFormattedTimeString();
            timeRemainingText.gameObject.SetActive(true);
        }
        else
        {
            timeRemainingText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update the time of day icon
    /// </summary>
    private void UpdateTimeOfDayIcon()
    {
        if (timeOfDayIcon == null || timeManager == null) return;
        
        Sprite iconToShow = timeManager.CurrentTimeOfDay switch
        {
            TimeOfDay.Dawn => dawnIcon,
            TimeOfDay.Day => dayIcon,
            TimeOfDay.Dusk => duskIcon,
            TimeOfDay.Night => nightIcon,
            _ => dayIcon
        };
        
        if (iconToShow != null)
        {
            timeOfDayIcon.sprite = iconToShow;
            timeOfDayIcon.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No icon to show for time of day: " + timeManager.CurrentTimeOfDay);
            timeOfDayIcon.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update the progress bar
    /// </summary>
    private void UpdateProgressBar(float progress)
    {
        if (timeProgressBar == null) return;
        
        if (showProgressBar)
        {
            timeProgressBar.value = progress;
            timeProgressBar.gameObject.SetActive(true);
            
            // Change progress bar colors based on time of day
            if (timeManager != null)
            {
                Color barColor = timeManager.CurrentTimeOfDay switch
                {
                    TimeOfDay.Dawn => new Color(1f, 0.8f, 0.4f, 0.8f),
                    TimeOfDay.Day => new Color(1f, 1f, 0.4f, 0.8f),
                    TimeOfDay.Dusk => new Color(1f, 0.6f, 0.2f, 0.8f),
                    TimeOfDay.Night => new Color(0.4f, 0.4f, 1f, 0.8f),
                    _ => new Color(1f, 1f, 1f, 0.8f)
                };
                
                // Apply color to the scrollbar's handle (the moving part)
                var handleImage = timeProgressBar.handleRect?.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.color = barColor;
                }
                
                // Optionally apply color to the background as well
                var backgroundImage = timeProgressBar.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    Color backgroundColor = barColor;
                    backgroundColor.a = 0.3f; // Make background more transparent
                    backgroundImage.color = backgroundColor;
                }
            }
        }
        else
        {
            timeProgressBar.gameObject.SetActive(false);
        }
    }
    
    #endregion
    
    #region Visibility Control
    
    /// <summary>
    /// Show or hide the time display
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Toggle visibility
    /// </summary>
    public void ToggleVisibility()
    {
        SetVisible(!isVisible);
    }
    
    #endregion
    
    #region Public Configuration
    
    /// <summary>
    /// Configure display options
    /// </summary>
    public void ConfigureDisplay(bool showRemaining, bool showProgress)
    {
        showTimeRemaining = showRemaining;
        showProgressBar = showProgress;
        
        UpdateDisplay();
    }
    
    #endregion
}
