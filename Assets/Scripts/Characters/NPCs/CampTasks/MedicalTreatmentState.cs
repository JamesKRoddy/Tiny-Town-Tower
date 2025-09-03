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

    public override TaskType GetTaskType()
    {
        return TaskType.MEDICAL_TREATMENT;
    }

    public override void OnEnterState()
    {
        Debug.Log($"[MedicalTreatmentState] {npc.name} entered medical treatment state");
        
        ResetAgentState();
        
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
    }

    public override void OnExitState()
    {
        Debug.Log($"[MedicalTreatmentState] {npc.name} exited medical treatment state");
        
        // Remove patient from medical building if assigned
        if (assignedMedicalBuilding != null)
        {
            assignedMedicalBuilding.RemovePatient(npc);
        }
        
        // Reset state variables
        assignedMedicalBuilding = null;
        assignedMedicalTask = null;
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
        }
        
        // Check if we're at the medical building
        if (assignedMedicalBuilding != null && assignedMedicalTask != null)
        {
            Transform treatmentPoint = assignedMedicalBuilding.GetAvailableTreatmentPoint();
            float distanceToTreatment = Vector3.Distance(npc.transform.position, treatmentPoint.position);
            
            // If we're close enough to the treatment point, receive treatment
            if (distanceToTreatment <= 2f)
            {
                // Set destination to treatment point and stop moving
                if (agent != null)
                {
                    agent.SetDestination(treatmentPoint.position);
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                
                // Apply accelerated healing while at medical building
                ApplyMedicalTreatment();
            }
            else
            {
                // Move towards the medical building
                if (agent != null)
                {
                    agent.SetDestination(treatmentPoint.position);
                    agent.isStopped = false;
                    agent.speed = npc.moveMaxSpeed * 0.8f; // Slightly slower movement when sick
                }
            }
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
        Debug.Log($"[MedicalTreatmentState] {npc.name} receiving medical treatment (speed: {treatmentSpeedMultiplier}x)");
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
}
