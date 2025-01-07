using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IPickupableItem
{
    [Header("General Weapon Stats")]
    public WeaponScriptableObj weaponScriptableObj;

    // Virtual method to override in child classes
    public abstract void Use();

    public abstract void StopUse();

    public abstract void OnEquipped();

    public string GetItemName() => weaponScriptableObj.resourceName;

    public string GetItemDescription() => weaponScriptableObj.resourceDescription;

    public Sprite GetItemImage() => weaponScriptableObj.resourceSprite;
}

public enum WeaponElement
{
    NONE,
    BASIC,
    FIE,
    ELECTRIC,
    BLEED,
    HOLY
}

public enum WeaponAnimationType
{
    NONE,
    ONE_HANDED,
    TWO_HANDED
}