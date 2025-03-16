using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettlerPreviewBtn : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    //[SerializeField] private TMP_Text ageText;
    //[SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;

    private SettlerNPC npc;

    void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    public void SetupButton(SettlerNPC settlerNPC)
    {
        npc = settlerNPC;

        if (npc != null)
        {
            nameText.text = npc.nPCDataObj.nPCName;
            //ageText.text = $"Age: {npcData.nPCAge}";
            //descriptionText.text = npcData.nPCDescription;
        }
        else
        {
            nameText.text = "Unknown";
            //ageText.text = "Age: N/A";
            //descriptionText.text = "No details available.";
        }

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        PlayerController.Instance.PossessNPC(npc);

        SettlerNPCMenu.Instance.SetScreenActive(false, 0.05f);
        PlayerUIManager.Instance.utilityMenu.ReturnToGame(PlayerControlType.CAMP_NPC_MOVEMENT); //Doing this to reset the player controlls
    }

    
}
