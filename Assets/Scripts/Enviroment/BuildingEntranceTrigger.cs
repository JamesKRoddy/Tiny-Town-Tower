using Managers;
using UnityEngine;

/// <summary>
/// Replaced with overworld door, keeping it here for now
/// </summary>
public class BuildingEntranceTrigger : SceneTransitionTrigger
{
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private BuildingType buildingType;

    BuildingDataScriptableObj buildingData;

    protected override void OnTriggerEnter(Collider other)
    {
        IPossessable npc = other.GetComponent<IPossessable>();
        if (npc != null && npc == PlayerController.Instance._possessedNPC)
        {
            if (nextSceneGameMode == GameMode.NONE)
            {
                Debug.LogWarning($"{gameObject.name} has no next game mode");
                return;
            }

            if(targetScene == string.Empty)
            {
                Debug.LogWarning($"{gameObject.name} has no next scene");
                return;
            }

            RogueLiteManager.Instance.EnteredBuilding(playerSpawnPoint.position);  
            buildingData = RogueLiteManager.Instance.BuildingManager.SetBuildingData(buildingType);

            SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPossessedNPC, OnSceneLoaded);
        }
    }

    protected override void OnSceneLoaded()
    {
        if (buildingData != null)
        {
            Transform buildingEntrance = buildingData.buildingEntrance.GetComponent<BuildingEntrance>().PlayerSpawnPoint;
            if (buildingEntrance == null)
            {
                Debug.LogError($"No buildingEntranceSpawnPoint found on gameobject: {buildingData.buildingEntrance.name}");
                return;
            }
            
            PlayerController.Instance.UpdateNPCPosition(buildingEntrance);
        }

        Instantiate(buildingData.buildingEntrance, Vector3.zero, Quaternion.identity);
    }
}
