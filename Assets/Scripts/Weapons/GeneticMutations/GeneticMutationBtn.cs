using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneticMutationBtn : PreviewButtonBase<GeneticMutationObj>
{
    protected override void OnButtonClicked()
    {
        Debug.LogWarning("Genetic Mutation Btn not setup");
    }

    public void SetupButton(GeneticMutationObj mutation)
    {
        base.SetupButton(mutation, mutation.resourceSprite, mutation.resourceName);
    }
}
