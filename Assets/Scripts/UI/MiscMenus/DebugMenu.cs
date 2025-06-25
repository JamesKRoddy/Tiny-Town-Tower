using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;

public class DebugMenu: MonoBehaviour
{
    [Header("Debug Menu UI")]
    [SerializeField] private Button waveButton;
    [SerializeField] private TMP_Text waveStatusText;
    
    private bool isWaveActive = false;

    private void Start()
    {
        waveButton.onClick.AddListener(ToggleWave);
    }

    private void UpdateWaveStatus()
    {
        if (CampManager.Instance != null)
        {
            isWaveActive = CampManager.Instance.IsWaveActive;
            waveStatusText.text = isWaveActive ? "Wave Active" : "No Wave";
            waveButton.GetComponentInChildren<TMP_Text>().text = isWaveActive ? "End Wave" : "Start Wave";
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
} 