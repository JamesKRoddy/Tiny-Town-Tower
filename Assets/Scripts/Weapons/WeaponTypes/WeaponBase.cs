using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IPickupableItem
{
    [Header("General Weapon Stats")]
    public WeaponScriptableObj weaponScriptableObj;

    // Virtual method to override in child classes
    public abstract void Use();

    public abstract void StopUse();

    public abstract void OnEquipped(Transform character);

    public string GetItemName() => weaponScriptableObj.resourceName;

    public string GetItemDescription() => weaponScriptableObj.resourceDescription;

    public Sprite GetItemImage() => weaponScriptableObj.resourceSprite;
}
