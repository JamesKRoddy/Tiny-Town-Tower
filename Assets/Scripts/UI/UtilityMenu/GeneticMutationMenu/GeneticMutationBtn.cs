using UnityEngine;
using TMPro;

public class GeneticMutationBtn : PreviewButtonBase<GeneticMutationObj>
{
    [SerializeField] private TextMeshProUGUI quantityText;

    protected override void OnDefaultButtonClicked()
    {
        if (data == null)
        {
            Debug.LogWarning("Genetic Mutation Btn clicked, but item is null.");
            return;
        }

        // Activate mutation grid and allow player to move mutation
        if (PlayerUIManager.Instance.geneticMutationMenu != null)
        {
            // Update preview
            PlayerUIManager.Instance.geneticMutationMenu.UpdatePreview(data);

            // Select mutation for placement
            PlayerUIManager.Instance.geneticMutationMenu.SelectMutation(data);

            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.GENETIC_MUTATION_MOVEMENT);
        }
    }

    public void SetupButton(GeneticMutationObj mutation, int quantity)
    {
        base.SetupButton(mutation, mutation.sprite, mutation.objectName);
        
        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
            quantityText.gameObject.SetActive(quantity > 0);
        }
    }
}
