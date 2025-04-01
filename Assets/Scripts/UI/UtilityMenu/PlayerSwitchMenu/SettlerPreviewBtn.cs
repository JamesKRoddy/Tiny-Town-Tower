using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettlerPreviewBtn : PreviewButtonBase<SettlerNPC>
{
    protected override void OnButtonClicked()
    {
        PlayerController.Instance.PossessNPC(data);
        PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false, 0.05f);
        PlayerUIManager.Instance.utilityMenu.ReturnToGame(PlayerControlType.CAMP_NPC_MOVEMENT);
    }

    public void SetupButton(SettlerNPC settlerNPC)
    {
        base.SetupButton(settlerNPC, null, settlerNPC?.nPCDataObj.nPCName ?? "Unknown");
    }
}

