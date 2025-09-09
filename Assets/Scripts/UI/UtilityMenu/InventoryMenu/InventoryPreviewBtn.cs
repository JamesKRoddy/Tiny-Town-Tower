using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryPreviewBtn : PreviewButtonBase<ResourceScriptableObj>
{
    public void SetupButton(ResourceScriptableObj resourceObjRef)
    {
        base.SetupButton(resourceObjRef, resourceObjRef.sprite, resourceObjRef.objectName);
    }

    protected override void OnDefaultButtonClicked()
    {
        // Open inventory submenu (if needed)
    }
}

