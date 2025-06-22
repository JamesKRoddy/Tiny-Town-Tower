using UnityEngine;
using Managers;

public class OverWorldDoor : RogueLiteDoor
{
    [Header("Overworld Door Settings")]
    [SerializeField] private int buildingDifficulty = 10;
    [SerializeField] private BuildingType buildingType;
    [SerializeField] private string buildingName = "Unknown Building";
    
    [Header("Scene Transition")]
    [SerializeField] private SceneNames targetScene = SceneNames.RogueLikeScene;
    [SerializeField] private GameMode nextSceneGameMode = GameMode.ROGUE_LITE;
    [SerializeField] private bool keepPossessedNPC = true;
    [SerializeField] private Transform playerSpawnPoint;
    
    [Header("Gizmo Display")]
    [SerializeField] private bool showDifficultyGizmos = true;
    [SerializeField] private float gizmoHeight = 2f;
    [SerializeField] private float gizmoRadius = 0.5f;
    [SerializeField] private bool showDifficultyText = true;
    [SerializeField] private bool showDifficultyNumber = true;
    [SerializeField] private bool showBuildingName = true;
    [SerializeField] private float textOffset = 0.5f;

    private BuildingDataScriptableObj buildingData;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnDoorEntered()
    {
        if (isLocked) return;

        // Initialize the difficulty manager with this building's difficulty
        DifficultyManager.Instance.InitializeBuildingDifficulty(buildingType, buildingDifficulty);
        
        // Handle scene transition and building setup
        HandleBuildingEntry();
    }

    private void HandleBuildingEntry()
    {
        if (nextSceneGameMode == GameMode.NONE)
        {
            Debug.LogWarning($"{gameObject.name} has no next game mode");
            return;
        }

        if (targetScene == SceneNames.NONE)
        {
            Debug.LogWarning($"{gameObject.name} has no next scene");
            return;
        }

        // Store player spawn point and set building data
        RogueLiteManager.Instance.OverworldManager.EnteredBuilding(playerSpawnPoint);
        buildingData = RogueLiteManager.Instance.BuildingManager.SetBuildingData(buildingType);

        // Load the scene with transition
        SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPossessedNPC, OnSceneLoaded);
    }

    private void OnSceneLoaded()
    {
        if (buildingData != null)
        {
            Transform buildingEntrance = buildingData.buildingEntrance.GetComponent<BuildingEntrance>().PlayerSpawnPoint;
            if (buildingEntrance == null)
            {
                Debug.LogError($"No buildingEntranceSpawnPoint found on gameobject: {buildingData.buildingEntrance.name}");
                return;
            }
        }

        // Instantiate the building entrance
        if (buildingData != null && buildingData.buildingEntrance != null)
        {
            Instantiate(buildingData.buildingEntrance, Vector3.zero, Quaternion.identity);
        }
    }

    public int GetBuildingDifficulty()
    {
        return buildingDifficulty;
    }

    public BuildingType GetBuildingType()
    {
        return buildingType;
    }

    public string GetBuildingName()
    {
        return buildingName;
    }

    // Draw gizmos to show difficulty in scene view
    private void OnDrawGizmos()
    {
        if (!showDifficultyGizmos) return;

        // Draw difficulty sphere
        Gizmos.color = GetDifficultyColor(buildingDifficulty);
        Vector3 gizmoPosition = transform.position + Vector3.up * gizmoHeight;
        Gizmos.DrawWireSphere(gizmoPosition, gizmoRadius);

        // Draw connection line from door to gizmo
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, gizmoPosition);

        // Draw simple difficulty number on wire sphere
        #if UNITY_EDITOR
        if (showDifficultyNumber && !showDifficultyText)
        {
            UnityEditor.Handles.color = GetDifficultyColor(buildingDifficulty);
            UnityEditor.Handles.Label(gizmoPosition, buildingDifficulty.ToString());
        }
        #endif
    }

    // Draw selected gizmos with more detail
    private void OnDrawGizmosSelected()
    {
        if (!showDifficultyGizmos) return;

        // Draw filled sphere for selected state
        Gizmos.color = GetDifficultyColor(buildingDifficulty);
        Vector3 gizmoPosition = transform.position + Vector3.up * gizmoHeight;
        Gizmos.DrawSphere(gizmoPosition, gizmoRadius * 0.8f);

        // Draw connection line
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, gizmoPosition);

        // Draw detailed difficulty text in scene view
        #if UNITY_EDITOR
        if (showDifficultyText)
        {
            // Set the text color to match the difficulty
            UnityEditor.Handles.color = GetDifficultyColor(buildingDifficulty);
            
            // Build the display text based on settings
            string displayText = "";
            
            if (showBuildingName)
            {
                displayText += buildingName;
            }
            
            if (showDifficultyNumber)
            {
                if (showBuildingName) displayText += "\n";
                displayText += $"Difficulty: {buildingDifficulty}";
            }
            
            // Add scene info if available
            if (targetScene != SceneNames.NONE)
            {
                displayText += $"\nScene: {targetScene}";
            }
            
            // Draw the label with colored text
            UnityEditor.Handles.Label(gizmoPosition + Vector3.up * textOffset, displayText);
        }
        #endif
    }

    private Color GetDifficultyColor(int difficulty)
    {
        if (difficulty < 20) return Color.green;      // Easy
        if (difficulty < 40) return Color.yellow;     // Medium
        if (difficulty < 60) return Color.red;        // Hard
        return Color.magenta;                         // Very Hard
    }

    // Override the interaction text to show difficulty
    public override string GetInteractionText()
    {
        switch (doorType)
        {
            case DoorStatus.LOCKED:
                return "Door Locked";
            case DoorStatus.ENTRANCE:
                return $"Enter {buildingName} (Difficulty: {buildingDifficulty})";
            case DoorStatus.EXIT:
                return "Can't Go Back";
            default:
                return "INVALID";
        }
    }
} 