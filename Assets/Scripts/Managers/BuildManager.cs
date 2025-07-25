using UnityEngine;
using System.Collections.Generic;
using CampBuilding;

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

        [Header("Full list of Turret Scriptable Objs")]
        public TurretScriptableObject[] turretScriptableObjs;

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

            // Add Building Stats option (now shows as tooltip)
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Building Stats",
                onSelected = () => {
                    // Do nothing - stats are shown as tooltip
                },
                customTooltip = building.GetBuildingStatsText()
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
                        onSelected = () => {
                            PlayerUIManager.Instance.selectionPreviewList.Setup(building.GetCurrentWorkTask(), null);
                            PlayerUIManager.Instance.selectionPreviewList.SetScreenActive(true);
                        },
                        workTask = workTask
                    });
                }
            }

            // Show the selection popup
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null);
        }

        public void TurretSelectionOptions(BaseTurret turret){
            Debug.Log($"TurretSelectionOptions called for turret: {turret.name}");
            var options = new List<SelectionPopup.SelectionOption>();

            // Add Turret Stats option (now shows as tooltip)
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Turret Stats",
                onSelected = () => {
                    // Do nothing - stats are shown as tooltip
                },
                customTooltip = turret.GetTurretStatsText()
            });

            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Assign Worker",
                onSelected = () => {
                    CampManager.Instance.WorkManager.buildingForAssignment = turret;
                    PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(true);
                },
            });

            foreach(var workTask in turret.GetComponents<WorkTask>()){
                if(workTask is QueuedWorkTask queuedTask && queuedTask.HasQueuedTasks){
                    options.Add(new SelectionPopup.SelectionOption
                    {
                        optionName = $"Work Queue: {workTask.GetType().Name.Replace("Task", "")}",
                        onSelected = () => {
                            PlayerUIManager.Instance.selectionPreviewList.Setup(turret.GetCurrentWorkTask(), null);
                            PlayerUIManager.Instance.selectionPreviewList.SetScreenActive(true);
                        },
                        workTask = workTask
                    });
                }
            }

            Debug.Log($"Created {options.Count} options for turret selection");
            // Show the selection popup
            Debug.Log($"Setting up selection popup with {options.Count} options");
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null);
        }

        public void ShowConstructionSiteSelectionOptions(StructureConstructionTask constructionTask)
        {
            Debug.Log($"ShowConstructionSiteSelectionOptions called for construction site: {constructionTask.name}");
            var options = new List<SelectionPopup.SelectionOption>();

            // Add Construction Stats option
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Construction Info",
                onSelected = () => {
                    // Do nothing - info is shown as tooltip
                },
                customTooltip = GetConstructionSiteInfoText(constructionTask)
            });

            // Add Assign Worker option
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Assign Worker",
                onSelected = () => {
                    CampManager.Instance.WorkManager.buildingForAssignment = constructionTask;
                    PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(true);
                },
            });

            Debug.Log($"Created {options.Count} options for construction site selection");
            // Show the selection popup
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null);
        }

        private string GetConstructionSiteInfoText(StructureConstructionTask constructionTask)
        {
            string info = $"Construction Site\n";
            info += $"Building: {constructionTask.GetInteractionText()}\n";
            info += $"Progress: {(constructionTask.GetProgress() * 100):F1}%\n";
            info += $"Workers: {(constructionTask.IsOccupied ? "Active" : "None")}\n";
            return info;
        }
    }
}
