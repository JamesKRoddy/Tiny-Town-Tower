using UnityEngine;
using System.Collections.Generic;
using Managers;

/// <summary>
/// WorkTask for medical buildings - handles treatment of sick NPCs
/// This task is automatically assigned to medical buildings and manages patient treatment
/// </summary>
public class MedicalTask : WorkTask
{
    [Header("Medical Task Settings")]
    [SerializeField] private float baseTreatmentTime = 30f; // Base time to treat one patient (seconds)
    
    private MedicalBuilding medicalBuilding;
    private Dictionary<SettlerNPC, float> patientTreatmentProgress = new Dictionary<SettlerNPC, float>();

    public override bool IsTaskCompleted => false; // Medical buildings are always operational
    public override bool HasQueuedTasks => medicalBuilding != null && medicalBuilding.CurrentPatientCount > 0;

    protected override void Start()
    {
        base.Start();
        
        // Get reference to medical building
        medicalBuilding = GetComponent<MedicalBuilding>();
        if (medicalBuilding == null)
        {
            Debug.LogError($"[MedicalTask] {name} - MedicalTask requires a MedicalBuilding component!");
            return;
        }

        // Setup task properties
        baseWorkTime = baseTreatmentTime;
        maxWorkers = medicalBuilding.MaxPatients; // Medical building handles patient capacity
        
        // Medical tasks manage treatment, NPCs assign themselves via MedicalTreatmentState
    }

    protected override void OnDestroy()
    {
        // Medical tasks are self-managed, no cleanup needed
        base.OnDestroy();
    }

    public override bool CanPerformTask()
    {
        if (medicalBuilding == null)
            return false;
            
        // Task can be performed if building is operational and has capacity
        return medicalBuilding.IsOperational() && 
               !medicalBuilding.IsUnderConstruction() && 
               medicalBuilding.HasAvailableSlots;
    }

    public new bool AssignNPC(HumanCharacterController worker)
    {
        // For medical tasks, "workers" are actually patients
        if (worker is SettlerNPC settler && settler.IsSick)
        {
            if (medicalBuilding.CanAcceptPatient(settler))
            {
                bool success = base.AssignNPC(worker);
                if (success)
                {
                    medicalBuilding.AddPatient(settler);
                    patientTreatmentProgress[settler] = 0f;
                    
                    Debug.Log($"[MedicalTask] {settler.name} assigned for medical treatment at {name}");
                }
                return success;
            }
            else
            {
                Debug.LogWarning($"[MedicalTask] Cannot accept patient {settler.name} at {name}");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[MedicalTask] Only sick NPCs can be assigned to medical tasks. {worker.name} is not sick or not a SettlerNPC");
            return false;
        }
    }

    public new void UnassignNPC(HumanCharacterController worker)
    {
        if (worker is SettlerNPC settler)
        {
            base.UnassignNPC(worker);
            medicalBuilding.RemovePatient(settler);
            patientTreatmentProgress.Remove(settler);
            
            Debug.Log($"[MedicalTask] {settler.name} unassigned from medical treatment at {name}");
        }
    }

    public override Transform GetNavMeshDestination()
    {
        if (medicalBuilding == null)
            return transform;
            
        return medicalBuilding.GetAvailableTreatmentPoint();
    }

    public new Transform GetPrecisePosition()
    {
        return GetNavMeshDestination();
    }

    void Update()
    {
        if (medicalBuilding == null)
            return;
            
        // Process treatment for all current patients
        ProcessPatientTreatment();
    }

    /// <summary>
    /// Process treatment for all current patients
    /// </summary>
    private void ProcessPatientTreatment()
    {
        var patientsToRemove = new List<SettlerNPC>();
        
        foreach (var patient in medicalBuilding.GetCurrentPatients())
        {
            if (patient == null || !patient.IsSick)
            {
                // Patient is no longer sick or is null, remove them
                patientsToRemove.Add(patient);
                continue;
            }
            
            // Only process treatment if patient is actually working on this medical task
            // (i.e., in WORK state working on this medical task and close to building)
            if (patient.GetCurrentTaskType() == TaskType.WORK && currentWorkers.Contains(patient))
            {
                float distance = Vector3.Distance(patient.transform.position, transform.position);
                if (distance <= 3f) // Close enough to receive treatment
                {
                    ProcessTreatmentForPatient(patient);
                }
            }
        }
        
        // Remove patients who are no longer sick
        foreach (var patient in patientsToRemove)
        {
            UnassignNPC(patient);
        }
    }

    /// <summary>
    /// Process treatment for a specific patient
    /// </summary>
    /// <param name="patient">The patient to treat</param>
    private void ProcessTreatmentForPatient(SettlerNPC patient)
    {
        if (!patientTreatmentProgress.ContainsKey(patient))
        {
            patientTreatmentProgress[patient] = 0f;
        }
        
        // Calculate treatment progress
        float treatmentSpeed = medicalBuilding.TreatmentSpeedMultiplier;
        float progressPerSecond = treatmentSpeed / baseTreatmentTime;
        patientTreatmentProgress[patient] += progressPerSecond * Time.deltaTime;
        
        // Check if treatment is complete
        if (patientTreatmentProgress[patient] >= 1f)
        {
            CompleteTreatment(patient);
        }
    }

    /// <summary>
    /// Complete treatment for a patient, curing them
    /// </summary>
    /// <param name="patient">The patient to cure</param>
    private void CompleteTreatment(SettlerNPC patient)
    {
        Debug.Log($"[MedicalTask] Treatment completed for {patient.name} at {name}");
        
        // Force recovery from sickness
        // We'll need to add a public method to SettlerNPC for this
        if (patient.TryGetComponent<SettlerNPC>(out var settlerNPC))
        {
            // Call a method to force recovery (we'll need to add this to SettlerNPC)
            patient.SendMessage("ForceRecoveryFromSickness", SendMessageOptions.DontRequireReceiver);
        }
        
        // Remove patient from medical building
        UnassignNPC(patient);
    }



    /// <summary>
    /// Get treatment progress for a specific patient (0-1)
    /// </summary>
    /// <param name="patient">The patient to check</param>
    /// <returns>Treatment progress from 0 to 1</returns>
    public float GetTreatmentProgress(SettlerNPC patient)
    {
        return patientTreatmentProgress.TryGetValue(patient, out float progress) ? progress : 0f;
    }

    public string GetTaskDescription()
    {
        if (medicalBuilding == null)
            return "Medical Treatment (No Building)";
            
        return $"Medical Treatment ({medicalBuilding.CurrentPatientCount}/{medicalBuilding.MaxPatients} patients)";
    }

    public override string GetTooltipText()
    {
        if (medicalBuilding == null)
            return "Medical treatment task without medical building";
            
        string tooltip = $"Medical Building Treatment\n";
        tooltip += $"Patients: {medicalBuilding.CurrentPatientCount}/{medicalBuilding.MaxPatients}\n";
        tooltip += $"Treatment Speed: {medicalBuilding.TreatmentSpeedMultiplier}x faster\n";
        tooltip += $"Base Treatment Time: {baseTreatmentTime}s\n";
        
        if (medicalBuilding.CurrentPatientCount > 0)
        {
            tooltip += "\nCurrent Patients:\n";
            foreach (var patient in medicalBuilding.GetCurrentPatients())
            {
                if (patient != null)
                {
                    float progress = GetTreatmentProgress(patient) * 100f;
                    tooltip += $"- {patient.name}: {progress:F0}% treated\n";
                }
            }
        }
        
        return tooltip;
    }
}
