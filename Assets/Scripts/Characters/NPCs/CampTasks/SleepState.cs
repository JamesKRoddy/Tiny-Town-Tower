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
    
    [Header("Sleep Settings")]
    [SerializeField] private float sleepSearchRadius = 20f;
    [SerializeField] private float sleepLocationCheckRadius = 2f;
    [SerializeField] private LayerMask sleepLocationLayerMask = -1;
    [SerializeField] private string sleepAnimationTrigger = "Sleep";
    
    #endregion
    
    #region Private Fields
    
    private Transform sleepLocation;
    private bool isAtSleepLocation = false;
    private bool isSleeping = false;
    private bool needsPrecisePositioning = false;
    private Coroutine sleepCoroutine;
    
    // Cached sleep locations for performance
    private static Transform[] cachedSleepLocations;
    private static float lastCacheTime = 0f;
    private const float CACHE_REFRESH_INTERVAL = 30f; // Refresh cache every 30 seconds
    
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
        RefreshSleepLocationCache();
        
        Debug.Log($"[SleepState] {npc.name} FindNearestSleepLocation - Found {cachedSleepLocations?.Length ?? 0} cached locations");
        
        if (cachedSleepLocations == null || cachedSleepLocations.Length == 0)
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
            foreach (var location in cachedSleepLocations)
            {
                if (location == null) continue;
                
                var sleepTask = location.GetComponent<SleepTask>();
                if (sleepTask != null && sleepTask.IsBedAssigned && sleepTask.AssignedSettler == currentSettler)
                {
                    // This is our assigned bed!
                    Debug.Log($"[SleepState] {npc.name} found their assigned bed at {location.position}");
                    return location;
                }
            }
            Debug.Log($"[SleepState] {npc.name} No assigned beds found");
        }
        
        // Second priority: Find unassigned beds that can be used
        Debug.Log($"[SleepState] {npc.name} Checking for available beds...");
        foreach (var location in cachedSleepLocations)
        {
            if (location == null) continue;
            
            float distance = Vector3.Distance(npc.transform.position, location.position);
            Debug.Log($"[SleepState] {npc.name} Checking location {location.name} at {location.position}, distance: {distance:F2}");
            
            // Check if location is within search radius and available
            if (distance <= sleepSearchRadius && distance < nearestDistance && IsLocationAvailable(location))
            {
                Debug.Log($"[SleepState] {npc.name} Location {location.name} is available and within range");
                
                // If this is a SleepTask, try to assign it to this NPC
                var sleepTask = location.GetComponent<SleepTask>();
                if (sleepTask != null && !sleepTask.IsBedAssigned && npc is SettlerNPC currentSettler2)
                {
                    Debug.Log($"[SleepState] {npc.name} Attempting to assign to unassigned bed {location.name}");
                    if (sleepTask.AssignSettlerToBed(currentSettler2))
                    {
                        Debug.Log($"[SleepState] {npc.name} Successfully assigned to bed at {location.position}");
                        nearestDistance = distance;
                        nearestLocation = location;
                        break; // Found a bed, use it immediately
                    }
                    else
                    {
                        Debug.LogWarning($"[SleepState] {npc.name} Failed to assign to bed {location.name}");
                    }
                }
                else if (sleepTask == null || sleepTask.IsBedAssigned)
                {
                    Debug.Log($"[SleepState] {npc.name} Location {location.name} is not a SleepTask or already assigned");
                    // Not a SleepTask or already assigned, check general availability
                    nearestDistance = distance;
                    nearestLocation = location;
                }
            }
            else
            {
                if (distance > sleepSearchRadius)
                {
                    Debug.Log($"[SleepState] {npc.name} Location {location.name} is too far: {distance:F2} > {sleepSearchRadius}");
                }
                else if (distance >= nearestDistance)
                {
                    Debug.Log($"[SleepState] {npc.name} Location {location.name} is not closer than current best: {distance:F2} >= {nearestDistance}");
                }
                else
                {
                    Debug.Log($"[SleepState] {npc.name} Location {location.name} is not available");
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
    /// Refresh the cache of available sleep locations (SleepTasks)
    /// </summary>
    private void RefreshSleepLocationCache()
    {
        if (Time.time - lastCacheTime < CACHE_REFRESH_INTERVAL && cachedSleepLocations != null)
        {
            return; // Cache is still fresh
        }
        
        // Find SleepTask components (beds)
        var sleepTasks = FindObjectsByType<SleepTask>(FindObjectsSortMode.None);
        var sleepLocationObjects = new System.Collections.Generic.List<Transform>();
        
        foreach (var sleepTask in sleepTasks)
        {
            if (sleepTask != null && sleepTask.IsOperational())
            {
                Transform bedTransform = sleepTask.WorkTaskTransform();
                if (bedTransform != null)
                {
                    sleepLocationObjects.Add(bedTransform);
                }
            }
        }
        
        // If no SleepTasks found, fall back to old system for backward compatibility
        if (sleepLocationObjects.Count == 0)
        {
            // Look for buildings that can serve as sleep locations
            var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                // Houses and barracks can serve as sleep locations
                if (building.name.ToLower().Contains("house") || 
                    building.name.ToLower().Contains("barracks") || 
                    building.name.ToLower().Contains("shelter"))
                {
                    sleepLocationObjects.Add(building.transform);
                }
            }
            
            // Look for specific sleep objects (beds, sleeping bags, etc.)
            var sleepObjects = GameObject.FindGameObjectsWithTag("SleepLocation");
            foreach (var obj in sleepObjects)
            {
                sleepLocationObjects.Add(obj.transform);
            }
            
            // If still no specific sleep locations found, use any building
            if (sleepLocationObjects.Count == 0)
            {
                foreach (var building in buildings)
                {
                    if (building.IsOperational())
                    {
                        sleepLocationObjects.Add(building.transform);
                    }
                }
            }
        }
        
        cachedSleepLocations = sleepLocationObjects.ToArray();
        lastCacheTime = Time.time;
        
        Debug.Log($"[SleepState] Refreshed sleep location cache, found {cachedSleepLocations.Length} locations");
    }
    
    /// <summary>
    /// Check if a sleep location is available
    /// </summary>
    private bool IsLocationAvailable(Transform location)
    {
        // First check if this is a SleepTask (bed)
        var sleepTask = location.GetComponent<SleepTask>();
        if (sleepTask != null)
        {
            // Check if this settler can use this bed
            if (npc is SettlerNPC settler)
            {
                return sleepTask.CanSettlerUseBed(settler);
            }
            return false;
        }
        
        // Fallback: Check for other NPCs in sleep state near this location
        var nearbyNPCs = Physics.OverlapSphere(location.position, sleepLocationCheckRadius, sleepLocationLayerMask);
        
        foreach (var collider in nearbyNPCs)
        {
            var otherNPC = collider.GetComponent<SettlerNPC>();
            if (otherNPC != null && otherNPC != npc && otherNPC.GetCurrentTaskType() == TaskType.SLEEP)
            {
                return false; // Location is occupied
            }
        }
        
        return true;
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
            else
            {
                // Fallback to trigger-based animation if no bed assignment
                if (!string.IsNullOrEmpty(sleepAnimationTrigger))
                {
                    Debug.Log($"[SleepState] {npc.name} using fallback trigger animation for sleep");
                    animator.SetTrigger(sleepAnimationTrigger);
                }
                animator.SetFloat("Speed", 0f);
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
            else
            {
                // Stop trigger-based animation
                animator.ResetTrigger(sleepAnimationTrigger);
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
            
            // Try to assign work, otherwise wander
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
