using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;
using Managers;

/// <summary>
/// Sleep state for NPCs during night time.
/// NPCs will find a suitable sleeping location and remain there until morning.
/// </summary>
public class SleepState : _TaskState
{
    #region Sleep Configuration
    
    
    #endregion
    
    #region Private Fields
    
    private Transform sleepLocation;
    private bool isAtSleepLocation = false;
    private bool isSleeping = false;
    private bool needsPrecisePositioning = false;
    private Coroutine sleepCoroutine;
    

    
    #endregion
    
    #region Unity Lifecycle
    
    protected override void Awake()
    {
        base.Awake();
        stoppingDistance = 1f; // Closer stopping distance for sleep locations
        Debug.Log($"[SleepState] {name} Awake - base stoppingDistance set to: {stoppingDistance}");
    }
    
    #endregion
    
    #region Task State Implementation
    
    public override TaskType GetTaskType()
    {
        return TaskType.SLEEP;
    }
    
    public override void OnEnterState()
    {
        Debug.Log($"[SleepState] {npc.name} entering sleep state");
        
        ResetAgentState();
        
        // Reset precise positioning flag
        needsPrecisePositioning = false;
        
        // Find a suitable sleep location
        sleepLocation = FindNearestSleepLocation();
        
        if (sleepLocation != null)
        {
            Debug.Log($"[SleepState] {npc.name} heading to sleep location at {sleepLocation.position}");
            NavigateToSleepLocation();
        }
        else
        {
            Debug.Log($"[SleepState] {npc.name} couldn't find sleep location, sleeping in place");
            StartSleeping();
        }
        
        // Subscribe to time events
        TimeManager.OnDayStarted += WakeUp;
    }
    
    public override void OnExitState()
    {
        Debug.Log($"[SleepState] {npc.name} exiting sleep state");
        
        StopSleeping();
        
        // Unsubscribe from time events
        TimeManager.OnDayStarted -= WakeUp;
        
        // Reset precise positioning flag
        needsPrecisePositioning = false;
        
        ResetAgentState();
    }
    
    public override void UpdateState()
    {
        if (sleepLocation == null) return;

        // Use base class helper for destination reached checking (exactly like WorkState)
        bool hasReachedDestination = HasReachedDestination(sleepLocation, 0.5f);

        if (hasReachedDestination)
        {
            HandleReachedSleepLocation();
        }
        else
        {
            HandleMovingToSleepLocation();
        }

        UpdateAnimations();
    }
    
    #endregion
    
    #region Sleep Handling (Like WorkState's Task Handling)
    
    private void HandleReachedSleepLocation()
    {
        var sleepTask = sleepLocation?.GetComponent<SleepTask>();
        var precisePosition = sleepTask?.GetPrecisePosition();
        
        bool justReached = HandleReachedDestination(ref isAtSleepLocation, ref needsPrecisePositioning, precisePosition);

        if (needsPrecisePositioning)
        {
            UpdatePrecisePositioning(precisePosition, ref needsPrecisePositioning);
        }

        StartSleepingIfReady();
    }
    
    private void HandleMovingToSleepLocation()
    {
        HandleMovingFromDestination(ref isAtSleepLocation, ref needsPrecisePositioning);
    }
    
    private void StartSleepingIfReady()
    {
        if (!isSleeping && !needsPrecisePositioning)
        {
            StartSleeping();
        }
    }
    
    private void UpdateAnimations()
    {
        if (!isSleeping)
        {
            // Use base class method for consistent animation updates
            UpdateMovementAnimation();
        }
    }

    #endregion
    
    #region Sleep Location Management
    
    /// <summary>
    /// Find the nearest suitable sleep location (assigned bed or available bed)
    /// </summary>
    private Transform FindNearestSleepLocation()
    {
        // Get SleepTasks from WorkManager's centralized list
        var sleepTasks = CampManager.Instance?.WorkManager?.GetAvailableSleepTasks();
        
        Debug.Log($"[SleepState] {npc.name} FindNearestSleepLocation - Found {sleepTasks?.Count ?? 0} sleep tasks");
        
        if (sleepTasks == null || sleepTasks.Count == 0)
        {
            Debug.Log($"[SleepState] No sleep locations found, {npc.name} will sleep in place");
            return null;
        }
        
        Transform nearestLocation = null;
        float nearestDistance = float.MaxValue;
        
        // First priority: Find assigned beds for this NPC
        if (npc is SettlerNPC currentSettler)
        {
            Debug.Log($"[SleepState] {npc.name} Checking for assigned beds...");
            foreach (var sleepTask in sleepTasks)
            {
                if (sleepTask == null) continue;
                
                if (sleepTask.IsBedAssigned && sleepTask.AssignedSettler == currentSettler)
                {
                    // This is our assigned bed!
                    Debug.Log($"[SleepState] {npc.name} found their assigned bed at {sleepTask.transform.position}");
                    return sleepTask.transform;
                }
            }
            Debug.Log($"[SleepState] {npc.name} No assigned beds found");
        }
        
        // Second priority: Find unassigned beds that can be used
        Debug.Log($"[SleepState] {npc.name} Checking for available beds...");
        foreach (var sleepTask in sleepTasks)
        {
            if (sleepTask == null) continue;
            
            float distance = Vector3.Distance(npc.transform.position, sleepTask.transform.position);
            Debug.Log($"[SleepState] {npc.name} Checking location {sleepTask.name} at {sleepTask.transform.position}, distance: {distance:F2}");
            
            // Check if location is available and closer than current best
            if (distance < nearestDistance && IsLocationAvailable(sleepTask.transform))
            {
                Debug.Log($"[SleepState] {npc.name} Location {sleepTask.name} is available and within range");
                
                // If this is a SleepTask, try to assign it to this NPC
                if (!sleepTask.IsBedAssigned && npc is SettlerNPC currentSettler2)
                {
                    Debug.Log($"[SleepState] {npc.name} Attempting to assign to unassigned bed {sleepTask.name}");
                    if (sleepTask.AssignSettlerToBed(currentSettler2))
                    {
                        Debug.Log($"[SleepState] {npc.name} Successfully assigned to bed at {sleepTask.transform.position}");
                        nearestDistance = distance;
                        nearestLocation = sleepTask.transform;
                        break; // Found a bed, use it immediately
                    }
                    else
                    {
                        Debug.LogWarning($"[SleepState] {npc.name} Failed to assign to bed {sleepTask.name}");
                    }
                }
                else if (sleepTask.IsBedAssigned)
                {
                    Debug.Log($"[SleepState] {npc.name} Location {sleepTask.name} is already assigned to someone else");
                    // Already assigned to someone else, skip
                    continue;
                }
            }
            else
            {
                if (distance >= nearestDistance)
                {
                    Debug.Log($"[SleepState] {npc.name} Location {sleepTask.name} is not closer than current best: {distance:F2} >= {nearestDistance}");
                }
                else
                {
                    Debug.Log($"[SleepState] {npc.name} Location {sleepTask.name} is not available");
                }
            }
        }
        
        if (nearestLocation != null)
        {
            Debug.Log($"[SleepState] {npc.name} Selected sleep location: {nearestLocation.name} at {nearestLocation.position}");
        }
        else
        {
            Debug.Log($"[SleepState] {npc.name} No suitable sleep location found");
        }
        
        return nearestLocation;
    }
    

    
    /// <summary>
    /// Check if a sleep location is available
    /// </summary>
    private bool IsLocationAvailable(Transform location)
    {
        // Since we're now working directly with SleepTask components,
        // availability is handled by the SleepTask's CanSettlerUseBed method
        var sleepTask = location.GetComponent<SleepTask>();
        if (sleepTask != null && npc is SettlerNPC settler)
        {
            return sleepTask.CanSettlerUseBed(settler);
        }
        return false; // Only SleepTasks are valid sleep locations
    }
    
    #endregion
    
    #region Sleep Behavior
    
    /// <summary>
    /// Navigate to the selected sleep location
    /// </summary>
    private void NavigateToSleepLocation()
    {
        if (sleepLocation == null || agent == null) return;
        
        Debug.Log($"[SleepState] {npc.name} NavigateToSleepLocation - sleepLocation: {sleepLocation.name}");
        
        // Check if this is a SleepTask (bed) and use its positioning system
        var sleepTask = sleepLocation.GetComponent<SleepTask>();
        if (sleepTask != null)
        {
            Debug.Log($"[SleepState] {npc.name} Found SleepTask component on {sleepLocation.name}");
            
            // Use the WorkTask positioning system - navigate to the work location
            Transform workDestination = sleepTask.GetNavMeshDestination();
            Transform precisePosition = sleepTask.GetPrecisePosition();
            
            Debug.Log($"[SleepState] {npc.name} WorkTask positioning - NavMeshDestination: {workDestination?.name ?? "null"}, PrecisePosition: {precisePosition?.name ?? "null"}");
            Debug.Log($"[SleepState] {npc.name} WorkTask positioning - NavMeshDestination pos: {workDestination?.position}, PrecisePosition pos: {precisePosition?.position}");
            
            // Use base class method for consistent NavMesh setup
            SetupNavMeshForWorkTask(workDestination, 0.5f);
            needsPrecisePositioning = false; // Will be set in HandleReachedSleepLocation()
            
            Debug.Log($"[SleepState] {npc.name} Set NavMesh destination to: {workDestination.position}, stoppingDistance: {agent.stoppingDistance}");
            Debug.Log($"[SleepState] {npc.name} GetEffectiveStoppingDistance returned: {GetEffectiveStoppingDistance(workDestination, 0.5f)}");
        }
        else
        {
            Debug.Log($"[SleepState] {npc.name} No SleepTask component found, using fallback positioning");
            
            // Use base class method for consistent NavMesh setup
            SetupNavMeshForWorkTask(sleepLocation, 0.5f);
            needsPrecisePositioning = false;
            
            Debug.Log($"[SleepState] {npc.name} Set fallback destination to: {sleepLocation.position}, stoppingDistance: {agent.stoppingDistance}");
        }
        
        isAtSleepLocation = false;
        isSleeping = false;
    }
    
    /// <summary>
    /// Start the sleeping behavior
    /// </summary>
    private void StartSleeping()
    {
        if (isSleeping) return;
        
        Debug.Log($"[SleepState] {npc.name} starting to sleep");
        
        isSleeping = true;
        
        // Note: Movement stopping and precise positioning is handled by HandleReachedSleepLocation() (like WorkState)
        
        // Play sleep animation - prioritize WorkTask animation system
        if (animator != null)
        {
            // Check if we have a SleepTask assigned (bed)
            var sleepTask = sleepLocation?.GetComponent<SleepTask>();
            if (sleepTask != null && sleepTask.IsBedAssigned)
            {
                // Use the WorkTask animation system - this is the preferred method
                if (npc is SettlerNPC settler)
                {
                    Debug.Log($"[SleepState] {npc.name} using WorkTask animation system for sleep");
                    settler.PlayWorkAnimation(sleepTask.GetAnimationClipName());
                }
            }
        }
        
        // Start sleep coroutine for periodic checks
        if (sleepCoroutine == null)
        {
            sleepCoroutine = StartCoroutine(SleepCoroutine());
        }
    }
    
    /// <summary>
    /// Stop the sleeping behavior
    /// </summary>
    private void StopSleeping()
    {
        if (!isSleeping) return;
        
        Debug.Log($"[SleepState] {npc.name} stopping sleep");
        
        isSleeping = false;
        
        // Stop sleep coroutine
        if (sleepCoroutine != null)
        {
            StopCoroutine(sleepCoroutine);
            sleepCoroutine = null;
        }
        
        // Stop sleep animation - check if we were using WorkTask or trigger system
        if (animator != null)
        {
            var sleepTask = sleepLocation?.GetComponent<SleepTask>();
            if (sleepTask != null && sleepTask.IsBedAssigned && npc is SettlerNPC settler)
            {
                // Stop WorkTask animation
                settler.StopWorkAnimation();
            }
        }
    }
    
    /// <summary>
    /// Sleep coroutine for handling sleep behavior
    /// </summary>
    private IEnumerator SleepCoroutine()
    {
        while (isSleeping)
        {
            // Check for threats that might wake the NPC
            if (CheckForNearbyThreats(15f)) // Increased detection range while sleeping
            {
                Debug.Log($"[SleepState] {npc.name} woken by nearby threat");
                npc.ChangeTask(TaskType.FLEE);
                yield break;
            }
            
            // Restore stamina/health while sleeping (if applicable)
            RestoreWhileSleeping();
            
            yield return new WaitForSeconds(1f); // Check every second
        }
    }
    
    /// <summary>
    /// Restore NPC stats while sleeping
    /// </summary>
    private void RestoreWhileSleeping()
    {
        // Restore stamina faster while sleeping
        if (npc is SettlerNPC settler)
        {
            // Restore stamina at 2x the normal rate while sleeping
            settler.RestoreStaminaAtRate(2f);
        }
    }
    
    /// <summary>
    /// Wake up when day starts
    /// </summary>
    private void WakeUp()
    {
        if (npc != null)
        {
            Debug.Log($"[SleepState] {npc.name} waking up for the day");
            
            // Simple wake-up logic: return to assigned work or find new work
            TryAssignWorkOrWander();
        }
    }
    
    #endregion
    
    #region Animation and Movement
    

    
    public override float MaxSpeed()
    {
        // Move slower when going to sleep
        return npc.moveMaxSpeed * 0.6f;
    }
    
    #endregion
    

}
