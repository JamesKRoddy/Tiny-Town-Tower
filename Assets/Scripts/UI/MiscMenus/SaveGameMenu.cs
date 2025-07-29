using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SaveGameMenu : MenuBase
{
    [Header("UI References")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button closePanelButton;

    private void Start()
    {
        // Setup button listeners
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);
            
        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButtonClicked);
            
        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(OnCloseButtonClicked);

        // Subscribe to PlayerInput events for keyboard shortcuts
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.OnQuickSavePressed += OnQuickSaveClicked;
            PlayerInput.Instance.OnQuickLoadPressed += OnQuickLoadPressed;
        }

        // Initialize UI
        UpdateUI();
    }

    private void OnQuickLoadPressed()
    {
        // Only handle quick load when this menu is active
        if (PlayerUIManager.Instance?.currentMenu == this)
        {
            OnLoadButtonClicked();
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        if (active)
        {
            UpdateUI(); // Refresh UI state when opening
        }
        
        base.SetScreenActive(active, delay, onDone);
    }

    private void UpdateUI()
    {
        // Update load button availability
        if (loadButton != null && SaveLoadManager.Instance != null)
        {
            loadButton.interactable = SaveLoadManager.Instance.HasSaveFile();
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
                SetScreenActive(false); // Close this menu
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
            // Note: You'd need to expose auto-save toggle functionality in SaveLoadManager
            // SaveLoadManager.Instance.SetAutoSave(!SaveLoadManager.Instance.IsAutoSaveEnabled);
            UpdateUI();
            ShowSaveMessage("Auto-save toggled!");
        }
    }

    private void OnCloseButtonClicked()
    {
        BackPressed();
    }

    private void ShowSaveMessage(string message, bool isError = false)
    {
        // Use the new notification system for success messages, error system for errors
        if (isError)
        {
            DisplayErrorMessage(message);
        }
        else if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.DisplayNotification(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from PlayerInput events to prevent memory leaks
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.OnQuickSavePressed -= OnQuickSaveClicked;
            PlayerInput.Instance.OnQuickLoadPressed -= OnQuickLoadPressed;
        }
    }
} 