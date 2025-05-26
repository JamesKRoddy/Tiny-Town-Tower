using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    [System.Serializable]
    public class ConstructionSiteMapping
    {
        public Vector2Int gridSize;
        public GameObject constructionSitePrefab;
    }

    [System.Serializable]
    public class DestructionPrefabMapping
    {
        public Vector2Int gridSize;
        public GameObject destructionPrefab;
    }

    public class BuildManager : MonoBehaviour
    {
        [Header("Full list of Building Scriptable Objs")]
        public BuildingScriptableObj[] buildingScriptableObjs;

        [Header("Construction Sites")]
        [SerializeField] private List<ConstructionSiteMapping> constructionSiteMappings = new List<ConstructionSiteMapping>();

        [Header("Destruction Prefabs")]
        [SerializeField] private List<DestructionPrefabMapping> destructionPrefabMappings = new List<DestructionPrefabMapping>();

        public GameObject GetConstructionSitePrefab(Vector2Int size)
        {
            foreach (var mapping in constructionSiteMappings)
            {
                if (mapping.gridSize == size)
                {
                    return mapping.constructionSitePrefab;
                }
            }
            
            Debug.LogError($"No construction site prefab found for size {size.x}x{size.y}");
            return constructionSiteMappings.Count > 0 ? constructionSiteMappings[0].constructionSitePrefab : null;
        }

        public GameObject GetDestructionPrefab(Vector2Int size)
        {
            foreach (var mapping in destructionPrefabMappings)
            {
                if (mapping.gridSize == size)
                {
                    return mapping.destructionPrefab;
                }
            }
            
            Debug.LogError($"No destruction prefab found for size {size.x}x{size.y}");
            return destructionPrefabMappings.Count > 0 ? destructionPrefabMappings[0].destructionPrefab : null;
        }

        public void BuildingSelectionOptions(Building building){
            var options = new List<SelectionPopup.SelectionOption>();

            // Add Destroy Building option first
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Building Stats",
                onSelected = () => {
                    PlayerUIManager.Instance.DisplayTextPopup(building.GetBuildingStatsText());
                    PlayerUIManager.Instance.selectionPopup.OnCloseClicked();
                },
                workTask = null
            });

            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Assign Worker",
                onSelected = () => {
                    CampManager.Instance.WorkManager.buildingForAssignment = building;
                    PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(true);
                },
            });

            foreach(var workTask in building.GetComponents<WorkTask>()){
                if(workTask is QueuedWorkTask queuedTask && queuedTask.HasQueuedTasks){
                    options.Add(new SelectionPopup.SelectionOption
                    {
                        optionName = $"Work Queue: {workTask.GetType().Name.Replace("Task", "")}",
                        onSelected = () => PlayerUIManager.Instance.selectionPreviewList.Setup(building.GetCurrentWorkTask(), null)
                    });
                }
            }

            // Show the selection popup
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null);
        }
    }
}
