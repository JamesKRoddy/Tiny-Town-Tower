using Managers;
using UnityEngine;

/// <summary>
/// Replaced with OverWorldDoor - kept for backward compatibility.
/// Updated to use new spawn point system.
/// </summary>
public class BuildingEntranceTrigger : SceneTransitionTrigger
{
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private RogueLikeBuildingType buildingType;

    protected override void OnTriggerEnter(Collider other)
    {
        // Prevent multiple scene loads (check parent's flag)
        if (hasTriggeredTransition)
        {
            return;
        }
        
        IPossessable npc = other.GetComponent<IPossessable>();
        if (npc != null && npc == PlayerController.Instance._possessedNPC)
        {
            if (nextSceneGameMode == GameMode.NONE)
            {
                Debug.LogWarning($"{gameObject.name} has no next game mode");
                return;
            }

            if(targetScene == SceneNames.NONE)
            {
                Debug.LogWarning($"{gameObject.name} has no next scene");
                return;
            }

            hasTriggeredTransition = true;

            // Store player spawn point for when they exit the building and return to overworld
            RogueLiteManager.Instance.OverworldManager.EnteredBuilding(playerSpawnPoint);
            
            // Set up building data (this prepares the building but doesn't instantiate it yet)
            RogueLikeBuildingDataScriptableObj buildingData = RogueLiteManager.Instance.BuildingManager.SetBuildingData(buildingType);

            if (buildingData == null)
            {
                Debug.LogError($"[BuildingEntranceTrigger] Failed to set building data for {buildingType}");
                return;
            }

            // Load the scene - building entrance will be instantiated automatically in GetPlayerSpawnPoint()
            SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPossessedNPC);
        }
    }
}
