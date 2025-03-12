using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MenuBase, IControllerInput
{
    public override void Setup()
    {
        audioSettingsBtn.onClick.AddListener(OpenAudioSettings);
        videoSettingsBtn.onClick.AddListener(OpenVideoSettings);
        controlsSettingsBtn.onClick.AddListener(OpenControlsSettings);
        backButton.onClick.AddListener(CloseSettingsMenu);

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public void OnEnable()
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
        EventSystem.current.SetSelectedGameObject(audioSettingsBtn.gameObject);
    }

    void OnDestroy()
    {
        audioSettingsBtn.onClick.RemoveAllListeners();
        videoSettingsBtn.onClick.RemoveAllListeners();
        controlsSettingsBtn.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    [SerializeField] Button audioSettingsBtn;
    [SerializeField] Button videoSettingsBtn;
    [SerializeField] Button controlsSettingsBtn;
    [SerializeField] Button backButton;

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active);
    }

    public void OpenAudioSettings()
    {
        //PlayerUIManager.Instance.HideMenus();
        //AudioPlayerUIManager.Instance.settingsMenu.SetScreenActive(true, 0.1f);
    }

    public void OpenVideoSettings()
    {
        //PlayerUIManager.Instance.HideMenus();
        //VideoPlayerUIManager.Instance.settingsMenu.SetScreenActive(true, 0.1f);
    }

    public void OpenControlsSettings()
    {
        //PlayerUIManager.Instance.HideMenus();
        //ControlsPlayerUIManager.Instance.settingsMenu.SetScreenActive(true, 0.1f);
    }

    public void CloseSettingsMenu()
    {
        SetScreenActive(false);
        PlayerUIManager.Instance.pauseMenu.SetScreenActive(true, 0.1f);
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;
        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnBPressed += () => CloseSettingsMenu();
                break;
            default:
                break;
        }
    }
}
