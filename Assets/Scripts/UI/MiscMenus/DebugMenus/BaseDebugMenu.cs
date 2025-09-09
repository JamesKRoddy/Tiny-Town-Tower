using UnityEngine;

public abstract class BaseDebugMenu : MonoBehaviour
{
    [Header("Base Debug Menu")]
    [SerializeField] protected string menuName = "Debug Menu";
    [SerializeField] protected KeyCode toggleKey = KeyCode.F1;
    
    public virtual void RegisterMenu()
    {
        // Hide menu by default
        gameObject.SetActive(false);
    }
    
    protected virtual void OnDestroy()
    {
        // Unregister from debug menu manager
        DebugMenuManager.Instance?.UnregisterDebugMenu(this);
    }
    
    public virtual void ToggleMenu()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
    
    public virtual void ShowMenu()
    {
        gameObject.SetActive(true);
    }
    
    public virtual void HideMenu()
    {
        gameObject.SetActive(false);
    }
    
    public KeyCode GetToggleKey() => toggleKey;
    public string GetMenuName() => menuName;
} 