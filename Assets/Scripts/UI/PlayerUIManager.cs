using UnityEngine;
using TMPro;
using System.Collections;

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
    [SerializeField] MenuBase buildMenu; 
    [SerializeField] MenuBase narrativeSystem;
    [SerializeField] MenuBase playerInventoryMenu;
    [SerializeField] MenuBase settlerNPCMenu;
    [SerializeField] MenuBase utilityMenu;

    [Header("Interaction")]
    [SerializeField] UIPanelController interactionPromptUI; // UI text for interactionPromptUI //TODO because ill be using panels with text being displayed (talk to, pickup, open etc.) might be an idea to create a separate class for all this stuff

    private Coroutine openingMenu;

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

        buildMenu.Setup();
        narrativeSystem.Setup();
        playerInventoryMenu.Setup();
        settlerNPCMenu.Setup();
        utilityMenu.Setup();
    }

    public void InteractionPrompt(string text)
    {
        interactionPromptUI.ShowPanel(text);
    }

    public void HideInteractionPropt()
    {
        interactionPromptUI.HidePanel();
    }

    public void HideMenus()
    {
        buildMenu.SetScreenActive(false);
        narrativeSystem.SetScreenActive(false);
        playerInventoryMenu.SetScreenActive(false);
        settlerNPCMenu.SetScreenActive(false);
        utilityMenu.SetScreenActive(false);
    }


    public void SetScreenActive(MenuBase menu, bool active, float delay = 0.0f)
    {
        if(delay > 0.0f)
        {
            if (openingMenu == null)
                openingMenu = StartCoroutine(EnableMenuAfterDelay(menu, active, delay));
        }
        else
        {
            menu.gameObject.SetActive(active);
        }
    }

    private IEnumerator EnableMenuAfterDelay(MenuBase menu, bool active, float delay) //This delay is needed to stop the build menu from appearing and disappearing quickly from the ui building button being pressed
    {
        yield return new WaitForSeconds(delay); // Add a slight delay
        menu.gameObject.SetActive(active);
        openingMenu = null;
    }
}
