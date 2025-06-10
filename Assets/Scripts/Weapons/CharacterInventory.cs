using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



/// <summary>
/// This is the NPCs characters inventory 
/// 
/// TODO once a player posesses an NPC items picked up are added to this inventory, once they return to base the items are added to the players inventory
/// </summary>
public class CharacterInventory : MonoBehaviour
{
    [Header("Equipment")]
    public WeaponScriptableObj equippedWeaponScriptObj;
    public WeaponBase equippedWeaponBase;
    public Transform weaponHolder; // Transform where the weapon will be instantiated

    [Header("Inventory")]
    [SerializeField]
    private List<ResourceItemCount> inventoryList = new List<ResourceItemCount>();

    // Event for when a weapon is equipped
    public event System.Action<WeaponScriptableObj> OnWeaponEquipped;

    public virtual void Start()
    {
        if(equippedWeaponScriptObj != null)
        {
            EquipWeapon(equippedWeaponScriptObj);
        }
    }

    protected virtual Transform GetCharacterTransform()
    {
        return this.transform;
    }

    #region Inventory_Items

    public void AddItem(ResourceScriptableObj item, int count = 1)
    {
        var existingItem = inventoryList.Find(i => i.resourceScriptableObj == item);
        if (existingItem != null)
        {
            existingItem.count += count;
        }
        else
        {
            inventoryList.Add(new ResourceItemCount(item, count));
        }
    }

    public void AddItem(List<ResourceItemCount> items)
    {
        foreach (var item in items)
        {
            AddItem(item.resourceScriptableObj, item.count);
        }
    }

    public void RemoveItem(ResourceScriptableObj item, int count = 1)
    {
        var existingItem = inventoryList.Find(i => i.resourceScriptableObj == item);
        if (existingItem != null)
        {
            existingItem.count -= count;
            if (existingItem.count <= 0)
            {
                inventoryList.Remove(existingItem);
            }
        }
    }

    public void ClearInventory()
    {
        inventoryList.Clear();
    }

    public bool HasItemByName(string itemName)
    {
        return inventoryList.Any(i => i.resourceScriptableObj.objectName == itemName);
    }

    public int GetItemCount(ResourceScriptableObj item)
    {
        var existingItem = inventoryList.Find(i => i.resourceScriptableObj == item);
        return existingItem != null ? existingItem.count : 0;
    }

    public List<ResourceItemCount> GetFullInventory()
    {
        return inventoryList;
    }

    public List<ResourceScriptableObj> GetAllItemsOfCategory(ResourceCategory resourceCategory){
        return inventoryList.Where(i => i.resourceScriptableObj.category == resourceCategory).Select(i => i.resourceScriptableObj).ToList();
    }

    #endregion

    #region NPC_Weapon

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
        GameObject weapon = Instantiate(weaponScriptableObj.prefab, weaponHolder);
        equippedWeaponBase = weapon.GetComponent<WeaponBase>();

        // Initialize the weapon with its data
        equippedWeaponBase.Initialize(weaponScriptableObj);
        equippedWeaponBase.OnEquipped(transform);
        equippedWeaponScriptObj = weaponScriptableObj;

        // Handle weapon-specific setup
        HandleWeaponType(weapon, weaponScriptableObj.animationType);

        // Notify listeners that a weapon was equipped
        OnWeaponEquipped?.Invoke(weaponScriptableObj);
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
        // Check if scriptable object exists
        if (weaponScriptableObj == null)
        {
            Debug.LogWarning("WeaponScriptableObj is null");
            return false;
        }

        // Check if prefab exists
        if (weaponScriptableObj.prefab == null)
        {
            Debug.LogWarning($"Prefab is missing on WeaponScriptableObj: {weaponScriptableObj.name}");
            return false;
        }

        GameObject prefab = weaponScriptableObj.prefab;

        // Check for WeaponBase component
        if (prefab.GetComponent<WeaponBase>() == null)
        {
            Debug.LogWarning($"WeaponBase component missing on prefab: {prefab.name}");
            return false;
        }

        // Check for Collider component
        if (prefab.GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"Collider component missing on prefab: {prefab.name}");
            return false;
        }

        return true;
    }

    private void HandleWeaponType(GameObject weapon, WeaponAnimationType animationType)
    {
        switch (equippedWeaponBase)
        {
            case MeleeWeapon meleeWeapon:
                GetComponent<HumanCharacterController>().EquipMeleeWeapon((int)animationType);
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

    #endregion
}
