using System;
using UnityEngine;

public abstract class MenuBase : MonoBehaviour
{
    public abstract void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null);
    public abstract void Setup();


    public virtual void DisplayErrorMessage(string message)
    {
        PlayerUIManager.Instance.DisplayUIErrorMessage(message);
    }
}
