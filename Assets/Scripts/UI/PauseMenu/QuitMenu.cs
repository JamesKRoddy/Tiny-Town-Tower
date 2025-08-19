using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuitMenu : MenuBase
{
    public void Awake()
    {
        yesBtn.onClick.AddListener(QuitApplication);
        noBtn.onClick.AddListener(CloseMenu);
    }

    [SerializeField] Button yesBtn;
    [SerializeField] Button noBtn;

    void CloseMenu()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PlayerUIManager.Instance.pauseMenu.SetScreenActive(true, 0.1f);
    }

    void QuitApplication()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
