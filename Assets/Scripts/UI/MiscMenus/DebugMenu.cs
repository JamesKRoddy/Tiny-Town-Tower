using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using Enemies;

public class DebugMenu: MonoBehaviour
{
    [Header("Debug Menu UI")]
    [SerializeField] private Button waveButton;
    [SerializeField] private TMP_Text statusText; // Single status text object
    
    [Header("Wave Test Controls")]
    [SerializeField] private Button clearEnemiesFadeButton;
    [SerializeField] private Toggle waveLoopToggle;
    [SerializeField] private Slider waveDelaySlider;
    [SerializeField] private TMP_Text waveDelayText;
    
    private bool isWaveActive = false;
    private bool waveLoopingEnabled = true;
    private float waveDelay = 5f;

    private void Start()
    {
        waveButton.onClick.AddListener(ToggleWave);
        
        // Setup additional wave test controls
        if (clearEnemiesFadeButton != null)
            clearEnemiesFadeButton.onClick.AddListener(ClearEnemiesWithFade);
        
        if (waveLoopToggle != null)
        {
            waveLoopToggle.isOn = waveLoopingEnabled;
            waveLoopToggle.onValueChanged.AddListener(OnWaveLoopToggled);
        }
        
        if (waveDelaySlider != null)
        {
            waveDelaySlider.value = waveDelay;
            waveDelaySlider.onValueChanged.AddListener(OnWaveDelayChanged);
            UpdateWaveDelayText();
        }
    }

    private void UpdateWaveStatus()
    {
        if (CampManager.Instance != null)
        {
            isWaveActive = CampManager.Instance.IsWaveActive;
            waveButton.GetComponentInChildren<TMP_Text>().text = isWaveActive ? "End Wave" : "Start Wave";
            
            // Update the single status text with all information
            if (statusText != null)
            {
                var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                int currentWave = CampManager.Instance.GetCurrentWaveNumber();
                int maxWaves = CampManager.Instance.GetCurrentMaxWaves();
                string statusInfo = $"Wave Status: {(isWaveActive ? "Active" : "Inactive")}\n";
                statusInfo += $"Current Wave: {currentWave}/{maxWaves}\n";
                statusInfo += $"Enemies: {enemies.Length}\n";
                
                // Add wave timer info if available
                if (PlayerUIManager.Instance?.waveUI != null)
                {
                    var waveUI = PlayerUIManager.Instance.waveUI;
                    if (waveUI.IsWaveActive)
                    {
                        float remainingTime = waveUI.MaxWaveTime - waveUI.CurrentWaveTime;
                        statusInfo += $"Time Remaining: {remainingTime:F1}s\n";
                    }
                    else
                    {
                        statusInfo += "Time Remaining: --\n";
                    }
                }
                
                // Add wave loop info
                statusInfo += $"Wave Looping: {(waveLoopingEnabled ? "Enabled" : "Disabled")}\n";
                statusInfo += $"Wave Delay: {waveDelay:F1}s";
                
                statusText.text = statusInfo;
            }
        }
    }

    public void ToggleWave()
    {
        if (CampManager.Instance == null) return;

        if (isWaveActive)
        {
            CampManager.Instance.ForceEndCampWave();
        }
        else
        {
            CampManager.Instance.StartCampWave();
        }
        
        UpdateWaveStatus();
    }

    public void SpawnEnemy()
    {
        if (CampManager.Instance != null)
        {
            CampManager.Instance.SpawnSingleEnemy();
        }
    }

    public void ClearEnemies()
    {
        if (CampManager.Instance != null)
        {
            CampManager.Instance.ClearAllEnemies();
        }
    }

    public void ToggleGodMode()
    {
        // TODO: Implement god mode functionality
        Debug.Log("God mode toggled - not implemented yet");
    }

    public void GiveResources()
    {
        // TODO: Implement give resources functionality
        Debug.Log("Give resources - not implemented yet");
    }

    private void Update()
    {
        // Update wave status periodically when menu is active
        if (gameObject.activeInHierarchy)
        {
            UpdateWaveStatus();
        }
    }
    
    // Additional wave test methods
    public void ClearEnemiesWithFade()
    {
        if (CampManager.Instance != null)
        {
            CampManager.Instance.ClearAllEnemiesWithFade();
        }
    }
    
    private void OnWaveLoopToggled(bool enabled)
    {
        waveLoopingEnabled = enabled;
        Debug.Log($"Wave looping toggled: {enabled}");
        // TODO: Expose this setting in CampManager
    }
    

    
    private void OnWaveDelayChanged(float value)
    {
        waveDelay = value;
        UpdateWaveDelayText();
        Debug.Log($"Wave delay changed: {value}s");
        // TODO: Expose this setting in CampManager
    }
    
    private void UpdateWaveDelayText()
    {
        if (waveDelayText != null)
        {
            waveDelayText.text = $"Delay: {waveDelay:F1}s";
        }
    }
    
    private void OnDestroy()
    {
        // Clean up listeners
        if (waveButton != null)
            waveButton.onClick.RemoveListener(ToggleWave);
        
        if (clearEnemiesFadeButton != null)
            clearEnemiesFadeButton.onClick.RemoveListener(ClearEnemiesWithFade);
        
        if (waveLoopToggle != null)
            waveLoopToggle.onValueChanged.RemoveListener(OnWaveLoopToggled);
        

        
        if (waveDelaySlider != null)
            waveDelaySlider.onValueChanged.RemoveListener(OnWaveDelayChanged);
    }
} 