using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Managers;
using Characters.NPC.Characteristic;
using System.Linq;

/// <summary>
/// Serializable structure for narrative flags (Unity doesn't serialize Dictionaries)
/// </summary>
[Serializable]
public class NarrativeFlagData
{
    public string flagName;
    public string flagValue;
    
    public NarrativeFlagData(string name, string value)
    {
        flagName = name;
        flagValue = value;
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
    public string uuid; // Universal unique identifier
    public Vector3 position;
    public string occupyingObjectName; // Name/ID of the placed object
    public string occupyingObjectPrefabPath; // Path to the prefab for recreation
    public Vector2Int size; // Size of the object for grid management
    
    // Consolidated narrative data (serializable format)
    public List<NarrativeFlagData> narrativeFlags = new List<NarrativeFlagData>();
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
    public string uuid; // Universal unique identifier
    public string npcId; // Legacy identifier (for compatibility)
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
    public string npcDataObjName; // Reference to NPCScriptableObj (for unique NPCs)
    public NPCAppearanceData appearanceData; // NPC appearance information
    
    // Procedural settler data (for NPCs without NPCScriptableObj)
    public string settlerName;
    public int settlerAge;
    public string settlerDescription;
    public bool isProceduralSettler; // Flag to distinguish between unique NPCs and procedural settlers
    
    // Consolidated narrative data (serializable format)
    public List<NarrativeFlagData> narrativeFlags = new List<NarrativeFlagData>();
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
    
    // Recruited NPCs data with full information
    public List<RecruitedNPCSaveData> recruitedNPCs = new List<RecruitedNPCSaveData>();
}

[Serializable]
public class RecruitedNPCSaveData
{
    public string componentId;
    public string settlerName;
    public int settlerAge;
    public string settlerDescription;
    public NPCAppearanceData appearanceData;
    
    public RecruitedNPCSaveData(string id, string name, int age, string description, NPCAppearanceData appearance)
    {
        this.componentId = id;
        this.settlerName = name;
        this.settlerAge = age;
        this.settlerDescription = description;
        this.appearanceData = appearance;
    }
}

[Serializable]
public class BuildingData
{
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
}

[Serializable]
public class BuildingSaveData
{
    public string uuid; // Universal unique identifier
    public string buildingId; // Legacy identifier (for compatibility)
    public Vector3 position;
    public float health;
    public bool isOperational;
    public bool isUnderConstruction;
    public string buildingScriptableObjName;
    public float constructionProgress; // For buildings under construction
    
    // Consolidated narrative data (serializable format)
    public List<NarrativeFlagData> narrativeFlags = new List<NarrativeFlagData>();
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

    #region Load/Save Game Public Methods

    /// <summary>
    /// Loads the game from the save file.
    /// </summary>
    /// <param name="currentGameMode">The game mode to load the game for. If not provided, it will be determined from the current scene.</param>
    public void LoadGame(GameMode currentGameMode = GameMode.NONE)
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

            if(currentGameMode == GameMode.NONE){
                // Get current game mode to determine what to load
                currentGameMode = GetCurrentGameMode();
            }

            // Load only the data relevant to the current game mode
            LoadGameModeSpecificData(data, currentGameMode);

            Debug.Log($"<color=green>Game Loaded Successfully for {currentGameMode} from: {data.saveTime}</color>");
            PlayerUIManager.Instance?.DisplayNotification("Game Loaded");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>Failed to load game: {e.Message}</color>");
            PlayerUIManager.Instance?.DisplayUIErrorMessage("Failed to load game!");
        }
    }

    /// <summary>
    /// Saves the game to the save file.
    /// </summary>
    /// <param name="currentGameMode">The game mode to save the game for. If not provided, it will be determined from the current scene.</param>
    public void SaveGame(GameMode currentGameMode = GameMode.NONE)
    {
        try
        {
            // Load existing save data first to preserve other scenes' data
            GameData data = LoadExistingGameData();
            
            // Update metadata
            data.saveTime = DateTime.Now;
            data.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if(currentGameMode == GameMode.NONE){
                // Get current game mode to determine what to save
                currentGameMode = GetCurrentGameMode();
            }
            
            // Save only the data relevant to the current game mode
            SaveGameModeSpecificData(data, currentGameMode);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFilePath, json);
            
            Debug.Log($"<color=green>Game Saved Successfully for {currentGameMode} at: {saveFilePath}</color>");
            PlayerUIManager.Instance?.DisplayNotification("Game Saved");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>Failed to save game: {e.Message}</color>");
            PlayerUIManager.Instance?.DisplayUIErrorMessage("Failed to save game!");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("<color=green>Save file deleted</color>");
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

    #endregion

    #region Grid Data Save/Load

    private void SaveGridData(GridData gridData)
    {
        if (CampManager.Instance?.SharedGridSlots == null) return;

        // Clear existing grid data to prevent accumulation
        gridData.occupiedSlots.Clear();

        // Track which UUIDs we've already saved to avoid duplicates
        HashSet<string> savedUuids = new HashSet<string>();

        foreach (var kvp in CampManager.Instance.SharedGridSlots)
        {
            var slot = kvp.Value;
            if (slot.IsOccupied && slot.OccupyingObject != null)
            {
                // Get or add SaveableObject component for UUID management
                SaveableObject saveableObj = slot.OccupyingObject.GetComponent<SaveableObject>();
                if (saveableObj == null)
                {
                    saveableObj = slot.OccupyingObject.gameObject.AddComponent<SaveableObject>();
                    Debug.Log($"[SaveLoadManager] Added SaveableObject component to {slot.OccupyingObject.name}");
                }

                // Skip if we've already saved this UUID (prevent duplicates)
                if (savedUuids.Contains(saveableObj.UUID))
                {
                    Debug.LogWarning($"[SaveLoadManager] Skipping duplicate building/turret UUID: {slot.OccupyingObject.name} with UUID {saveableObj.UUID}");
                    continue;
                }

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

                var slotSaveData = new GridSlotSaveData
                {
                    uuid = saveableObj.UUID, // Use persistent UUID
                    position = buildingPosition,
                    occupyingObjectName = scriptableObjName,
                    occupyingObjectPrefabPath = GetPrefabPath(slot.OccupyingObject),
                    size = size,
                    narrativeFlags = saveableObj.GetAllNarrativeFlagsSerializable() // Save consolidated narrative data
                };

                gridData.occupiedSlots.Add(slotSaveData);

                // Track this UUID as saved
                savedUuids.Add(saveableObj.UUID);

                // Debug logging for narrative data
                if (slotSaveData.narrativeFlags.Count > 0)
                {
                    Debug.Log($"[SaveLoadManager] Saved {slotSaveData.narrativeFlags.Count} narrative flags for {scriptableObjName} (UUID: {saveableObj.UUID})");
                }
            }
        }

        Debug.Log($"[SaveLoadManager] Saved {gridData.occupiedSlots.Count} grid objects with UUIDs");
    }

    private void LoadGridData(GridData gridData)
    {
        if (CampManager.Instance == null) return;

        // Clear existing grid state
        CampManager.Instance.ResetSharedGridObjects();

        int loadedObjects = 0;
        int totalNarrativeFlags = 0;

        // Recreate buildings from save data with UUID support
        foreach (var slotData in gridData.occupiedSlots)
        {
            // Find the appropriate scriptable object and recreate the building/turret
            RecreateGridObject(slotData);
            loadedObjects++;
            
            if (slotData.narrativeFlags != null)
            {
                totalNarrativeFlags += slotData.narrativeFlags.Count;
            }
        }

        Debug.Log($"[SaveLoadManager] Loaded {loadedObjects} grid objects with {totalNarrativeFlags} total narrative flags using UUID system");
    }

    #endregion

    #region Research Data Save/Load

    private void SaveResearchData(ResearchData researchData)
    {
        if (CampManager.Instance?.ResearchManager == null) return;

        var researchManager = CampManager.Instance.ResearchManager;
        
        // Clear existing data to prevent duplicates
        researchData.completedResearchIds.Clear();
        researchData.availableResearchIds.Clear();
        researchData.currentlyResearchingIds.Clear();
        researchData.unlockedItemIds.Clear();
        
        // Use HashSet to prevent duplicates during collection
        var completedSet = new HashSet<string>();
        var availableSet = new HashSet<string>();
        var unlockedSet = new HashSet<string>();
        
        // Save completed research
        foreach (var research in researchManager.GetCompletedResearch())
        {
            completedSet.Add(research.name);
        }

        // Save available research
        foreach (var research in researchManager.GetAvailableResearch())
        {
            availableSet.Add(research.name);
        }

        // Save unlocked items
        foreach (var item in researchManager.GetUnlockedItems())
        {
            unlockedSet.Add(item.name);
        }
        
        // Convert sets back to lists
        researchData.completedResearchIds.AddRange(completedSet);
        researchData.availableResearchIds.AddRange(availableSet);
        researchData.unlockedItemIds.AddRange(unlockedSet);
        
    }

    private void LoadResearchData(ResearchData researchData)
    {
        if (CampManager.Instance?.ResearchManager == null) return;

        var researchManager = CampManager.Instance.ResearchManager;
        
        // TODO: Implement research loading
        // You'd need to find research objects by ID and restore their states
        Debug.LogWarning($"TODO: Load research data - {researchData.completedResearchIds.Count} completed research items");
    }

    #endregion

    #region NPC Data Save/Load

    private void SaveNPCData(NPCData npcData)
    {
        // Clear existing NPC data to prevent accumulation
        npcData.npcs.Clear();
        
        var npcs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);
        var savedUuids = new HashSet<string>(); // Track saved NPCs by UUID to prevent duplicates
        
        foreach (var npc in npcs)
        {
            // Skip test NPCs and invalid NPCs
            if (IsTestOrInvalidNPC(npc))
            {
                continue;
            }
            
            // Get or add SaveableObject component for UUID management
            SaveableObject saveableObj = npc.GetComponent<SaveableObject>();
            if (saveableObj == null)
            {
                saveableObj = npc.gameObject.AddComponent<SaveableObject>();
                Debug.Log($"[SaveLoadManager] Added SaveableObject component to {npc.name}");
            }
            
            // Skip if we've already saved this NPC UUID (prevent duplicates)
            if (savedUuids.Contains(saveableObj.UUID))
            {
                Debug.LogWarning($"[SaveLoadManager] Skipping duplicate NPC UUID: {npc.GetSettlerName()} with UUID {saveableObj.UUID}");
                continue;
            }
            
            var npcSaveData = new NPCSaveData
            {
                uuid = saveableObj.UUID, // Use persistent UUID
                npcId = npc.gameObject.GetInstanceID().ToString(), // Keep legacy ID for compatibility
                position = npc.transform.position,
                health = npc.Health,
                maxHealth = npc.MaxHealth,
                stamina = npc.currentStamina,
                hunger = npc.GetHungerPercentage() * 100f, // Convert percentage back to value
                additionalMutationSlots = npc.additionalMutationSlots,
                currentTaskType = npc.GetCurrentTaskType().ToString(),
                npcDataObjName = null // SettlerNPC no longer uses NPCScriptableObj
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

            // Save appearance data
            if (npc.appearanceSystem != null)
            {
                npcSaveData.appearanceData = npc.appearanceSystem.GetCurrentAppearanceData();
            }
            else
            {
                Debug.LogWarning($"[SaveLoadManager] No appearance system found for {npc.name}");
            }

            // Save settler data (all SettlerNPCs are now procedural)
            npcSaveData.isProceduralSettler = true;
            npcSaveData.settlerName = npc.GetSettlerName();
            npcSaveData.settlerAge = npc.GetSettlerAge();
            npcSaveData.settlerDescription = npc.GetSettlerDescription();
            
            // Save consolidated narrative data in serializable format
            npcSaveData.narrativeFlags = saveableObj.GetAllNarrativeFlagsSerializable();
            
            // Add debug logging to help track recruited NPC issues
            if (npcSaveData.settlerName == "Unknown Settler" || npcSaveData.settlerAge == 0)
            {
                Debug.LogWarning($"[SaveLoadManager] NPC {npc.name} has default settler data - Name: '{npcSaveData.settlerName}', Age: {npcSaveData.settlerAge}. This might indicate a data application issue.");
            }
            
            // Debug logging for narrative data
            if (npcSaveData.narrativeFlags.Count > 0)
            {
                Debug.Log($"[SaveLoadManager] Saved {npcSaveData.narrativeFlags.Count} narrative flags for {npc.GetSettlerName()} (UUID: {saveableObj.UUID})");
                foreach (var flag in npcSaveData.narrativeFlags)
                {
                    Debug.Log($"  - {flag.flagName}: {flag.flagValue}");
                }
            }
            else
            {
                Debug.Log($"[SaveLoadManager] No narrative flags found for {npc.GetSettlerName()} (UUID: {saveableObj.UUID})");
            }
            
            npcData.npcs.Add(npcSaveData);
            savedUuids.Add(saveableObj.UUID); // Track this NPC UUID as saved
        }
        
    }

    private void LoadNPCData(NPCData npcData)
    {
        if (npcData?.npcs == null || npcData.npcs.Count == 0)
        {
            return;
        }

        foreach (var npcSaveData in npcData.npcs)
        {
            // Validate NPC save data before loading
            if (!IsValidNPCSaveData(npcSaveData))
            {
                Debug.LogWarning($"[SaveLoadManager] Skipping invalid NPC data: Name='{npcSaveData.settlerName}', Age={npcSaveData.settlerAge}");
                continue;
            }
            
            GameObject npcPrefab = null;
            
            // All saved NPCs are now procedural settlers, use NPCManager's settler prefab
            if (NPCManager.Instance != null && NPCManager.Instance.IsSettlerGenerationConfigured())
            {
                npcPrefab = NPCManager.Instance.GetSettlerPrefab();
            }
            else
            {
                Debug.LogError($"[SaveLoadManager] NPCManager not configured for settler generation. Cannot load settler: {npcSaveData.settlerName}");
                continue;
            }

            // Instantiate the NPC prefab
            GameObject npcObject = Instantiate(npcPrefab, npcSaveData.position, Quaternion.identity);
            
            // Get or add SaveableObject component and restore UUID
            SaveableObject saveableObj = npcObject.GetComponent<SaveableObject>();
            if (saveableObj == null)
            {
                saveableObj = npcObject.AddComponent<SaveableObject>();
            }
            
            // Restore the UUID from save data
            if (!string.IsNullOrEmpty(npcSaveData.uuid))
            {
                saveableObj.SetUUID(npcSaveData.uuid);
            }
            
            // Restore narrative data from serializable format
            if (npcSaveData.narrativeFlags != null)
            {
                saveableObj.SetAllNarrativeFlagsFromSerializable(npcSaveData.narrativeFlags);
                if (npcSaveData.narrativeFlags.Count > 0)
                {
                    Debug.Log($"[SaveLoadManager] Restored {npcSaveData.narrativeFlags.Count} narrative flags for {npcSaveData.settlerName} (UUID: {saveableObj.UUID})");
                }
            }
            
            if (npcObject.TryGetComponent<SettlerNPC>(out var settlerNPC))
            {
                // Restore the saved settler data (all NPCs are now procedural)
                var savedSettlerData = new Managers.SettlerData(
                    npcSaveData.settlerName,
                    npcSaveData.settlerAge,
                    npcSaveData.settlerDescription ?? "A mysterious settler."
                );
                
                // Add debug logging for recruited NPCs
                if (savedSettlerData.name == "Unknown Settler" || savedSettlerData.age == 0)
                {
                    Debug.LogWarning($"[SaveLoadManager] Loading NPC with default settler data - this might be a recruited NPC that had data issues: Name='{savedSettlerData.name}', Age={savedSettlerData.age}");
                }
                
                settlerNPC.ApplySettlerData(savedSettlerData);
                
                // Set initialization context for loaded NPCs
                settlerNPC.SetInitializationContext(NPCInitializationContext.LOADED_FROM_SAVE);
                
                // Restore the NPC's state from save data
                settlerNPC.RestoreFromSaveData(npcSaveData);
                
            }
            else
            {
                Debug.LogWarning($"[SaveLoadManager] Spawned NPC prefab does not have SettlerNPC component!");
                Destroy(npcObject);
            }
        }
        
    }

    /// <summary>
    /// Validates NPC save data to prevent loading corrupted or invalid NPCs
    /// </summary>
    private bool IsValidNPCSaveData(NPCSaveData npcSaveData)
    {
        // Check for critical corruption only - be more lenient with settler data
        
        // Check for invalid health values (critical)
        if (npcSaveData.maxHealth <= 0)
        {
            Debug.LogWarning($"[SaveLoadManager] Invalid max health: {npcSaveData.maxHealth} for NPC: {npcSaveData.settlerName}");
            return false;
        }
        
        // Check for invalid position (not NaN or infinity)
        if (float.IsNaN(npcSaveData.position.x) || float.IsNaN(npcSaveData.position.y) || float.IsNaN(npcSaveData.position.z) ||
            float.IsInfinity(npcSaveData.position.x) || float.IsInfinity(npcSaveData.position.y) || float.IsInfinity(npcSaveData.position.z))
        {
            Debug.LogWarning($"[SaveLoadManager] Invalid position for NPC: {npcSaveData.settlerName}");
            return false;
        }
        
        // Allow NPCs with "Unknown Settler" names and age 0 - they might be recruited NPCs
        // that had data application issues but should still be loaded
        return true;
    }

    /// <summary>
    /// Checks if an NPC is a test NPC or has invalid data that should not be saved
    /// </summary>
    private bool IsTestOrInvalidNPC(SettlerNPC npc)
    {
        if (npc == null) return true;
        
        // Check if it's a test NPC by name (be more specific to avoid false positives)
        if (npc.name.Contains("Test_Character_Player_NPC") || npc.name.StartsWith("Test_"))
        {
            return true;
        }
        
        // Check for critical health issues (but be less strict about settler data)
        if (npc.MaxHealth <= 0)
        {
            Debug.LogWarning($"[SaveLoadManager] Filtering out NPC with invalid max health: {npc.name}");
            return true;
        }
        
        // Allow NPCs even if they have "Unknown Settler" as name - they might be legitimate recruited NPCs
        // that had issues with data application but should still be saved
        return false;
    }

    #endregion

    #region Player Data Save/Load

    private void SavePlayerData(PlayerData playerData)
    {
        if (PlayerInventory.Instance == null) return;

        var playerInventory = PlayerInventory.Instance;

        // Clear existing data to prevent duplicates
        playerData.inventory.Clear();
        playerData.equippedMutationIds.Clear();
        playerData.availableMutations.Clear();
        playerData.recruitedNPCs.Clear();

        // Consolidate inventory items by resourceId
        var inventoryDict = new Dictionary<string, int>();
        foreach (var item in playerInventory.GetFullInventory())
        {
            string resourceId = item.resourceScriptableObj.name;
            if (inventoryDict.ContainsKey(resourceId))
            {
                inventoryDict[resourceId] += item.count;
            }
            else
            {
                inventoryDict[resourceId] = item.count;
            }
        }
        
        // Convert consolidated inventory to save format
        foreach (var kvp in inventoryDict)
        {
            playerData.inventory.Add(new ResourceItemData(kvp.Key, kvp.Value));
        }

        // Save equipped mutations (use HashSet to prevent duplicates)
        var equippedMutationSet = new HashSet<string>();
        foreach (var mutation in playerInventory.EquippedMutations)
        {
            equippedMutationSet.Add(mutation.name);
        }
        playerData.equippedMutationIds.AddRange(equippedMutationSet);

        // Consolidate available mutations by mutationId
        var mutationDict = new Dictionary<string, int>();
        foreach (var mutation in playerInventory.availableMutations)
        {
            string mutationId = mutation.mutation.name;
            if (mutationDict.ContainsKey(mutationId))
            {
                mutationDict[mutationId] += mutation.quantity;
            }
            else
            {
                mutationDict[mutationId] = mutation.quantity;
            }
        }
        
        // Convert consolidated mutations to save format
        foreach (var kvp in mutationDict)
        {
            playerData.availableMutations.Add(new MutationQuantityData(kvp.Key, kvp.Value));
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

        // Save recruited NPCs with full data for proper restoration
        if (playerInventory.HasRecruitedNPCs())
        {
            var recruitedNPCDataList = playerInventory.GetRecruitedNPCData();
            var recruitedNPCSet = new HashSet<string>(); // Track by componentId to prevent duplicates
            
            foreach (var recruitedNPCData in recruitedNPCDataList)
            {
                // Skip if already saved (prevent duplicates by componentId)
                if (recruitedNPCSet.Contains(recruitedNPCData.componentId))
                {
                    continue;
                }
                
                // Convert PlayerInventory's RecruitedNPCData to our save format
                var recruitedNPCSaveData = new RecruitedNPCSaveData(
                    recruitedNPCData.componentId,
                    recruitedNPCData.settlerData.name,
                    recruitedNPCData.settlerData.age,
                    recruitedNPCData.settlerData.description,
                    recruitedNPCData.appearanceData
                );
                
                playerData.recruitedNPCs.Add(recruitedNPCSaveData);
                recruitedNPCSet.Add(recruitedNPCData.componentId);
            }
            
        }
        
    }

    private void LoadPlayerData(PlayerData playerData)
    {
        if (PlayerInventory.Instance == null) return;

        var playerInventory = PlayerInventory.Instance;

        // Clear existing inventory
        playerInventory.ClearInventory();
        playerInventory.ClearAvailableMutations();

        // Recruited NPCs are now handled by immediate transfer to camp
        // No longer need to load them separately as they become camp residents immediately
        // and are saved as part of the camp's NPC data

        // TODO: Implement remaining player data loading
        // You'd need to find resource/mutation objects by ID and restore inventory
        Debug.LogWarning($"[SaveLoadManager] TODO: Load remaining player data - {playerData.inventory.Count} inventory items");
    }

    /// <summary>
    /// Load player data specifically for camp mode, handling recruited NPCs appropriately
    /// </summary>
    private void LoadPlayerDataForCamp(PlayerData playerData)
    {
        if (PlayerInventory.Instance == null) return;

        var playerInventory = PlayerInventory.Instance;

        // In camp mode, we want to restore recruited NPCs to PlayerInventory 
        // so they can be properly transferred by CampNPCTransferManager
        if (playerData?.recruitedNPCs != null && playerData.recruitedNPCs.Count > 0)
        {            
            // Restore recruited NPCs to PlayerInventory for transfer
            // This will be handled by the CampNPCTransferManager when it detects recruited NPCs
            RestoreRecruitedNPCsToPlayerInventory(playerData);
        }

        // Load other player data as normal
        // TODO: Implement remaining player data loading (inventory, mutations, etc.)
        Debug.LogWarning($"[SaveLoadManager] TODO: Load remaining player data for camp - {playerData?.inventory?.Count ?? 0} inventory items");
    }

    /// <summary>
    /// Restore recruited NPCs to PlayerInventory from save data
    /// </summary>
    private void RestoreRecruitedNPCsToPlayerInventory(PlayerData playerData)
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("[SaveLoadManager] PlayerInventory.Instance is null, cannot restore recruited NPCs");
            return;
        }
        
        foreach (var recruitedNPCSaveData in playerData.recruitedNPCs)
        {
            // Create SettlerData from saved data
            var settlerData = new Managers.SettlerData(
                recruitedNPCSaveData.settlerName ?? "Unknown Settler",
                recruitedNPCSaveData.settlerAge,
                recruitedNPCSaveData.settlerDescription ?? "A mysterious settler."
            );

            // Create a full RecruitedNPCData object using the existing PlayerInventory structure
            var recruitedNPCData = new RecruitedNPCData(
                settlerData,
                recruitedNPCSaveData.componentId,
                recruitedNPCSaveData.appearanceData ?? new NPCAppearanceData()
            );

            // Restore to PlayerInventory
            RestoreRecruitedNPCDataToInventory(recruitedNPCData);
            
        }
        
    }

    /// <summary>
    /// Restore a recruited NPC data object to PlayerInventory
    /// </summary>
    private void RestoreRecruitedNPCDataToInventory(RecruitedNPCData recruitedNPCData)
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.RestoreRecruitedNPC(recruitedNPCData);
        }
        else
        {
            Debug.LogError("[SaveLoadManager] PlayerInventory.Instance is null, cannot restore recruited NPC");
        }
    }



    /// <summary>
    /// Find an NPCScriptableObj by name (helper method)
    /// </summary>
    private NPCScriptableObj FindNPCScriptableByName(string npcName)
    {
        if (string.IsNullOrEmpty(npcName))
        {
            Debug.LogWarning("[SaveLoadManager] NPCScriptableObj name is null or empty");
            return null;
        }

        // Try to load from Resources folder first
        NPCScriptableObj npcScriptable = Resources.Load<NPCScriptableObj>($"SettlerNPCs/{npcName}");
        if (npcScriptable != null)
        {
            return npcScriptable;
        }

        // Try alternative Resources path
        npcScriptable = Resources.Load<NPCScriptableObj>(npcName);
        if (npcScriptable != null)
        {
            return npcScriptable;
        }

        // Search through all NPCScriptableObj assets in the project
        NPCScriptableObj[] allNPCs = Resources.FindObjectsOfTypeAll<NPCScriptableObj>();
        foreach (var npc in allNPCs)
        {
            if (npc.name == npcName)
            {
                return npc;
            }
        }

        Debug.LogWarning($"[SaveLoadManager] Could not find NPCScriptableObj: {npcName}");
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
        Debug.LogWarning($"TODO: Load building data for {buildingData.buildings.Count} buildings");
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
        Debug.LogWarning("TODO: Load manager data");
    }

    #endregion

    #region Scene-Aware Save/Load Methods

    /// <summary>
    /// Get the current game mode to determine what data should be saved/loaded
    /// </summary>
    private GameMode GetCurrentGameMode()
    {
        // Try to get from GameManager first
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.CurrentGameMode;
        }

        // Fallback to scene name detection
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return GetGameModeFromSceneName(currentSceneName);
    }

    /// <summary>
    /// Map scene names to game modes for fallback detection
    /// </summary>
    private GameMode GetGameModeFromSceneName(string sceneName)
    {
        switch (sceneName)
        {
            case "CampScene":
                return GameMode.CAMP;
            case "RogueLikeScene":
            case "OverworldScene":
                return GameMode.ROGUE_LITE;
            case "MainMenuScene":
                return GameMode.MAIN_MENU;
            default:
                Debug.LogWarning($"Unknown scene name for game mode detection: {sceneName}");
                return GameMode.NONE;
        }
    }

    /// <summary>
    /// Load existing save data to preserve other scenes' data, or create new data if none exists
    /// </summary>
    private GameData LoadExistingGameData()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                GameData existingData = JsonUtility.FromJson<GameData>(json);
                return existingData;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveLoadManager] Failed to load existing save data: {e.Message}. Creating new save data.");
            }
        }

        // Return new game data if no existing save or failed to load
        return new GameData
        {
            saveTime = DateTime.Now,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };
    }

    /// <summary>
    /// Save only the data relevant to the current game mode
    /// </summary>
    private void SaveGameModeSpecificData(GameData data, GameMode currentGameMode)
    {
        switch (currentGameMode)
        {
            case GameMode.CAMP:
            case GameMode.CAMP_ATTACK:
                // Save camp-specific data
                SaveGridData(data.gridData);
                SaveNPCData(data.npcData);
                SaveBuildingData(data.buildingData);
                SaveManagerData(data.managerData);
        
                SaveResearchData(data.researchData);
                // IMPORTANT: Also save player data in camp mode to capture any recruited NPCs 
                // that may still be in PlayerInventory before transfer
                SavePlayerData(data.playerData);
                break;

            case GameMode.ROGUE_LITE:
                // Save roguelike-specific data (primarily player data)
                SavePlayerData(data.playerData);
                // Save narrative data as it might be relevant for story progression
        
                break;

            case GameMode.MAIN_MENU:
                // For main menu, we might want to save global player data only
                SavePlayerData(data.playerData);
                break;

            default:
                Debug.LogWarning($"[SaveLoadManager] Unknown game mode for saving: {currentGameMode}. Saving all data as fallback.");
                // Fallback to saving all data
                SaveGridData(data.gridData);
                SaveResearchData(data.researchData);
                SaveNPCData(data.npcData);
                SavePlayerData(data.playerData);
                SaveBuildingData(data.buildingData);
                SaveManagerData(data.managerData);
        
                break;
        }
    }

    /// <summary>
    /// Load only the data relevant to the current game mode
    /// </summary>
    private void LoadGameModeSpecificData(GameData data, GameMode currentGameMode)
    {
        switch (currentGameMode)
        {
            case GameMode.CAMP:
            case GameMode.CAMP_ATTACK:
                // Load camp-specific data
                LoadGridData(data.gridData);
                LoadNPCData(data.npcData);
                LoadBuildingData(data.buildingData);
                LoadManagerData(data.managerData);
        
                LoadResearchData(data.researchData);
                // IMPORTANT: Also load player data in camp mode to restore any recruited NPCs
                // that need to be transferred to camp
                LoadPlayerDataForCamp(data.playerData);
                break;

            case GameMode.ROGUE_LITE:
                // Load roguelike-specific data
                LoadPlayerData(data.playerData);
        
                break;

            case GameMode.MAIN_MENU:
                // For main menu, load global player data only
                LoadPlayerData(data.playerData);
                break;

            default:
                Debug.LogWarning($"[SaveLoadManager] Unknown game mode for loading: {currentGameMode}. Loading all data as fallback.");
                // Fallback to loading all data
                LoadGridData(data.gridData);
                LoadResearchData(data.researchData);
                LoadNPCData(data.npcData);
                LoadPlayerData(data.playerData);
                LoadBuildingData(data.buildingData);
                LoadManagerData(data.managerData);
        
                break;
        }
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
    }

    /// <summary>
    /// Get time until next auto-save
    /// </summary>
    public float GetTimeUntilAutoSave()
    {
        if (!autoSaveEnabled) return -1f;
        return autoSaveInterval - (Time.time - lastAutoSaveTime);
    }

    /// <summary>
    /// Check if save data exists for a specific game mode
    /// </summary>
    public bool HasSaveDataForGameMode(GameMode gameMode)
    {
        if (!HasSaveFile()) return false;

        try
        {
            string json = File.ReadAllText(saveFilePath);
            GameData data = JsonUtility.FromJson<GameData>(json);
            
            switch (gameMode)
            {
                case GameMode.CAMP:
                case GameMode.CAMP_ATTACK:
                    return data.gridData?.occupiedSlots?.Count > 0 || 
                           data.npcData?.npcs?.Count > 0 || 
                           data.buildingData?.buildings?.Count > 0;
                case GameMode.ROGUE_LITE:
                    return data.playerData?.inventory?.Count > 0 || 
                           data.playerData?.equippedMutationIds?.Count > 0 ||
                           data.playerData?.recruitedNPCs?.Count > 0;
                case GameMode.MAIN_MENU:
                    return data.playerData != null;
                default:
                    return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to check save data for game mode {gameMode}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get information about what data is saved for debugging/UI purposes
    /// </summary>
    public string GetSaveDataInfo()
    {
        if (!HasSaveFile()) return "No save file found";

        try
        {
            string json = File.ReadAllText(saveFilePath);
            GameData data = JsonUtility.FromJson<GameData>(json);
            
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Save Date: {data.saveTime}");
            info.AppendLine($"Last Scene: {data.sceneName}");
            info.AppendLine($"Version: {data.saveVersion}");
            info.AppendLine();
            
            // Camp data
            info.AppendLine("CAMP DATA:");
            info.AppendLine($"  Grid Objects: {data.gridData?.occupiedSlots?.Count ?? 0}");
            info.AppendLine($"  NPCs: {data.npcData?.npcs?.Count ?? 0}");
            info.AppendLine($"  Buildings: {data.buildingData?.buildings?.Count ?? 0}");
            info.AppendLine($"  Research Items: {data.researchData?.completedResearchIds?.Count ?? 0}");
            info.AppendLine();
            
            // Player data
            info.AppendLine("PLAYER DATA:");
            info.AppendLine($"  Inventory Items: {data.playerData?.inventory?.Count ?? 0}");
            info.AppendLine($"  Equipped Mutations: {data.playerData?.equippedMutationIds?.Count ?? 0}");
            info.AppendLine($"  Available Mutations: {data.playerData?.availableMutations?.Count ?? 0}");
            info.AppendLine($"  Equipped Weapon: {(data.playerData?.equippedWeapon?.weaponId ?? "None")}");
            info.AppendLine($"  Recruited NPCs: {data.playerData?.recruitedNPCs?.Count ?? 0}");
            
            return info.ToString();
        }
        catch (Exception e)
        {
            return $"Error reading save data: {e.Message}";
        }
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
        
        // Get or add SaveableObject component and restore UUID
        SaveableObject saveableObj = buildingObj.GetComponent<SaveableObject>();
        if (saveableObj == null)
        {
            saveableObj = buildingObj.AddComponent<SaveableObject>();
        }
        
        // Restore the UUID from save data
        if (!string.IsNullOrEmpty(slotData.uuid))
        {
            saveableObj.SetUUID(slotData.uuid);
        }
        
        // Restore narrative data from serializable format
        if (slotData.narrativeFlags != null)
        {
            saveableObj.SetAllNarrativeFlagsFromSerializable(slotData.narrativeFlags);
            if (slotData.narrativeFlags.Count > 0)
            {
                Debug.Log($"[SaveLoadManager] Restored {slotData.narrativeFlags.Count} narrative flags for building {buildingScriptable.name} (UUID: {saveableObj.UUID})");
            }
        }
        
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
        
        // Get or add SaveableObject component and restore UUID
        SaveableObject saveableObj = turretObj.GetComponent<SaveableObject>();
        if (saveableObj == null)
        {
            saveableObj = turretObj.AddComponent<SaveableObject>();
        }
        
        // Restore the UUID from save data
        if (!string.IsNullOrEmpty(slotData.uuid))
        {
            saveableObj.SetUUID(slotData.uuid);
        }
        
        // Restore narrative data from serializable format
        if (slotData.narrativeFlags != null)
        {
            saveableObj.SetAllNarrativeFlagsFromSerializable(slotData.narrativeFlags);
            if (slotData.narrativeFlags.Count > 0)
            {
                Debug.Log($"[SaveLoadManager] Restored {slotData.narrativeFlags.Count} narrative flags for turret {turretScriptable.name} (UUID: {saveableObj.UUID})");
            }
        }
        
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

    }

    #endregion
}
