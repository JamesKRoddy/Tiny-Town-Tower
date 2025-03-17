using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryPreviewBtn : PreviewButtonBase<ResourceScriptableObj>
{
    public void SetupButton(ResourceScriptableObj resourceObjRef)
    {
        base.SetupButton(resourceObjRef, resourceObjRef.resourceSprite, resourceObjRef.resourceName);
    }

    protected override void OnButtonClicked()
    {
        // Open inventory submenu (if needed)
    }
}

