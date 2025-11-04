using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Managers;

public class SettlerNPCMenu : PreviewListMenuBase<string, HumanCharacterController>, IControllerInput
{
    // Filter function to determine which settlers should be shown
    private Func<SettlerNPC, bool> settlerFilter = null;
    
    // Custom click handler for special modes (like bed assignment)
    private Action<HumanCharacterController> customClickHandler = null;
    
    // Set a filter for which settlers to show (e.g., only available settlers for bed assignment)
    public void SetSettlerFilter(Func<SettlerNPC, bool> filter)
    {
        settlerFilter = filter;
    }
    
    // Clear the filter to show all settlers
    public void ClearSettlerFilter()
    {
        settlerFilter = null;
    }
    
    // Set a custom click handler for special modes (like bed assignment)
    public void SetCustomClickHandler(Action<HumanCharacterController> handler)
    {
        customClickHandler = handler;
    }
    
    // Clear the custom click handler to return to normal behavior
    public void ClearCustomClickHandler()
    {
        customClickHandler = null;
    }
    
    // Clear filter and custom click handler when menu is disabled
    private void OnDisable()
    {
        ClearSettlerFilter();
        ClearCustomClickHandler();
    }
    
    // Retrieve all NPCs in the scene that inherit from HumanCharacterController
    public override IEnumerable<HumanCharacterController> GetItems()
    {
        
        // First, get the robot if it exists
        var robot = FindFirstObjectByType<RobotCharacterController>();
        if (robot != null)
        {
            yield return robot;
        }

        // Then get all settler NPCs (filtered if a filter is set)
        var npcs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);

        foreach (var npc in npcs)
        {
            // Apply filter if one is set
            if (settlerFilter == null || settlerFilter(npc))
            {
                yield return npc;
            }
        }
    }

    public override string GetItemCategory(HumanCharacterController item)
    {
        return "Default"; // Grouping not relevant; returning default category
    }

    public override void SetupItemButton(HumanCharacterController item, GameObject button)
    {
        var buttonComponent = button.GetComponent<PreviewButtonBase>();
        if (buttonComponent == null) return;
        
        if (customClickHandler != null)
        {
            if (item is RobotCharacterController robot)
            {
                buttonComponent.SetupButton(robot, (obj) => customClickHandler(obj as HumanCharacterController), null, "Robot");
            }
            else if (item is SettlerNPC settler)
            {
                buttonComponent.SetupButton(settler, (obj) => customClickHandler(obj as HumanCharacterController), null, settler.SettlerName);
            }
        }
        else
        {
            // Capture button in closure for click handler
            if (item is RobotCharacterController robot)
            {
                buttonComponent.SetupButton(robot, (obj) => OnCharacterClicked(obj as HumanCharacterController, button), null, "Robot");
            }
            else if (item is SettlerNPC settler)
            {
                buttonComponent.SetupButton(settler, (obj) => OnCharacterClicked(obj as HumanCharacterController, button), null, settler.SettlerName);
            }
        }
    }

    private void OnCharacterClicked(HumanCharacterController character, GameObject buttonObj)
    {
        Debug.Log($"[SettlerNPCMenu] OnCharacterClicked called for: {character?.name}");
        
        if (character is RobotCharacterController robot)
        {
            // Check if camp is under attack before possessing robot
            if (CampManager.Instance != null && CampManager.Instance.IsCampUnderAttack)
            {
                Debug.Log("[SettlerNPCMenu] Cannot possess robot - camp is under attack!");
                PlayerUIManager.Instance.DisplayNotification("Cannot possess characters during an attack!");
                return;
            }
            
            Debug.Log("[SettlerNPCMenu] Robot clicked - possessing robot");
            // Immediately possess the robot
            PlayerController.Instance.PossessNPC(robot);
            SetScreenActive(false);
            PlayerUIManager.Instance.utilityMenu.ReturnToGame(PlayerControlType.ROBOT_MOVEMENT);
        }
        else if (character is SettlerNPC settler)
        {
            Debug.Log($"[SettlerNPCMenu] Settler NPC clicked: {settler.name}");
            
            // Check if we're in building assignment mode
            var buildingForAssignment = CampManager.Instance.WorkManager.buildingForAssignment;
            Debug.Log($"[SettlerNPCMenu] buildingForAssignment: {buildingForAssignment?.ToString() ?? "null"}");
            
            if (buildingForAssignment != null)
            {
                Debug.Log($"[SettlerNPCMenu] Building assignment mode detected - bypassing popup for building: {buildingForAssignment}");
                // Directly assign work without showing the popup
                SetScreenActive(false, 0.05f);
                CampManager.Instance.WorkManager.SetNPCForAssignment(settler);
                
                // Handle both IPlaceableStructure (buildings/turrets) and WorkTask (construction sites)
                if (buildingForAssignment is IPlaceableStructure structure)
                {
                    Debug.Log($"[SettlerNPCMenu] Showing work task options for IPlaceableStructure: {structure}");
                    CampManager.Instance.WorkManager.ShowWorkTaskOptions(structure, settler, (task) => {
                        CampManager.Instance.WorkManager.AssignWorkToBuilding(task);
                    });
                }
                else if (buildingForAssignment is StructureConstructionTask constructionTask)
                {
                    Debug.Log($"[SettlerNPCMenu] Showing work task options for StructureConstructionTask: {constructionTask}");
                    CampManager.Instance.WorkManager.ShowWorkTaskOptions(constructionTask, settler, (task) => {
                        CampManager.Instance.WorkManager.AssignWorkToBuilding(task);
                    });
                }
                else
                {
                    Debug.LogWarning($"[SettlerNPCMenu] Unknown buildingForAssignment type: {buildingForAssignment.GetType()}");
                }
            }
            else
            {
                Debug.Log("[SettlerNPCMenu] Normal mode - showing NPC popup");
                // Show popup for settler NPCs (normal behavior)
                DisplayPopup(character, buttonObj);
            }
        }
        else
        {
            Debug.LogWarning($"[SettlerNPCMenu] Unknown data type: {character?.GetType()}");
        }
    }

    public override string GetPreviewName(HumanCharacterController item)
    {
        if (item is RobotCharacterController robot)
        {
            return "Robot";
        }
        else if (item is SettlerNPC settler)
    {
            return settler.SettlerName;
        }
        return string.Empty;
    }

    public override Sprite GetPreviewSprite(HumanCharacterController item)
    {
        return null; // Assuming no sprite for NPCs; modify if sprites exist
    }

    public override string GetPreviewDescription(HumanCharacterController item)
    {
        if (item is RobotCharacterController robot)
        {
            return "A versatile robot that can perform various tasks.";
        }
        else if (item is SettlerNPC settler)
        {
            string baseDescription = settler.SettlerDescription;
            
            // Add stats and status information
            baseDescription += "\n\nStats & Status:";
            
            // Health status
            var healthStatus = settler.GetHealthStatus();
            string healthStatusText = GetHealthStatusText(healthStatus);
            baseDescription += $"\n• Health: {healthStatusText}";
            
            // Stamina
            baseDescription += $"\n• Stamina: {settler.currentStamina:F0}/{settler.maxStamina:F0}";
            
            // Hunger (if hunger system is enabled)
            if (!CampDebugMenu.DisableHungerSystem)
            {
                string hungerStatus = GetHungerStatusText(settler);
                baseDescription += $"\n• Hunger: {hungerStatus}";
            }
            
            // Work status
            string workStatus = GetWorkStatusText(settler);
            baseDescription += $"\n• Work Status: {workStatus}";
            
            // Current task
            string currentTask = GetCurrentTaskText(settler);
            baseDescription += $"\n• Current Task: {currentTask}";
            
            // Add characteristics descriptions if any exist
            if (settler.characteristicSystem != null && settler.characteristicSystem.EquippedCharacteristics.Count > 0)
            {
                baseDescription += "\n\nCharacteristics:";
                foreach (var characteristic in settler.characteristicSystem.EquippedCharacteristics)
                {
                    if (characteristic != null && !string.IsNullOrEmpty(characteristic.CharicteristicDescription()))
                    {
                        baseDescription += $"\n• {characteristic.CharicteristicDescription()}";
                    }
                }
            }
            
            return baseDescription;
        }
        return string.Empty;
    }
    
    private string GetHealthStatusText(HealthStatus healthStatus)
    {
        switch (healthStatus)
        {
            case HealthStatus.Healthy:
                return "Healthy";
            case HealthStatus.Hungry:
                return "Hungry";
            case HealthStatus.Starving:
                return "Starving";
            case HealthStatus.Tired:
                return "Tired";
            case HealthStatus.Exhausted:
                return "Exhausted";
            case HealthStatus.Sick:
                return "Sick";
            default:
                return "Unknown";
        }
    }
    
    private string GetHungerStatusText(SettlerNPC settler)
    {
        if (settler.IsStarving())
            return "Starving";
        else if (settler.IsHungry())
            return "Hungry";
        else
            return "Well Fed";
    }
    
    private string GetWorkStatusText(SettlerNPC settler)
    {
        if (settler.HasAssignedWorkTask)
        {
            var workTask = settler.GetAssignedWork();
            if (workTask != null)
            {
                string taskName = workTask.GetType().Name.Replace("Task", "");
                if (settler.IsOnBreak)
                    return $"On Break from {taskName}";
                else
                    return $"Assigned to {taskName}";
            }
        }
        return "No Work Assignment";
    }
    
    private string GetCurrentTaskText(SettlerNPC settler)
    {
        var currentTask = settler.GetCurrentTaskType();
        switch (currentTask)
        {
            case TaskType.WORK:
                return "Working";
            case TaskType.WANDER:
                return "Wandering";
            case TaskType.EAT:
                return "Eating";
            case TaskType.SLEEP:
                return "Sleeping";
            case TaskType.ATTACK:
                return "Attacking";
            case TaskType.FLEE:
                return "Fleeing";
            case TaskType.SHELTERED:
                return "Sheltered";
            case TaskType.MEDICAL_TREATMENT:
                return "Receiving Medical Treatment";
            default:
                return "Idle";
        }
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(HumanCharacterController item)
    {
        yield break; // No resource costs for NPCs
    }

    public override void UpdatePreviewSpecifics(HumanCharacterController item)
    {
        //Debug.Log($"Displaying details for NPC: {GetPreviewName(item)}");
    }

    public override void DestroyPreviewSpecifics()
    {
        
    }
}
