using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UtilityMenu : MenuBase, IControllerInput
{
    private static UtilityMenu _instance;

    public static UtilityMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UtilityMenu>();
                if (_instance == null)
                {
                    Debug.LogError("UtilityMenu instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    public override void Setup()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }

        playerInventoryBtn.onClick.AddListener(EnablePlayerInventoryMenu);
        buildMenuBtn.onClick.AddListener(EnableBuildMenu);
        settlerNPCBtn.onClick.AddListener(EnableSettlerNPCMenu);
        turretBuildBtn.onClick.AddListener(EnableTurretBuildMenu);

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    private PlayerControlType returnToControls; //Used for when the menu is closed which controlls are gonna be used

    public void OnEnable()
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);

        EventSystem.current.SetSelectedGameObject(playerInventoryBtn.gameObject);
    }

    void OnDestroy()
    {
        playerInventoryBtn.onClick.RemoveAllListeners();
        buildMenuBtn.onClick.RemoveAllListeners();
        settlerNPCBtn.onClick.RemoveAllListeners();
        turretBuildBtn.onClick.RemoveAllListeners();

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    [SerializeField] Button playerInventoryBtn;
    [SerializeField] Button buildMenuBtn;
    [SerializeField] Button settlerNPCBtn;
    [SerializeField] Button turretBuildBtn;

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        if (active)
        {
            CurrentGameMode currentGameMode = GameManager.Instance.CurrentGameMode;

            switch (currentGameMode)
            {
                case CurrentGameMode.NONE:
                    returnToControls = PlayerControlType.NONE;
                    break;
                case CurrentGameMode.ROGUE_LITE:
                    returnToControls = PlayerControlType.COMBAT_NPC_MOVEMENT;
                    break;
                case CurrentGameMode.CAMP:
                    if (PlayerController.Instance._possessedNPC != null)
                        returnToControls = PlayerControlType.CAMP_NPC_MOVEMENT;
                    else
                        returnToControls = PlayerControlType.BUILDING_CAMERA_MOVEMENT;
                    break;
                case CurrentGameMode.TURRET:
                    returnToControls = PlayerControlType.TURRET_CAMERA_MOVEMENT;
                    break;
                default:
                    break;
            }
        }
        else
        {
            if (gameObject.activeInHierarchy == true)
                ReturnToGame();
        }

        PlayerUIManager.Instance.SetScreenActive(this, active);
    }

    public void ReturnToGame()
    {
        PlayerInput.Instance.UpdatePlayerControls(returnToControls);
    }

    public void EnableUtilityMenu()
    {
        PlayerUIManager.Instance.HideMenus();
        SetScreenActive(true, 0.1f);
    }

    public void EnablePlayerInventoryMenu()
    {
        PlayerUIManager.Instance.HideMenus();
        PlayerInventoryMenu.Instance.SetScreenActive(true, 0.1f);
    }

    public void EnableBuildMenu()
    {
        PlayerUIManager.Instance.HideMenus();
        BuildMenu.Instance.SetScreenActive(true, 0.1f);
    }


    private void EnableSettlerNPCMenu()
    {
        PlayerUIManager.Instance.HideMenus();
        SettlerNPCMenu.Instance.SetScreenActive(true, 0.1f);
    }

    private void EnableTurretBuildMenu()
    {
        PlayerUIManager.Instance.HideMenus();
        TurretMenu.Instance.SetScreenActive(true, 0.1f);
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;
        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnBPressed += () => PlayerUIManager.Instance.HideMenus();
                break;
            default:
                break;
        }
    }

    internal void OpenMenu(PlayerControlType playerControlType) //TODO need to figure out how to differenciate if the player is in combat or in camp, use returnToControls above
    {
        playerInventoryBtn.gameObject.SetActive(false);
        buildMenuBtn.gameObject.SetActive(false);
        settlerNPCBtn.gameObject.SetActive(false);
        turretBuildBtn.gameObject.SetActive(false);

        switch (playerControlType)
        {
            case PlayerControlType.COMBAT_NPC_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                break;
            case PlayerControlType.CAMP_NPC_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                buildMenuBtn.gameObject.SetActive(true);
                settlerNPCBtn.gameObject.SetActive(true);
                break;
            case PlayerControlType.TURRET_CAMERA_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                turretBuildBtn.gameObject.SetActive(true);
                break;
            default:
                break;
        }

        SetScreenActive(true, 0.1f);
    }
}
