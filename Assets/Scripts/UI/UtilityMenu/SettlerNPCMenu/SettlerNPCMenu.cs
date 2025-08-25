using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SettlerNPCMenu : PreviewListMenuBase<string, HumanCharacterController>, IControllerInput
{
    // Filter function to determine which settlers should be shown
    private Func<SettlerNPC, bool> settlerFilter = null;
    
    // Custom click handler for special modes (like bed assignment)
    private Action<HumanCharacterController> customClickHandler = null;
    
    // Set a filter for which settlers to show (e.g., only available settlers for bed assignment)
    public void SetSettlerFilter(Func<SettlerNPC, bool> filter)
    {
        Debug.Log("[SettlerNPCMenu] SetSettlerFilter called");
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
        Debug.Log("[SettlerNPCMenu] SetCustomClickHandler called");
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
        Debug.Log("[SettlerNPCMenu] OnDisable called");
        ClearSettlerFilter();
        ClearCustomClickHandler();
    }
    
    // Retrieve all NPCs in the scene that inherit from HumanCharacterController
    public override IEnumerable<HumanCharacterController> GetItems()
    {
        Debug.Log("[SettlerNPCMenu] GetItems called");
        
        // First, get the robot if it exists
        var robot = FindFirstObjectByType<RobotCharacterController>();
        if (robot != null)
        {
            yield return robot;
        }

        // Then get all settler NPCs (filtered if a filter is set)
        var npcs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);
        Debug.Log($"[SettlerNPCMenu] Found {npcs.Length} settler NPCs, filter is {(settlerFilter == null ? "null" : "set")}");

        foreach (var npc in npcs)
        {
            // Apply filter if one is set
            if (settlerFilter == null || settlerFilter(npc))
            {
                Debug.Log($"[SettlerNPCMenu] Yielding NPC: {npc.SettlerName}");
                yield return npc;
            }
            else
            {
                Debug.Log($"[SettlerNPCMenu] Filtered out NPC: {npc.SettlerName}");
            }
        }
    }

    public override string GetItemCategory(HumanCharacterController item)
    {
        return "Default"; // Grouping not relevant; returning default category
    }

    public override void SetupItemButton(HumanCharacterController item, GameObject button)
    {
        var buttonComponent = button.GetComponent<SettlerPreviewBtn>();
        
        if (customClickHandler != null)
        {
            Debug.Log($"[SettlerNPCMenu] Setting up button with custom click handler for: {item.name}");
            if (item is RobotCharacterController robot)
            {
                buttonComponent.SetupButton(robot, customClickHandler, null, "Robot");
            }
            else if (item is SettlerNPC settler)
            {
                buttonComponent.SetupButton(settler, customClickHandler, null, settler.SettlerName);
            }
        }
        else
        {
            Debug.Log($"[SettlerNPCMenu] Setting up button with default behavior for: {item.name}");
            if (item is RobotCharacterController robot)
            {
                // Special setup for robot
                buttonComponent.SetupButton(robot);
            }
            else if (item is SettlerNPC settler)
            {
                buttonComponent.SetupButton(settler);
            }
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
