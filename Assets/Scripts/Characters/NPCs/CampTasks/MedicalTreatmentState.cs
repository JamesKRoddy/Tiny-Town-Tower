using UnityEngine;
using UnityEngine.AI;
using Managers;

/// <summary>
/// Task state for NPCs receiving medical treatment at a medical building
/// NPCs in this state will move to the medical building and receive accelerated healing
/// </summary>
public class MedicalTreatmentState : _TaskState
{
    private MedicalBuilding assignedMedicalBuilding;
    private MedicalTask assignedMedicalTask;
    private bool hasReachedTreatment = false;
    private bool needsPrecisePositioning = false;
    private bool isPlayingTreatmentAnimation = false;

    public override TaskType GetTaskType()
    {
        return TaskType.MEDICAL_TREATMENT;
    }

    public override void OnEnterState()
    {
        Debug.Log($"[MedicalTreatmentState] {npc.name} entered medical treatment state");
        
        ResetAgentState();
        
        // Initialize positioning variables
        hasReachedTreatment = false;
        needsPrecisePositioning = false;
        isPlayingTreatmentAnimation = false;
        
        // Check if NPC is actually sick
        if (!npc.IsSick)
        {
            Debug.LogWarning($"[MedicalTreatmentState] {npc.name} entered medical treatment but is not sick - changing to wander");
            TryAssignWorkOrWander();
            return;
        }
        
        // Try to find and assign to a medical building
        if (!FindAndAssignMedicalBuilding())
        {
            Debug.LogWarning($"[MedicalTreatmentState] {npc.name} could not find available medical building - changing to wander");
            TryAssignWorkOrWander();
            return;
        }
        
        // Set up navigation immediately after assignment (like WorkState does)
        SetupNavigationToTreatment();
    }

    public override void OnExitState()
    {
        Debug.Log($"[MedicalTreatmentState] {npc.name} exited medical treatment state");
        
        // Stop treatment animation if playing
        if (isPlayingTreatmentAnimation)
        {
            StopTreatmentAnimation();
        }
        
        // Remove patient from medical building if assigned
        if (assignedMedicalBuilding != null)
        {
            assignedMedicalBuilding.RemovePatient(npc);
        }
        
        // Reset state variables
        assignedMedicalBuilding = null;
        assignedMedicalTask = null;
        isPlayingTreatmentAnimation = false;
    }

    public override void UpdateState()
    {
        // Check if NPC is still sick
        if (!npc.IsSick)
        {
            Debug.Log($"[MedicalTreatmentState] {npc.name} is no longer sick - completing treatment");
            TryAssignWorkOrWander();
            return;
        }
        
        // If we don't have an assigned medical building, try to find one
        if (assignedMedicalBuilding == null || assignedMedicalTask == null)
        {
            if (!FindAndAssignMedicalBuilding())
            {
                TryAssignWorkOrWander();
                return;
            }
            // Note: Navigation setup happens in OnEnterState, not here
        }
        
        // Handle navigation and positioning
        if (assignedMedicalBuilding != null && assignedMedicalTask != null)
        {
            HandleTreatmentNavigation();
        }
    }

    /// <summary>
    /// Find and assign NPC to an available medical building
    /// </summary>
    /// <returns>True if successfully assigned to a medical building</returns>
    private bool FindAndAssignMedicalBuilding()
    {
        // Find all medical buildings
        var medicalBuildings = FindObjectsByType<MedicalBuilding>(FindObjectsSortMode.None);
        var availableBuildings = new System.Collections.Generic.List<MedicalBuilding>();
        
        foreach (var building in medicalBuildings)
        {
            if (building.CanAcceptPatient(npc))
            {
                availableBuildings.Add(building);
            }
        }
        
        if (availableBuildings.Count == 0)
        {
            Debug.LogWarning($"[MedicalTreatmentState] No available medical buildings found for {npc.name}");
            return false;
        }
        
        // Find the closest available medical building
        MedicalBuilding closestBuilding = null;
        float closestDistance = float.MaxValue;
        
        foreach (var building in availableBuildings)
        {
            float distance = Vector3.Distance(npc.transform.position, building.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBuilding = building;
            }
        }
        
        // Assign to the closest building
        if (closestBuilding != null)
        {
            assignedMedicalBuilding = closestBuilding;
            assignedMedicalTask = closestBuilding.GetComponent<MedicalTask>();
            
            if (assignedMedicalTask != null)
            {
                // Add patient to the medical building without starting work task
                if (assignedMedicalBuilding.AddPatient(npc))
                {
                    Debug.Log($"[MedicalTreatmentState] {npc.name} assigned to medical building: {closestBuilding.name}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[MedicalTreatmentState] Failed to add {npc.name} as patient to {closestBuilding.name}");
                    assignedMedicalBuilding = null;
                    assignedMedicalTask = null;
                    return false;
                }
            }
            else
            {
                Debug.LogError($"[MedicalTreatmentState] Medical building {closestBuilding.name} has no MedicalTask component");
                assignedMedicalBuilding = null;
                return false;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Setup navigation to the treatment point
    /// </summary>
    private void SetupNavigationToTreatment()
    {
        if (assignedMedicalBuilding == null)
            return;
            
        Transform treatmentPoint = assignedMedicalBuilding.GetAvailableTreatmentPoint();
        
        // Use base class method for consistent NavMesh setup
        SetupNavMeshForWorkTask(treatmentPoint, 0.5f);
        
        // Set slower movement speed when sick
        if (agent != null)
        {
            agent.speed = npc.moveMaxSpeed * 0.8f;
        }
        
        // Reset positioning flags
        hasReachedTreatment = false;
        needsPrecisePositioning = false;
        
        Debug.Log($"[MedicalTreatmentState] {npc.name} navigating to treatment point at {treatmentPoint.position}");
    }
    
    /// <summary>
    /// Handle navigation to treatment point and precise positioning
    /// </summary>
    private void HandleTreatmentNavigation()
    {
        Transform treatmentPoint = assignedMedicalBuilding.GetAvailableTreatmentPoint();
        
        // Use the same two-phase approach as WorkState:
        // Phase 1: Use NavMesh to get close to the treatment area
        // Phase 2: Use precise positioning to lerp to exact position
        
        // Check if we've reached the treatment area using NavigationUtils (Phase 1)
        bool hasReachedDestination = NavigationUtils.HasReachedDestination(agent, treatmentPoint, 0.5f, 1f);
        
        if (hasReachedDestination)
        {
            HandleReachedTreatment(treatmentPoint);
        }
        else
        {
            HandleMovingToTreatment();
        }
    }
    
    /// <summary>
    /// Handle reaching the treatment point (Phase 2)
    /// </summary>
    private void HandleReachedTreatment(Transform treatmentPoint)
    {
        // Use the same logic as WorkState for precise positioning
        bool justReached = HandleReachedDestination(ref hasReachedTreatment, ref needsPrecisePositioning, treatmentPoint);
        
        if (justReached)
        {
            Debug.Log($"[MedicalTreatmentState] {npc.name} reached treatment point, starting precise positioning");
        }
        
        // Handle precise positioning if needed (Phase 2)
        if (needsPrecisePositioning)
        {
            bool positioningComplete = UpdatePrecisePositioning(treatmentPoint, ref needsPrecisePositioning);
            if (positioningComplete)
            {
                Debug.Log($"[MedicalTreatmentState] {npc.name} precise positioning complete, starting treatment");
                StartTreatmentAnimation();
            }
        }
        
        // Apply treatment once positioned (or if no precise positioning needed)
        if (!needsPrecisePositioning)
        {
            // Start animation if not already playing and we're positioned
            if (!isPlayingTreatmentAnimation && hasReachedTreatment)
            {
                StartTreatmentAnimation();
            }
            
            ApplyMedicalTreatment();
        }
    }
    
    /// <summary>
    /// Handle moving towards the treatment point (Phase 1)
    /// </summary>
    private void HandleMovingToTreatment()
    {
        // Reset the reached flag if we're moving away from destination
        if (hasReachedTreatment)
        {
            hasReachedTreatment = false;
            needsPrecisePositioning = false;
            
            // Stop treatment animation if we're moving away
            if (isPlayingTreatmentAnimation)
            {
                StopTreatmentAnimation();
            }
            
            // Re-enable NavMesh control
            if (agent != null)
            {
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
            }
        }
    }
    
    /// <summary>
    /// Apply medical treatment to accelerate healing
    /// </summary>
    private void ApplyMedicalTreatment()
    {
        if (assignedMedicalBuilding == null || !npc.IsSick)
            return;
            
        // Apply accelerated healing based on medical building's treatment speed multiplier
        float treatmentSpeedMultiplier = assignedMedicalBuilding.TreatmentSpeedMultiplier;
        
        // The medical treatment accelerates the natural recovery process
        // We don't directly heal here, but the medical building's presence 
        // speeds up the natural sickness recovery time
        
        // Only log every few seconds to avoid spam
        if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
        {
            Debug.Log($"[MedicalTreatmentState] {npc.name} receiving medical treatment (speed: {treatmentSpeedMultiplier}x)");
        }
    }

    /// <summary>
    /// Check if there are any available medical buildings
    /// Used by other states to determine if medical treatment is an option
    /// </summary>
    /// <returns>True if medical buildings are available</returns>
    public static bool HasAvailableMedicalBuildings()
    {
        var medicalBuildings = FindObjectsByType<MedicalBuilding>(FindObjectsSortMode.None);
        
        foreach (var building in medicalBuildings)
        {
            if (building.IsOperational() && !building.IsUnderConstruction() && building.HasAvailableSlots)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Start the medical treatment animation (NPC lying in bed)
    /// </summary>
    private void StartTreatmentAnimation()
    {
        if (isPlayingTreatmentAnimation || assignedMedicalTask == null)
            return;
            
        // Play the treatment animation using the task's animation
        string animationName = assignedMedicalTask.GetAnimationClipName();
        npc.PlayWorkAnimation(animationName);
        isPlayingTreatmentAnimation = true;
        
        Debug.Log($"[MedicalTreatmentState] {npc.name} started medical treatment animation: {animationName}");
    }
    
    /// <summary>
    /// Stop the medical treatment animation
    /// </summary>
    private void StopTreatmentAnimation()
    {
        if (!isPlayingTreatmentAnimation)
            return;
            
        // Stop the treatment animation
        npc.StopWorkAnimation();
        isPlayingTreatmentAnimation = false;
        
        Debug.Log($"[MedicalTreatmentState] {npc.name} stopped medical treatment animation");
    }
}
