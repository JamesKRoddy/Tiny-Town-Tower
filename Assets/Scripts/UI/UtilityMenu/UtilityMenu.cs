using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UtilityMenu : MenuBase, IControllerInput
{
    public void Awake()
    {
        playerInventoryBtn.onClick.AddListener(EnablePlayerInventoryMenu);
        buildMenuBtn.onClick.AddListener(EnableBuildMenu);
        settlerNPCBtn.onClick.AddListener(EnableSettlerNPCMenu);
        turretBuildBtn.onClick.AddListener(EnableTurretBuildMenu);
        geneticMutationBtn.onClick.AddListener(EnableGeneticMutationMenu);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        playerInventoryBtn.onClick.RemoveAllListeners();
        buildMenuBtn.onClick.RemoveAllListeners();
        settlerNPCBtn.onClick.RemoveAllListeners();
        turretBuildBtn.onClick.RemoveAllListeners();
        geneticMutationBtn.onClick.RemoveAllListeners();
    }

    [SerializeField] Button playerInventoryBtn;
    [SerializeField] Button buildMenuBtn;
    [SerializeField] Button settlerNPCBtn;
    [SerializeField] Button turretBuildBtn;
    [SerializeField] Button geneticMutationBtn;

    public void ReturnToGame(PlayerControlType playerControlType = PlayerControlType.NONE)
    {
        PlayerUIManager.Instance.HideUtilityMenus();

        if (playerControlType != PlayerControlType.NONE)
        {
            PlayerInput.Instance.UpdatePlayerControls(playerControlType);
        }
        else
        {
            PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
        }        
    }

    #region Menus Active

    public void EnableMenu(MenuBase menu) //TODO replace the below functions with this!!!!!!!!!
    {
        PlayerUIManager.Instance.HideUtilityMenus();
        menu.SetScreenActive(true, 0.1f);
    }

    public void EnableUtilityMenu()
    {
        PlayerUIManager.Instance.HideUtilityMenus();
        SetScreenActive(true, 0.1f);
    }

    public void EnablePlayerInventoryMenu()
    {
        PlayerUIManager.Instance.HideUtilityMenus();
        PlayerUIManager.Instance.playerInventoryMenu.SetScreenActive(true, 0.1f);
    }

    public void EnableBuildMenu()
    {
        PlayerUIManager.Instance.HideUtilityMenus();
        PlayerUIManager.Instance.buildMenu.SetScreenActive(true, 0.1f);
    }


    private void EnableSettlerNPCMenu()
    {
        PlayerUIManager.Instance.HideUtilityMenus();
        PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(true, 0.1f);
    }

    private void EnableTurretBuildMenu()
    {
        PlayerUIManager.Instance.HideUtilityMenus();
        PlayerUIManager.Instance.turretMenu.SetScreenActive(true, 0.1f);
    }

    public void EnableGeneticMutationMenu()
    {
        PlayerUIManager.Instance.HideUtilityMenus();
        PlayerUIManager.Instance.geneticMutationMenu.SetScreenActive(true, 0.1f);
    }

    #endregion

    internal void OpenMenu(PlayerControlType playerControlType)//TODO change this to use GameMode instead of player controls
    {
        playerInventoryBtn.gameObject.SetActive(false);
        buildMenuBtn.gameObject.SetActive(false);
        settlerNPCBtn.gameObject.SetActive(false);
        turretBuildBtn.gameObject.SetActive(false);
        geneticMutationBtn.gameObject.SetActive(false);

        switch (playerControlType)
        {
            case PlayerControlType.COMBAT_NPC_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                geneticMutationBtn.gameObject.SetActive(true);
                break;
            case PlayerControlType.CAMP_NPC_MOVEMENT:
                playerInventoryBtn.gameObject.SetActive(true);
                buildMenuBtn.gameObject.SetActive(true);
                settlerNPCBtn.gameObject.SetActive(true);
                geneticMutationBtn.gameObject.SetActive(true);
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
