using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Characters;

public class SettlerPreviewBtn : PreviewButtonBase<HumanCharacterController>
{
    protected override void OnButtonClicked()
    {
        if (data is RobotCharacterController robot)
        {
            // Immediately possess the robot
            PlayerController.Instance.PossessNPC(robot);
            PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false);
            PlayerUIManager.Instance.utilityMenu.ReturnToGame();
        }
        else if (data is SettlerNPC)
        {
            // Show popup for settler NPCs
            PlayerUIManager.Instance.settlerNPCMenu.DisplayPopup(data, gameObject);
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
            nameText.text = settler.nPCDataObj.nPCName;
            base.SetupButton(settler, null, settler.nPCDataObj.nPCName);
        }
    }
}

