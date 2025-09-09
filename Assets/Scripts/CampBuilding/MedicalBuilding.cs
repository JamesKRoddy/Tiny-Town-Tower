using UnityEngine;
using Managers;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(MedicalTask))]
public class MedicalBuilding : Building
{
    [Header("Medical Building Settings")]
    [SerializeField] private int maxPatients = 2; // How many NPCs can be treated simultaneously
    [SerializeField] private float treatmentSpeedMultiplier = 3f; // How much faster recovery is at medical building
    [SerializeField] private Transform[] treatmentPoints; // Specific positions where NPCs receive treatment
    
    // Event to notify when treatment slots become available
    public event Action<MedicalBuilding> OnTreatmentSlotAvailable;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject medicalCrossIcon; // Optional visual indicator
    [SerializeField] private ParticleSystem healingEffect; // Optional healing particles
    
    private List<SettlerNPC> currentPatients = new List<SettlerNPC>();
    private MedicalTask medicalTask;
    
    public int CurrentPatientCount => currentPatients.Count;
    public int MaxPatients => maxPatients;
    public bool HasAvailableSlots => currentPatients.Count < maxPatients;
    public float TreatmentSpeedMultiplier => treatmentSpeedMultiplier;

    protected override void Start()
    {
        base.Start();
        medicalTask = GetComponent<MedicalTask>();
        
        // Setup treatment points if not manually assigned
        if (treatmentPoints == null || treatmentPoints.Length == 0)
        {
            SetupDefaultTreatmentPoints();
        }
    }

    public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        base.SetupBuilding(buildingScriptableObj);
        
        // Enable medical cross icon if available
        if (medicalCrossIcon != null)
        {
            medicalCrossIcon.SetActive(true);
        }
    }

    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        
        // Register with CampManager's medical system
        if (CampManager.Instance != null)
        {
            CampManager.Instance.RegisterMedicalBuilding(this);
        }
    }

    public override void StartDestruction()
    {
        // Clear all current patients before destruction
        var patientsToRemove = new List<SettlerNPC>(currentPatients);
        foreach (var patient in patientsToRemove)
        {
            RemovePatient(patient);
        }
        
        // Unregister from CampManager
        if (CampManager.Instance != null)
        {
            CampManager.Instance.UnregisterMedicalBuilding(this);
        }
        
        base.StartDestruction();
    }

    /// <summary>
    /// Check if this medical building can accept a new patient
    /// </summary>
    /// <param name="patient">The NPC that needs treatment</param>
    /// <returns>True if the patient can be accepted</returns>
    public bool CanAcceptPatient(SettlerNPC patient)
    {
        if (!IsOperational() || isUnderConstruction)
            return false;
            
        if (!HasAvailableSlots)
            return false;
            
        if (currentPatients.Contains(patient))
            return false; // Already being treated
            
        return patient.IsSick;
    }

    /// <summary>
    /// Add a patient to this medical building
    /// </summary>
    /// <param name="patient">The sick NPC to treat</param>
    /// <returns>True if patient was successfully added</returns>
    public bool AddPatient(SettlerNPC patient)
    {
        if (!CanAcceptPatient(patient))
        {
            Debug.LogWarning($"[MedicalBuilding] Cannot accept patient {patient.name} at {name}");
            return false;
        }

        currentPatients.Add(patient);
        Debug.Log($"[MedicalBuilding] {patient.name} started treatment at {name} ({CurrentPatientCount}/{MaxPatients} slots occupied)");
        
        // Start healing effect if available
        if (healingEffect != null && !healingEffect.isPlaying)
        {
            healingEffect.Play();
        }
        
        return true;
    }

    /// <summary>
    /// Remove a patient from this medical building
    /// </summary>
    /// <param name="patient">The NPC to remove</param>
    public void RemovePatient(SettlerNPC patient)
    {
        if (currentPatients.Remove(patient))
        {
            Debug.Log($"[MedicalBuilding] {patient.name} finished treatment at {name} ({CurrentPatientCount}/{MaxPatients} slots occupied)");
            
            // Notify that a slot is available
            OnTreatmentSlotAvailable?.Invoke(this);
            
            // Stop healing effect if no more patients and effect is playing
            if (currentPatients.Count == 0 && healingEffect != null && healingEffect.isPlaying)
            {
                healingEffect.Stop();
            }
        }
    }

    /// <summary>
    /// Get the next available treatment point for a patient
    /// </summary>
    /// <returns>Transform of an available treatment point, or building transform if none available</returns>
    public Transform GetAvailableTreatmentPoint()
    {
        if (treatmentPoints == null || treatmentPoints.Length == 0)
            return transform;
            
        // Simple round-robin assignment based on current patient count
        int index = currentPatients.Count % treatmentPoints.Length;
        return treatmentPoints[index] != null ? treatmentPoints[index] : transform;
    }

    /// <summary>
    /// Setup default treatment points around the building
    /// </summary>
    private void SetupDefaultTreatmentPoints()
    {
        treatmentPoints = new Transform[maxPatients];
        
        for (int i = 0; i < maxPatients; i++)
        {
            GameObject treatmentPoint = new GameObject($"TreatmentPoint_{i}");
            treatmentPoint.transform.SetParent(transform);
            
            // Position treatment points around the building
            float angle = (360f / maxPatients) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * 2f, 0f, Mathf.Sin(angle) * 2f);
            treatmentPoint.transform.localPosition = offset;
            
            treatmentPoints[i] = treatmentPoint.transform;
        }
    }

    /// <summary>
    /// Check if a specific NPC is currently being treated here
    /// </summary>
    /// <param name="npc">The NPC to check</param>
    /// <returns>True if the NPC is a current patient</returns>
    public bool IsPatient(SettlerNPC npc)
    {
        return currentPatients.Contains(npc);
    }

    /// <summary>
    /// Get all current patients (for debugging/UI purposes)
    /// </summary>
    /// <returns>Read-only list of current patients</returns>
    public IReadOnlyList<SettlerNPC> GetCurrentPatients()
    {
        return currentPatients.AsReadOnly();
    }

    public override string GetInteractionText()
    {
        if (isUnderConstruction) 
            return "Medical Building under construction";
            
        if (!isOperational) 
            return "Medical Building not operational";
        
        string text = $"Medical Building:\n";
        text += $"Patients: {CurrentPatientCount}/{MaxPatients}\n";
        text += $"Treatment Speed: {treatmentSpeedMultiplier}x\n";
        
        if (repairTask != null && repairTask.CanPerformTask())
            text += "- Repair\n";
        if (upgradeTask != null && upgradeTask.CanPerformTask())
            text += "- Upgrade\n";
            
        return text;
    }

    public override string GetBuildingStatsText()
    {
        string baseStats = base.GetBuildingStatsText();
        
        string medicalStats = $"\nMedical Stats:\n" +
                             $"Max Patients: {MaxPatients}\n" +
                             $"Current Patients: {CurrentPatientCount}\n" +
                             $"Treatment Speed Multiplier: {treatmentSpeedMultiplier}x\n";
        
        return baseStats + medicalStats;
    }

    #region Debug Visualization
    
    private void OnDrawGizmosSelected()
    {
        // Draw treatment points
        if (treatmentPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (var point in treatmentPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireCube(point.position, Vector3.one * 0.5f);
                }
            }
        }
    }
    
    #endregion
}
