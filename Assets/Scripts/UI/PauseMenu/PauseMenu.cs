using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Managers;

public class PauseMenu : MenuBase, IControllerInput
{
    public void Awake()
    {
        resumeGameBtn.onClick.AddListener(() => ReturnToGame());
        settingsBtn.onClick.AddListener(OpenSettings);
        returnToCampBtn.onClick.AddListener(ReturnToCamp);
        quitGameBtn.onClick.AddListener(QuitGame);
        saveGameBtn.onClick.AddListener(OpenSaveGameMenu);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        resumeGameBtn.onClick.RemoveAllListeners();
        settingsBtn.onClick.RemoveAllListeners();
        returnToCampBtn.onClick.RemoveAllListeners();
        quitGameBtn.onClick.RemoveAllListeners();
        saveGameBtn.onClick.RemoveAllListeners();
    }

    [SerializeField] Button resumeGameBtn;
    [SerializeField] Button settingsBtn;
    [SerializeField] Button returnToCampBtn;
    [SerializeField] Button saveGameBtn;
    [SerializeField] Button quitGameBtn;

    public void ReturnToGame(PlayerControlType playerControlType = PlayerControlType.NONE)
    {
        PlayerUIManager.Instance.HidePauseMenus();

        if (playerControlType != PlayerControlType.NONE)
        {
            PlayerInput.Instance.UpdatePlayerControls(playerControlType);
        }
        else
        {
            PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
        }
    }

    public void EnablePauseMenu()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        SetScreenActive(true, 0.1f);
    }
    
    private void OpenSettings()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PlayerUIManager.Instance.settingsMenu.SetScreenActive(true, 0.1f);
    }

    private void ReturnToCamp()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PlayerUIManager.Instance.returnToCampMenu.SetScreenActive(true, 0.1f);
    }

    private void OpenSaveGameMenu()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PlayerUIManager.Instance.saveGameMenu.SetScreenActive(true, 0.1f);
    }

    private void QuitGame()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PlayerUIManager.Instance.quitMenu.SetScreenActive(true, 0.1f);
    }

    internal void OpenMenu()
    {
        resumeGameBtn.gameObject.SetActive(false);
        settingsBtn.gameObject.SetActive(false);
        returnToCampBtn.gameObject.SetActive(false);
        quitGameBtn.gameObject.SetActive(false);
        saveGameBtn.gameObject.SetActive(false);
        switch (GameManager.Instance.CurrentGameMode)
        {
            case GameMode.ROGUE_LITE:
                resumeGameBtn.gameObject.SetActive(true);
                settingsBtn.gameObject.SetActive(true);
                returnToCampBtn.gameObject.SetActive(true);
                break;
            case GameMode.CAMP:
                resumeGameBtn.gameObject.SetActive(true);
                settingsBtn.gameObject.SetActive(true);
                quitGameBtn.gameObject.SetActive(true);
                saveGameBtn.gameObject.SetActive(true);
                break;
            case GameMode.CAMP_ATTACK:
                resumeGameBtn.gameObject.SetActive(true);
                settingsBtn.gameObject.SetActive(true);
                returnToCampBtn.gameObject.SetActive(true);
                break;
            default:
                break;
        }

        SetScreenActive(true, 0.1f);
    }
}
