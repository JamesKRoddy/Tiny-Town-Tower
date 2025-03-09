using System;
using UnityEngine;
using UnityEngine.UI;

public class ReturnToCampMenu : MenuBase
{
    private static ReturnToCampMenu _instance;

    public static ReturnToCampMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ReturnToCampMenu>();
                if (_instance == null)
                {
                    Debug.LogError("ReturnToCampMenu instance not found in the scene!");
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

        yesBtn.onClick.AddListener(CloseMenu);
        noBtn.onClick.AddListener(ReturnToCamp);
    }

    [SerializeField] Button yesBtn;
    [SerializeField] Button noBtn;

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active);
    }

    void CloseMenu()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        PauseMenu.Instance.SetScreenActive(true, 0.1f);
    }

    void ReturnToCamp()
    {
        LoadScene("CampScene", GameMode.CAMP);
    }
}
