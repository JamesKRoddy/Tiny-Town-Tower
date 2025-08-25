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
                    
                    // Clear any filters and show all settlers for regular work assignment
                    var settlerMenu = PlayerUIManager.Instance.settlerNPCMenu;
                    settlerMenu.ClearSettlerFilter();
                    settlerMenu.SetScreenActive(true);
                },
            });

            foreach(var workTask in building.GetComponents<WorkTask>()){
                // Handle SleepTask specially - show bed assignment options
                if(workTask is SleepTask sleepTask){
                    options.Add(new SelectionPopup.SelectionOption
                    {
                        optionName = "Assign Settler to Bed",
                        onSelected = () => {                            
                            // Set the building for assignment and open settler menu with filter
                            CampManager.Instance.WorkManager.buildingForAssignment = building;
                            
                            // Set filter to only show settlers without bed assignments
                            var settlerMenu = PlayerUIManager.Instance.settlerNPCMenu;
                            if (settlerMenu == null)
                            {
                                Debug.LogError("[BuildManager] settlerNPCMenu is null!");
                                return;
                            }
                            
                            // Temporarily show all settlers for debugging
                            settlerMenu.SetSettlerFilter(settler => {
                                bool hasBed = settler.HasAssignedBed();
                                return !hasBed; // Show settlers without beds
                            });
                            
                            // Set custom click handler for bed assignment
                            settlerMenu.SetCustomClickHandler(selectedCharacter => {
                                
                                if (selectedCharacter is SettlerNPC selectedSettler)
                                {
                                    // Get the sleep task from the building
                                    var sleepTask = building.GetComponent<SleepTask>();
                                    if (sleepTask != null)
                                    {
                                        sleepTask.AssignSettlerToBed(selectedSettler);
                                        
                                        // Close the menu
                                        settlerMenu.SetScreenActive(false);
                                        
                                        // Clear the building assignment
                                        CampManager.Instance.WorkManager.buildingForAssignment = null;

                                        // Return to game control type
                                        PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
                                        
                                    }
                                    else
                                    {
                                        Debug.LogError($"[BuildManager] No SleepTask found on building: {building.name}");
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"[BuildManager] Selected character is not a SettlerNPC: {selectedCharacter.GetType()}");
                                }
                            });
                            
                            // Open the settler menu directly using the menu's SetScreenActive method
                            settlerMenu.SetScreenActive(true);
                        },
                        workTask = workTask
                    });
                }
                else if(workTask is QueuedWorkTask queuedTask && queuedTask.HasQueuedTasks){
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

            // Show the selection popup in assignment mode (so "Assign Worker" doesn't clear assignments)
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null, true);
        }

        public void TurretSelectionOptions(BaseTurret turret){
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
                    
                    // Clear any filters and show all settlers for regular work assignment
                    var settlerMenu = PlayerUIManager.Instance.settlerNPCMenu;
                    settlerMenu.ClearSettlerFilter();
                    settlerMenu.SetScreenActive(true);
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

            // Show the selection popup in assignment mode
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null, true);
        }

        public void ShowConstructionSiteSelectionOptions(StructureConstructionTask constructionTask)
        {
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
                    
                    // Clear any filters and show all settlers for regular work assignment
                    var settlerMenu = PlayerUIManager.Instance.settlerNPCMenu;
                    settlerMenu.ClearSettlerFilter();
                    settlerMenu.SetScreenActive(true);
                },
            });

            // Show the selection popup in assignment mode
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null, true);
        }

        private string GetConstructionSiteInfoText(StructureConstructionTask constructionTask)
        {
            string info = $"Construction Site\n";
            info += $"Building: {constructionTask.GetInteractionText()}\n";
            info += $"Progress: {(constructionTask.GetProgress() * 100):F1}%\n";
            info += $"Workers: {(constructionTask.IsOccupied ? "Active" : "None")}\n";
            return info;
        }
        
        /// <summary>
        /// Show bed assignment options for SleepTask
        /// </summary>
        /// <param name="building">The building containing the bed</param>
        /// <param name="sleepTask">The SleepTask component</param>
        private void ShowBedAssignmentOptions(Building building, SleepTask sleepTask)
        {
            var options = new List<SelectionPopup.SelectionOption>();
            
            // Get current bed assignment status
            bool isBedAssigned = sleepTask.IsBedAssigned;
            SettlerNPC assignedSettler = sleepTask.AssignedSettler;
            
            // Show current assignment status
            if (isBedAssigned && assignedSettler != null)
            {
                options.Add(new SelectionPopup.SelectionOption
                {
                    optionName = $"Currently Assigned to: {assignedSettler.name}",
                    onSelected = () => {
                        // Show unassign option
                        ShowBedUnassignOptions(building, sleepTask, assignedSettler);
                    },
                    customTooltip = $"This bed is currently assigned to {assignedSettler.name}"
                });
            }
            else
            {
                options.Add(new SelectionPopup.SelectionOption
                {
                    optionName = "Assign Settler to Bed",
                    onSelected = () => {
                        // Set the building for assignment and open settler menu with filter
                        CampManager.Instance.WorkManager.buildingForAssignment = building;
                        
                        // Set filter to only show settlers without bed assignments
                        var settlerMenu = PlayerUIManager.Instance.settlerNPCMenu;
                        if (settlerMenu == null)
                        {
                            Debug.LogError("[BuildManager] settlerNPCMenu is null!");
                            return;
                        }
                        
                        // Temporarily show all settlers for debugging
                        settlerMenu.SetSettlerFilter(settler => {
                            bool hasBed = settler.HasAssignedBed();
                            return !hasBed; // Show settlers without beds
                        });
                        
                        // Open the settler menu directly using the menu's SetScreenActive method
                        settlerMenu.SetScreenActive(true);
                    },
                    customTooltip = "Select a settler to assign to this bed"
                });
            }
            
            // Show the bed assignment popup
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null, true);
        }
        

        
        /// <summary>
        /// Show bed unassignment options
        /// </summary>
        private void ShowBedUnassignOptions(Building building, SleepTask sleepTask, SettlerNPC assignedSettler)
        {
            var options = new List<SelectionPopup.SelectionOption>();
            
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = $"Unassign {assignedSettler.name}",
                onSelected = () => {
                    // Unassign the settler
                    sleepTask.UnassignSettlerFromBed();
                    
                    // Close the popup
                    PlayerUIManager.Instance.selectionPopup.OnCloseClicked();
                },
                customTooltip = $"Remove {assignedSettler.name}'s assignment to this bed"
            });
            
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Cancel",
                onSelected = () => {
                    // Close the popup
                    PlayerUIManager.Instance.selectionPopup.OnCloseClicked();
                }
            });
            
            // Show the unassign options popup
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null, true);
        }
    }
}
