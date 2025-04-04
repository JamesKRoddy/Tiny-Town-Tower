using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReturnToCampMenu : MenuBase
{
    public void Awake()
    {
        yesBtn.onClick.AddListener(ReturnToCamp);
        noBtn.onClick.AddListener(CloseMenu);
    }

    [SerializeField] Button yesBtn;
    [SerializeField] Button noBtn;

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
