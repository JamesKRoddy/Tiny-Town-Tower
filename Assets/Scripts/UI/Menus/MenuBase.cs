using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class MenuBase : MonoBehaviour
{
    public abstract void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null);
    public abstract void Setup();

    public virtual void DisplayErrorMessage(string message)
    {
        PlayerUIManager.Instance.DisplayUIErrorMessage(message);
    }

    public void StartBtn()
    {
        LoadScene("CampScene", GameMode.CAMP);
    }

    public void ContinueBtn()
    {
        Debug.LogWarning("Not Implemented!!!!");
    }

    public void OptionsBtn()
    {
        Debug.LogWarning("Not Implemented!!!!");
    }

    public void QuitBtn()
    {
        Debug.LogWarning("Not Implemented!!!!");
    }

    public virtual void LoadScene(string targetScene, GameMode nextSceneGameMode, bool keepPlayerControls = false, bool keepPossessedNPC = false)
    {
        SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPlayerControls, keepPossessedNPC);
    }
}
