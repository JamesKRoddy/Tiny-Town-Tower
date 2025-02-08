using UnityEngine;

public class NarrativeInteractive : MonoBehaviour, IInteractive<NarrativeAsset>
{
    [SerializeField] NarrativeAsset narrativeAsset;

    public string GetInteractionText() => "Start Conversation";

    public bool CanInteract() => true;

    public NarrativeAsset Interact() {
        return narrativeAsset;
    }

    object IInteractiveBase.Interact() => Interact();
}

[System.Serializable]
public class NarrativeAsset
{
    public TextAsset dialogueFile;
}
