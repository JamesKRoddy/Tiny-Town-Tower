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
    public WeaponScriptableObj equippedWeapon;
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

        if(equippedWeapon != null)
        {
            EquipWeapon(equippedWeapon);
        }
    }

    public virtual void EquipWeapon(WeaponScriptableObj weaponScriptableObj)
    {
        // Unequip the currently equipped weapon
        if (equippedWeapon != null && weaponHolder.childCount > 0)
        {
            Destroy(weaponHolder.GetChild(0).gameObject);
        }

        // Instantiate the new weapon and set it as equipped
        if (weaponScriptableObj != null && weaponScriptableObj.weaponPrefab != null)
        {
            Instantiate(weaponScriptableObj.weaponPrefab, weaponHolder);
            equippedWeapon = weaponScriptableObj;
            PlayerController.Instance.EquipMeleeWeapon((int)weaponScriptableObj.animationType);
        }
        else
        {
            Debug.LogWarning("Attempted to equip a null weapon scriptable object or prefab!");
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
