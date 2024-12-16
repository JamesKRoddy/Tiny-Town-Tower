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

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public void OnEnable()
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);

        EventSystem.current.SetSelectedGameObject(playerInventoryBtn.gameObject);
    }

    void OnDestroy()
    {
        playerInventoryBtn.onClick.RemoveAllListeners();
        buildMenuBtn.onClick.RemoveAllListeners();

        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    [SerializeField] Button playerInventoryBtn;
    [SerializeField] Button buildMenuBtn;

    public override void SetScreenActive(bool active)
    {
        gameObject.SetActive(active);

        if (!active)
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_MOVEMENT); //TODO figure out which controls to go back to combat or camp movement
    }

    void EnablePlayerInventory()
    {
        PlayerUIManager.Instance.HideMenus();
        PlayerInventoryMenu.Instance.SetScreenActive(true);
    }

    void EnableBuildMenu()
    {
        PlayerUIManager.Instance.HideMenus();
        BuildMenu.Instance.SetScreenActive(true);
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        return;
    }

    internal void OpenMenu(PlayerControlType playerControlType) //TODO need to figure out how to differenciate if the player is in combat or in camp
    {
        playerInventoryBtn.gameObject.SetActive(false);
        buildMenuBtn.gameObject.SetActive(false);

        switch (playerControlType)
        {
            case PlayerControlType.COMBAT_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                break;
            case PlayerControlType.CAMP_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                buildMenuBtn.gameObject.SetActive(true);
                break;
            default:
                break;
        }

        SetScreenActive(true);
    }
}
