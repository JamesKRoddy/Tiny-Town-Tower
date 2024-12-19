using UnityEngine;

public abstract class MenuBase : MonoBehaviour
{
    public abstract void SetScreenActive(bool active, float delay = 0.0f);
    public abstract void Setup();
}
