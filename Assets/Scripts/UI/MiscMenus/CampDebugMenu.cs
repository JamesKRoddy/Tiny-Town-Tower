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
    }
    
    private void Start()
    {
        // The DebugMenuManager automatically finds and registers all BaseDebugMenu components
        // No need to manually register
        
        // Hide by default
        gameObject.SetActive(false);
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
            
            foreach (var npc in npcs)
            {
                if (npc.GetCurrentTaskType() == TaskType.WORK)
                    workingNPCs++;
                
                if (npc.IsHungry())
                    hungryNPCs++;
                    
                if (npc.IsStarving())
                    starvingNPCs++;
            }
            
            stringBuilder.AppendLine($"Working NPCs: {workingNPCs}");
            stringBuilder.AppendLine($"Hungry NPCs: {hungryNPCs}");
            stringBuilder.AppendLine($"Starving NPCs: {starvingNPCs}");
            
            // Show average hunger percentage
            if (npcs.Count > 0)
            {
                float totalHunger = 0f;
                foreach (var npc in npcs)
                {
                    totalHunger += npc.GetHungerPercentage();
                }
                float avgHunger = (totalHunger / npcs.Count) * 100f;
                stringBuilder.AppendLine($"Average Hunger: {avgHunger:F1}%");
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
