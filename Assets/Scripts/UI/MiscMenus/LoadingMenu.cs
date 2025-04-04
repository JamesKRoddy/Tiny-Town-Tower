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
    [SerializeField] private Slider progressBar;         // Progress bar UI.
    [SerializeField] private TMP_Text progressText;            // Progress text UI.

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
