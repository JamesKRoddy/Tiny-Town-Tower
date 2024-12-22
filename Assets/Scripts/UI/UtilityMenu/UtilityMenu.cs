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

        playerInventoryBtn.onClick.AddListener(EnablePlayerInventory);
        buildMenuBtn.onClick.AddListener(EnableBuildMenu);
        settlerNPCBtn.onClick.AddListener(EnableSettlerNPCMenu);

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

        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    [SerializeField] Button playerInventoryBtn;
    [SerializeField] Button buildMenuBtn;
    [SerializeField] Button settlerNPCBtn;

    public override void SetScreenActive(bool active, float delay = 0.0f)
    {
        gameObject.SetActive(active);

        if (active)
        {
            PlayerControlType controlType = PlayerInput.Instance.currentControlType;

            if (controlType == PlayerControlType.CAMP_MOVEMENT || controlType == PlayerControlType.COMBAT_MOVEMENT)
                returnToControls = controlType;
        }
        else
        {
            PlayerInput.Instance.UpdatePlayerControls(returnToControls);
        }
    }

    public void EnableUtilityMenu()
    {
        PlayerUIManager.Instance.HideMenus();
        SetScreenActive(true, 0.1f);
    }

    public void EnablePlayerInventory()
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

    public void SetPlayerControlType(PlayerControlType controlType)
    {
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
        Debug.Log($"Open Menu: {playerControlType}");
        playerInventoryBtn.gameObject.SetActive(false);
        buildMenuBtn.gameObject.SetActive(false);
        settlerNPCBtn.gameObject.SetActive(false);

        switch (playerControlType)
        {
            case PlayerControlType.COMBAT_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                break;
            case PlayerControlType.CAMP_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                buildMenuBtn.gameObject.SetActive(true);
                settlerNPCBtn.gameObject.SetActive(true);
                break;
            default:
                break;
        }

        SetScreenActive(true);
    }
}
