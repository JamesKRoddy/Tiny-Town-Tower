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
    public WeaponBase equippedWeapon;
    protected WeaponBase[] prefabWeapons; // Array of all available weapons that the player could equip
    public Transform weaponHolder; // Parent object containing all weapons

    [Header("Inventory")]
    [SerializeField]
    private List<InventoryItem> inventoryList = new List<InventoryItem>();

    public virtual void Start()
    {
        // Cache all weapons under the weapon holder
        if (weaponHolder != null)
        {
            prefabWeapons = weaponHolder.GetComponentsInChildren<WeaponBase>(true);
        }
        else
        {
            Debug.LogError("WeaponHolder not assigned!");
        }

        // Ensure all weapons are disabled at start
        foreach (var weapon in prefabWeapons)
        {
            weapon.gameObject.SetActive(false);
        }
    }

    public virtual void EquipWeapon(WeaponBase weapon)
    {
        // Disable the currently active weapon
        if (equippedWeapon != null)
        {
            equippedWeapon.gameObject.SetActive(false);
        }

        // Find and activate the new weapon in the player's weapon holder
        foreach (var prefabWeapon in prefabWeapons)
        {            
            if (prefabWeapon.weaponScriptableObj.resourceName == weapon.weaponScriptableObj.resourceName)
            {
                prefabWeapon.gameObject.SetActive(true);
                equippedWeapon = prefabWeapon;
                return;
            }
        }

        Debug.LogWarning($"Weapon {weapon.weaponScriptableObj.resourceName} not found in weapon holder!");
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
