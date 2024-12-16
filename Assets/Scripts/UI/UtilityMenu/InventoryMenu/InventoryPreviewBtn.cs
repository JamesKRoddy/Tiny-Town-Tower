using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryPreviewBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] Button button;
    [SerializeField] Image InventoryImage;
    [SerializeField] TMP_Text inventoryNameText;

    ResourceScriptableObj buildingObj;

    void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayerInventoryMenu.Instance.UpdatePreview(buildingObj);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayerInventoryMenu.Instance.UpdatePreview();
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlayerInventoryMenu.Instance.UpdatePreview(buildingObj);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        PlayerInventoryMenu.Instance.UpdatePreview();
    }

    public void SetupButton(ResourceScriptableObj resourceObjRef)
    {
        buildingObj = resourceObjRef;

        //button.onClick.AddListener(InstanciateBuildingConstruction); TODO //Open up sub menu with drop and stuff in it

        if (resourceObjRef.resourceSprite != null)
            InventoryImage.sprite = resourceObjRef.resourceSprite;

        inventoryNameText.text = resourceObjRef.resourceName;

    }
}
