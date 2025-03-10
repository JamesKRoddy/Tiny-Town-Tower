using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuitMenu : MenuBase
{
    private static QuitMenu _instance;

    public static QuitMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<QuitMenu>();
                if (_instance == null)
                {
                    Debug.LogError("QuitMenu instance not found in the scene!");
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

        yesBtn.onClick.AddListener(QuitApplication);
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
        PauseMenu.Instance.SetScreenActive(true, 0.1f);
    }

    void QuitApplication()
    {
        PlayerUIManager.Instance.HidePauseMenus();
        Application.Quit();
    }
}
