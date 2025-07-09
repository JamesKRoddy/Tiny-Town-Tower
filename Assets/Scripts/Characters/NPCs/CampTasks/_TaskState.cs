using UnityEngine;
using UnityEngine.AI;

public abstract class _TaskState : MonoBehaviour
{
    protected SettlerNPC npc;
    protected NavMeshAgent agent;
    protected float stoppingDistance = 1.5f;
    protected Animator animator;

    protected virtual void Awake()
    {
        // Ensure NPC reference is set when the state starts
        if (npc == null)
        {
            SetNPCReference(GetComponent<SettlerNPC>());
        }
    }

    // This method will be called to set the NPC reference once the state is added to the NPC
    public void SetNPCReference(SettlerNPC npc)
    {
        this.npc = npc;
        UpdateReferences();
    }

    // Update component references
    protected void UpdateReferences()
    {
        if (npc != null)
        {
            agent = npc.GetAgent();
            animator = npc.GetAnimator();
        }
        else
        {
            Debug.LogWarning($"NPC reference is null in {gameObject.name}'s {GetType().Name}");
        }
    }

    /// <summary>
    /// Get the effective stopping distance for this task state using shared navigation utilities
    /// </summary>
    /// <param name="target">The target transform (can be null for general stopping distance)</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>The effective stopping distance</returns>
    protected float GetEffectiveStoppingDistance(Transform target = null, float obstacleBoundsOffset = 0.5f)
    {
        if (agent == null) return stoppingDistance;
        
        return NavigationUtils.GetEffectiveStoppingDistance(agent, target, stoppingDistance, obstacleBoundsOffset);
    }

    /// <summary>
    /// Check if the agent has reached its destination using shared navigation utilities
    /// </summary>
    /// <param name="target">The target transform (can be null for general destination checking)</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>True if the agent has reached the destination</returns>
    protected bool HasReachedDestination(Transform target = null, float obstacleBoundsOffset = 0.5f)
    {
        if (agent == null) return false;
        
        return NavigationUtils.HasReachedDestination(agent, target, stoppingDistance, obstacleBoundsOffset);
    }

    public abstract TaskType GetTaskType();

    // Called when the state is entered
    public abstract void OnEnterState();

    // Called when the state is exited
    public abstract void OnExitState();

    // Called every frame to update the state
    public abstract void UpdateState();

    // Max speed for the NPC in this state
    public virtual float MaxSpeed() { return npc.moveMaxSpeed; }
}
