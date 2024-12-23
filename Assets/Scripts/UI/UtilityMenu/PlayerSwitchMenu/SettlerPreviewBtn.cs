using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettlerPreviewBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        SettlerNPCMenu.Instance.UpdatePreview(npc);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SettlerNPCMenu.Instance.UpdatePreview();
    }

    public void OnSelect(BaseEventData eventData)
    {
        SettlerNPCMenu.Instance.UpdatePreview(npc);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SettlerNPCMenu.Instance.UpdatePreview();
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
        //TODO swap player controlls to npc, make it a child of the player, disable NPC stuff, exit current state, clear work, disable nav agent, enable CharacterAnimationEvents

        //npc.game
    }
}
