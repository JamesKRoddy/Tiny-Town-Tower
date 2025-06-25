using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Managers;

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

    internal void OpenMenu()
    {
        playerInventoryBtn.gameObject.SetActive(false);
        buildMenuBtn.gameObject.SetActive(false);
        settlerNPCBtn.gameObject.SetActive(false);
        turretBuildBtn.gameObject.SetActive(false);
        geneticMutationBtn.gameObject.SetActive(false);

        switch(GameManager.Instance.CurrentGameMode){
            case GameMode.ROGUE_LITE:
                playerInventoryBtn.gameObject.SetActive(true);
                geneticMutationBtn.gameObject.SetActive(true);
                break;
            case GameMode.CAMP:
                playerInventoryBtn.gameObject.SetActive(true);
                buildMenuBtn.gameObject.SetActive(true);
                settlerNPCBtn.gameObject.SetActive(true);
                turretBuildBtn.gameObject.SetActive(true);
                geneticMutationBtn.gameObject.SetActive(true);
                break;
            case GameMode.CAMP_ATTACK:
                playerInventoryBtn.gameObject.SetActive(true);
                break;
            default:
                Debug.LogError("UtilityMenu: OpenMenu: Invalid game mode");
                break;
        }

        SetScreenActive(true, 0.1f);
    }
}
