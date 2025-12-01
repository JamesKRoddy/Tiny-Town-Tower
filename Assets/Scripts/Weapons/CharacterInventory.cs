using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Combat.Attacks;

/// <summary>
/// This is the NPCs characters inventory 
/// 
/// WEAPON SYSTEM:
/// - Uses WeaponAttack component on the character for attack functionality
/// - Weapon prefabs are now just visual models (no WeaponBase required)
/// - WeaponScriptableObj defines stats, element, and visual prefab
/// 
/// TODO once a player posesses an NPC items picked up are added to this inventory, 
/// once they return to base the items are added to the players inventory
/// </summary>
public class CharacterInventory : MonoBehaviour
{
    [Header("Equipment")]
    public WeaponScriptableObj equippedWeaponScriptObj;
    public Transform weaponHolder; // Transform where the weapon will be instantiated
    
    [Header("Weapon Attack Component")]
    [Tooltip("WeaponAttack component on this character (handles damage dealing)")]
    public WeaponAttack weaponAttack;
    
    // The spawned weapon model (visual only)
    private GameObject spawnedWeaponModel;

    [Header("Inventory")]
    [SerializeField]
    private List<ResourceItemCount> inventoryList = new List<ResourceItemCount>();

    // Event for when a weapon is equipped
    public event System.Action<WeaponScriptableObj> OnWeaponEquipped;

    public virtual void Start()
    {
        // Get WeaponAttack component if not assigned
        if (weaponAttack == null)
        {
            weaponAttack = GetComponent<WeaponAttack>();
        }
        
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

    public virtual void AddItem(ResourceScriptableObj item, int count = 1)
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

        // Determine if this is player inventory or NPC inventory
        bool isPlayerInventory = this is PlayerInventory;
        PlayerUIManager.Instance.inventoryPopup.ShowInventoryPopup(item, count, isPlayerInventory);
        
    }

    public virtual void AddItem(List<ResourceItemCount> items)
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

    public List<ResourceScriptableObj> GetAllItemsOfCategory(ItemCategory resourceCategory){
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
        
        Debug.Log($"[CharacterInventory] {gameObject.name} equipping weapon: {weaponScriptableObj.objectName}");
        
        // Unequip the currently equipped weapon
        UnequipCurrentWeapon();
        
        equippedWeaponScriptObj = weaponScriptableObj;

        // Configure WeaponAttack component - it will spawn the weapon model with collider setup
        if (weaponAttack != null && weaponHolder != null)
        {
            Debug.Log($"[CharacterInventory] Calling SetWeaponData with holder: {weaponHolder.name}");
            weaponAttack.SetWeaponData(weaponScriptableObj, weaponHolder);
            
            // Store reference to spawned model from WeaponAttack
            spawnedWeaponModel = weaponAttack.SpawnedWeaponModel;
            
            if (spawnedWeaponModel != null)
            {
                Debug.Log($"[CharacterInventory] Weapon successfully equipped! Model: {spawnedWeaponModel.name}");
            }
            else
            {
                Debug.LogWarning($"[CharacterInventory] Weapon spawned but SpawnedWeaponModel is null!");
            }
        }
        else if (weaponAttack == null)
        {
            Debug.LogError($"[CharacterInventory] No WeaponAttack component found on {gameObject.name}! Cannot equip weapon.");
        }
        else if (weaponHolder == null)
        {
            Debug.LogError($"[CharacterInventory] No weaponHolder assigned on {gameObject.name}! Cannot equip weapon.");
        }

        // Handle weapon-specific animation setup
        HandleWeaponType(weaponScriptableObj.animationType);

        // Determine if this is player inventory or NPC inventory
        bool isPlayerInventory = this is PlayerInventory;
        PlayerUIManager.Instance.inventoryPopup.ShowWeaponPopup(weaponScriptableObj, isPlayerInventory);
        
        // Notify listeners that a weapon was equipped
        OnWeaponEquipped?.Invoke(weaponScriptableObj);
    }

    private void UnequipCurrentWeapon()
    {
        if (equippedWeaponScriptObj != null)
        {
            // Destroy spawned model
            if (spawnedWeaponModel != null)
            {
                Destroy(spawnedWeaponModel);
                spawnedWeaponModel = null;
            }
            else if (weaponHolder != null && weaponHolder.childCount > 0)
            {
                Destroy(weaponHolder.GetChild(0).gameObject);
            }
            
            equippedWeaponScriptObj = null;
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

        return true;
    }

    private void HandleWeaponType(WeaponAnimationType animationType)
    {
        // Set up animations based on weapon type
        var controller = GetComponent<HumanCharacterController>();
        if (controller != null)
        {
            controller.EquipMeleeWeapon((int)animationType);
        }
    }
    
    /// <summary>
    /// Called by animation events to trigger weapon use
    /// </summary>
    public void UseWeapon()
    {
        if (weaponAttack != null && equippedWeaponScriptObj != null)
        {
            weaponAttack.OnAttack();
        }
    }
    
    /// <summary>
    /// Called by animation events to stop weapon use
    /// </summary>
    public void StopWeapon()
    {
        if (weaponAttack != null)
        {
            weaponAttack.OnAttackEnd();
        }
    }
    
    /// <summary>
    /// Get current attack speed for animation timing
    /// </summary>
    public float GetCurrentAttackSpeed()
    {
        if (weaponAttack != null)
        {
            return weaponAttack.GetCurrentAttackSpeed();
        }
        return equippedWeaponScriptObj?.attackSpeed ?? 1f;
    }

    #endregion
}
