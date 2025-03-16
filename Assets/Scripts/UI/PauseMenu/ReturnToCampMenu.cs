using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReturnToCampMenu : MenuBase
{
    public override void Setup()
    {
        yesBtn.onClick.AddListener(ReturnToCamp);
        noBtn.onClick.AddListener(CloseMenu);
    }

    [SerializeField] Button yesBtn;
    [SerializeField] Button noBtn;

    public void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(yesBtn.gameObject);
    }

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active);
    }

    void CloseMenu()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PlayerUIManager.Instance.pauseMenu.SetScreenActive(true, 0.1f);
    }

    void ReturnToCamp()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        LoadScene("CampScene", GameMode.CAMP);
    }
}
