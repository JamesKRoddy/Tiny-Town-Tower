using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IPickupableItem
{
    [Header("General Weapon Stats")]
    protected WeaponScriptableObj weaponData;

    public WeaponScriptableObj WeaponData => weaponData;

    public virtual void Initialize(ResourceScriptableObj data)
    {
        if (data is WeaponScriptableObj weaponScriptableObj)
        {
            weaponData = weaponScriptableObj;
        }
        else
        {
            Debug.LogError($"Attempted to initialize weapon with incorrect data type: {data.GetType()}");
        }
    }

    // Virtual method to override in child classes
    public abstract void Use();

    public abstract void StopUse();

    public abstract void OnEquipped(Transform character);

    public string GetItemName() => weaponData?.objectName ?? "Unnamed Weapon";

    public string GetItemDescription() => weaponData?.description ?? "No description available";

    public Sprite GetItemImage() => weaponData?.sprite;
}
