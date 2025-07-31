using System;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class MainMenu : MonoBehaviour, IControllerInput
{
    [Header("Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // Setup button listeners
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);
            
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(LoadGame);
            // Disable load button if no save file exists
            loadGameButton.interactable = SaveLoadManager.Instance?.HasSaveFile() ?? false;
        }
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Set initial control type
        if (PlayerInput.Instance != null)
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.MAIN_MENU);
    }

    private void StartNewGame()
    {
        Debug.Log("Starting new game...");
        
        // Delete existing save file to start fresh
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.HasSaveFile())
        {
            SaveLoadManager.Instance.DeleteSave();
        }
        
        // Load the camp scene to start the game
        SceneTransitionManager.Instance?.LoadScene(SceneNames.CampScene, GameMode.CAMP, false);
    }

    private void LoadGame()
    {
        Debug.Log("<color=green>Loading game...</color>");
        
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("SaveLoadManager not found!");
            return;
        }

        if (!SaveLoadManager.Instance.HasSaveFile())
        {
            Debug.LogWarning("No save file found to load!");
            return;
        }

        // Load the camp scene first, then load the save data
        SceneTransitionManager.Instance?.LoadScene(SceneNames.CampScene, GameMode.CAMP, false);
    }

    private void OpenSettings()
    {
        Debug.Log("Opening settings...");
        // TODO: Implement settings menu
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        // Main menu doesn't need to handle control type changes
    }
}
