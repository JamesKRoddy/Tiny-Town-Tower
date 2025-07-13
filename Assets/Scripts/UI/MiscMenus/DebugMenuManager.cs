using System.Collections.Generic;
using UnityEngine;

public class DebugMenuManager : MonoBehaviour
{
    private static DebugMenuManager _instance;
    public static DebugMenuManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DebugMenuManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DebugMenuManager");
                    _instance = go.AddComponent<DebugMenuManager>();
                }
            }
            return _instance;
        }
    }
    
    [Header("Debug Menu Manager")]
    [SerializeField] private bool enableDebugMenus = true;
    [SerializeField, ReadOnly] private List<BaseDebugMenu> registeredMenus = new List<BaseDebugMenu>();
    
    private Dictionary<KeyCode, BaseDebugMenu> keyToMenuMap = new Dictionary<KeyCode, BaseDebugMenu>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
    }

    void Start()
    {
        //#if UNITY_EDITOR
        enableDebugMenus = true;
        
        // Find all BaseDebugMenu components, including inactive ones
        registeredMenus = new List<BaseDebugMenu>(FindObjectsByType<BaseDebugMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        
        foreach (var menu in registeredMenus)
        {
            menu.RegisterMenu();
            keyToMenuMap[menu.GetToggleKey()] = menu;
            Debug.Log($"[DebugMenuManager] Registered {menu.GetMenuName()} with key {menu.GetToggleKey()}");
        }
        //#else
        //enableDebugMenus = false;
        //#endif
    }
    
    private void Update()
    {
        #if UNITY_EDITOR
        if (!enableDebugMenus) return;
        
        // Check for F key inputs
        foreach (var kvp in keyToMenuMap)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                kvp.Value.ToggleMenu();
                Debug.Log($"[DebugMenuManager] Toggled {kvp.Value.GetMenuName()} with {kvp.Key}");
            }
        }
        #endif
    }
    
    public void UnregisterDebugMenu(BaseDebugMenu menu)
    {
        if (menu == null) return;
        
        if (registeredMenus.Contains(menu))
        {
            registeredMenus.Remove(menu);
            keyToMenuMap.Remove(menu.GetToggleKey());
            Debug.Log($"[DebugMenuManager] Unregistered {menu.GetMenuName()}");
        }
    }
    
    public void HideAllMenus()
    {
        foreach (var menu in registeredMenus)
        {
            if (menu != null)
            {
                menu.HideMenu();
            }
        }
    }
    
    public void SetEnableDebugMenus(bool enable)
    {
        enableDebugMenus = enable;
        if (!enable)
        {
            HideAllMenus();
        }
    }
    
    [ContextMenu("List Registered Menus")]
    public void ListRegisteredMenus()
    {
        Debug.Log($"[DebugMenuManager] Registered Menus ({registeredMenus.Count}):");
        foreach (var menu in registeredMenus)
        {
            if (menu != null)
            {
                Debug.Log($"  {menu.GetMenuName()} - {menu.GetToggleKey()}");
            }
        }
    }
} 