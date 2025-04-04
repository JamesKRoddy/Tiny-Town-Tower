using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MenuBase, IControllerInput
{
    public void Awake()
    {
        audioSettingsBtn.onClick.AddListener(OpenAudioSettings);
        videoSettingsBtn.onClick.AddListener(OpenVideoSettings);
        controlsSettingsBtn.onClick.AddListener(OpenControlsSettings);
        backButton.onClick.AddListener(CloseSettingsMenu);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        audioSettingsBtn.onClick.RemoveAllListeners();
        videoSettingsBtn.onClick.RemoveAllListeners();
        controlsSettingsBtn.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    [SerializeField] Button audioSettingsBtn;
    [SerializeField] Button videoSettingsBtn;
    [SerializeField] Button controlsSettingsBtn;
    [SerializeField] Button backButton;

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
}
