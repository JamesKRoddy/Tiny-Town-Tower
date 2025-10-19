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
    
    // Prevent multiple scene loads from the same trigger
    protected bool hasTriggeredTransition = false;

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Prevent multiple scene loads
        if (hasTriggeredTransition)
        {
            return;
        }
        
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

            hasTriggeredTransition = true;
            SceneTransitionManager.Instance.LoadScene(targetScene, nextSceneGameMode, keepPossessedNPC, OnSceneLoaded);
        }
    }

    protected virtual void OnSceneLoaded()
    {
        
    }
    
    // Reset the trigger when the object is enabled (for reusability)
    protected virtual void OnEnable()
    {
        hasTriggeredTransition = false;
    }
}


