using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Managers;
using Characters.NPC.Characteristic;
using System.Linq;

[Serializable]
public class NarrativeAssetData
{
    public List<NarrativeInteractiveSaveData> narrativeInteractives = new List<NarrativeInteractiveSaveData>();
}

[Serializable]
public class NarrativeInteractiveSaveData
{
    public string componentInstanceId; // Instance ID of the NarrativeInteractive component
    public string gameObjectName; // For debugging/reference
    public Vector3 position; // Position for identification
    public List<NarrativeAssetFlagSaveData> assetFlags = new List<NarrativeAssetFlagSaveData>();
}

[Serializable]
public class NarrativeAssetFlagSaveData
{
    public string flagName;
    public string value;
    
    public NarrativeAssetFlagSaveData(string name, string val)
    {
        flagName = name;
        value = val;
    }
}

[Serializable]
public class GameData
{
    public GridData gridData = new GridData();
    public ResearchData researchData = new ResearchData();
    public NPCData npcData = new NPCData();
    public PlayerData playerData = new PlayerData();
    public BuildingData buildingData = new BuildingData();
    public ManagerData managerData = new ManagerData();
    public NarrativeAssetData narrativeAssetData = new NarrativeAssetData();
    
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
    
    // Recruited NPCs data
    public List<string> recruitedNPCIds = new List<string>(); // References to NPCScriptableObj names
    public List<string> recruitedNPCComponentIds = new List<string>(); // Component IDs for tracking
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
    [SerializeField] private float autoSaveInterval = 30f; // 30 seconds
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
            SaveNarrativeAssetData(data.narrativeAssetData);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFilePath, json);
            
            Debug.Log($"<color=green>Game Saved Successfully at: {saveFilePath}</color>");
            PlayerUIManager.Instance?.DisplayNotification("Game Saved");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>Failed to save game: {e.Message}</color>");
            PlayerUIManager.Instance?.DisplayUIErrorMessage("Failed to save game!");
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found");
            PlayerUIManager.Instance?.DisplayNotification("No save file found");
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
            LoadNarrativeAssetData(data.narrativeAssetData);

            Debug.Log($"<color=green>Game Loaded Successfully from: {data.saveTime}</color>");
            PlayerUIManager.Instance?.DisplayNotification("Game Loaded");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>Failed to load game: {e.Message}</color>");
            PlayerUIManager.Instance?.DisplayUIErrorMessage("Failed to load game!");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file deleted");
            PlayerUIManager.Instance?.DisplayNotification("Save file deleted");
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

        // Track which buildings we've already saved to avoid duplicates
        HashSet<GameObject> savedBuildings = new HashSet<GameObject>();

        foreach (var kvp in CampManager.Instance.SharedGridSlots)
        {
            var slot = kvp.Value;
            if (slot.IsOccupied && slot.OccupyingObject != null && !savedBuildings.Contains(slot.OccupyingObject))
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

                // Get the scriptable object name from the building/turret component
                string scriptableObjName = "";
                if (slot.OccupyingObject.TryGetComponent<Building>(out var buildingComponent))
                {
                    var buildingScriptable = buildingComponent.GetStructureScriptableObj();
                    scriptableObjName = buildingScriptable?.name ?? slot.OccupyingObject.name.Replace("(Clone)", "").Trim();
                }
                else if (slot.OccupyingObject.TryGetComponent<BaseTurret>(out var turretComponent))
                {
                    var turretScriptable = turretComponent.GetStructureScriptableObj();
                    scriptableObjName = turretScriptable?.name ?? slot.OccupyingObject.name.Replace("(Clone)", "").Trim();
                }
                else
                {
                    // Fallback to GameObject name if no scriptable object found
                    scriptableObjName = slot.OccupyingObject.name.Replace("(Clone)", "").Trim();
                }

                // Save the actual building position instead of grid slot position
                Vector3 buildingPosition = slot.OccupyingObject.transform.position;

                gridData.occupiedSlots.Add(new GridSlotSaveData
                {
                    position = buildingPosition,
                    occupyingObjectName = scriptableObjName,
                    occupyingObjectPrefabPath = GetPrefabPath(slot.OccupyingObject),
                    size = size
                });

                // Mark this building as saved to avoid duplicates
                savedBuildings.Add(slot.OccupyingObject);
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
        if (npcData?.npcs == null || npcData.npcs.Count == 0)
        {
            Debug.Log("[SaveLoadManager] No NPC data to load");
            return;
        }

        foreach (var npcSaveData in npcData.npcs)
        {
            // Find the NPCScriptableObj by name
            NPCScriptableObj npcScriptable = FindNPCScriptableByName(npcSaveData.npcDataObjName);
            if (npcScriptable?.prefab == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Could not find NPCScriptableObj or prefab for: {npcSaveData.npcDataObjName}");
                continue;
            }

            // Instantiate the NPC prefab
            GameObject npcObject = Instantiate(npcScriptable.prefab, npcSaveData.position, Quaternion.identity);
            
            if (npcObject.TryGetComponent<SettlerNPC>(out var settlerNPC))
            {
                // Set initialization context for loaded NPCs
                settlerNPC.SetInitializationContext(NPCInitializationContext.LOADED_FROM_SAVE);
                
                // Restore the NPC's state from save data
                settlerNPC.RestoreFromSaveData(npcSaveData);
                
                Debug.Log($"[SaveLoadManager] Loaded NPC: {npcSaveData.npcDataObjName} at {npcSaveData.position}");
            }
            else
            {
                Debug.LogWarning($"[SaveLoadManager] Spawned NPC '{npcSaveData.npcDataObjName}' does not have SettlerNPC component!");
                Destroy(npcObject);
            }
        }
        
        Debug.Log($"[SaveLoadManager] Finished loading {npcData.npcs.Count} NPCs");
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

        // Save recruited NPCs with component tracking
        var recruitedNPCs = playerInventory.RecruitedNPCs;
        foreach (var npc in recruitedNPCs)
        {
            if (npc != null)
            {
                playerData.recruitedNPCIds.Add(npc.name);
            }
        }

        // Get component IDs (this accesses the private field through reflection or a public getter)
        var componentIds = playerInventory.GetRecruitedNPCComponentIds();
        playerData.recruitedNPCComponentIds.AddRange(componentIds);
    }

    private void LoadPlayerData(PlayerData playerData)
    {
        if (PlayerInventory.Instance == null) return;

        var playerInventory = PlayerInventory.Instance;

        // Clear existing inventory
        playerInventory.ClearInventory();
        playerInventory.ClearAvailableMutations();

        // Load recruited NPCs with component tracking
        if (playerData.recruitedNPCIds != null && playerData.recruitedNPCIds.Count > 0)
        {
            // Clear existing recruited NPCs
            playerInventory.TransferRecruitedNPCsToCamp(); // This clears the lists
            
            // Load recruited NPCs
            for (int i = 0; i < playerData.recruitedNPCIds.Count; i++)
            {
                string npcId = playerData.recruitedNPCIds[i];
                string componentId = i < playerData.recruitedNPCComponentIds.Count ? playerData.recruitedNPCComponentIds[i] : null;
                
                // Find the NPCScriptableObj by name
                NPCScriptableObj npcScriptable = FindNPCScriptableByName(npcId);
                if (npcScriptable != null)
                {
                    playerInventory.RecruitNPC(npcScriptable, componentId);
                    Debug.Log($"[SaveLoadManager] Loaded recruited NPC: {npcId} with component ID: {componentId}");
                }
                else
                {
                    Debug.LogWarning($"[SaveLoadManager] Could not find NPCScriptableObj for: {npcId}");
                }
            }
        }

        // TODO: Implement remaining player data loading
        // You'd need to find resource/mutation objects by ID and restore inventory
        Debug.Log($"[SaveLoadManager] TODO: Load remaining player data - {playerData.inventory.Count} inventory items");
    }

    /// <summary>
    /// Get recruited NPC component IDs from PlayerInventory (helper method)
    /// </summary>
    private List<string> GetRecruitedNPCComponentIds(PlayerInventory playerInventory)
    {
        return playerInventory.GetRecruitedNPCComponentIds();
    }

    /// <summary>
    /// Find an NPCScriptableObj by name (helper method)
    /// </summary>
    private NPCScriptableObj FindNPCScriptableByName(string npcName)
    {
        // TODO: Implement NPC scriptable object lookup
        // You'll need to access your NPC database/collection
        // For now, return null - you can implement this based on your NPC data structure
        Debug.LogWarning($"[SaveLoadManager] TODO: Implement FindNPCScriptableByName for: {npcName}");
        return null;
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

    #region Narrative Asset Data Save/Load

    private void SaveNarrativeAssetData(NarrativeAssetData narrativeAssetData)
    {
        // Find all NarrativeInteractive components in the scene
        var narrativeInteractives = FindObjectsByType<NarrativeInteractive>(FindObjectsSortMode.None);
        
        foreach (var interactive in narrativeInteractives)
        {
            var saveData = new NarrativeInteractiveSaveData
            {
                componentInstanceId = interactive.gameObject.GetInstanceID().ToString(),
                gameObjectName = interactive.gameObject.name,
                position = interactive.transform.position
            };

            // Get the narrative asset and save its flags
            var narrativeAsset = interactive.GetOrCreateNarrativeAsset();
            if (narrativeAsset?.flags != null)
            {
                foreach (var flag in narrativeAsset.flags)
                {
                    saveData.assetFlags.Add(new NarrativeAssetFlagSaveData(flag.flagName, flag.value));
                }
                
                Debug.Log($"[SaveLoadManager] Saved {saveData.assetFlags.Count} flags for {interactive.gameObject.name}");
            }
            else
            {
                Debug.Log($"[SaveLoadManager] No flags to save for {interactive.gameObject.name}");
            }

            narrativeAssetData.narrativeInteractives.Add(saveData);
        }
        
        Debug.Log($"[SaveLoadManager] Saved {narrativeInteractives.Length} NarrativeInteractive components with narrative asset data");
    }

    private void LoadNarrativeAssetData(NarrativeAssetData narrativeAssetData)
    {
        // Find all existing NarrativeInteractive components in the scene
        var existingInteractives = FindObjectsByType<NarrativeInteractive>(FindObjectsSortMode.None);
        var interactiveDict = new Dictionary<string, NarrativeInteractive>();
        
        // Build a lookup dictionary by instance ID
        foreach (var interactive in existingInteractives)
        {
            string instanceId = interactive.gameObject.GetInstanceID().ToString();
            interactiveDict[instanceId] = interactive;
        }
        
        // Load narrative asset data
        foreach (var saveData in narrativeAssetData.narrativeInteractives)
        {
            if (interactiveDict.TryGetValue(saveData.componentInstanceId, out NarrativeInteractive interactive))
            {
                // Clear existing flags
                interactive.GetOrCreateNarrativeAsset().flags?.Clear();
                
                // Restore flags from save data
                if (saveData.assetFlags != null)
                {
                    foreach (var flagData in saveData.assetFlags)
                    {
                        interactive.SetFlag(flagData.flagName, flagData.value);
                    }
                }
                
                Debug.Log($"[SaveLoadManager] Loaded flags for NarrativeInteractive: {saveData.gameObjectName} ({saveData.assetFlags?.Count ?? 0} flags)");
            }
            else
            {
                Debug.LogWarning($"[SaveLoadManager] Could not find NarrativeInteractive with instance ID: {saveData.componentInstanceId} for {saveData.gameObjectName}");
            }
        }
        
        Debug.Log($"[SaveLoadManager] Loaded narrative asset data for {narrativeAssetData.narrativeInteractives.Count} components");
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

        // Use the saved position directly since it's already the building's actual position
        Vector3 buildingPosition = slotData.position;

        // Instantiate the building at the saved position
        GameObject buildingObj = Instantiate(buildingScriptable.prefab, buildingPosition, Quaternion.identity);
        
        // Setup the building component
        Building buildingComponent = buildingObj.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.SetupBuilding(buildingScriptable);
            buildingComponent.CompleteConstruction(); // Mark as completed since it was saved
        }

        // Mark grid slots as occupied using the building position
        if (CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsOccupied(buildingPosition, slotData.size, buildingObj);
        }

        Debug.Log($"Recreated building: {buildingScriptable.name} at {buildingPosition}");
    }

    private void CreateTurretFromSave(TurretScriptableObject turretScriptable, GridSlotSaveData slotData)
    {
        if (turretScriptable.prefab == null)
        {
            Debug.LogError($"Turret prefab is null for {turretScriptable.name}");
            return;
        }

        // Use the saved position directly since it's already the turret's actual position
        Vector3 turretPosition = slotData.position;

        // Instantiate the turret at the saved position
        GameObject turretObj = Instantiate(turretScriptable.prefab, turretPosition, Quaternion.identity);
        
        // Setup the turret component
        BaseTurret turretComponent = turretObj.GetComponent<BaseTurret>();
        if (turretComponent != null)
        {
            turretComponent.SetupStructure(turretScriptable);
            turretComponent.CompleteConstruction(); // Mark as completed since it was saved
        }

        // Mark grid slots as occupied using the turret position
        if (CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsOccupied(turretPosition, slotData.size, turretObj);
        }

        Debug.Log($"Recreated turret: {turretScriptable.name} at {turretPosition}");
    }

    #endregion
}
