using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Characters;

public class SettlerPreviewBtn : PreviewButtonBase<HumanCharacterController>
{
    protected override void OnButtonClicked()
    {
        PlayerUIManager.Instance.settlerNPCMenu.DisplayPopup(data, gameObject);
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

