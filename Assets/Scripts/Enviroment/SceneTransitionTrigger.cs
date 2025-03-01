using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    // Name of the scene to load when the possessed NPC enters this area.
    [SerializeField] private string targetScene;
    [SerializeField] GameMode nextSceneGameMode;
    [SerializeField] bool keepPlayerControls;
    [SerializeField] bool keepPossessedNPC;

    private void OnTriggerEnter(Collider other)
    {
        // Try to get an IPossessable component from the object entering the trigger.
        IPossessable npc = other.GetComponent<IPossessable>();
        if (npc != null && npc == PlayerController.Instance._possessedNPC)
        {
            SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPlayerControls, keepPossessedNPC);
        }
    }
}


