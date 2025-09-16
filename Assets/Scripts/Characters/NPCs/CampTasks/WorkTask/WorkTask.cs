using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;

public abstract class WorkTask : MonoBehaviour
{
    [Header("Task Settings")]
    [SerializeField] protected Transform workLocationTransform; // Optional specific work location
    [SerializeField] protected int maxWorkers = 1; // Maximum number of workers that can be assigned to this task
    [SerializeField] protected bool autoQueue = false; // Whether this task should be automatically added to the work queue for NPCs to pick up
    protected List<HumanCharacterController> currentWorkers = new List<HumanCharacterController>(); // List of NPCs performing this task
    [HideInInspector] public ResourceItemCount[] requiredResources; // Resources needed to perform this task
    [SerializeField] protected bool showTooltip = false; // Whether to show tooltips for this task

    [Header("Electricity Requirements")]
    [SerializeField] protected float electricityRequired = 0f;
    protected bool isOperational = true; // Whether the task is operational, different from the buildings operational status

    [Header("Task Animation")]
    public TaskAnimation taskAnimation;

    [Header("Ambient Effects")]
    [Tooltip("Particle systems that play when the task is in use")]
    [SerializeField] protected ParticleSystem[] inUseParticleSystems = new ParticleSystem[0];
    
    [Tooltip("Particle systems that play when the task is idle/not in use")]
    [SerializeField] protected ParticleSystem[] idleParticleSystems = new ParticleSystem[0];
    
    [Tooltip("Audio sources that play when the task is in use")]
    [SerializeField] protected AudioSource[] inUseAudioSources = new AudioSource[0];
    
    [Tooltip("Audio sources that play when the task is idle/not in use")]
    [SerializeField] protected AudioSource[] idleAudioSources = new AudioSource[0];
    
    private bool currentlyInUse = false;

    // Work progress tracking
    protected float workProgress = 0f;
    protected float baseWorkTime = 5f;
    protected int resourceAmount = 1;

    // Progress bar settings
    [Header("Progress Display")]
    [SerializeField] protected bool showProgressBar = true; // Whether to show progress bar for this task
    protected bool progressBarActive = false;

    // Properties to access the assigned NPCs
    public List<HumanCharacterController> AssignedNPCs => currentWorkers;
    public HumanCharacterController AssignedNPC => currentWorkers.Count > 0 ? currentWorkers[0] : null; // For backward compatibility
    public bool IsOccupied => currentWorkers.Count > 0;
    public bool IsFullyOccupied => currentWorkers.Count >= maxWorkers;
    public int CurrentWorkerCount => currentWorkers.Count;
    public int MaxWorkerCount => maxWorkers;
    public bool IsMultiWorkerTask => maxWorkers > 1;
    public bool IsAutoQueued => autoQueue;
    public virtual bool IsTaskCompleted => true; // Base WorkTask is always completed when done
    public virtual bool HasQueuedTasks => false; // Base WorkTask has no queue

    protected IPlaceableStructure taskStructure;

    protected virtual void Start()
    {
        taskStructure = GetComponent<IPlaceableStructure>();
        
        // Initialize ambient effects arrays if they're null
        if (inUseParticleSystems == null) inUseParticleSystems = new ParticleSystem[0];
        if (idleParticleSystems == null) idleParticleSystems = new ParticleSystem[0];
        if (inUseAudioSources == null) inUseAudioSources = new AudioSource[0];
        if (idleAudioSources == null) idleAudioSources = new AudioSource[0];
        
        // Start with idle effects if operational
        if (isOperational)
        {
            SetAmbientEffectsState(false); // Start in idle state (looping effects)
        }
    }

    // Virtual method for tooltip text
    public virtual string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = $"{GetType().Name}\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        tooltip += $"Workers: {currentWorkers.Count}/{maxWorkers}\n";
        tooltip += $"Status: {(isOperational ? "Operational" : "Not Operational")}\n";
        
        if (electricityRequired > 0)
        {
            tooltip += $"Electricity Required: {electricityRequired}\n";
            tooltip += $"Current Power: {CampManager.Instance.ElectricityManager.GetElectricityPercentage():F1}%\n";
        }
        
        if (requiredResources != null)
        {
            tooltip += "Required Resources:\n";
            foreach (var resource in requiredResources)
            {
                tooltip += $"- {resource.resourceScriptableObj.objectName}: {resource.count}\n";
            }
        }
        
        return tooltip;
    }

    // Virtual method called by WorkState when NPC reaches work position
    // This is now primarily for animation/positioning setup since work execution is handled by worker coroutines
    public virtual void PerformTask(HumanCharacterController npc)
    {
        // Ensure worker is in the list (in case this is called before AssignNPC)
        if (!currentWorkers.Contains(npc) && currentWorkers.Count < maxWorkers)
        {
            currentWorkers.Add(npc);
        }
        
        // Start in-use ambient effects when work actually begins (NPC has arrived and is starting work)
        // Show progress bar when the first worker calls PerformTask, regardless of how many workers are already assigned
        if (!progressBarActive) // Only show progress bar once
        {
            Debug.Log($"[WorkTask] Starting work for {GetType().Name} - progressBarActive: {progressBarActive}, showProgressBar: {showProgressBar}");
            SetAmbientEffectsState(true);
            
            // Show progress bar if enabled and manager exists
            if (showProgressBar && CampManager.Instance?.WorkManager != null)
            {
                Debug.Log($"[WorkTask] Showing progress bar for {GetType().Name}");
                CampManager.Instance.WorkManager.ShowProgressBar(this);
                progressBarActive = true;
            }
        }
        else
        {
            Debug.Log($"[WorkTask] Progress bar already active for {GetType().Name}, skipping show");
        }
        
        // Work execution is now handled by the worker's coroutine started in AssignNPC
        // This method is mainly for WorkState coordination and animation
    }
    
    // Virtual method that can be overridden by specific tasks
    public virtual Transform WorkTaskTransform()
    {
        return workLocationTransform;
    }

    // Method for NavMeshAgent pathfinding - should always return a valid position
    public virtual Transform GetNavMeshDestination()
    {
        // If we have a specific work location, use that
        if (workLocationTransform != null)
        {
            return workLocationTransform;
        }
        // Otherwise use the task's position
        return transform;
    }

    // Method for precise positioning - can return null if no precise position needed
    public virtual Transform GetPrecisePosition()
    {
        return workLocationTransform;
    }

    // Virtual method to check if the task can be performed
    public virtual bool CanPerformTask()
    {
        Debug.Log($"[WorkTask] CanPerformTask check for {GetType().Name} - isOperational: {isOperational}, electricityRequired: {electricityRequired}");
        
        if (!isOperational)
        {
            Debug.Log($"[WorkTask] {GetType().Name} cannot be performed - not operational");
            return false;
        }

        // Check if there is enough electricity for the entire task duration
        if (electricityRequired > 0)
        {
            float totalElectricityNeeded = electricityRequired;
            bool hasElectricity = CampManager.Instance.ElectricityManager.HasEnoughElectricity(totalElectricityNeeded);
            Debug.Log($"[WorkTask] {GetType().Name} electricity check - needed: {totalElectricityNeeded}, available: {hasElectricity}");
            
            if (!hasElectricity)
            {
                Debug.Log($"[WorkTask] {GetType().Name} cannot be performed - insufficient electricity");
                SetOperationalStatus(false);
                return false;
            }
        }

        Debug.Log($"[WorkTask] {GetType().Name} can be performed");
        return true;
    }

    // Method to check if the player has required resources
    public bool HasRequiredResources()
    {
        if (requiredResources == null || requiredResources.Length == 0)
            return true; // No resources required

        foreach (var resource in requiredResources)
        {
            if (resource == null || resource.resourceScriptableObj == null)
            {
                Debug.LogWarning($"[WorkTask] Invalid resource in requiredResources array");
                return false;
            }

            if (PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj) < resource.count)
            {
                return false;
            }
        }
        return true;
    }

    // Method to consume required resources
    protected void ConsumeResources()
    {
        if (requiredResources == null || requiredResources.Length == 0)
            return; // No resources to consume

        foreach (var resourceItem in requiredResources)
        {
            if (resourceItem == null || resourceItem.resourceScriptableObj == null)
            {
                Debug.LogWarning($"[WorkTask] Invalid resource in requiredResources array");
                continue;
            }

            PlayerInventory.Instance.RemoveItem(resourceItem.resourceScriptableObj, resourceItem.count);
        }
    }

    // Declare StopWork as an event
    public event Action StopWork; // Called when a construction is complete, building is broken, etc..

    public event Action OnTaskCompleted;

    protected virtual void OnDestroy()
    {
        // No need to unregister electricity consumption anymore since it's handled during work
    }

    protected void AddWorkTask()
    {
        // Only add to work queue if autoQueue is enabled
        if (autoQueue)
        {
            CampManager.Instance.WorkManager.AddWorkTask(this);
        }
    }

    // Helper method to trigger the event safely (other classes can call this to invoke StopWork)
    protected void InvokeStopWork()
    {
        StopWork?.Invoke();
    }

    // Method to assign an NPC to this task
    public bool AssignNPC(HumanCharacterController npc)
    {
        Debug.Log($"[WorkTask] AssignNPC called for {GetType().Name} - NPC: {npc.name}, currentWorkers: {currentWorkers.Count}, maxWorkers: {maxWorkers}");
        
        if (currentWorkers.Contains(npc))
        {
            Debug.Log($"[WorkTask] NPC {npc.name} already assigned to {GetType().Name}");
            return false; // Already assigned
        }
        
        if (currentWorkers.Count >= maxWorkers)
        {
            Debug.Log($"[WorkTask] Task {GetType().Name} is full, cannot assign {npc.name}");
            return false; // Task is full
        }
        
        currentWorkers.Add(npc);
        Debug.Log($"[WorkTask] Assigned {npc.name} to {GetType().Name}, currentWorkers: {currentWorkers.Count}");
        
        if (taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(this);
        }
        
        // Don't start effects here - they should start when work actually begins
        // Effects will be managed in PerformTask() and CompleteWork()
        
        // Notify the worker that they can start working
        npc.StartWork(this);
        
        return true;
    }

    // Method to unassign the current NPC
    public void UnassignNPC()
    {
        if (currentWorkers.Count > 0)
        {
            currentWorkers.RemoveAt(currentWorkers.Count - 1); // Remove the last assigned NPC
        }
        
        // Effects will be managed by work lifecycle, not assignment
        if (currentWorkers.Count == 0 && taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(null);
        }
    }

    // Method to unassign a specific NPC
    public void UnassignNPC(HumanCharacterController npc)
    {
        if (currentWorkers.Contains(npc))
        {
            currentWorkers.Remove(npc);
        }
        
        // Effects will be managed by work lifecycle, not assignment
        if (currentWorkers.Count == 0 && taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(null);
        }
    }

    // Method to check if the task is currently assigned
    public bool IsAssigned()
    {
        return currentWorkers.Count > 0;
    }
    
    /// <summary>
    /// Get the final work speed including all modifiers (hunger, cleanliness, time of day, etc.)
    /// </summary>
    /// <param name="worker">The worker performing the work</param>
    /// <returns>Final work speed multiplier</returns>
    protected float GetFinalWorkSpeed(HumanCharacterController worker)
    {
        // Get worker speed multiplier (hunger, etc.)
        float workSpeed = 1f;
        if (worker is SettlerNPC settler)
        {
            workSpeed = settler.GetWorkSpeedMultiplier();
            // If settler is starving, they can't work
            if (workSpeed <= 0)
            {
                return 0f;
            }
        }
        
        // Apply cleanliness productivity modifier
        float cleanlinessMultiplier = 1f;
        if (CampManager.Instance?.CleanlinessManager != null)
        {
            cleanlinessMultiplier = CampManager.Instance.CleanlinessManager.GetProductivityMultiplier();
        }
        
        // Apply time of day modifier
        float timeMultiplier = 1f;
        if (GameManager.Instance?.TimeManager != null)
        {
            timeMultiplier = GameManager.Instance.TimeManager.GetWorkEfficiencyMultiplier(this);
        }
        
        // Combine all speed modifiers
        return workSpeed * cleanlinessMultiplier * timeMultiplier;
    }

    /// <summary>
    /// Simple method called by WorkState to advance work progress
    /// </summary>
    /// <param name="worker">The worker performing the work</param>
    /// <param name="deltaTime">Time since last frame</param>
    /// <returns>True if work can continue, false if should stop</returns>
    public virtual bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        if (!isOperational || !currentWorkers.Contains(worker))
        {
            return false;
        }

        // Get final work speed (including cleanliness modifier)
        float finalWorkSpeed = GetFinalWorkSpeed(worker);
        
        // If worker can't work (starving, etc.), stop
        if (finalWorkSpeed <= 0)
        {
            return false;
        }

        // Calculate work progress for this frame
        float workDelta = deltaTime * finalWorkSpeed;
        
        // Generate dirt from work activity (only for non-cleaning tasks)
        if (CampManager.Instance?.CleanlinessManager != null && !(this is DirtPileTask))
        {
            CampManager.Instance.CleanlinessManager.GenerateDirtFromWork(workDelta);
        }
        
        // Validate work task data
        if (baseWorkTime <= 0)
        {
            Debug.LogError($"[WorkTask] Invalid baseWorkTime ({baseWorkTime}) for {GetType().Name}. Work task cannot be performed. NPC {worker.name} will return to wander state.");
            SetOperationalStatus(false);
            return false;
        }
        
        // All tasks consume electricity while working (default 1 unit per baseWorkTime)
        // Distribute electricity consumption across all current workers
        float electricityConsumption = electricityRequired > 0 ? electricityRequired : 1f;
        float electricityRate = electricityConsumption / baseWorkTime;
        float electricityPerWorker = electricityRate / Mathf.Max(1, currentWorkers.Count);
        float electricityNeeded = electricityPerWorker * workDelta;
    
        
        // Check and consume electricity
        if (electricityNeeded > 0)
        {
            if (!CampManager.Instance.ElectricityManager.ConsumeElectricity(electricityNeeded, 1f))
            {
                // Not enough electricity, task becomes non-operational
                SetOperationalStatus(false);
                return false;
            }
        }
        
        // Advance work progress
        workProgress += workDelta;
        
        // Update progress bar if active
        if (progressBarActive && CampManager.Instance?.WorkManager != null)
        {
            float progressPercentage = workProgress / baseWorkTime;
            WorkTaskProgressState state = finalWorkSpeed <= 0 ? WorkTaskProgressState.Paused : WorkTaskProgressState.Normal;
            CampManager.Instance.WorkManager.UpdateProgress(this, progressPercentage, state);
        }
        
        // Check if work is complete
        if (workProgress >= baseWorkTime)
        {
            workProgress = baseWorkTime;
            CompleteWork();
            return false; // Work is done
        }
        
        return true; // Continue working
    }

    // Virtual method for completing work that can be overridden
    protected virtual void CompleteWork()
    {
        // Store the first worker as previous worker before clearing (for backward compatibility)
        if (currentWorkers.Count > 0)
        {
            CampManager.Instance.WorkManager.StorePreviousWorker(this, currentWorkers[0]);
        }
        
        // Reset state
        workProgress = 0f;
        
        // Stop all workers from working on this task
        var workersToStop = new List<HumanCharacterController>(currentWorkers);
        
        foreach (var worker in workersToStop)
        {
            worker.StopWork();
        }
        
        // Clear all workers
        currentWorkers.Clear();
        
        // Remove this task from the work queue now that it's completed
        if (autoQueue)
        {
            CampManager.Instance.WorkManager.RemoveTaskFromQueue(this);
        }
        
        // Hide progress bar when work is complete
        if (progressBarActive && CampManager.Instance?.WorkManager != null)
        {
            CampManager.Instance.WorkManager.HideProgressBar(this);
            progressBarActive = false;
        }
        
        // Switch back to idle effects when work is complete
        if (isOperational)
        {
            SetAmbientEffectsState(false); // Back to idle state
        }
        
        if (taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(null);
            Debug.Log($"[WorkTask] Set task structure current work task to null");
        }
        
        // Notify completion
        OnTaskCompleted?.Invoke();
        InvokeStopWork();
        Debug.Log($"[WorkTask] Work completion notifications sent");
    }

    // New method to notify task completion without stopping work
    protected void NotifyTaskCompletion()
    {
        OnTaskCompleted?.Invoke();
    }

    public string GetAnimationClipName()
    {
        return taskAnimation.ToString();
    }

    protected void AddResourceToInventory(ResourceItemCount resourceItemCount)
    {
        PlayerInventory.Instance.AddItem(resourceItemCount.resourceScriptableObj, resourceItemCount.count);
    }

    protected void AddResourceToInventory(ResourceItemCount[] resourceItemCounts)
    {
        foreach (var resourceItemCount in resourceItemCounts)
        {
            PlayerInventory.Instance.AddItem(resourceItemCount.resourceScriptableObj, resourceItemCount.count);
        }
    }

    protected virtual void OnDisable()
    {
        // No need to stop coroutine here, workers manage their own
    }

    public void SetOperationalStatus(bool operational)
    {
        if (isOperational != operational)
        {
            Debug.Log($"[WorkTask] {GetType().Name} operational status changed from {isOperational} to {operational}");
            isOperational = operational;
            
            if (!isOperational)
            {
                Debug.Log($"[WorkTask] {GetType().Name} becoming non-operational, stopping all workers");
                
                // Hide progress bar when becoming non-operational
                if (progressBarActive && CampManager.Instance?.WorkManager != null)
                {
                    CampManager.Instance.WorkManager.HideProgressBar(this);
                    progressBarActive = false;
                }
                
                // Stop all ambient effects when becoming non-operational
                StopAmbientEffects(inUseParticleSystems, inUseAudioSources);
                StopAmbientEffects(idleParticleSystems, idleAudioSources);
                currentlyInUse = false; // Reset the state tracker
                
                // Stop all current workers
                if (currentWorkers.Count > 0)
                {
                    var workersToUnassign = new List<HumanCharacterController>(currentWorkers);
                    foreach (var worker in workersToUnassign)
                    {
                        
                        if (worker is SettlerNPC settler)
                        {
                            settler.ClearAssignedWork(); // Clear the assigned work
                            settler.ChangeTask(TaskType.WANDER);
                        }
                        else if (worker is RobotCharacterController robot)
                        {
                            robot.StopWork();
                        }
                    }
                    currentWorkers.Clear();
                    
                    if (taskStructure != null)
                    {
                        taskStructure.SetCurrentWorkTask(null);
                    }
                }
            }
            else
            {
                // Start idle ambient effects when becoming operational
                SetAmbientEffectsState(false);
                
                // If we become operational again and have a previous worker, try to reassign them
                var previousWorker = CampManager.Instance.WorkManager.GetPreviousWorkerForTask(this);
                if (previousWorker != null)
                {
                    // Check if we have enough electricity before reassigning
                    if (electricityRequired > 0 && !CampManager.Instance.ElectricityManager.HasEnoughElectricity(electricityRequired))
                    {
                        // Still not enough electricity, keep as non-operational
                        isOperational = false;
                        return;
                    }
                    
                    CampManager.Instance.WorkManager.SetNPCForAssignment(previousWorker);
                    AssignNPC(previousWorker);
                    if (previousWorker is SettlerNPC settler)
                    {
                        settler.StartWork(this);
                    }
                }
            }
        }
    }

    public bool IsOperational()
    {
        return isOperational;
    }

    public float GetElectricityRequired()
    {
        return electricityRequired;
    }

    public float GetProgress()
    {
        if (baseWorkTime <= 0)
        {
            return 0f;
        }
        return workProgress / baseWorkTime;
    }

    /// <summary>
    /// Set the work location transform for this task
    /// </summary>
    /// <param name="location">The transform to set as the work location</param>
    public void SetWorkLocation(Transform location)
    {
        workLocationTransform = location;
    }

    // Method to remove a specific worker from the task
    public bool RemoveWorker(HumanCharacterController npc)
    {
        if (currentWorkers.Contains(npc))
        {
            currentWorkers.Remove(npc);
            
            // Stop the worker from working on this task
            npc.StopWork();
            
            // Effects will be managed by work lifecycle, not worker management
            if (currentWorkers.Count == 0 && taskStructure != null)
            {
                taskStructure.SetCurrentWorkTask(null);
            }
            
            return true;
        }
        return false;
    }

    /// <summary>
    /// Set the ambient effects state based on whether the task is in use
    /// </summary>
    /// <param name="inUse">True if task is being used, false if idle</param>
    protected virtual void SetAmbientEffectsState(bool inUse)
    {
        if (currentlyInUse == inUse) return; // No change needed
        
        currentlyInUse = inUse;
        
        if (inUse)
        {
            // Stop idle effects
            StopAmbientEffects(idleParticleSystems, idleAudioSources);
            
            // Start in-use effects
            StartAmbientEffects(inUseParticleSystems, inUseAudioSources);
        }
        else
        {
            // Stop in-use effects
            StopAmbientEffects(inUseParticleSystems, inUseAudioSources);
            
            // Start idle effects
            StartAmbientEffects(idleParticleSystems, idleAudioSources);
        }
    }

    /// <summary>
    /// Start ambient effects (particle systems and audio) - all effects should be set to loop
    /// </summary>
    /// <param name="particles">Particle systems to start</param>
    /// <param name="audioSources">Audio sources to start</param>
    protected virtual void StartAmbientEffects(ParticleSystem[] particles, AudioSource[] audioSources)
    {
        if (particles != null)
        {
            foreach (var ps in particles)
            {
                if (ps != null)
                {
                    ps.gameObject.SetActive(true);
                    if (!ps.isPlaying)
                    {
                        ps.Play();
                    }
                }
            }
        }
        
        if (audioSources != null)
        {
            foreach (var audio in audioSources)
            {
                if (audio != null)
                {
                    audio.gameObject.SetActive(true);
                    if (!audio.isPlaying)
                    {
                        audio.loop = true; // Ensure audio loops
                        audio.Play();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Stop ambient effects (particle systems and audio)
    /// </summary>
    /// <param name="particles">Particle systems to stop</param>
    /// <param name="audioSources">Audio sources to stop</param>
    protected virtual void StopAmbientEffects(ParticleSystem[] particles, AudioSource[] audioSources)
    {
        if (particles != null)
        {
            foreach (var ps in particles)
            {
                if (ps != null)
                {
                    if (ps.isPlaying)
                    {
                        ps.Stop();
                    }
                    ps.gameObject.SetActive(false);
                }
            }
        }
        
        if (audioSources != null)
        {
            foreach (var audio in audioSources)
            {
                if (audio != null)
                {
                    if (audio.isPlaying)
                    {
                        audio.Stop();
                    }
                    audio.gameObject.SetActive(false);
                }
            }
        }
    }

}
