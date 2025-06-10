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

    // Event that fires when a scene transition begins, passing the next game mode
    public event System.Action<GameMode> OnSceneTransitionBegin;

    // Actions passed in from the previous scene that are invoked before fade out
    public Action OnActionsFromPreviousScene;

    // The name of your dedicated loading scene.
    [SerializeField]
    private string loadingSceneName = "LoadingScene";

    // Ensure that there is only one instance
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

    public void LoadScene(string sceneName, GameMode nextGameMode, bool keepPossessedNPC, Action OnSceneLoaded = null)
    {
        Debug.Log("Loading scene " + sceneName);
        OnActionsFromPreviousScene = OnSceneLoaded;
        PreviousScene = SceneManager.GetActiveScene().name;
        NextScene = sceneName;
        NextGameMode = nextGameMode;

        StartCoroutine(LoadSceneNextFrame(nextGameMode, keepPossessedNPC));
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
    private IEnumerator LoadSceneNextFrame(GameMode nextGameMode, bool keepPossessedNPC)
    {
        if (PlayerController.Instance != null)
        {
            MonoBehaviour npc = PlayerController.Instance._possessedNPC as MonoBehaviour;
            if (npc != null && !keepPossessedNPC)
            {
                PlayerController.Instance.PossessNPC(null);
                Destroy(npc.gameObject);
            }
        }

        yield return null; // Wait one frame

        LoadScene(nextGameMode);
    }

    /// <summary>
    /// Begins the transition by setting the next scene and loading the dedicated loading scene.
    /// Call this (for example, from your main menu) when you want to load a new scene.
    /// </summary>
    /// <param name="sceneName">Name of the target scene to load.</param>
    private void LoadScene(GameMode nextGameMode = GameMode.NONE)
    {        
        OnSceneTransitionBegin?.Invoke(nextGameMode); // Pass the next game mode to subscribers
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
        //1. Fade in and wait for it to complete
        if (PlayerUIManager.Instance.transitionMenu != null)
        {
            yield return PlayerUIManager.Instance.transitionMenu.FadeIn();
        }

        //2. Load the next scene
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(NextScene);
        asyncOperation.allowSceneActivation = false;

        //3. Wait for the scene to load
        while (!asyncOperation.isDone)
        {
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            progressCallback?.Invoke(progress);

            if (asyncOperation.progress >= 0.9f)
            {
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }

        //4. Invoke actions passed in from the previous scene
        OnActionsFromPreviousScene?.Invoke();

        //5. Short pause for camera transition
        yield return new WaitForSeconds(0.5f);

        //6. Fade out
        if (PlayerUIManager.Instance.transitionMenu != null)
        {
            yield return PlayerUIManager.Instance.transitionMenu.FadeOut();
        }

        //7. Update scene tracking after the new scene is active
        CurrentScene = NextScene;
        NextScene = string.Empty;
        GameManager.Instance.CurrentGameMode = NextGameMode;
    }

    #endregion
}