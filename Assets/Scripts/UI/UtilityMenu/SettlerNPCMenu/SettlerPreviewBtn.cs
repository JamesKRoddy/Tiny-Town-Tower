using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Characters;

public class SettlerPreviewBtn : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    public void SetupButton(HumanCharacterController character)
    {
        if (character is RobotCharacterController robot)
        {
            nameText.text = "Robot";
            button.onClick.AddListener(() => OnRobotSelected(robot));
        }
        else if (character is SettlerNPC settler)
        {
            nameText.text = settler.nPCDataObj.nPCName;
            button.onClick.AddListener(() => OnSettlerSelected(settler));
        }
    }

    private void OnRobotSelected(RobotCharacterController robot)
    {
        PlayerController.Instance.PossessNPC(robot);
        PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false);
    }

    private void OnSettlerSelected(SettlerNPC settler)
    {
        PlayerController.Instance.PossessNPC(settler);
        PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false);
    }
}

