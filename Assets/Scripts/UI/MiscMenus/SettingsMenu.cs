using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MenuBase, IControllerInput
{
    private static SettingsMenu _instance;

    public static SettingsMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettingsMenu>();
                if (_instance == null)
                {
                    Debug.LogError("SettingsMenu instance not found in the scene!");
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
        //AudioSettingsMenu.Instance.SetScreenActive(true, 0.1f);
    }

    public void OpenVideoSettings()
    {
        //PlayerUIManager.Instance.HideMenus();
        //VideoSettingsMenu.Instance.SetScreenActive(true, 0.1f);
    }

    public void OpenControlsSettings()
    {
        //PlayerUIManager.Instance.HideMenus();
        //ControlsSettingsMenu.Instance.SetScreenActive(true, 0.1f);
    }

    public void CloseSettingsMenu()
    {
        SetScreenActive(false);
        PauseMenu.Instance.SetScreenActive(true, 0.1f);
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
