using System;
using UnityEngine;
using Managers;

public abstract class WorkTask : MonoBehaviour
{
    [HideInInspector] public WorkType workType;
    [SerializeField] protected Transform workLocationTransform; // Optional specific work location
    private SettlerNPC assignedNPC; // Reference to the NPC performing this task
    [SerializeField] public ResourceItemCount[] requiredResources; // Resources needed to perform this task

    // Property to access the assigned NPC
    public SettlerNPC AssignedNPC => assignedNPC;

    // Abstract method for NPC to perform the work task
    public abstract void PerformTask(SettlerNPC npc);
    
    // Virtual method that can be overridden by specific tasks
    public virtual Transform WorkTaskTransform()
    {
        return workLocationTransform != null ? workLocationTransform : transform;
    }

    // Virtual method to check if the task can be performed
    public virtual bool CanPerformTask()
    {
        return true; // Default implementation assumes task can always be performed
    }

    // Method to check if the player has required resources
    public bool HasRequiredResources()
    {
        if (requiredResources == null || requiredResources.Length == 0)
            return true; // No resources required

        foreach (var resource in requiredResources)
        {
            if (PlayerInventory.Instance.GetItemCount(resource.resource) < resource.count)
            {
                return false;
            }
        }
        return true;
    }

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

    // Method to assign an NPC to this task
    public void AssignNPC(SettlerNPC npc)
    {
        assignedNPC = npc;
    }

    // Method to unassign the current NPC
    public void UnassignNPC()
    {
        assignedNPC = null;
    }

    // Method to check if the task is currently assigned
    public bool IsAssigned()
    {
        return assignedNPC != null;
    }
}
