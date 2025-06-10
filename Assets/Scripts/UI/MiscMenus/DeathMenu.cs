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
        SceneTransitionManager.Instance.LoadScene("CampScene", GameMode.CAMP, false);

        SetScreenActive(false);
    }
}
