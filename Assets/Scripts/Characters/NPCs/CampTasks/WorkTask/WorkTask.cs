using System;
using UnityEngine;
using Managers;

public abstract class WorkTask : MonoBehaviour
{
    [HideInInspector] public WorkType workType;

    // Abstract method for NPC to perform the work task
    public abstract void PerformTask(SettlerNPC npc);
    // Point the npc has to go to to perform the work
    public abstract Transform WorkTaskTransform();

    // Declare StopWork as an event
    public event Action StopWork; // Called when a construction is complete, building is broken, etc..

    protected virtual void Start()
    {
        // Ensure AddWorkTask is called for all inheriting classes
        CampManager.Instance.WorkManager.AddWorkTask(this);
    }

    // Helper method to trigger the event safely (other classes can call this to invoke StopWork)
    protected void InvokeStopWork()
    {
        StopWork?.Invoke();
    }
}
