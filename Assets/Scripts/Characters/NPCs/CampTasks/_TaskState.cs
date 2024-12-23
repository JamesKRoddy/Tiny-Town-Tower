using UnityEngine;
using UnityEngine.AI;

public abstract class _TaskState : MonoBehaviour
{
    protected SettlerNPC npc;
    protected NavMeshAgent agent;

    // This method will be called to set the NPC reference once the state is added to the NPC
    public void SetNPCReference(SettlerNPC npc)
    {
        this.npc = npc;
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
