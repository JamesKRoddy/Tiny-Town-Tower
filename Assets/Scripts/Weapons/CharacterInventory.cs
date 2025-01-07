using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ResourceScriptableObj resource;
    public int count;
}

public class CharacterInventory : MonoBehaviour
{
    [Header("Equipment")]
    public WeaponScriptableObj equippedWeaponScriptObj;
    [HideInInspector] public WeaponBase equippedWeaponBase;
    public Transform weaponHolder; // Transform where the weapon will be instantiated

    [Header("Inventory")]
    [SerializeField]
    private List<InventoryItem> inventoryList = new List<InventoryItem>();

    public virtual void Start()
    {
        if (weaponHolder == null)
        {
            Debug.LogError("WeaponHolder not assigned!");
        }

        if(equippedWeaponScriptObj != null)
        {
            EquipWeapon(equippedWeaponScriptObj);
        }
    }

    public virtual void EquipWeapon(WeaponScriptableObj weaponScriptableObj)
    {       
        // Validate the new weapon
        if (!IsValidWeaponPrefab(weaponScriptableObj))
        {
            Debug.LogWarning("Attempted to equip an invalid or null weapon scriptable object or prefab!");
            return;
        }

        // Unequip the currently equipped weapon
        UnequipCurrentWeapon();

        // Instantiate and equip the new weapon
        GameObject weapon = Instantiate(weaponScriptableObj.weaponPrefab, weaponHolder);
        equippedWeaponBase = weapon.GetComponent<WeaponBase>();
        equippedWeaponBase.OnEquipped();
        equippedWeaponScriptObj = weaponScriptableObj;

        // Handle weapon-specific setup
        HandleWeaponType(weapon, weaponScriptableObj.animationType);
    }

    private void UnequipCurrentWeapon()
    {
        if (equippedWeaponScriptObj != null && weaponHolder.childCount > 0)
        {
            Destroy(weaponHolder.GetChild(0).gameObject);
            equippedWeaponScriptObj = null;
            equippedWeaponBase = null;
        }
    }

    private bool IsValidWeaponPrefab(WeaponScriptableObj weaponScriptableObj)
    {
        if (weaponScriptableObj == null || weaponScriptableObj.weaponPrefab == null)
            return false;

        GameObject prefab = weaponScriptableObj.weaponPrefab;
        return prefab.GetComponent<WeaponBase>() != null &&
               prefab.GetComponent<Rigidbody>() != null &&
               prefab.GetComponent<Collider>() != null;
    }

    private void HandleWeaponType(GameObject weapon, WeaponAnimationType animationType)
    {
        switch (equippedWeaponBase)
        {
            case MeleeWeapon meleeWeapon:
                PlayerController.Instance.EquipMeleeWeapon((int)animationType);
                break;
            case RangedWeapon rangedWeapon:
                Debug.LogError("TODO: Implement ranged weapon setup.");
                break;
            case ThrowableWeapon throwableWeapon:
                Debug.LogError("TODO: Implement throwable weapon setup.");
                break;
            default:
                Debug.LogWarning($"{weapon.name} is of an unsupported weapon type!");
                break;
        }
    }


    public void AddItem(ResourceScriptableObj item, int count = 1)
    {
        var existingItem = inventoryList.Find(i => i.resource == item);
        if (existingItem != null)
        {
            existingItem.count += count;
        }
        else
        {
            inventoryList.Add(new InventoryItem { resource = item, count = count });
        }
    }

    public void RemoveItem(ResourceScriptableObj item, int count = 1)
    {
        var existingItem = inventoryList.Find(i => i.resource == item);
        if (existingItem != null)
        {
            existingItem.count -= count;
            if (existingItem.count <= 0)
            {
                inventoryList.Remove(existingItem);
            }
        }
    }

    public bool HasItemByName(string itemName)
    {
        return inventoryList.Any(i => i.resource.resourceName == itemName);
    }

    public int GetItemCount(ResourceScriptableObj item)
    {
        var existingItem = inventoryList.Find(i => i.resource == item);
        return existingItem != null ? existingItem.count : 0;
    }

    public List<InventoryItem> GetFullInventory()
    {
        return inventoryList;
    }
}
