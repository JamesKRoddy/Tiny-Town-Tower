using UnityEngine;
using TMPro;
using System.Collections;
using System;


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
    public TurretMenu turretMenu;
    public TurretUpgradeMenu turretUpgradeMenu;
    public GeneticMutationUI geneticMutationMenu;

    [Header("Overlay Menu References")]
    [SerializeField] TMP_Text errorMessage;
    [SerializeField] UIPanelController interactionPromptUI; // UI text for interactionPromptUI
    public NarrativeSystem narrativeSystem;
    public WeaponComparisonMenu weaponComparisonMenu;

    [Header("Game UI References")]
    public RogueLikeGameUI rogueLikeGameUI;


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
    }

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
        narrativeSystem.SetScreenActive(false);
        playerInventoryMenu.SetScreenActive(false);
        settlerNPCMenu.SetScreenActive(false);
        turretMenu.SetScreenActive(false);
        turretUpgradeMenu.SetScreenActive(false);
        geneticMutationMenu.SetScreenActive(false);
        utilityMenu.SetScreenActive(false);
    }

    public void HidePauseMenus()
    {
        pauseMenu.SetScreenActive(false);
        settingsMenu.SetScreenActive(false);
        returnToCampMenu.SetScreenActive(false);
        quitMenu.SetScreenActive(false);
    }

    internal void BackPressed()
    {
        switch (currentMenu)
        {
            case PauseMenu:
                pauseMenu.ReturnToGame();
                break;
            case UtilityMenu:
                utilityMenu.ReturnToGame();
                break;
            case BuildMenu:
            case GeneticMutationUI:
            case PlayerInventoryMenu:
            case SettlerNPCMenu:
            case TurretMenu:
            case TurretUpgradeMenu:
                utilityMenu.EnableUtilityMenu();
                break;
            case SettingsMenu:
            case ReturnToCampMenu:
            case QuitMenu:
                pauseMenu.EnablePauseMenu();
                break;
            default:
                break;  
        }
    }
}
