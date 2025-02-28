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

    public virtual void LoadScene(string scene)
    {
        SceneTransitionManager.Instance.LoadScene(scene);
    }
}
