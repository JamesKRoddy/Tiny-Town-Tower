using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using System.Text;
using System.Linq;

public class CampDebugMenu : BaseDebugMenu
{
    [Header("Camp Debug Menu")]
    [SerializeField] private TextMeshProUGUI campInfoText;
    [SerializeField] private float updateInterval = 0.5f; // Update every 0.5 seconds
    
    [Header("NPC Feature Toggles")]
    [SerializeField] private Toggle hungerToggle;
    [SerializeField] private Toggle staminaToggle;
    [SerializeField] private Toggle sicknessToggle;
    [SerializeField] private Toggle sleepToggle;
    
    [Header("NPC Testing Buttons")]
    [SerializeField] private Button addRandomNPCButton;
    [SerializeField] private Button killRandomNPCButton;
    [SerializeField] private Button killAllNPCsButton;
    
    // Static debug flags for controlling NPC features
    public static bool DisableHungerSystem = false;
    public static bool DisableStaminaSystem = false;
    public static bool DisableSicknessSystem = false;
    public static bool DisableSleepSystem = false;
    
    private float lastUpdateTime;
    private StringBuilder stringBuilder = new StringBuilder();
    
    protected void Awake()
    {
        // Set menu properties
        menuName = "Camp Debug Menu";
        
        // Find text component if not assigned
        if (campInfoText == null)
        {
            campInfoText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (campInfoText == null)
        {
            Debug.LogError("[CampDebugMenu] No TextMeshProUGUI component found! Please assign campInfoText in the inspector.");
        }
        
        // Setup toggle listeners
        SetupToggleListeners();
        
        // Setup button listeners
        SetupButtonListeners();
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Clean up button listeners
        if (addRandomNPCButton != null)
        {
            addRandomNPCButton.onClick.RemoveListener(OnAddRandomNPC);
        }
        
        if (killRandomNPCButton != null)
        {
            killRandomNPCButton.onClick.RemoveListener(OnKillRandomNPC);
        }
        
        if (killAllNPCsButton != null)
        {
            killAllNPCsButton.onClick.RemoveListener(OnKillAllNPCs);
        }
        
        // Clean up toggle listeners
        if (hungerToggle != null)
        {
            hungerToggle.onValueChanged.RemoveListener(OnHungerToggleChanged);
        }
        
        if (staminaToggle != null)
        {
            staminaToggle.onValueChanged.RemoveListener(OnStaminaToggleChanged);
        }
        
        if (sicknessToggle != null)
        {
            sicknessToggle.onValueChanged.RemoveListener(OnSicknessToggleChanged);
        }
        
        if (sleepToggle != null)
        {
            sleepToggle.onValueChanged.RemoveListener(OnSleepToggleChanged);
        }
    }
    
    private void SetupToggleListeners()
    {
        if (hungerToggle != null)
        {
            hungerToggle.isOn = !DisableHungerSystem;
            hungerToggle.onValueChanged.AddListener(OnHungerToggleChanged);
        }
        
        if (staminaToggle != null)
        {
            staminaToggle.isOn = !DisableStaminaSystem;
            staminaToggle.onValueChanged.AddListener(OnStaminaToggleChanged);
        }
        
        if (sicknessToggle != null)
        {
            sicknessToggle.isOn = !DisableSicknessSystem;
            sicknessToggle.onValueChanged.AddListener(OnSicknessToggleChanged);
        }
        
        if (sleepToggle != null)
        {
            sleepToggle.isOn = !DisableSleepSystem;
            sleepToggle.onValueChanged.AddListener(OnSleepToggleChanged);
        }
    }
    
    private void OnHungerToggleChanged(bool isEnabled)
    {
        DisableHungerSystem = !isEnabled;
        Debug.Log($"[CampDebugMenu] Hunger system {(isEnabled ? "enabled" : "disabled")}");
    }
    
    private void OnStaminaToggleChanged(bool isEnabled)
    {
        DisableStaminaSystem = !isEnabled;
        Debug.Log($"[CampDebugMenu] Stamina system {(isEnabled ? "enabled" : "disabled")}");
    }
    
    private void OnSicknessToggleChanged(bool isEnabled)
    {
        DisableSicknessSystem = !isEnabled;
        Debug.Log($"[CampDebugMenu] Sickness system {(isEnabled ? "enabled" : "disabled")}");
    }
    
    private void OnSleepToggleChanged(bool isEnabled)
    {
        DisableSleepSystem = !isEnabled;
        Debug.Log($"[CampDebugMenu] Sleep system {(isEnabled ? "enabled" : "disabled")}");
    }
    
    private void SetupButtonListeners()
    {
        if (addRandomNPCButton != null)
        {
            addRandomNPCButton.onClick.AddListener(OnAddRandomNPC);
        }
        
        if (killRandomNPCButton != null)
        {
            killRandomNPCButton.onClick.AddListener(OnKillRandomNPC);
        }
        
        if (killAllNPCsButton != null)
        {
            killAllNPCsButton.onClick.AddListener(OnKillAllNPCs);
        }
    }
    
    private void OnAddRandomNPC()
    {
        if (NPCManager.Instance == null)
        {
            Debug.LogWarning("[CampDebugMenu] NPCManager not available");
            return;
        }
        
        // Get settler prefab
        GameObject settlerPrefab = NPCManager.Instance.GetSettlerPrefab();
        if (settlerPrefab == null)
        {
            Debug.LogError("[CampDebugMenu] Settler prefab not available");
            return;
        }
        
        // Generate random settler data
        var settlerData = NPCManager.Instance.GenerateRandomSettlerData();
        if (settlerData == null)
        {
            Debug.LogError("[CampDebugMenu] Failed to generate settler data");
            return;
        }
        
        // Spawn the NPC at origin or near other NPCs
        Vector3 spawnPosition = Vector3.zero;
        var existingNPCs = NPCManager.Instance.GetAllNPCs();
        if (existingNPCs.Count > 0)
        {
            // Spawn near a random existing NPC
            var randomNPC = existingNPCs[Random.Range(0, existingNPCs.Count)];
            spawnPosition = randomNPC.transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        }
        
        // Instantiate the settler
        GameObject npcObj = Instantiate(settlerPrefab, spawnPosition, Quaternion.identity);
        SettlerNPC settler = npcObj.GetComponent<SettlerNPC>();
        
        if (settler != null)
        {
            // Apply settler data
            settler.ApplySettlerData(settlerData);
            
            // Set initialization context to CAMP_SPAWN (spawned directly in camp for testing)
            settler.SetInitializationContext(NPCInitializationContext.CAMP_SPAWN);
            
            Debug.Log($"[CampDebugMenu] Spawned new NPC: {settlerData.name} at {spawnPosition}");
        }
        else
        {
            Debug.LogError("[CampDebugMenu] Failed to get SettlerNPC component");
            Destroy(npcObj);
        }
    }
    
    private void OnKillRandomNPC()
    {
        if (NPCManager.Instance == null)
        {
            Debug.LogWarning("[CampDebugMenu] NPCManager not available");
            return;
        }
        
        var npcs = NPCManager.Instance.GetAllNPCs();
        if (npcs.Count == 0)
        {
            Debug.LogWarning("[CampDebugMenu] No NPCs available to kill");
            return;
        }
        
        // Pick a random ALIVE NPC
        var aliveNPCs = npcs.FindAll(npc => npc != null && npc.Health > 0);
        if (aliveNPCs.Count == 0)
        {
            Debug.LogWarning("[CampDebugMenu] No alive NPCs to kill");
            return;
        }
        
        var randomNPC = aliveNPCs[Random.Range(0, aliveNPCs.Count)];
        string npcName = randomNPC.GetSettlerName();
        if (string.IsNullOrEmpty(npcName) || npcName == "Unknown Settler")
        {
            npcName = randomNPC.name;
        }
        
        Debug.Log($"[CampDebugMenu] Killing NPC: {npcName} (Health: {randomNPC.Health})");
        
        // Kill the NPC
        if (randomNPC is SettlerNPC settler)
        {
            settler.Die(); // Call Die() directly for clean unregistration
        }
        else
        {
            randomNPC.Die();
        }
        
        Debug.Log($"[CampDebugMenu] Killed {npcName}. Remaining NPCs: {NPCManager.Instance.TotalNPCs}");
    }
    
    private void OnKillAllNPCs()
    {
        if (NPCManager.Instance == null)
        {
            Debug.LogWarning("[CampDebugMenu] NPCManager not available");
            return;
        }
        
        var npcs = NPCManager.Instance.GetAllNPCs();
        if (npcs.Count == 0)
        {
            Debug.LogWarning("[CampDebugMenu] No NPCs available to kill");
            return;
        }
        
        // Filter to only alive NPCs
        var aliveNPCs = npcs.FindAll(npc => npc != null && npc.Health > 0);
        if (aliveNPCs.Count == 0)
        {
            Debug.LogWarning("[CampDebugMenu] No alive NPCs to kill");
            return;
        }
        
        int npcCount = aliveNPCs.Count;
        Debug.Log($"[CampDebugMenu] Killing all {npcCount} alive NPCs...");
        
        // Kill all NPCs (create a copy of the list since it will be modified during iteration)
        var npcsCopy = new System.Collections.Generic.List<SettlerNPC>(aliveNPCs);
        foreach (var npc in npcsCopy)
        {
            if (npc != null && npc.Health > 0)
            {
                string npcName = npc.GetSettlerName();
                Debug.Log($"[CampDebugMenu] Killing {npcName}...");
                npc.Die(); // Call Die() directly for clean unregistration
            }
        }
        
        Debug.Log($"[CampDebugMenu] All NPCs killed. This should trigger game restart!");
    }
    
    private void Update()
    {
        // Only update if menu is active and enough time has passed
        if (gameObject.activeInHierarchy && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateCampInfo();
            lastUpdateTime = Time.time;
        }
    }
    
    private void UpdateCampInfo()
    {
        if (campInfoText == null) return;
        
        stringBuilder.Clear();
        stringBuilder.AppendLine("=== CAMP DEBUG INFO ===");
        
        // Game state information
        if (GameManager.Instance != null)
        {
            stringBuilder.AppendLine($"Game Mode: {GameManager.Instance.CurrentGameMode}");
        }
        stringBuilder.AppendLine($"Game Time: {Time.time:F1}s");
        stringBuilder.AppendLine();
        
        // Debug toggles information
        AddDebugTogglesInfo();
        stringBuilder.AppendLine();
        
        // Cleanliness Information
        AddCleanlinessInfo();
        stringBuilder.AppendLine();
        
        // NPC Information
        AddNPCInfo();
        stringBuilder.AppendLine();
        
        // Electricity Information
        AddElectricityInfo();
        stringBuilder.AppendLine();
        
        // Work Tasks Information
        AddWorkTasksInfo();
        stringBuilder.AppendLine();
        
        // Resource Information
        AddResourceInfo();
        
        campInfoText.text = stringBuilder.ToString();
    }
    
    private void AddCleanlinessInfo()
    {
        stringBuilder.AppendLine("--- CLEANLINESS ---");
        
        if (CampManager.Instance?.CleanlinessManager != null)
        {
            var cleanlinessManager = CampManager.Instance.CleanlinessManager;
            float cleanlinessPercentage = cleanlinessManager.GetCleanlinessPercentage();
            float productivityMultiplier = cleanlinessManager.GetProductivityMultiplier();
            
            stringBuilder.AppendLine($"Cleanliness: {cleanlinessPercentage:F1}%");
            stringBuilder.AppendLine($"Productivity Multiplier: {productivityMultiplier:F2}x");
            stringBuilder.AppendLine($"Impact: {cleanlinessManager.GetProductivityImpactDescription()}");
            
            var dirtPiles = cleanlinessManager.GetActiveDirtPiles();
            stringBuilder.AppendLine($"Active Dirt Piles: {dirtPiles.Count}");
            
            var fullToilets = cleanlinessManager.GetFullToilets();
            var fullWasteBins = cleanlinessManager.GetFullWasteBins();
            stringBuilder.AppendLine($"Full Toilets: {fullToilets.Count}");
            stringBuilder.AppendLine($"Full Waste Bins: {fullWasteBins.Count}");
        }
        else
        {
            stringBuilder.AppendLine("CleanlinessManager not available");
        }
    }
    
    private void AddNPCInfo()
    {
        stringBuilder.AppendLine("--- NPCs ---");
        
        if (NPCManager.Instance != null)
        {
            stringBuilder.AppendLine($"Total NPCs: {NPCManager.Instance.TotalNPCs}");
            
            var npcs = NPCManager.Instance.GetAllNPCs();
            int workingNPCs = 0;
            int hungryNPCs = 0;
            int starvingNPCs = 0;
            int sickNPCs = 0;
            int tiredNPCs = 0;
            int exhaustedNPCs = 0;
            
            foreach (var npc in npcs)
            {
                if (npc.GetCurrentTaskType() == TaskType.WORK)
                    workingNPCs++;
                
                if (npc.IsHungry())
                    hungryNPCs++;
                    
                if (npc.IsStarving())
                    starvingNPCs++;
                
                if (npc.IsSick)
                    sickNPCs++;
                
                if (npc.IsVeryTired())
                    exhaustedNPCs++;
                else if (npc.IsTired())
                    tiredNPCs++;
            }
            
            stringBuilder.AppendLine($"Working NPCs: {workingNPCs}");
            stringBuilder.AppendLine($"Hungry NPCs: {hungryNPCs}");
            stringBuilder.AppendLine($"Starving NPCs: {starvingNPCs}");
            stringBuilder.AppendLine($"Sick NPCs: {sickNPCs}");
            stringBuilder.AppendLine($"Tired NPCs: {tiredNPCs}");
            stringBuilder.AppendLine($"Exhausted NPCs: {exhaustedNPCs}");
            
            // Show averages if we have NPCs
            if (npcs.Count > 0)
            {
                float totalHunger = 0f;
                float totalStamina = 0f;
                
                foreach (var npc in npcs)
                {
                    totalHunger += npc.GetHungerPercentage();
                    totalStamina += npc.GetStaminaPercentage();
                }
                
                float avgHunger = (totalHunger / npcs.Count) * 100f;
                float avgStamina = totalStamina / npcs.Count;
                
                stringBuilder.AppendLine($"Average Hunger: {avgHunger:F1}%");
                stringBuilder.AppendLine($"Average Stamina: {avgStamina:F1}%");
            }
            
            // Show individual NPC details for first few NPCs
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--- INDIVIDUAL NPCs ---");
            
            int maxNPCsToShow = Mathf.Min(5, npcs.Count); // Show max 5 NPCs to avoid clutter
            for (int i = 0; i < maxNPCsToShow; i++)
            {
                var npc = npcs[i];
                string npcName = npc.GetSettlerName();
                if (string.IsNullOrEmpty(npcName) || npcName == "Unknown Settler")
                {
                    npcName = npc.name; // Fallback to GameObject name
                }
                
                HealthStatus healthStatus = npc.GetHealthStatus();
                TaskType currentTask = npc.GetCurrentTaskType();
                float workSpeed = npc.GetWorkSpeedMultiplier();
                
                stringBuilder.AppendLine($"{npcName}:");
                stringBuilder.AppendLine($"  Status: {healthStatus}");
                stringBuilder.AppendLine($"  Task: {currentTask}");
                stringBuilder.AppendLine($"  Health: {npc.GetSicknessStatusDescription()}");
                stringBuilder.AppendLine($"  Hunger: {(npc.GetHungerPercentage() * 100f):F0}%");
                stringBuilder.AppendLine($"  Stamina: {npc.GetStaminaPercentage():F0}%");
                stringBuilder.AppendLine($"  Work Speed: {(workSpeed * 100f):F0}%");
            }
            
            if (npcs.Count > maxNPCsToShow)
            {
                stringBuilder.AppendLine($"... and {npcs.Count - maxNPCsToShow} more NPCs");
            }
        }
        else
        {
            stringBuilder.AppendLine("NPCManager not available");
        }
    }
    
    private void AddElectricityInfo()
    {
        stringBuilder.AppendLine("--- ELECTRICITY ---");
        
        if (CampManager.Instance?.ElectricityManager != null)
        {
            var electricityManager = CampManager.Instance.ElectricityManager;
            stringBuilder.AppendLine($"Current Power: {electricityManager.GetElectricityPercentage():F1}%");
            stringBuilder.AppendLine($"Power Level: {electricityManager.GetCurrentElectricity():F0} / {electricityManager.GetMaxElectricity():F0}");
        }
        else
        {
            stringBuilder.AppendLine("ElectricityManager not available");
        }
    }
    
    private void AddWorkTasksInfo()
    {
        stringBuilder.AppendLine("--- WORK TASKS ---");
        
        if (CampManager.Instance?.WorkManager != null)
        {
            var workManager = CampManager.Instance.WorkManager;
            
            // Find all WorkTask components in the scene
            var allTasks = FindObjectsByType<WorkTask>(FindObjectsSortMode.None);
            
            int totalTasks = allTasks.Length;
            int occupiedTasks = 0;
            int operationalTasks = 0;
            
            foreach (var task in allTasks)
            {
                if (task.IsOccupied)
                    occupiedTasks++;
                    
                if (task.IsOperational())
                    operationalTasks++;
            }
            
            stringBuilder.AppendLine($"Total Work Tasks: {totalTasks}");
            stringBuilder.AppendLine($"Occupied Tasks: {occupiedTasks}");
            stringBuilder.AppendLine($"Operational Tasks: {operationalTasks}");
            stringBuilder.AppendLine($"Idle Tasks: {totalTasks - occupiedTasks}");
            stringBuilder.AppendLine($"Work Queue: {workManager.GetWorkQueueCount()}");
        }
        else
        {
            stringBuilder.AppendLine("WorkManager not available");
        }
    }
    
    private void AddResourceInfo()
    {
        stringBuilder.AppendLine("--- RESOURCES ---");
        
        if (PlayerInventory.Instance != null)
        {
            var inventory = PlayerInventory.Instance.GetFullInventory();
            
            if (inventory.Count > 0)
            {
                // Show top 10 resources by count
                var sortedInventory = inventory.OrderByDescending(item => item.count).Take(10);
                
                foreach (var item in sortedInventory)
                {
                    stringBuilder.AppendLine($"{item.resourceScriptableObj.objectName}: {item.count}");
                }
                
                if (inventory.Count > 10)
                {
                    stringBuilder.AppendLine($"... and {inventory.Count - 10} more items");
                }
            }
            else
            {
                stringBuilder.AppendLine("No items in inventory");
            }
        }
        else
        {
            stringBuilder.AppendLine("PlayerInventory not available");
        }
    }
    
    private void AddDebugTogglesInfo()
    {
        stringBuilder.AppendLine("--- DEBUG TOGGLES ---");
        stringBuilder.AppendLine($"Hunger System: {(DisableHungerSystem ? "DISABLED" : "ENABLED")}");
        stringBuilder.AppendLine($"Stamina System: {(DisableStaminaSystem ? "DISABLED" : "ENABLED")}");
        stringBuilder.AppendLine($"Sickness System: {(DisableSicknessSystem ? "DISABLED" : "ENABLED")}");
        stringBuilder.AppendLine($"Sleep System: {(DisableSleepSystem ? "DISABLED" : "ENABLED")}");
    }
    
    public override void RegisterMenu()
    {
        base.RegisterMenu();
        // Additional registration logic if needed
    }
    
    public override void ToggleMenu()
    {
        base.ToggleMenu();
        
        // Update immediately when menu is shown
        if (gameObject.activeInHierarchy)
        {
            UpdateCampInfo();
        }
    }
}
