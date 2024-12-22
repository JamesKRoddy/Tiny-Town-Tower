using UnityEngine;
using UnityEngine.AI;

public class AttackState : _TaskState
{
    private NavMeshAgent agent;

    private void Awake()
    {
        // Ensure NPC reference is set when the state starts
        if (npc == null)
        {
            SetNPCReference(GetComponent<SettlerNPC>());
        }

        agent = npc.GetAgent(); // Store reference to NavMeshAgent
    }

    public override void OnEnterState()
    {
        Debug.Log("Starting Attack task");
    }

    public override void OnExitState()
    {
        Debug.Log("Exiting Attack task");
    }

    public override void UpdateState()
    {
        Debug.Log("Attacking...");
        // Attack logic can be added here, like moving towards enemy or dealing damage
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed; // Full speed while attacking (could be adjusted for combat)
    }

    public override TaskType GetTaskType()
    {
        return TaskType.ATTACK;
    }
}
