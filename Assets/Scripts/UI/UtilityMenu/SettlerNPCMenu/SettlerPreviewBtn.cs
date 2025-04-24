using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettlerPreviewBtn : PreviewButtonBase<SettlerNPC>
{
    protected override void OnButtonClicked()
    {
        PlayerUIManager.Instance.settlerNPCMenu.DisplayPopup(data, gameObject);
    }

    public void SetupButton(SettlerNPC settlerNPC)
    {
        base.SetupButton(settlerNPC, null, settlerNPC?.nPCDataObj.nPCName ?? "Unknown");
    }
}

