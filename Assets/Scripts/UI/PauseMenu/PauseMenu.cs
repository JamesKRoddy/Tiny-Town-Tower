using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MenuBase, IControllerInput
{
    public override void Setup()
    {
        resumeGameBtn.onClick.AddListener(() => ReturnToGame());
        settingsBtn.onClick.AddListener(OpenSettings);
        returnToCampBtn.onClick.AddListener(ReturnToCamp);
        quitGameBtn.onClick.AddListener(QuitGame);

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public void OnEnable()
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
        EventSystem.current.SetSelectedGameObject(resumeGameBtn.gameObject);
    }

    void OnDestroy()
    {
        resumeGameBtn.onClick.RemoveAllListeners();
        settingsBtn.onClick.RemoveAllListeners();
        returnToCampBtn.onClick.RemoveAllListeners();
        quitGameBtn.onClick.RemoveAllListeners();

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    [SerializeField] Button resumeGameBtn;
    [SerializeField] Button settingsBtn;
    [SerializeField] Button returnToCampBtn;
    [SerializeField] Button quitGameBtn;

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active);
    }

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

    private void QuitGame()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PlayerUIManager.Instance.quitMenu.SetScreenActive(true, 0.1f);
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;
        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnBPressed += () => ReturnToGame();
                break;
            default:
                break;
        }
    }

    internal void OpenMenu(PlayerControlType playerControlType)
    {
        resumeGameBtn.gameObject.SetActive(false);
        settingsBtn.gameObject.SetActive(false);
        returnToCampBtn.gameObject.SetActive(false);
        quitGameBtn.gameObject.SetActive(false);

        switch (playerControlType)
        {
            case PlayerControlType.COMBAT_NPC_MOVEMENT:
                resumeGameBtn.gameObject.SetActive(true);
                settingsBtn.gameObject.SetActive(true);
                returnToCampBtn.gameObject.SetActive(true);
                break;
            case PlayerControlType.CAMP_NPC_MOVEMENT:
                resumeGameBtn.gameObject.SetActive(true);
                settingsBtn.gameObject.SetActive(true);
                quitGameBtn.gameObject.SetActive(true);
                break;
            case PlayerControlType.TURRET_CAMERA_MOVEMENT:
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
