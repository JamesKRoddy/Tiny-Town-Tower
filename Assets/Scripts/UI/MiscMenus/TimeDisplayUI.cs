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
    [SerializeField] private TextMeshProUGUI timeOfDayText;      // Shows current time (e.g., "2:30 PM")
    [SerializeField] private TextMeshProUGUI timePeriodText;     // Shows time period (e.g., "Afternoon")
    [SerializeField] private Scrollbar timeProgressBar;
    [SerializeField] private Image timeOfDayIcon;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Time of Day Icons")]
    [SerializeField] private Sprite dawnIcon;
    [SerializeField] private Sprite dayIcon;
    [SerializeField] private Sprite duskIcon;
    [SerializeField] private Sprite nightIcon;
    
    [Header("Display Settings")]
    [SerializeField] private bool showCurrentTime = true;
    [SerializeField] private bool showTimePeriod = true;
    [SerializeField] private bool showProgressBar = true;
    [SerializeField] private bool use24HourFormat = false;
    
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
        Debug.Log("OnGameModeChanged: " + newGameMode);
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
        // Update time display every frame for smooth time progression
        if (timeManager != null)
        {
            UpdateTimeOfDayText();
            UpdateTimePeriodText();
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
        UpdateTimePeriodText();
        UpdateTimeOfDayIcon();
        UpdateProgressBar(timeManager.TotalDayProgress);
    }
    
    /// <summary>
    /// Update the time of day text
    /// </summary>
    private void UpdateTimeOfDayText()
    {
        if (timeOfDayText == null || timeManager == null) return;
        
        if (showCurrentTime)
        {
            string timeString = use24HourFormat ? 
                timeManager.GetFormattedTime24Hour() : 
                timeManager.GetFormattedTime12Hour();
            timeOfDayText.text = timeString;
            timeOfDayText.gameObject.SetActive(true);
        }
        else
        {
            timeOfDayText.gameObject.SetActive(false);
        }
        
        // Change text color based on time of day
        Color textColor = GetTimeBasedColor();
        timeOfDayText.color = textColor;
    }
    
    /// <summary>
    /// Update the time period text
    /// </summary>
    private void UpdateTimePeriodText()
    {
        if (timePeriodText == null || timeManager == null) return;
        
        if (showTimePeriod)
        {
            string periodString = timeManager.GetTimePeriodDescription();
            timePeriodText.text = periodString;
            timePeriodText.gameObject.SetActive(true);
        }
        else
        {
            timePeriodText.gameObject.SetActive(false);
        }
        
        // Change text color based on time of day
        Color textColor = GetTimeBasedColor();
        timePeriodText.color = textColor;
    }
    
    /// <summary>
    /// Get color based on current time of day
    /// </summary>
    private Color GetTimeBasedColor()
    {
        float timeHours = timeManager.GetCurrentTimeHours();
        
        return timeHours switch
        {
            >= 5f and < 7f => new Color(1f, 0.8f, 0.6f),   // Early morning - light orange
            >= 7f and < 12f => new Color(1f, 1f, 0.7f),    // Morning - light yellow
            >= 12f and < 17f => new Color(1f, 1f, 0.5f),   // Afternoon - bright yellow
            >= 17f and < 19f => new Color(1f, 0.7f, 0.4f), // Evening - orange
            >= 19f and < 22f => new Color(0.8f, 0.6f, 1f), // Night - light purple
            _ => new Color(0.6f, 0.6f, 1f)                  // Late night - light blue
        };
    }
    

    
    /// <summary>
    /// Update the time of day icon based on actual time
    /// </summary>
    private void UpdateTimeOfDayIcon()
    {
        if (timeOfDayIcon == null || timeManager == null) return;
        
        float timeHours = timeManager.GetCurrentTimeHours();
        
        Sprite iconToShow = timeHours switch
        {
            >= 5f and < 7f => dawnIcon,     // Dawn/Early Morning
            >= 7f and < 17f => dayIcon,    // Day (7 AM to 5 PM)
            >= 17f and < 19f => duskIcon,  // Dusk/Evening
            _ => nightIcon                  // Night (7 PM to 5 AM)
        };
        
        if (iconToShow != null)
        {
            timeOfDayIcon.sprite = iconToShow;
            timeOfDayIcon.gameObject.SetActive(true);
        }
        else
        {
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
                    TimeOfDay.Day => new Color(1f, 1f, 0.4f, 0.8f),
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
    public void ConfigureDisplay(bool showTime, bool showPeriod, bool showProgress, bool use24Hour = false)
    {
        showCurrentTime = showTime;
        showTimePeriod = showPeriod;
        showProgressBar = showProgress;
        use24HourFormat = use24Hour;
        
        UpdateDisplay();
    }
    
    #endregion
}
