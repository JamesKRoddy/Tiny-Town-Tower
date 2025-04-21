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
