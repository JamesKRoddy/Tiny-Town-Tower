using UnityEngine;
using UnityEngine.AI;
using Managers;

/// <summary>
/// State for when an NPC is sheltered in a bunker. Disables movement and AI.
/// </summary>
public class ShelteredState : _TaskState
{
    private BunkerBuilding currentBunker;

    public void SetBunker(BunkerBuilding bunker)
    {
        currentBunker = bunker;
    }

    public override TaskType GetTaskType()
    {
        return TaskType.SHELTERED;
    }

    public override void OnEnterState()
    {
        // Disable movement and AI
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
        // Optionally: Hide NPC visuals if not already handled by BunkerBuilding
        // npc.gameObject.SetActive(false); // Usually handled by BunkerBuilding
    }

    public override void OnExitState()
    {
        // Re-enable movement and AI
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }
        if (animator != null)
        {
            // No need to set IsSheltered since that parameter doesn't exist
        }
        // Optionally: Show NPC visuals if not already handled by BunkerBuilding
        // npc.gameObject.SetActive(true); // Usually handled by BunkerBuilding
        currentBunker = null;
    }

    public override void UpdateState()
    {
        // Do nothing while sheltered - stamina is handled by SettlerNPC.Update()
    }
    
    /// <summary>
    /// Override stamina update for sheltered-specific behavior
    /// Being sheltered in a bunker provides safety with no stamina change (not sleep, just safety)
    /// </summary>
    public override void UpdateStamina()
    {
        // Sheltered NPCs have no stamina drain (safe, but not sleeping)
        // They don't lose stamina while hidden, but they don't gain any either
        // This represents being safe and resting, but not actual sleep
        npc.ApplyStaminaChange(0f, "Sheltered (no change)");
    }

    public override float MaxSpeed() { return 0f; }

    public BunkerBuilding GetCurrentBunker() => currentBunker;
} 