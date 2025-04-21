using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A building that has a permanent work task.
/// This is an abstract base class for buildings that NPCs can work at for ongoing tasks.
/// </summary>
public abstract class TaskBuilding : Building
{
    [Header("Work Task")]
    [SerializeField] protected WorkTask permanentWorkTask;

    protected override void Awake()
    {
        base.Awake();
        
        // Ensure work task is set up
        if (permanentWorkTask == null)
        {
            Debug.LogError($"No permanent work task assigned to {gameObject.name}!");
            enabled = false;
            return;
        }

        // Set up the work task's transform to be at the building's position
        permanentWorkTask.transform.position = transform.position;
    }

    /// <summary>
    /// Gets the permanent work task associated with this building
    /// </summary>
    public WorkTask GetPermanentWorkTask() => permanentWorkTask;

    protected override void DestroyBuilding()
    {
        // Clean up the permanent work task
        if (permanentWorkTask != null)
        {
            Destroy(permanentWorkTask.gameObject);
        }
        
        base.DestroyBuilding();
    }
} 