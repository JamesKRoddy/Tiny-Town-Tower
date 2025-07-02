using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using System.Collections;
using Enemies;

public class CampWaveUI : MonoBehaviour
{
    [Header("Wave Warning UI")]
    [SerializeField] private GameObject waveWarningPanel;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Image warningBackground;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;
    
    [Header("Wave Timer UI")]
    [SerializeField] private GameObject waveTimerPanel;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Color timerNormalColor = Color.green;
    [SerializeField] private Color timerWarningColor = Color.yellow;
    [SerializeField] private Color timerDangerColor = Color.red;
    
    [Header("Wave Status UI")]
    [SerializeField] private GameObject waveStatusPanel;
    [SerializeField] private TMP_Text waveStatusText;
    [SerializeField] private TMP_Text enemyCountText;
    [SerializeField] private TMP_Text waveNumberText;
    
    [Header("Animation Settings")]
    [SerializeField] private float warningDuration = 3f;
    [SerializeField] private float warningBlinkInterval = 0.5f;
    
    private Coroutine warningCoroutine;
    private Coroutine timerCoroutine;
    private bool isWaveActive = false;
    private float currentWaveTime = 0f;
    private float maxWaveTime = 60f;
    private int currentEnemyCount = 0;
    
    private void Start()
    {
        // Initialize UI panels
        SetAllPanelsActive(false);
    }
    
    public void Setup()
    {
        // Subscribe to camp wave events
        if (CampManager.Instance != null)
        {
            CampManager.Instance.OnCampWaveStarted += OnWaveStarted;
            CampManager.Instance.OnCampWaveEnded += OnWaveEnded;
        }
        
        // Subscribe to game mode changes
        GameManager.Instance.OnGameModeChanged += OnGameModeChanged;
        
        // Set initial visibility based on current game mode
        gameObject.SetActive(GameManager.Instance.CurrentGameMode == GameMode.CAMP);
    }
    
    private void OnGameModeChanged(GameMode newGameMode)
    {
        // Only show the UI in CAMP mode
        gameObject.SetActive(newGameMode == GameMode.CAMP);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from camp wave events
        if (CampManager.Instance != null)
        {
            CampManager.Instance.OnCampWaveStarted -= OnWaveStarted;
            CampManager.Instance.OnCampWaveEnded -= OnWaveEnded;
        }
        
        // Unsubscribe from game mode changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameModeChanged -= OnGameModeChanged;
        }
        
        // Stop any running coroutines
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
    }
    
    private void OnWaveStarted()
    {
        isWaveActive = true;
        currentWaveTime = 0f;
        
        // Get wave duration from current wave config
        var waveConfig = CampManager.Instance.GetWaveConfig(CampManager.Instance.GetCurrentWaveDifficulty());
        if (waveConfig != null && waveConfig is CampEnemyWaveConfig campConfig)
        {
            maxWaveTime = campConfig.WaveDuration;
        }
        
        // Update wave number immediately
        UpdateWaveNumber();
        
        // Start warning sequence
        StartWarningSequence();
    }
    
    private void OnWaveEnded()
    {
        isWaveActive = false;
        
        // Stop any running coroutines
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        // Hide all panels
        SetAllPanelsActive(false);
    }
    
    private void StartWarningSequence()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }
        warningCoroutine = StartCoroutine(WarningSequence());
    }
    
    private IEnumerator WarningSequence()
    {
        // Show warning panel instantly (no fade in)
        waveWarningPanel.SetActive(true);
        warningText.text = "ATTACK INCOMING!";
        countdownText.text = "Prepare for battle!";
        
        // Blink warning for duration
        float elapsedTime = 0f;
        bool isBlinking = false;
        
        while (elapsedTime < warningDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Blink effect
            if (Time.time % warningBlinkInterval < warningBlinkInterval * 0.5f)
            {
                if (!isBlinking)
                {
                    warningBackground.color = warningColor;
                    isBlinking = true;
                }
            }
            else
            {
                if (isBlinking)
                {
                    warningBackground.color = normalColor;
                    isBlinking = false;
                }
            }
            
            // Update countdown
            float remainingTime = warningDuration - elapsedTime;
            countdownText.text = $"Starting in {remainingTime:F1}s";
            
            yield return null;
        }
        
        // Hide warning panel instantly (no fade out)
        waveWarningPanel.SetActive(false);
        
        // Start wave timer
        StartWaveTimer();
    }
    
    private void StartWaveTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(WaveTimer());
    }
    
    private IEnumerator WaveTimer()
    {
        // Show timer panel instantly
        waveTimerPanel.SetActive(true);
        waveStatusPanel.SetActive(true);
        
        // Update timer
        while (isWaveActive && currentWaveTime < maxWaveTime)
        {
            currentWaveTime += Time.deltaTime;
            
            // Update timer display
            float remainingTime = maxWaveTime - currentWaveTime;
            timerText.text = $"Time: {remainingTime:F1}s";
            
            // Update slider
            float progress = currentWaveTime / maxWaveTime;
            timerSlider.value = progress;
            
            // Update timer color based on remaining time
            UpdateTimerColor(remainingTime);
            
            // Update status text
            waveStatusText.text = "Wave Active";
            UpdateEnemyCount();
            UpdateWaveNumber();
            
            yield return null;
        }
        
        // Wave time expired
        if (isWaveActive)
        {
            waveStatusText.text = "Time's Up!";
            timerText.text = "Time: 0.0s";
            timerFillImage.color = timerDangerColor;
        }
    }
    
    private void UpdateTimerColor(float remainingTime)
    {
        float timePercentage = remainingTime / maxWaveTime;
        
        if (timePercentage > 0.5f)
        {
            timerFillImage.color = timerNormalColor;
        }
        else if (timePercentage > 0.2f)
        {
            timerFillImage.color = timerWarningColor;
        }
        else
        {
            timerFillImage.color = timerDangerColor;
        }
    }
    
    private void UpdateEnemyCount()
    {
        // Count active enemies
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        currentEnemyCount = enemies.Length;
        enemyCountText.text = $"Enemies: {currentEnemyCount}";
    }
    
    private void UpdateWaveNumber()
    {
        if (waveNumberText != null && CampManager.Instance != null)
        {
            // Get current wave number from CampManager
            int currentWave = CampManager.Instance.GetCurrentWaveNumber();
            waveNumberText.text = $"Wave: {currentWave}";
        }
    }
    

    
    private void SetAllPanelsActive(bool active)
    {
        if (waveWarningPanel != null) waveWarningPanel.SetActive(active);
        if (waveTimerPanel != null) waveTimerPanel.SetActive(active);
        if (waveStatusPanel != null) waveStatusPanel.SetActive(active);
    }
    
    // Public methods for external control
    public void ForceEndWave()
    {
        if (CampManager.Instance != null)
        {
            CampManager.Instance.ForceEndCampWave();
        }
    }
    
    public bool IsWaveActive => isWaveActive;
    public float CurrentWaveTime => currentWaveTime;
    public float MaxWaveTime => maxWaveTime;
    public int CurrentEnemyCount => currentEnemyCount;
} 