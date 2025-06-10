using Managers;
using UnityEngine;
using UnityEngine.UI;

public class DeathMenu : MenuBase
{
    [SerializeField] Button returnToCampBtn;

    void Awake()
    {
        returnToCampBtn.onClick.AddListener(ReturnToCamp);
    }

    void ReturnToCamp()
    {
        RogueLiteManager.Instance.ReturnToCamp(false);

        SetScreenActive(false);
    }
}
