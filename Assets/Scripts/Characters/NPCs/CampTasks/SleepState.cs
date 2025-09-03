using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

/// <summary>
/// Simple sleep state for NPCs.
/// NPCs sleep at night (or when stamina is below 10) and wake when stamina reaches 80+.
/// </summary>
public class SleepState : _TaskState
{
    #region Private Fields
    
    private Transform sleepLocation;
    private bool isSleeping = false;
    private bool isWanderingForSleep = false;
    private float wanderStartTime;
    private float wanderDuration = 2f; // Wander for 2 seconds before sleeping on ground
    
    #endregion
    
    #region Task State Implementation
    
    public override TaskType GetTaskType()
    {
        return TaskType.SLEEP;
    }
    
    public override void OnEnterState()
    {
        Debug.Log($"[SleepState] {npc.name} entering sleep state");
        
        // Try to find a bed
        sleepLocation = FindBestBed();
        
        if (sleepLocation != null)
        {
            Debug.Log($"[SleepState] {npc.name} heading to bed at {sleepLocation.position}");
            GoToBed();
        }
        else
        {
            // No bed available - check if we should sleep on ground
            if (npc is SettlerNPC settler && settler.IsVeryTired())
            {
                Debug.Log($"[SleepState] {npc.name} no bed available but very tired (stamina: {npc.GetStaminaPercentage():F1}%), wandering before sleeping on ground");
                StartWandering();
            }
            else
            {
                Debug.Log($"[SleepState] {npc.name} no bed available and not very tired (stamina: {npc.GetStaminaPercentage():F1}%), returning to previous activity");
                // Exit sleep state and return to wandering
                ExitSleepAndReturnToActivity();
            }
        }
    }
    
    public override void OnExitState()
    {
        Debug.Log($"[SleepState] {npc.name} exiting sleep state");
        
        if (isSleeping)
        {
            StopSleeping();
        }
        
        isWanderingForSleep = false;
        isSleeping = false;
    }
    
    public override void UpdateState()
    {
        // Check if we should wake up
        if (isSleeping && ShouldWakeUp())
        {
            WakeUp();
            return;
        }
        
        // Handle wandering before sleeping
        if (isWanderingForSleep)
        {
            UpdateWandering();
            return;
        }
        
        // Handle moving to bed
        if (sleepLocation != null && !isSleeping)
        {
            if (HasReachedDestination(sleepLocation, 1f))
            {
                StartSleeping();
            }
            else
            {
                UpdateMovementAnimation();
            }
        }
    }
    
    #endregion
    
    #region Sleep Logic
    
    /// <summary>
    /// Find the best available bed for this NPC
    /// </summary>
    private Transform FindBestBed()
    {
        var sleepTasks = CampManager.Instance?.WorkManager?.GetAvailableSleepTasks();
        if (sleepTasks == null || sleepTasks.Count == 0)
        {
            return null;
        }
        
        // First priority: Assigned bed
        if (npc is SettlerNPC settler)
        {
            foreach (var sleepTask in sleepTasks)
            {
                if (sleepTask.IsBedAssigned && sleepTask.AssignedSettler == settler)
                {
                    Debug.Log($"[SleepState] {npc.name} found assigned bed");
                    return sleepTask.transform;
                }
            }
            
            // Second priority: Available bed
            foreach (var sleepTask in sleepTasks)
            {
                if (!sleepTask.IsBedAssigned && sleepTask.CanSettlerUseBed(settler))
                {
                    if (sleepTask.AssignSettlerToBed(settler))
                    {
                        Debug.Log($"[SleepState] {npc.name} assigned to available bed");
                        return sleepTask.transform;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Navigate to the bed
    /// </summary>
    private void GoToBed()
    {
        if (sleepLocation == null || agent == null) return;
        
        agent.SetDestination(sleepLocation.position);
        agent.speed = npc.moveMaxSpeed * 0.6f; // Slow, tired movement
        agent.stoppingDistance = 1f;
        agent.isStopped = false;
    }
    
    /// <summary>
    /// Start wandering behavior before sleeping on ground
    /// </summary>
    private void StartWandering()
    {
        isWanderingForSleep = true;
        wanderStartTime = Time.time;
        
        // Set random nearby destination
        Vector3 wanderDirection = Random.insideUnitSphere * 2f;
        wanderDirection.y = 0;
        Vector3 wanderTarget = npc.transform.position + wanderDirection;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(wanderTarget, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            agent.speed = npc.moveMaxSpeed * 0.3f; // Very slow, tired movement
            Debug.Log($"[SleepState] {npc.name} wandering to {hit.position}");
        }
        else
        {
            // Can't find valid position, sleep immediately
            isWanderingForSleep = false;
            StartSleeping();
        }
    }
    
    /// <summary>
    /// Update wandering behavior
    /// </summary>
    private void UpdateWandering()
    {
        float timeElapsed = Time.time - wanderStartTime;
        
        // Stop wandering after duration or if reached destination
        if (timeElapsed >= wanderDuration || (agent.remainingDistance <= 0.5f && !agent.pathPending))
        {
            Debug.Log($"[SleepState] {npc.name} finished wandering, sleeping on ground");
            isWanderingForSleep = false;
            agent.isStopped = true;
            StartSleeping();
        }
        else
        {
            UpdateMovementAnimation();
        }
    }
    
    /// <summary>
    /// Start sleeping
    /// </summary>
    private void StartSleeping()
    {
        if (isSleeping) return;
        
        Debug.Log($"[SleepState] {npc.name} starting to sleep");
        
        isSleeping = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        
        // Play sleep animation
        if (npc is SettlerNPC settler)
        {
            var sleepTask = sleepLocation?.GetComponent<SleepTask>();
            if (sleepTask != null && sleepTask.IsBedAssigned)
            {
                Debug.Log($"[SleepState] {npc.name} using bed sleep animation");
                settler.PlayWorkAnimation(sleepTask.GetAnimationClipName());
            }
            else
            {
                Debug.Log($"[SleepState] {npc.name} using ground sleep animation");
                settler.PlayWorkAnimation("SLEEPING");
            }
        }
    }
    
    /// <summary>
    /// Stop sleeping
    /// </summary>
    private void StopSleeping()
    {
        if (!isSleeping) return;
        
        Debug.Log($"[SleepState] {npc.name} stopping sleep");
        
        isSleeping = false;
        
        // Stop animation
        if (npc is SettlerNPC settler)
        {
            settler.StopWorkAnimation();
        }
    }
    
    /// <summary>
    /// Check if NPC should wake up
    /// </summary>
    private bool ShouldWakeUp()
    {
        bool isDay = GameManager.Instance?.TimeManager?.IsDay == true;
        bool hasProperBed = sleepLocation != null && npc.HasAssignedBed();
        float staminaPercentage = npc.GetStaminaPercentage();
        
        // If in a proper bed, sleep until morning unless stamina is completely full
        if (hasProperBed)
        {
            // Only wake up if it's day and stamina is at least 50%, or if stamina is completely full
            if (staminaPercentage >= 100f)
            {
                return true; // Fully rested, can wake up anytime
            }
            
            if (isDay && staminaPercentage >= 50f)
            {
                return true; // It's morning and we have decent stamina
            }
            
            return false; // Stay in bed during night
        }
        else
        {
            // Sleeping on ground - wake up when stamina is restored (80+) regardless of time
            if (staminaPercentage >= 80f)
            {
                return true;
            }
            
            // Also wake up if day started and stamina is decent (50+)
            if (isDay && staminaPercentage >= 50f)
            {
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Wake up and return to normal activities
    /// </summary>
    private void WakeUp()
    {
        Debug.Log($"[SleepState] {npc.name} waking up (stamina: {npc.GetStaminaPercentage():F1}%)");
        
        StopSleeping();
        
        // Return to work or wander
        TryAssignWorkOrWander();
    }
    
    /// <summary>
    /// Exit sleep state and return to normal activities (used when no bed available and not tired)
    /// </summary>
    private void ExitSleepAndReturnToActivity()
    {
        Debug.Log($"[SleepState] {npc.name} exiting sleep state, returning to normal activities");
        
        // Return to work or wander
        TryAssignWorkOrWander();
    }
    
    #endregion
    
    #region Stamina Override
    
    /// <summary>
    /// Handle stamina changes during sleep
    /// </summary>
    public override void UpdateStamina()
    {
        if (isSleeping)
        {
            // Sleeping: regenerate stamina
            bool hasProperBed = sleepLocation != null && npc.HasAssignedBed();
            float sleepRegen = npc.GetSleepStaminaRegenRate(hasProperBed) * Time.deltaTime;
            npc.ApplyStaminaChange(sleepRegen, hasProperBed ? "Bed sleep" : "Ground sleep");
        }
        else
        {
            // Moving to bed or wandering: normal drain
            base.UpdateStamina();
        }
    }
    
    #endregion
    
    #region Utility
    
    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed * 0.5f; // Tired movement
    }
    
    #endregion
}