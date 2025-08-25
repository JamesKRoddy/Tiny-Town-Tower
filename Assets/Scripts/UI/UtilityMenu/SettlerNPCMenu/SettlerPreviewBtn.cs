using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Characters;
using Managers;

public class SettlerPreviewBtn : PreviewButtonBase<HumanCharacterController>
{
    protected override void OnDefaultButtonClicked()
    {
        Debug.Log($"[SettlerPreviewBtn] OnDefaultButtonClicked called for: {data?.name}");
        
        if (data is RobotCharacterController robot)
        {
            Debug.Log("[SettlerPreviewBtn] Robot clicked - possessing robot");
            // Immediately possess the robot
            PlayerController.Instance.PossessNPC(robot);
            PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false);
            PlayerUIManager.Instance.utilityMenu.ReturnToGame(PlayerControlType.ROBOT_MOVEMENT);
        }
        else if (data is SettlerNPC settler)
        {
            Debug.Log($"[SettlerPreviewBtn] Settler NPC clicked: {settler.name}");
            
            // Check if we're in building assignment mode
            var buildingForAssignment = CampManager.Instance.WorkManager.buildingForAssignment;
            Debug.Log($"[SettlerPreviewBtn] buildingForAssignment: {buildingForAssignment?.ToString() ?? "null"}");
            
            if (buildingForAssignment != null)
            {
                Debug.Log($"[SettlerPreviewBtn] Building assignment mode detected - bypassing popup for building: {buildingForAssignment}");
                // Directly assign work without showing the popup
                PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false, 0.05f);
                CampManager.Instance.WorkManager.SetNPCForAssignment(settler);
                
                // Handle both IPlaceableStructure (buildings/turrets) and WorkTask (construction sites)
                if (buildingForAssignment is IPlaceableStructure structure)
                {
                    Debug.Log($"[SettlerPreviewBtn] Showing work task options for IPlaceableStructure: {structure}");
                    CampManager.Instance.WorkManager.ShowWorkTaskOptions(structure, settler, (task) => {
                        CampManager.Instance.WorkManager.AssignWorkToBuilding(task);
                    });
                }
                else if (buildingForAssignment is StructureConstructionTask constructionTask)
                {
                    Debug.Log($"[SettlerPreviewBtn] Showing work task options for StructureConstructionTask: {constructionTask}");
                    CampManager.Instance.WorkManager.ShowWorkTaskOptions(constructionTask, settler, (task) => {
                        CampManager.Instance.WorkManager.AssignWorkToBuilding(task);
                    });
                }
                else
                {
                    Debug.LogWarning($"[SettlerPreviewBtn] Unknown buildingForAssignment type: {buildingForAssignment.GetType()}");
                }
            }
            else
            {
                Debug.Log("[SettlerPreviewBtn] Normal mode - showing NPC popup");
                // Show popup for settler NPCs (normal behavior)
                PlayerUIManager.Instance.settlerNPCMenu.DisplayPopup(data, gameObject);
            }
        }
        else
        {
            Debug.LogWarning($"[SettlerPreviewBtn] Unknown data type: {data?.GetType()}");
        }
    }

    public void SetupButton(HumanCharacterController character)
    {
        if (character is RobotCharacterController robot)
        {
            nameText.text = "Robot";
            base.SetupButton(robot, null, "Robot");
        }
        else if (character is SettlerNPC settler)
        {
            nameText.text = settler.SettlerName;
            base.SetupButton(settler, null, settler.SettlerName);
        }
    }
}

