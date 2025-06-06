using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Managers;

// This singleton manages scene transitions, tracking previous, current, and next scenes.
// It loads a dedicated loading scene, which in turn starts loading the target scene.
public class SceneTransitionManager : MonoBehaviour
{
    // Static instance of the SceneTransitionManager class
    private static SceneTransitionManager _instance;

    // Public property to access the instance
    public static SceneTransitionManager Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<SceneTransitionManager>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("SceneTransitionManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    // Tracks the scenes.
    public string CurrentScene { get; private set; }
    public string PreviousScene { get; private set; }
    public string NextScene { get; private set; }
    public GameMode NextGameMode { get; private set; }

    // The name of your dedicated loading scene.
    [SerializeField]
    private string loadingSceneName = "LoadingScene";

    // Ensure that there is only one instance of PlayerCombat
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

    public void LoadScene(string sceneName, GameMode nextGameMode, bool keepPossessedNPC)
    {
        StartCoroutine(LoadSceneNextFrame(sceneName, nextGameMode, keepPossessedNPC));
    }

    /// <summary>
    /// Used when npcs are unpossessed to move them back to be destroyed
    /// </summary>
    /// <param name="gameObject"></param>
    public void MoveGameObjectBackToCurrent(GameObject gameObject)
    {
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
    }

    #region private

    /// <summary>
    /// Need to wait for destroy and unhooking possessed npc
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="nextGameMode"></param>
    /// <param name="keepPlayerControls"></param>
    /// <param name="keepPossessedNPC"></param>
    /// <returns></returns>
    private IEnumerator LoadSceneNextFrame(string sceneName, GameMode nextGameMode, bool keepPossessedNPC)
    {
        if (PlayerController.Instance != null)
        {
            if (PlayerController.Instance._possessedNPC is MonoBehaviour npc && (!keepPossessedNPC))
            {
                PlayerController.Instance.PossessNPC(null);
                Destroy(npc.gameObject);
            }
        }

        yield return null; // Wait one frame

        LoadScene(sceneName, nextGameMode);
    }

    /// <summary>
    /// Begins the transition by setting the next scene and loading the dedicated loading scene.
    /// Call this (for example, from your main menu) when you want to load a new scene.
    /// </summary>
    /// <param name="sceneName">Name of the target scene to load.</param>
    private void LoadScene(string sceneName, GameMode nextGameMode = GameMode.NONE)
    {
        PreviousScene = SceneManager.GetActiveScene().name;
        NextScene = sceneName;
        NextGameMode = nextGameMode;
        SceneManager.LoadScene(loadingSceneName);
    }

    /// <summary>
    /// Called by the LoadingMenu to start asynchronous loading of the next scene.
    /// The progressCallback is invoked repeatedly to update the UI.
    /// </summary>
    /// <param name="progressCallback">Action to receive loading progress (0.0 to 1.0).</param>
    public void StartLoadingNextScene(Action<float> progressCallback)
    {
        StartCoroutine(LoadNextSceneAsync(progressCallback));
    }

    private IEnumerator LoadNextSceneAsync(Action<float> progressCallback)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(NextScene);
        // Prevent immediate activation until loading is complete (or until you choose to activate it).
        asyncOperation.allowSceneActivation = false;

        while (!asyncOperation.isDone)
        {
            // Unity's asyncOperation.progress value goes from 0 to 0.9 while loading.
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            progressCallback?.Invoke(progress);

            // When progress reaches 0.9, the scene is fully loaded.
            if (asyncOperation.progress >= 0.9f)
            {
                // Optionally, add a delay or wait for user input before activating the scene.
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }

        // Update scene tracking after the new scene is active.
        CurrentScene = NextScene;
        NextScene = string.Empty;
        GameManager.Instance.CurrentGameMode = NextGameMode;
    }

    #endregion
}