using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using System.Collections.Generic;

public class RoomDebugMenu : BaseDebugMenu
{
    [Header("Room Debug Menu UI")]
    [SerializeField] private Button spawnRoomButton;
    [SerializeField] private Button clearAllRoomsButton;
    [SerializeField] private TMP_Text statusText;
    
    [Header("Room Configuration")]
    [SerializeField] private TMP_Dropdown buildingTypeDropdown;
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TMP_Text difficultyText;
    
    private RogueLikeBuildingType currentBuildingType = RogueLikeBuildingType.NONE;
    private int currentDifficulty = 1;
    private List<GameObject> spawnedTestRooms = new List<GameObject>();
    
    public override void RegisterMenu()
    {
        // Set menu properties
        menuName = "Room Debug Menu";
        toggleKey = KeyCode.F2;
        
        // Call base Start to handle registration
        base.RegisterMenu();
        
        SetupUI();
    }
    
    private void SetupUI()
    {
        // Setup main buttons
        if (spawnRoomButton != null)
            spawnRoomButton.onClick.AddListener(SpawnRoom);
        
        if (clearAllRoomsButton != null)
            clearAllRoomsButton.onClick.AddListener(ClearAllRooms);
        
        // Setup configuration controls
        SetupBuildingTypeDropdown();
        SetupDifficultySlider();
        
        UpdateStatusDisplay();
    }
    
    private void SetupBuildingTypeDropdown()
    {
        if (buildingTypeDropdown == null) return;
        
        buildingTypeDropdown.ClearOptions();
        List<string> options = new List<string>();
        
        foreach (RogueLikeBuildingType buildingType in System.Enum.GetValues(typeof(RogueLikeBuildingType)))
        {
            options.Add(buildingType.ToString());
        }
        
        buildingTypeDropdown.AddOptions(options);
        buildingTypeDropdown.onValueChanged.AddListener(OnBuildingTypeChanged);
    }
    
    private void SetupDifficultySlider()
    {
        if (difficultySlider == null) return;
        
        difficultySlider.minValue = 1;
        difficultySlider.maxValue = 100;
        difficultySlider.value = currentDifficulty;
        difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        
        UpdateDifficultyText();
    }
    
    private void OnBuildingTypeChanged(int index)
    {
        RogueLikeBuildingType[] buildingTypes = (RogueLikeBuildingType[])System.Enum.GetValues(typeof(RogueLikeBuildingType));
        if (index >= 0 && index < buildingTypes.Length)
        {
            currentBuildingType = buildingTypes[index];
            Debug.Log($"[RoomDebugMenu] Building type changed to: {currentBuildingType}");
            UpdateStatusDisplay();
        }
    }
    
    private void OnDifficultyChanged(float value)
    {
        currentDifficulty = Mathf.RoundToInt(value);
        UpdateDifficultyText();
        UpdateStatusDisplay();
    }
    
    private void UpdateDifficultyText()
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"Difficulty: {currentDifficulty}";
        }
    }
    
    private void SpawnRoom()
    {
        if (RogueLiteManager.Instance.BuildingManager == null)
        {
            Debug.LogError("[RoomDebugMenu] BuildingManager not found!");
            return;
        }
        
        // Clear existing rooms first
        ClearAllRooms();
        
        // Set building data
        var buildingData = RogueLiteManager.Instance.BuildingManager.SetBuildingData(currentBuildingType);
        if (buildingData == null)
        {
            Debug.LogError($"[RoomDebugMenu] Failed to set building data for {currentBuildingType}");
            return;
        }
        
        // Initialize difficulty
        if (GameManager.Instance.DifficultyManager != null)
        {
            GameManager.Instance.DifficultyManager.InitializeBuildingDifficulty(currentBuildingType, currentDifficulty);
        }
        
        // Use BuildingManager's actual room spawning system
        SpawnActualRoom();
        
        Debug.Log($"[RoomDebugMenu] Spawning room - Type: {currentBuildingType}, Difficulty: {currentDifficulty}");
        UpdateStatusDisplay();
    }
    
    private void SpawnActualRoom()
    {
        var buildingManager = RogueLiteManager.Instance.BuildingManager;
        
        // Calculate spawn position (offset from previous rooms)
        Vector3 spawnPosition = new Vector3(spawnedTestRooms.Count * 50f, 0, 0);
        
        // Use BuildingManager's SpawnRoom method directly
        buildingManager.SpawnRoom(currentBuildingType, spawnPosition);
        
        // Track the spawned room
        if (buildingManager.CurrentRoomParent != null)
        {
            spawnedTestRooms.Add(buildingManager.CurrentRoomParent);
            Debug.Log($"[RoomDebugMenu] Spawned building parent at {spawnPosition}");
        }
        else
        {
            Debug.LogError("[RoomDebugMenu] Building parent not found after spawn");
        }
        
        UpdateStatusDisplay();
    }
    

    
    private void ClearAllRooms()
    {
        // Clear spawned rooms
        foreach (var room in spawnedTestRooms)
        {
            if (room != null)
            {
                DestroyImmediate(room);
            }
        }
        spawnedTestRooms.Clear();
        
        // Reset BuildingManager state
        var buildingManager = RogueLiteManager.Instance.BuildingManager;
        if (buildingManager != null)
        {
            buildingManager.ClearDebugState();
        }
        
        Debug.Log("[RoomDebugMenu] Cleared all test rooms and reset BuildingManager state");
        UpdateStatusDisplay();
    }
    
    private void UpdateStatusDisplay()
    {
        if (statusText == null) return;
        
        string status = $"Room Debug Menu\n";
        status += $"Building Type: {currentBuildingType}\n";
        status += $"Difficulty: {currentDifficulty}\n";
        status += $"Spawned Buildings: {spawnedTestRooms.Count}\n";
        
        if (RogueLiteManager.Instance.BuildingManager != null)
        {
            var buildingManager = RogueLiteManager.Instance.BuildingManager;
            status += $"Building Manager: Active\n";
            
            if (buildingManager.CurrentBuilding != null)
            {
                status += $"Current Building: {buildingManager.CurrentBuilding.buildingType}\n";
                status += $"Building Difficulty: {buildingManager.RogueLikeBuildingDifficulty}\n";
                status += $"Current Room: {buildingManager.CurrentRoom}\n";
            }
            
            if (buildingManager.CurrentRoomParent != null)
            {
                status += $"Current Room Parent: {buildingManager.CurrentRoomParent.name}\n";
            }
        }
        else
        {
            status += $"Building Manager: Not Found\n";
        }
        
        statusText.text = status;
    }
    
    private void Update()
    {
        // Update status periodically when menu is active
        if (gameObject.activeInHierarchy)
        {
            UpdateStatusDisplay();
        }
    }


    
    private void OnDestroy()
    {
        ClearAllRooms();
    }
} 