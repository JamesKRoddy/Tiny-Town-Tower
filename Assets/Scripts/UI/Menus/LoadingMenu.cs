using System;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// LoadingMenu that displays a dedicated loading screen.
// It updates its UI as the scene loads asynchronously.
public class LoadingMenu : MenuBase
{
    // Static instance of the LoadingMenu class
    private static LoadingMenu _instance;

    // Public property to access the instance
    public static LoadingMenu Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<LoadingMenu>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("LoadingMenu instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [SerializeField] private Slider progressBar;         // Progress bar UI.
    [SerializeField] private TMP_Text progressText;            // Progress text UI.

    // Ensure that there is only one instance of LoadingMenu
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            _instance = this; // Set the instance
        }
    }

    // No setup required for screen activation here.
    public override void Setup() { }

    // Since we're not toggling any screen, simply invoke the callback.
    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        onDone?.Invoke();
    }

    // When the loading scene starts, begin loading the next scene.
    private void Start()
    {
        // Begin asynchronous loading and supply the UpdateProgress callback.
        SceneTransitionManager.Instance.StartLoadingNextScene(UpdateProgress);
    }

    // Callback to update the progress UI.
    private void UpdateProgress(float progress)
    {
        if (progressBar != null)
            progressBar.value = progress;
        if (progressText != null)
            progressText.text = (progress * 100f).ToString("F0") + "%";
    }
}
