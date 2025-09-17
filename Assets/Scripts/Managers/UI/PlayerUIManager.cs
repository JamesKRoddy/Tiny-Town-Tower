using UnityEngine;
using TMPro;
using System.Collections;
using System;
using UnityEngine.EventSystems;


public class PlayerUIManager : MonoBehaviour
{
    // Static instance of the PlayerUIManager class
    private static PlayerUIManager _instance;

    // Public property to access the instance
    public static PlayerUIManager Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<PlayerUIManager>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("PlayerUIManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("Debug Menu References")]
    public CampWaveDebugMenu campWaveDebugMenu;
    public RoomDebugMenu roomDebugMenu;

    [Header("UI References")]
    [SerializeField, ReadOnly] public MenuBase currentMenu;
    [SerializeField, ReadOnly] public MenuBase previousMenu;

    [Header("Pause Menu References")]
    public PauseMenu pauseMenu;
    public SettingsMenu settingsMenu;
    public ReturnToCampMenu returnToCampMenu;
    public QuitMenu quitMenu;
    public SaveGameMenu saveGameMenu;

    [Header("Utility Menu References")]
    public UtilityMenu utilityMenu;
    public BuildMenu buildMenu;
    public PlayerInventoryMenu playerInventoryMenu;
    public SettlerNPCMenu settlerNPCMenu;
    public GeneticMutationUI geneticMutationMenu;
    public SelectionPopup selectionPopup;
    public SelectionPreviewList selectionPreviewList;

    [Header("Overlay Menu References")]
    [SerializeField] public TransitionMenu transitionMenu;
    [SerializeField] TMP_Text errorMessage;
    public DeathMenu deathMenu;
    [SerializeField] UIPanelController interactionPromptUI; // UI text for interactionPromptUI
    [SerializeField] TextPopup textPopup; //Used for notifications that require input from the player
    public NarrativeMenu narrativeMenu;
    public WeaponComparisonMenu weaponComparisonMenu;
    [SerializeField] public AddedToInventoryPopup inventoryPopup; // Reference to inventory popup system
    [SerializeField] TMP_Text notificationText; //Used for notifications that dont require input from the player

    [Header("Game UI References")]
    public RogueLikeGameUI rogueLikeGameUI;
    public CampUI campUI;
    public CampWaveUI waveUI;

    [Header("Time Display References")]
    public TimeDisplayUI timeDisplayUI;

    [Header("Progress Bar References")]
    [SerializeField] private Transform worldUIParent; // Transform to parent all work task progress bars
    
    [Header("Floating Text References")]
    [SerializeField] private GameObject floatingTextPrefab; // Prefab for floating text
    [SerializeField] private int floatingTextPoolSize = 20; // Number of floating text objects to pool
    
    [Header("Progress Bar References")]
    [SerializeField] private GameObject progressBarPrefab; // Prefab for work task progress bars
    [SerializeField] private int maxActiveProgressBars = 10; // Limit to prevent performance issues

    private Coroutine openingMenuCoroutine;
    private Coroutine notificationCoroutine; // For managing notification text timing
    
    // Floating text pooling
    private System.Collections.Generic.Queue<FloatingTextUI> floatingTextPool = new System.Collections.Generic.Queue<FloatingTextUI>();
    private System.Collections.Generic.List<FloatingTextUI> activeFloatingTexts = new System.Collections.Generic.List<FloatingTextUI>();
    
    // Progress bar management
    private System.Collections.Generic.Dictionary<WorkTask, WorkTaskProgressBar> activeProgressBars = new System.Collections.Generic.Dictionary<WorkTask, WorkTaskProgressBar>();
    private System.Collections.Generic.Queue<WorkTaskProgressBar> progressBarPool = new System.Collections.Generic.Queue<WorkTaskProgressBar>();

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
            DontDestroyOnLoad(gameObject); // Optionally persist across scenes
        }   
    }

    void Start()
    {
        rogueLikeGameUI.Setup();
        campUI.Setup();
        waveUI.Setup();
        timeDisplayUI.Setup();
        
        // Ensure progress bar parent exists
        EnsureProgressBarParent();
        
        // Initialize floating text pool
        InitializeFloatingTextPool();
        
        // Initialize progress bar pool
        InitializeProgressBarPool();
    }

    private void Update()
    {
        #if UNITY_EDITOR
        // F1 - Wave Debug Menu
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugMenu(campWaveDebugMenu);
        }
        
        // F2 - Room Debug Menu
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ToggleDebugMenu(roomDebugMenu);
        }
        #endif
    }
    
    #if UNITY_EDITOR
    private void ToggleDebugMenu(MonoBehaviour menu)
    {
        if (menu != null)
        {
            if (!menu.gameObject.activeInHierarchy)
            {
                // Hide other debug menus first
                HideAllDebugMenus();
                menu.gameObject.SetActive(true);
            }
            else
            {
                menu.gameObject.SetActive(false);
            }
        }
    }
    
    private void HideAllDebugMenus()
    {
        if (campWaveDebugMenu != null) campWaveDebugMenu.gameObject.SetActive(false);
        if (roomDebugMenu != null) roomDebugMenu.gameObject.SetActive(false);
    }
    #endif

    /// <summary>
    /// Use this to open any menus
    /// </summary>
    /// <param name="menu"></param>
    /// <param name="active"></param>
    /// <param name="delay"></param>
    public void SetScreenActive(MenuBase menu, bool active, float delay = 0.0f, Action callback = null)
    {
        Debug.Log($"[PlayerUIManager] SetScreenActive called for menu: {menu?.name}, active: {active}, delay: {delay}");
        
        if (delay > 0.0f)
        {
            if (openingMenuCoroutine == null)
                openingMenuCoroutine = StartCoroutine(EnableMenuAfterDelay(menu, active, delay, callback));
        }
        else
        {
            Debug.Log($"[PlayerUIManager] Setting menu active immediately: {menu?.name} -> {active}");
            previousMenu = currentMenu;
            currentMenu = menu;
            menu.gameObject.SetActive(active);
            callback?.Invoke(); // Invoke the callback immediately if there's no delay
        }
    }
    
    private IEnumerator EnableMenuAfterDelay(MenuBase menu, bool active, float delay, Action OnDone)
    {
        yield return new WaitForSeconds(delay);

        previousMenu = currentMenu;
        currentMenu = menu;
        menu.gameObject.SetActive(active);

        OnDone?.Invoke(); // Invoke the callback after enabling the menu

        openingMenuCoroutine = null; // Reset the coroutine reference
    }

    public void DisplayUIErrorMessage(string message, float duration = 2.0f)
    {
        errorMessage.gameObject.SetActive(true);
        errorMessage.text = message;
        PlayerInput.Instance.DisablePlayerInput(true);
        StartCoroutine(HideErrorMessageAfterDelay(duration));
    }

    public void DisplayTextPopup(string message)
    {
        textPopup.Setup(message);
    }

    /// <summary>
    /// Display a notification message for 3 seconds. If a new message comes in, replace it and restart the timer.
    /// </summary>
    /// <param name="message">The notification message to display</param>
    public void DisplayNotification(string message)
    {
        if (notificationText == null)
        {
            Debug.LogWarning("Notification text component is not assigned!");
            return;
        }

        // Stop any existing notification coroutine
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        // Set the new message and show the notification
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        // Start the new timer
        notificationCoroutine = StartCoroutine(HideNotificationAfterDelay(3.0f));
    }

    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
            notificationText.text = "";
        }
        
        notificationCoroutine = null;
    }

    private IEnumerator HideErrorMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        errorMessage.gameObject.SetActive(false);
        errorMessage.text = "";
        PlayerInput.Instance.DisablePlayerInput(false);
    }

    public void InteractionPrompt(string text)
    {
        interactionPromptUI.ShowPanel(text);
    }

    public void HideInteractionPropt()
    {
        interactionPromptUI.HidePanel();
    }

    public void HideUtilityMenus()
    {
        buildMenu.SetScreenActive(false);
        narrativeMenu.SetScreenActive(false);
        playerInventoryMenu.SetScreenActive(false);
        settlerNPCMenu.SetScreenActive(false);
        geneticMutationMenu.SetScreenActive(false);
        utilityMenu.SetScreenActive(false);
        
        #if UNITY_EDITOR
        HideAllDebugMenus();
        #endif
    }

    public void HidePauseMenus()
    {
        pauseMenu.SetScreenActive(false);
        settingsMenu.SetScreenActive(false);
        returnToCampMenu.SetScreenActive(false);
        quitMenu.SetScreenActive(false);
        saveGameMenu.SetScreenActive(false);
    }

    /// <summary>
    /// Open the save game menu
    /// </summary>
    public void OpenSaveGameMenu()
    {
        if (saveGameMenu != null)
        {
            saveGameMenu.SetScreenActive(true);
        }
    }

    public void SetSelectedGameObject(GameObject gameObject)
    {        
        StartCoroutine(SetSelectedGameObjectCoroutine(gameObject));
    }

    private IEnumerator SetSelectedGameObjectCoroutine(GameObject gameObject)
    {
        Debug.Log($"[PlayerUIManager] SetSelectedGameObjectCoroutine called for gameObject: {gameObject}");
        yield return new WaitForSeconds(0.1f);
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    internal void BackPressed()
    {
        Debug.Log($"[PlayerUIManager] BackPressed called for menu: {currentMenu?.name}");
        switch (currentMenu)
        {
            case PauseMenu:
                pauseMenu.ReturnToGame();
                currentMenu = null;
                break;
            case UtilityMenu:
                utilityMenu.ReturnToGame();
                currentMenu = null;
                break;
            case SelectionPreviewList:
                selectionPreviewList.ReturnToGame();
                currentMenu = null;
                break;
            case BuildMenu:
            case PlayerInventoryMenu:
            case SettlerNPCMenu:
                utilityMenu.EnableUtilityMenu();
                break;
            case GeneticMutationUI:
                geneticMutationMenu.HandleBackButtonPress();
                break;
            case SettingsMenu:
            case ReturnToCampMenu:
            case QuitMenu:
                pauseMenu.EnablePauseMenu();
                break;
            case SaveGameMenu:
                pauseMenu.EnablePauseMenu();
                currentMenu = null;
                break;
            default:
                Debug.LogWarning("BackPressed no menu found");
                break;  
        }
    }

    /// <summary>
    /// Get the transform to parent work task progress bars
    /// </summary>
    /// <returns>Transform to parent progress bars</returns>
    public Transform GetProgressBarParent()
    {
        EnsureProgressBarParent();
        return worldUIParent;
    }

    /// <summary>
    /// Ensure the progress bar parent transform exists
    /// </summary>
    private void EnsureProgressBarParent()
    {
        if (worldUIParent == null)
        {
            // Create a child GameObject to hold progress bars
            GameObject progressBarContainer = new GameObject("ProgressBars");
            worldUIParent = progressBarContainer.transform;
            worldUIParent.SetParent(transform);
            worldUIParent.localPosition = Vector3.zero;
            worldUIParent.localRotation = Quaternion.identity;
            worldUIParent.localScale = Vector3.one;
            
            Debug.Log("[PlayerUIManager] Created progress bar parent transform");
        }
    }

    /// <summary>
    /// Initialize the floating text object pool
    /// </summary>
    private void InitializeFloatingTextPool()
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("[PlayerUIManager] Floating text prefab not assigned!");
            return;
        }
        
        for (int i = 0; i < floatingTextPoolSize; i++)
        {
            GameObject obj = Instantiate(floatingTextPrefab, worldUIParent);
            FloatingTextUI floatingText = obj.GetComponent<FloatingTextUI>();
            if (floatingText == null)
            {
                floatingText = obj.AddComponent<FloatingTextUI>();
            }
            
            obj.SetActive(false);
            floatingTextPool.Enqueue(floatingText);
        }
    }
    
    /// <summary>
    /// Show floating text above a target
    /// </summary>
    public FloatingTextUI ShowFloatingText(Transform target, string text, FloatingTextType textType = FloatingTextType.Normal)
    {
        FloatingTextUI floatingText = GetPooledFloatingText();
        if (floatingText != null)
        {
            floatingText.Initialize(target, text, textType);
            activeFloatingTexts.Add(floatingText);
        }
        return floatingText;
    }
    
    /// <summary>
    /// Show floating text at a specific world position
    /// </summary>
    public FloatingTextUI ShowFloatingText(Vector3 worldPosition, string text, FloatingTextType textType = FloatingTextType.Normal)
    {
        // Create a temporary transform for positioning
        GameObject tempTarget = new GameObject("TempFloatingTextTarget");
        tempTarget.transform.position = worldPosition;
        
        FloatingTextUI floatingText = ShowFloatingText(tempTarget.transform, text, textType);
        
        // Clean up temp target after a delay
        StartCoroutine(CleanupTempTarget(tempTarget, 5f));
        
        return floatingText;
    }
    
    /// <summary>
    /// Return a floating text to the pool
    /// </summary>
    public void ReturnFloatingText(FloatingTextUI floatingText)
    {
        if (floatingText == null) return;
        
        if (activeFloatingTexts.Contains(floatingText))
        {
            activeFloatingTexts.Remove(floatingText);
            floatingText.gameObject.SetActive(false);
            floatingTextPool.Enqueue(floatingText);
        }
    }
    
    private FloatingTextUI GetPooledFloatingText()
    {
        if (floatingTextPool.Count > 0)
        {
            return floatingTextPool.Dequeue();
        }
        else
        {
            // Create new one if pool is empty
            if (floatingTextPrefab != null)
            {
                GameObject obj = Instantiate(floatingTextPrefab, worldUIParent);
                return obj.GetComponent<FloatingTextUI>();
            }
        }
        return null;
    }
    
    private IEnumerator CleanupTempTarget(GameObject tempTarget, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tempTarget != null)
        {
            Destroy(tempTarget);
        }
    }
    
    /// <summary>
    /// Initialize the progress bar object pool
    /// </summary>
    private void InitializeProgressBarPool()
    {
        if (progressBarPrefab == null)
        {
            Debug.LogWarning("[PlayerUIManager] Progress bar prefab not assigned!");
            return;
        }
        
        for (int i = 0; i < maxActiveProgressBars; i++)
        {
            GameObject obj = Instantiate(progressBarPrefab, worldUIParent);
            WorkTaskProgressBar progressBar = obj.GetComponent<WorkTaskProgressBar>();
            if (progressBar == null)
            {
                Debug.LogError("[PlayerUIManager] Progress bar prefab missing WorkTaskProgressBar component!");
                Destroy(obj);
                continue;
            }
            
            obj.SetActive(false);
            progressBarPool.Enqueue(progressBar);
        }
    }
    
    /// <summary>
    /// Show a progress bar for a work task
    /// </summary>
    public void ShowProgressBar(WorkTask task)
    {
        if (task == null) return;
        
        // Don't show if already active
        if (activeProgressBars.ContainsKey(task))
        {
            return;
        }
        
        // Check if we've reached the maximum number of active progress bars
        if (activeProgressBars.Count >= maxActiveProgressBars)
        {
            Debug.LogWarning($"[PlayerUIManager] Maximum number of active progress bars ({maxActiveProgressBars}) reached. Cannot show progress for {task.name}");
            return;
        }
        
        WorkTaskProgressBar progressBar = GetPooledProgressBar();
        if (progressBar != null)
        {
            progressBar.Initialize(task);
            progressBar.Show();
            activeProgressBars[task] = progressBar;
        }
    }
    
    /// <summary>
    /// Update progress for a work task
    /// </summary>
    public void UpdateProgressBar(WorkTask task, float progress, WorkTaskProgressState state = WorkTaskProgressState.Normal)
    {
        if (activeProgressBars.TryGetValue(task, out WorkTaskProgressBar progressBar))
        {
            progressBar.UpdateProgress(progress, state);
        }
    }
    
    /// <summary>
    /// Hide a progress bar for a work task
    /// </summary>
    public void HideProgressBar(WorkTask task)
    {
        if (task == null) return;
        
        if (activeProgressBars.TryGetValue(task, out WorkTaskProgressBar progressBar))
        {
            progressBar.Hide();
            activeProgressBars.Remove(task);
            ReturnProgressBarToPool(progressBar);
        }
    }
    
    /// <summary>
    /// Check if a work task has an active progress bar
    /// </summary>
    public bool HasProgressBar(WorkTask task)
    {
        return task != null && activeProgressBars.ContainsKey(task);
    }
    
    /// <summary>
    /// Clear all active progress bars
    /// </summary>
    public void ClearAllProgressBars()
    {
        foreach (var progressBar in activeProgressBars.Values)
        {
            if (progressBar != null)
            {
                progressBar.Hide();
                ReturnProgressBarToPool(progressBar);
            }
        }
        activeProgressBars.Clear();
    }
    
    private WorkTaskProgressBar GetPooledProgressBar()
    {
        if (progressBarPool.Count > 0)
        {
            var progressBar = progressBarPool.Dequeue();
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                return progressBar;
            }
        }
        
        return CreatePooledProgressBar();
    }
    
    private WorkTaskProgressBar CreatePooledProgressBar()
    {
        if (progressBarPrefab == null)
        {
            Debug.LogError("[PlayerUIManager] Progress bar prefab is null!");
            return null;
        }
        
        GameObject progressBarObj = Instantiate(progressBarPrefab, worldUIParent);
        progressBarObj.SetActive(false);
        
        WorkTaskProgressBar progressBar = progressBarObj.GetComponent<WorkTaskProgressBar>();
        if (progressBar == null)
        {
            Debug.LogError("[PlayerUIManager] Progress bar prefab does not have WorkTaskProgressBar component!");
            Destroy(progressBarObj);
            return null;
        }
        
        return progressBar;
    }
    
    private void ReturnProgressBarToPool(WorkTaskProgressBar progressBar)
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBarPool.Enqueue(progressBar);
        }
    }

    private void OnDestroy()
    {
        // Clean up any running coroutines
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }
    }
}
