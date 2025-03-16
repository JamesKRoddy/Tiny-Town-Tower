using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneticMutationBtn : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;
    [SerializeField] private GeneticMutationData geneticMutationData;

    void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    public void SetupButton(GeneticMutationData mutation)
    {
        geneticMutationData = mutation;

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        Debug.LogWarning("Genetic Mutation Btn not setup");
    }
}
