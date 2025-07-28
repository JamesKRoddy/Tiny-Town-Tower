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
    [SerializeField] TextPopup textPopup;
    public NarrativeMenu narrativeMenu;
    public WeaponComparisonMenu weaponComparisonMenu;
    [SerializeField] public AddedToInventoryPopup inventoryPopup; // Reference to inventory popup system

    [Header("Game UI References")]
    public RogueLikeGameUI rogueLikeGameUI;
    public CampUI campUI;
    public CampWaveUI waveUI;


    private Coroutine openingMenuCoroutine;

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
        if (delay > 0.0f)
        {
            if (openingMenuCoroutine == null)
                openingMenuCoroutine = StartCoroutine(EnableMenuAfterDelay(menu, active, delay, callback));
        }
        else
        {
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
    }

    public void SetSelectedGameObject(GameObject gameObject)
    {        
        StartCoroutine(SetSelectedGameObjectCoroutine(gameObject));
    }

    private IEnumerator SetSelectedGameObjectCoroutine(GameObject gameObject)
    {
        yield return new WaitForSeconds(0.1f);
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    internal void BackPressed()
    {
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
            default:
                Debug.LogWarning("BackPressed no menu found");
                break;  
        }
    }
}
