using Managers;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SceneTransitionTrigger : MonoBehaviour
{
    // Name of the scene to load when the possessed NPC enters this area.
    [SerializeField] protected SceneNames targetScene;
    [SerializeField] protected GameMode nextSceneGameMode;
    [SerializeField] protected bool keepPossessedNPC;

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Try to get an IPossessable component from the object entering the trigger.
        IPossessable npc = other.GetComponent<IPossessable>();
        if (npc != null && npc == PlayerController.Instance._possessedNPC)
        {
            if (nextSceneGameMode == GameMode.NONE)
            {
                Debug.LogWarning($"{gameObject.name} has no next game mode");
            }

            if(targetScene == SceneNames.NONE)
            {
                Debug.LogWarning($"{gameObject.name} has no next scene");
            }

            SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPossessedNPC, OnSceneLoaded);
        }
    }

    protected virtual void OnSceneLoaded()
    {
        
    }
}


