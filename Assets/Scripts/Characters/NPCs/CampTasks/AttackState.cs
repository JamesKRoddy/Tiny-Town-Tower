using UnityEngine;
using UnityEngine.AI;

public class AttackState : _TaskState
{

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnEnterState()
    {
        if (agent == null)
        {
            agent = npc.GetAgent(); // Store reference to NavMeshAgent
        }

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
    
    /// <summary>
    /// Override stamina update for combat-specific enhanced drain
    /// Combat is very exhausting and drains stamina 2x faster than normal
    /// </summary>
    public override void UpdateStamina()
    {
        // Combat drains stamina 2x faster than normal activities (more than work)
        float combatDrain = npc.GetBaseStaminaDrainRate() * 2.0f * Time.deltaTime;
        npc.ApplyStaminaChange(-combatDrain, "Combat drain");
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
