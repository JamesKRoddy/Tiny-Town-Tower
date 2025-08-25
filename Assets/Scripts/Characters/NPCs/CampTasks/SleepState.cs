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
        
        ResetAgentState();
    }
    
    public override void UpdateState()
    {
        if (sleepLocation != null && !isAtSleepLocation)
        {
            // Check if we've reached the sleep location
            if (HasReachedDestination(sleepLocation))
            {
                Debug.Log($"[SleepState] {npc.name} reached sleep location");
                isAtSleepLocation = true;
                agent.isStopped = true;
                StartSleeping();
            }
        }
        
        // Update movement animation if moving
        if (!isSleeping && agent != null)
        {
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
        
        if (cachedSleepLocations == null || cachedSleepLocations.Length == 0)
        {
            Debug.Log($"[SleepState] No sleep locations found, {npc.name} will sleep in place");
            return null;
        }
        
        Transform nearestLocation = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var location in cachedSleepLocations)
        {
            if (location == null) continue;
            
            float distance = Vector3.Distance(npc.transform.position, location.position);
            
            // Check if location is within search radius and available
            if (distance <= sleepSearchRadius && distance < nearestDistance && IsLocationAvailable(location))
            {
                nearestDistance = distance;
                nearestLocation = location;
            }
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
        
        agent.stoppingDistance = GetEffectiveStoppingDistance(sleepLocation);
        agent.SetDestination(sleepLocation.position);
        agent.isStopped = false;
        
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
        
        // Stop movement
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        
        // Play sleep animation
        if (animator != null && !string.IsNullOrEmpty(sleepAnimationTrigger))
        {
            animator.SetTrigger(sleepAnimationTrigger);
            animator.SetFloat("Speed", 0f);
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
        
        // Reset animation
        if (animator != null)
        {
            animator.ResetTrigger(sleepAnimationTrigger);
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
    
    /// <summary>
    /// Update movement animation while walking to sleep location
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (agent == null || animator == null) return;
        
        float maxSpeed = MaxSpeed();
        float currentSpeedNormalized = agent.velocity.magnitude / maxSpeed;
        animator.SetFloat("Speed", currentSpeedNormalized);
    }
    
    public override float MaxSpeed()
    {
        // Move slower when going to sleep
        return npc.moveMaxSpeed * 0.6f;
    }
    
    #endregion
}
