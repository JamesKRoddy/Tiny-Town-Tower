using UnityEngine;

public class GeneticMutationBtn : PreviewButtonBase<GeneticMutationObj>
{
    protected override void OnButtonClicked()
    {
        if (data == null)
        {
            Debug.LogWarning("Genetic Mutation Btn clicked, but item is null.");
            return;
        }

        Debug.Log($"Selected Mutation: {data.objectName}");

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

    public void SetupButton(GeneticMutationObj mutation)
    {
        base.SetupButton(mutation, mutation.sprite, mutation.objectName);
    }
}
