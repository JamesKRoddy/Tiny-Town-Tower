using UnityEngine;
using Managers;

/// <summary>
/// Bed building that provides sleeping locations for settlers.
/// Each bed can be assigned to one settler.
/// </summary>
public class BedBuilding : Building
{    
    private SleepTask sleepTask;
    
    protected override void Start()
    {
        base.Start();
        
        // Add SleepTask component if not already present
        sleepTask = GetComponent<SleepTask>();
        if (sleepTask == null)
        {
            sleepTask = gameObject.AddComponent<SleepTask>();
        }
    }
    
    protected override void OnStructureSetup()
    {
        // Call base setup first (handles repair and upgrade tasks)
        base.OnStructureSetup();
        
        // Set the SleepTask as the current work task for the base class
        // This is done here instead of Start() to ensure proper initialization order
        if (sleepTask != null)
        {
            SetCurrentWorkTask(sleepTask);
        }
    }
    
    /// <summary>
    /// Get the SleepTask component for bed operations
    /// </summary>
    public SleepTask GetSleepTask()
    {
        return sleepTask;
    }
    
    /// <summary>
    /// Get building stats text including bed assignment information
    /// </summary>
    public override string GetBuildingStatsText()
    {
        string stats = base.GetBuildingStatsText();
        
        if (sleepTask != null)
        {
            stats += "\n=== BED INFORMATION ===\n";
            
            if (sleepTask.IsBedAssigned && sleepTask.AssignedSettler != null)
            {
                stats += $"Assigned to: {sleepTask.AssignedSettler.name}\n";
                stats += "Status: Occupied";
            }
            else
            {
                stats += "Status: Available\n";
                stats += "No settler assigned";
            }
        }
        
        return stats;
    }
}
