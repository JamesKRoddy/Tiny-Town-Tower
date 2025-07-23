using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Managers;
using Characters.NPC.Characteristic;
using System.Linq;

[Serializable]
public class GameData
{
    public GridData gridData = new GridData();
    public ResearchData researchData = new ResearchData();
    public NPCData npcData = new NPCData();
    public PlayerData playerData = new PlayerData();
    public BuildingData buildingData = new BuildingData();
    public ManagerData managerData = new ManagerData();
    
    // Metadata
    public string saveVersion = "1.0";
    public DateTime saveTime;
    public string sceneName;
}

[Serializable]
public class GridData
{
    public List<GridSlotSaveData> occupiedSlots = new List<GridSlotSaveData>();
}

[Serializable]
public class GridSlotSaveData
{
    public Vector3 position;
    public string occupyingObjectName; // Name/ID of the placed object
    public string occupyingObjectPrefabPath; // Path to the prefab for recreation
    public Vector2Int size; // Size of the object for grid management
}

[Serializable]
public class ResearchData
{
    public List<string> completedResearchIds = new List<string>();
    public List<string> availableResearchIds = new List<string>();
    public List<string> currentlyResearchingIds = new List<string>();
    public List<string> unlockedItemIds = new List<string>();
}

[Serializable]
public class NPCData
{
    public List<NPCSaveData> npcs = new List<NPCSaveData>();
}

[Serializable]
public class NPCSaveData
{
    public string npcId; // Unique identifier
    public Vector3 position;
    public float health;
    public float maxHealth;
    public float stamina;
    public float hunger;
    public int additionalMutationSlots;
    public string currentTaskType;
    public List<string> equippedCharacteristicIds = new List<string>();
    public List<ResourceItemData> inventory = new List<ResourceItemData>();
    public WeaponData equippedWeapon;
    public string npcDataObjName; // Reference to SettlerNPCScriptableObj
}

[Serializable]
public class PlayerData
{
    public List<ResourceItemData> inventory = new List<ResourceItemData>();
    public List<string> equippedMutationIds = new List<string>();
    public List<MutationQuantityData> availableMutations = new List<MutationQuantityData>();
    public int maxMutationSlots;
    public WeaponData equippedWeapon;
    public string dashElement;
}

[Serializable]
public class BuildingData
{
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
}

[Serializable]
public class BuildingSaveData
{
    public string buildingId; // Unique identifier
    public Vector3 position;
    public float health;
    public bool isOperational;
    public bool isUnderConstruction;
    public string buildingScriptableObjName;
    public float constructionProgress; // For buildings under construction
}

[Serializable]
public class ManagerData
{
    public CookingManagerData cookingData = new CookingManagerData();
    public ResourceUpgradeManagerData upgradeData = new ResourceUpgradeManagerData();
    public CleanlinessManagerData cleanlinessData = new CleanlinessManagerData();
}

[Serializable]
public class CookingManagerData
{
    public List<string> unlockedRecipeIds = new List<string>();
    public List<string> availableRecipeIds = new List<string>();
}

[Serializable]
public class ResourceUpgradeManagerData
{
    public List<string> unlockedUpgradeIds = new List<string>();
    public List<string> availableUpgradeIds = new List<string>();
}

[Serializable]
public class CleanlinessManagerData
{
    public float currentCleanliness;
    public List<Vector3> dirtPilePositions = new List<Vector3>();
}

[Serializable]
public class ResourceItemData
{
    public string resourceId;
    public int count;
    
    public ResourceItemData(string id, int amount)
    {
        resourceId = id;
        count = amount;
    }
}

[Serializable]
public class WeaponData
{
    public string weaponId;
    public string weaponPrefabPath;
    
    public WeaponData(string id, string path)
    {
        weaponId = id;
        weaponPrefabPath = path;
    }
}

[Serializable]
public class MutationQuantityData
{
    public string mutationId;
    public int quantity;
    
    public MutationQuantityData(string id, int amount)
    {
        mutationId = id;
        quantity = amount;
    }
}

public class SaveLoadManager : MonoBehaviour
{
    private static SaveLoadManager _instance;
    public static SaveLoadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SaveLoadManager>();
                if (_instance == null)
                {
                    Debug.LogError("SaveLoadManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private string saveFilePath;
    
    [Header("Save Settings")]
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    private float lastAutoSaveTime;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    private void Update()
    {
        if (autoSaveEnabled && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            SaveGame();
            lastAutoSaveTime = Time.time;
        }
    }

    public void SaveGame()
    {
        try
        {
            GameData data = new GameData
            {
                saveTime = DateTime.Now,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            };

            // Save all game systems
            SaveGridData(data.gridData);
            SaveResearchData(data.researchData);
            SaveNPCData(data.npcData);
            SavePlayerData(data.playerData);
            SaveBuildingData(data.buildingData);
            SaveManagerData(data.managerData);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFilePath, json);
            
            Debug.Log($"Game Saved Successfully at: {saveFilePath}");
            PlayerUIManager.Instance?.DisplayTextPopup("Game Saved");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
            PlayerUIManager.Instance?.DisplayUIErrorMessage("Failed to save game!");
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found");
            PlayerUIManager.Instance?.DisplayTextPopup("No save file found");
            return;
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            GameData data = JsonUtility.FromJson<GameData>(json);

            // Load all game systems
            LoadGridData(data.gridData);
            LoadResearchData(data.researchData);
            LoadNPCData(data.npcData);
            LoadPlayerData(data.playerData);
            LoadBuildingData(data.buildingData);
            LoadManagerData(data.managerData);

            Debug.Log($"Game Loaded Successfully from: {data.saveTime}");
            PlayerUIManager.Instance?.DisplayTextPopup("Game Loaded");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            PlayerUIManager.Instance?.DisplayUIErrorMessage("Failed to load game!");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file deleted");
            PlayerUIManager.Instance?.DisplayTextPopup("Save file deleted");
        }
        else
        {
            Debug.LogWarning("No save file to delete");
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(saveFilePath);
    }

    #region Grid Data Save/Load

    private void SaveGridData(GridData gridData)
    {
        if (CampManager.Instance?.SharedGridSlots == null) return;

        foreach (var kvp in CampManager.Instance.SharedGridSlots)
        {
            var slot = kvp.Value;
            if (slot.IsOccupied && slot.OccupyingObject != null)
            {
                // Get the building component to determine size
                Vector2Int size = Vector2Int.one;
                if (slot.OccupyingObject.TryGetComponent<IPlaceableStructure>(out var structure))
                {
                    // Try to get size from building scriptable object
                                         if (slot.OccupyingObject.TryGetComponent<Building>(out var building) && building.GetStructureScriptableObj() != null)
                     {
                         size = building.GetStructureScriptableObj().size;
                    }
                }

                gridData.occupiedSlots.Add(new GridSlotSaveData
                {
                    position = kvp.Key,
                    occupyingObjectName = slot.OccupyingObject.name.Replace("(Clone)", "").Trim(),
                    occupyingObjectPrefabPath = GetPrefabPath(slot.OccupyingObject),
                    size = size
                });
            }
        }
    }

    private void LoadGridData(GridData gridData)
    {
        if (CampManager.Instance == null) return;

        // Clear existing grid state
        CampManager.Instance.ResetSharedGridObjects();

        // Recreate buildings from save data
        foreach (var slotData in gridData.occupiedSlots)
        {
            // Find the appropriate scriptable object and recreate the building
            // This would need to reference your building/turret databases
            RecreateGridObject(slotData);
        }
    }

    #endregion

    #region Research Data Save/Load

    private void SaveResearchData(ResearchData researchData)
    {
        if (CampManager.Instance?.ResearchManager == null) return;

        var researchManager = CampManager.Instance.ResearchManager;
        
        // Save completed research
        foreach (var research in researchManager.GetCompletedResearch())
        {
            researchData.completedResearchIds.Add(research.name);
        }

        // Save available research
        foreach (var research in researchManager.GetAvailableResearch())
        {
            researchData.availableResearchIds.Add(research.name);
        }

        // Save unlocked items
        foreach (var item in researchManager.GetUnlockedItems())
        {
            researchData.unlockedItemIds.Add(item.name);
        }
    }

    private void LoadResearchData(ResearchData researchData)
    {
        if (CampManager.Instance?.ResearchManager == null) return;

        var researchManager = CampManager.Instance.ResearchManager;
        
        // TODO: Implement research loading
        // You'd need to find research objects by ID and restore their states
        Debug.Log($"TODO: Load research data - {researchData.completedResearchIds.Count} completed research items");
    }

    #endregion

    #region NPC Data Save/Load

    private void SaveNPCData(NPCData npcData)
    {
        var npcs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);
        
        foreach (var npc in npcs)
        {
            var npcSaveData = new NPCSaveData
            {
                npcId = npc.gameObject.GetInstanceID().ToString(), // Use instance ID as unique identifier
                position = npc.transform.position,
                health = npc.Health,
                maxHealth = npc.MaxHealth,
                stamina = npc.currentStamina,
                                 hunger = npc.GetHungerPercentage() * 100f, // Convert percentage back to value
                additionalMutationSlots = npc.additionalMutationSlots,
                currentTaskType = npc.GetCurrentTaskType().ToString(),
                npcDataObjName = npc.nPCDataObj?.name
            };

            // Save equipped characteristics
            if (npc.characteristicSystem?.EquippedCharacteristics != null)
            {
                foreach (var characteristic in npc.characteristicSystem.EquippedCharacteristics)
                {
                    npcSaveData.equippedCharacteristicIds.Add(characteristic.name);
                }
            }

            // Save NPC inventory
            var inventory = npc.GetComponent<CharacterInventory>();
            if (inventory != null)
            {
                foreach (var item in inventory.GetFullInventory())
                {
                    npcSaveData.inventory.Add(new ResourceItemData(item.resourceScriptableObj.name, item.count));
                }

                // Save equipped weapon
                if (inventory.equippedWeaponScriptObj != null)
                {
                    npcSaveData.equippedWeapon = new WeaponData(
                        inventory.equippedWeaponScriptObj.name,
                        GetPrefabPath(inventory.equippedWeaponScriptObj.prefab)
                    );
                }
            }

            npcData.npcs.Add(npcSaveData);
        }
    }

    private void LoadNPCData(NPCData npcData)
    {
        // TODO: Implement NPC loading
        // This would involve finding existing NPCs and restoring their states
        // or spawning new NPCs if they don't exist
        Debug.Log($"TODO: Load NPC data for {npcData.npcs.Count} NPCs");
    }

    #endregion

    #region Player Data Save/Load

    private void SavePlayerData(PlayerData playerData)
    {
        if (PlayerInventory.Instance == null) return;

        var playerInventory = PlayerInventory.Instance;

        // Save player inventory
        foreach (var item in playerInventory.GetFullInventory())
        {
            playerData.inventory.Add(new ResourceItemData(item.resourceScriptableObj.name, item.count));
        }

        // Save equipped mutations
        foreach (var mutation in playerInventory.EquippedMutations)
        {
            playerData.equippedMutationIds.Add(mutation.name);
        }

        // Save available mutations
        foreach (var mutation in playerInventory.availableMutations)
        {
            playerData.availableMutations.Add(new MutationQuantityData(mutation.mutation.name, mutation.quantity));
        }

        playerData.maxMutationSlots = playerInventory.MaxMutationSlots;
        playerData.dashElement = playerInventory.dashElement.ToString();

        // Save equipped weapon
        if (playerInventory.equippedWeaponScriptObj != null)
        {
            playerData.equippedWeapon = new WeaponData(
                playerInventory.equippedWeaponScriptObj.name,
                GetPrefabPath(playerInventory.equippedWeaponScriptObj.prefab)
            );
        }
    }

    private void LoadPlayerData(PlayerData playerData)
    {
        if (PlayerInventory.Instance == null) return;

        var playerInventory = PlayerInventory.Instance;

        // Clear existing inventory
        playerInventory.ClearInventory();
        playerInventory.ClearAvailableMutations();

        // TODO: Implement player data loading
        // You'd need to find resource/mutation objects by ID and restore inventory
        Debug.Log($"TODO: Load player data - {playerData.inventory.Count} inventory items");
    }

    #endregion

    #region Building Data Save/Load

    private void SaveBuildingData(BuildingData buildingData)
    {
        var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
        
        foreach (var building in buildings)
        {
            buildingData.buildings.Add(new BuildingSaveData
            {
                buildingId = building.gameObject.GetInstanceID().ToString(),
                position = building.transform.position,
                health = building.Health,
                isOperational = building.IsOperational(),
                                 isUnderConstruction = building.IsUnderConstruction(),
                 buildingScriptableObjName = building.GetStructureScriptableObj()?.name
            });
        }
    }

    private void LoadBuildingData(BuildingData buildingData)
    {
        // TODO: Implement building data loading
        Debug.Log($"TODO: Load building data for {buildingData.buildings.Count} buildings");
    }

    #endregion

    #region Manager Data Save/Load

    private void SaveManagerData(ManagerData managerData)
    {
        // Save cooking manager data
        if (CampManager.Instance?.CookingManager != null)
        {
            var cookingManager = CampManager.Instance.CookingManager;
            foreach (var recipe in cookingManager.GetUnlockedRecipes())
            {
                managerData.cookingData.unlockedRecipeIds.Add(recipe.name);
            }
        }

        // Save resource upgrade manager data
        if (CampManager.Instance?.ResourceUpgradeManager != null)
        {
            var upgradeManager = CampManager.Instance.ResourceUpgradeManager;
            foreach (var upgrade in upgradeManager.GetUnlockedUpgrades())
            {
                managerData.upgradeData.unlockedUpgradeIds.Add(upgrade.name);
            }
        }

        // Save cleanliness manager data
        if (CampManager.Instance?.CleanlinessManager != null)
        {
            // TODO: Add cleanliness data when the manager exposes the necessary properties
        }
    }

    private void LoadManagerData(ManagerData managerData)
    {
        // TODO: Implement manager data loading
        Debug.Log("TODO: Load manager data");
    }

    #endregion

    #region Utility Methods

    private string GetPrefabPath(GameObject prefab)
    {
        // This would need to be implemented to get the asset path of a prefab
        // For now, return the name as a placeholder
        return prefab?.name ?? "";
    }

    /// <summary>
    /// Get a building scriptable object by name from the BuildManager database
    /// </summary>
    private BuildingScriptableObj GetBuildingScriptableByName(string name)
    {
        if (CampManager.Instance?.BuildManager?.buildingScriptableObjs == null) return null;

        foreach (var building in CampManager.Instance.BuildManager.buildingScriptableObjs)
        {
            if (building.name == name || building.objectName == name)
            {
                return building;
            }
        }
        return null;
    }

    /// <summary>
    /// Get a turret scriptable object by name from the BuildManager database
    /// </summary>
    private TurretScriptableObject GetTurretScriptableByName(string name)
    {
        if (CampManager.Instance?.BuildManager?.turretScriptableObjs == null) return null;

        foreach (var turret in CampManager.Instance.BuildManager.turretScriptableObjs)
        {
            if (turret.name == name || turret.objectName == name)
            {
                return turret;
            }
        }
        return null;
    }

    /// <summary>
    /// Get a resource scriptable object by name
    /// </summary>
    private ResourceScriptableObj GetResourceScriptableByName(string name)
    {
        // This would need to access a resource database
        // For now, we'll need to search through the existing inventories or a central database
        
        // Try to find from player inventory first
        if (PlayerInventory.Instance != null)
        {
            foreach (var item in PlayerInventory.Instance.GetFullInventory())
            {
                if (item.resourceScriptableObj.name == name)
                {
                    return item.resourceScriptableObj;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Get a research scriptable object by name
    /// </summary>
    private ResearchScriptableObj GetResearchScriptableByName(string name)
    {
        if (CampManager.Instance?.ResearchManager == null) return null;

        foreach (var research in CampManager.Instance.ResearchManager.GetAllResearch())
        {
            if (research.name == name)
            {
                return research;
            }
        }
        return null;
    }

    /// <summary>
    /// Force save the game (useful for debugging or manual saves)
    /// </summary>
    public void ForceSave()
    {
        lastAutoSaveTime = 0f; // Reset auto-save timer
        SaveGame();
    }

    /// <summary>
    /// Enable or disable auto-save
    /// </summary>
    public void SetAutoSave(bool enabled)
    {
        autoSaveEnabled = enabled;
        Debug.Log($"Auto-save {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Get time until next auto-save
    /// </summary>
    public float GetTimeUntilAutoSave()
    {
        if (!autoSaveEnabled) return -1f;
        return autoSaveInterval - (Time.time - lastAutoSaveTime);
    }

    #endregion

    #region Improved Grid Recreation

    private void RecreateGridObject(GridSlotSaveData slotData)
    {
        try
        {
            // First try to find a building scriptable object
            BuildingScriptableObj buildingScriptable = GetBuildingScriptableByName(slotData.occupyingObjectName);
            if (buildingScriptable != null)
            {
                CreateBuildingFromSave(buildingScriptable, slotData);
                return;
            }

            // Try to find a turret scriptable object
            TurretScriptableObject turretScriptable = GetTurretScriptableByName(slotData.occupyingObjectName);
            if (turretScriptable != null)
            {
                CreateTurretFromSave(turretScriptable, slotData);
                return;
            }

            Debug.LogWarning($"Could not find scriptable object for: {slotData.occupyingObjectName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to recreate grid object {slotData.occupyingObjectName}: {e.Message}");
        }
    }

    private void CreateBuildingFromSave(BuildingScriptableObj buildingScriptable, GridSlotSaveData slotData)
    {
        if (buildingScriptable.prefab == null)
        {
            Debug.LogError($"Building prefab is null for {buildingScriptable.name}");
            return;
        }

        // Instantiate the building
        GameObject buildingObj = Instantiate(buildingScriptable.prefab, slotData.position, Quaternion.identity);
        
        // Setup the building component
        Building buildingComponent = buildingObj.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.SetupBuilding(buildingScriptable);
            buildingComponent.CompleteConstruction(); // Mark as completed since it was saved
        }

        // Mark grid slots as occupied
        if (CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsOccupied(slotData.position, slotData.size, buildingObj);
        }

        Debug.Log($"Recreated building: {buildingScriptable.name} at {slotData.position}");
    }

    private void CreateTurretFromSave(TurretScriptableObject turretScriptable, GridSlotSaveData slotData)
    {
        if (turretScriptable.prefab == null)
        {
            Debug.LogError($"Turret prefab is null for {turretScriptable.name}");
            return;
        }

        // Instantiate the turret
        GameObject turretObj = Instantiate(turretScriptable.prefab, slotData.position, Quaternion.identity);
        
        // Setup the turret component
        BaseTurret turretComponent = turretObj.GetComponent<BaseTurret>();
        if (turretComponent != null)
        {
            turretComponent.SetupStructure(turretScriptable);
            turretComponent.CompleteConstruction(); // Mark as completed since it was saved
        }

        // Mark grid slots as occupied
        if (CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsOccupied(slotData.position, slotData.size, turretObj);
        }

        Debug.Log($"Recreated turret: {turretScriptable.name} at {slotData.position}");
    }

    #endregion
}
