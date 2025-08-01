using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SettlerNPCMenu : PreviewListMenuBase<string, HumanCharacterController>, IControllerInput
{
    // Retrieve all NPCs in the scene that inherit from HumanCharacterController
    public override IEnumerable<HumanCharacterController> GetItems()
    {
        // First, get the robot if it exists
        var robot = FindFirstObjectByType<RobotCharacterController>();
        if (robot != null)
        {
            yield return robot;
        }

        // Then get all settler NPCs
        var npcs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);

        foreach (var npc in npcs)
            {
                yield return npc;
        }
    }

    public override string GetItemCategory(HumanCharacterController item)
    {
        return "Default"; // Grouping not relevant; returning default category
    }

    public override void SetupItemButton(HumanCharacterController item, GameObject button)
    {
        var buttonComponent = button.GetComponent<SettlerPreviewBtn>();
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
