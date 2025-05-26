using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for tasks that manage other tasks (like CleaningTask managing DirtPileTasks)
/// </summary>
public abstract class ManagerTask : WorkTask
{
    protected WorkTask currentSubtask;

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (currentSubtask != null)
        {
            currentSubtask.StopWorkCoroutine();
        }
    }

    public override void PerformTask(HumanCharacterController npc)
    {
        if (currentSubtask != null)
        {
            currentSubtask.PerformTask(npc);
        }
        else
        {
            base.PerformTask(npc);
        }
    }

    protected void TransitionToSubtask(WorkTask subtask, HumanCharacterController npc)
    {
        if (currentSubtask != null)
        {
            currentSubtask.StopWorkCoroutine();
        }

        currentSubtask = subtask;
        currentWorker = npc;

        if (currentSubtask != null)
        {
            currentSubtask.AssignNPC(npc);
            currentSubtask.PerformTask(npc);
        }
    }

    public virtual void HandleSubtaskCompleted()
    {
        if (currentSubtask != null)
        {
            currentSubtask.UnassignNPC();
            currentSubtask = null;
        }
    }

    public override void StopWorkCoroutine()
    {
        base.StopWorkCoroutine();
        if (currentSubtask != null)
        {
            currentSubtask.StopWorkCoroutine();
        }
    }

    public override string GetTooltipText()
    {
        if (currentSubtask != null)
        {
            return currentSubtask.GetTooltipText();
        }
        return base.GetTooltipText();
    }

    public override Transform GetNavMeshDestination()
    {
        if (currentSubtask != null)
        {
            return currentSubtask.GetNavMeshDestination();
        }
        return base.GetNavMeshDestination();
    }

    public override Transform GetPrecisePosition()
    {
        if (currentSubtask != null)
        {
            return currentSubtask.GetPrecisePosition();
        }
        return base.GetPrecisePosition();
    }

    public override bool CanPerformTask()
    {
        if (currentSubtask != null)
        {
            return currentSubtask.CanPerformTask();
        }
        return base.CanPerformTask();
    }
} 