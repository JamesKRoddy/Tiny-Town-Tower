using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SaveGameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject savePanel;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button quickSaveButton;
    [SerializeField] private Button autoSaveToggleButton;
    [SerializeField] private TMP_Text autoSaveStatusText;
    [SerializeField] private TMP_Text nextAutoSaveText;
    [SerializeField] private Button closePanelButton;

    [Header("Quick Access")]
    [SerializeField] private KeyCode quickSaveKey = KeyCode.F5;
    [SerializeField] private KeyCode quickLoadKey = KeyCode.F9;
    [SerializeField] private KeyCode saveMenuKey = KeyCode.Escape;

    private bool isPanelOpen = false;

    private void Start()
    {
        // Setup button listeners
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);
            
        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButtonClicked);
            
        if (quickSaveButton != null)
            quickSaveButton.onClick.AddListener(OnQuickSaveClicked);
            
        if (autoSaveToggleButton != null)
            autoSaveToggleButton.onClick.AddListener(OnAutoSaveToggleClicked);
            
        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(CloseSavePanel);

        // Initialize panel state
        if (savePanel != null)
            savePanel.SetActive(false);

        UpdateUI();
    }

    private void Update()
    {
        // Handle keyboard shortcuts
        if (Input.GetKeyDown(quickSaveKey))
        {
            OnQuickSaveClicked();
        }
        
        if (Input.GetKeyDown(quickLoadKey))
        {
            OnLoadButtonClicked();
        }
        
        if (Input.GetKeyDown(saveMenuKey) && !isPanelOpen)
        {
            OpenSavePanel();
        }

        // Update auto-save countdown
        UpdateAutoSaveCountdown();
    }

    private void UpdateUI()
    {
        // Update load button availability
        if (loadButton != null && SaveLoadManager.Instance != null)
        {
            loadButton.interactable = SaveLoadManager.Instance.HasSaveFile();
        }

        // Update auto-save status
        if (autoSaveStatusText != null && SaveLoadManager.Instance != null)
        {
            bool autoSaveEnabled = SaveLoadManager.Instance != null; // You'd need to expose this property
            autoSaveStatusText.text = autoSaveEnabled ? "Auto-Save: ON" : "Auto-Save: OFF";
        }
    }

    private void UpdateAutoSaveCountdown()
    {
        if (nextAutoSaveText != null && SaveLoadManager.Instance != null)
        {
            float timeUntilSave = SaveLoadManager.Instance.GetTimeUntilAutoSave();
            if (timeUntilSave > 0)
            {
                TimeSpan time = TimeSpan.FromSeconds(timeUntilSave);
                nextAutoSaveText.text = $"Next Auto-Save: {time.Minutes:D2}:{time.Seconds:D2}";
            }
            else if (timeUntilSave == -1)
            {
                nextAutoSaveText.text = "Auto-Save: Disabled";
            }
            else
            {
                nextAutoSaveText.text = "Auto-Save: Soon";
            }
        }
    }

    public void OpenSavePanel()
    {
        if (savePanel != null)
        {
            savePanel.SetActive(true);
            isPanelOpen = true;
            UpdateUI();
            
            // Pause the game when save menu is open
            Time.timeScale = 0f;
        }
    }

    public void CloseSavePanel()
    {
        if (savePanel != null)
        {
            savePanel.SetActive(false);
            isPanelOpen = false;
            
            // Resume the game
            Time.timeScale = 1f;
        }
    }

    private void OnSaveButtonClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.ForceSave();
            ShowSaveMessage("Game Saved!");
        }
        else
        {
            ShowSaveMessage("Save failed - SaveManager not found!", true);
        }
    }

    private void OnLoadButtonClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            if (SaveLoadManager.Instance.HasSaveFile())
            {
                SaveLoadManager.Instance.LoadGame();
                CloseSavePanel();
                ShowSaveMessage("Game Loaded!");
            }
            else
            {
                ShowSaveMessage("No save file found!", true);
            }
        }
        else
        {
            ShowSaveMessage("Load failed - SaveManager not found!", true);
        }
    }

    private void OnQuickSaveClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.ForceSave();
            ShowSaveMessage("Quick Save Complete!");
        }
    }

    private void OnAutoSaveToggleClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            // You'd need to expose auto-save toggle functionality
            // SaveLoadManager.Instance.SetAutoSave(!SaveLoadManager.Instance.IsAutoSaveEnabled);
            UpdateUI();
        }
    }

    private void ShowSaveMessage(string message, bool isError = false)
    {
        // Show a temporary message to the player
        if (PlayerUIManager.Instance != null)
        {
            if (isError)
            {
                PlayerUIManager.Instance.DisplayUIErrorMessage(message);
            }
            else
            {
                PlayerUIManager.Instance.DisplayTextPopup(message);
            }
        }
        else
        {
            Debug.Log(message);
        }
    }

    private void OnDestroy()
    {
        // Make sure to resume time when this UI is destroyed
        Time.timeScale = 1f;
    }
} 