using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryMenu : PreviewListMenuBase<ResourceCategory, ResourceScriptableObj>, IControllerInput
{
    private static PlayerInventoryMenu _instance;

    public static PlayerInventoryMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerInventoryMenu>();
                if (_instance == null)
                {
                    Debug.LogError("PlayerInventoryMenu instance not found in the scene!");
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
    }

    public override void OnEnable()
    {
        base.OnEnable();

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;

        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;

        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnRBPressed += rightScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnLBPressed += leftScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnBPressed += () => UtilityMenu.Instance.EnableUtilityMenu();
                break;
            default:
                break;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay);
    }

    // Retrieve the player's inventory, returning the `ResourceScriptableObj` directly
    public override IEnumerable<ResourceScriptableObj> GetItems()
    {
        // Extract resources from the inventory list
        var inventory = PlayerInventory.Instance.GetFullInventory();
        foreach (var item in inventory)
        {
            yield return item.resource;
        }
    }

    // Group resources by their category
    public override ResourceCategory GetItemCategory(ResourceScriptableObj item)
    {
        return item.resourceCategory;
    }

    // Setup the button to display the resource details
    public override void SetupItemButton(ResourceScriptableObj item, GameObject button)
    {
        var buttonComponent = button.GetComponent<InventoryPreviewBtn>();
        buttonComponent.SetupButton(item); 
    }


    public override string GetPreviewName(ResourceScriptableObj item)
    {
        return item.resourceName;
    }

    // Fetch the sprite for the preview
    public override Sprite GetPreviewSprite(ResourceScriptableObj item)
    {
        return item.resourceSprite;
    }

    // Fetch the description for the preview
    public override string GetPreviewDescription(ResourceScriptableObj item)
    {
        return item.resourceDescription;
    }

    // Resource costs are not relevant for inventory items; return an empty list
    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(ResourceScriptableObj item)
    {
        yield break;
    }

    public override void UpdatePreviewSpecifics(ResourceScriptableObj item)
    {
        Debug.Log("UNIMPLEMENTED FUNCTION");
    }

    public override void DestroyPreviewSpecifics()
    {
        Debug.Log("UNIMPLEMENTED FUNCTION");
    }
}
