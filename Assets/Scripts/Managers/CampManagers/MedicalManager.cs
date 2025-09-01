using UnityEngine;
using System.Collections.Generic;
using System;

namespace Managers
{
    /// <summary>
    /// Manages medical buildings and treatment in the camp
    /// </summary>
    public class MedicalManager : MonoBehaviour
    {
            [Header("Medical Settings")]
    [SerializeField] private float treatmentCheckInterval = 5f; // How often to check for sick NPCs needing treatment
        
        private List<MedicalBuilding> registeredMedicalBuildings = new List<MedicalBuilding>();
        private float lastTreatmentCheck = 0f;
        
        // Events
        public event Action<MedicalBuilding> OnMedicalBuildingRegistered;
        public event Action<MedicalBuilding> OnMedicalBuildingUnregistered;
        public event Action<SettlerNPC, MedicalBuilding> OnPatientAssignedToTreatment;
        public event Action<SettlerNPC> OnPatientRecovered;
        
        public void Initialize()
        {
            registeredMedicalBuildings = new List<MedicalBuilding>();
            
            // Find and register any existing medical buildings in the scene
            var existingMedicalBuildings = FindObjectsByType<MedicalBuilding>(FindObjectsSortMode.None);
            foreach (var building in existingMedicalBuildings)
            {
                if (building.IsOperational())
                {
                    RegisterMedicalBuilding(building);
                }
            }
            
            Debug.Log($"[MedicalManager] Initialized with {registeredMedicalBuildings.Count} medical buildings");
        }
        
        private void Update()
        {
            // Periodically check for sick NPCs that need treatment
            if (Time.time - lastTreatmentCheck >= treatmentCheckInterval)
            {
                CheckForSickNPCsNeedingTreatment();
                lastTreatmentCheck = Time.time;
            }
        }
        
        /// <summary>
        /// Register a medical building with the medical system
        /// </summary>
        /// <param name="medicalBuilding">The medical building to register</param>
        public void RegisterMedicalBuilding(MedicalBuilding medicalBuilding)
        {
            if (medicalBuilding != null && !registeredMedicalBuildings.Contains(medicalBuilding))
            {
                registeredMedicalBuildings.Add(medicalBuilding);
                
                // Subscribe to treatment slot availability events
                medicalBuilding.OnTreatmentSlotAvailable += HandleTreatmentSlotAvailable;
                
                OnMedicalBuildingRegistered?.Invoke(medicalBuilding);
                Debug.Log($"[MedicalManager] Registered medical building: {medicalBuilding.name}");
            }
        }
        
        /// <summary>
        /// Unregister a medical building from the medical system
        /// </summary>
        /// <param name="medicalBuilding">The medical building to unregister</param>
        public void UnregisterMedicalBuilding(MedicalBuilding medicalBuilding)
        {
            if (medicalBuilding != null && registeredMedicalBuildings.Remove(medicalBuilding))
            {
                // Unsubscribe from events
                medicalBuilding.OnTreatmentSlotAvailable -= HandleTreatmentSlotAvailable;
                
                OnMedicalBuildingUnregistered?.Invoke(medicalBuilding);
                Debug.Log($"[MedicalManager] Unregistered medical building: {medicalBuilding.name}");
            }
        }
        
        /// <summary>
        /// Handle when a treatment slot becomes available
        /// </summary>
        /// <param name="medicalBuilding">The building with available slots</param>
        private void HandleTreatmentSlotAvailable(MedicalBuilding medicalBuilding)
        {
            // Try to assign waiting sick NPCs to the available slot
            AssignSickNPCsToMedicalBuilding(medicalBuilding);
        }
        
        /// <summary>
        /// Get all registered medical buildings
        /// </summary>
        /// <returns>Read-only list of medical buildings</returns>
        public IReadOnlyList<MedicalBuilding> GetRegisteredMedicalBuildings()
        {
            return registeredMedicalBuildings.AsReadOnly();
        }
        
        /// <summary>
        /// Get all operational medical buildings
        /// </summary>
        /// <returns>List of operational medical buildings</returns>
        public List<MedicalBuilding> GetOperationalMedicalBuildings()
        {
            var operationalBuildings = new List<MedicalBuilding>();
            
            foreach (var building in registeredMedicalBuildings)
            {
                if (building != null && building.IsOperational())
                {
                    operationalBuildings.Add(building);
                }
            }
            
            return operationalBuildings;
        }
        
        /// <summary>
        /// Find the best medical building for a specific NPC
        /// </summary>
        /// <param name="npc">The sick NPC needing treatment</param>
        /// <returns>Best medical building, or null if none available</returns>
        public MedicalBuilding FindBestMedicalBuildingForNPC(SettlerNPC npc)
        {
            if (npc == null || !npc.IsSick)
                return null;
                
            var operationalBuildings = GetOperationalMedicalBuildings();
            var availableBuildings = new List<MedicalBuilding>();
            
            // Filter buildings that can accept this patient (no distance limit)
            foreach (var building in operationalBuildings)
            {
                if (building.CanAcceptPatient(npc))
                {
                    availableBuildings.Add(building);
                }
            }
            
            if (availableBuildings.Count == 0)
                return null;
                
            // Find the closest available building
            MedicalBuilding bestBuilding = null;
            float closestDistance = float.MaxValue;
            
            foreach (var building in availableBuildings)
            {
                float distance = Vector3.Distance(npc.transform.position, building.transform.position);
                
                // Prioritize buildings with more available slots
                float priorityScore = distance - (building.MaxPatients - building.CurrentPatientCount) * 5f;
                
                if (priorityScore < closestDistance)
                {
                    closestDistance = priorityScore;
                    bestBuilding = building;
                }
            }
            
            return bestBuilding;
        }
        
        /// <summary>
        /// Check if there are available medical buildings for treatment
        /// </summary>
        /// <returns>True if medical buildings are available</returns>
        public bool HasAvailableMedicalBuildings()
        {
            var operationalBuildings = GetOperationalMedicalBuildings();
            
            foreach (var building in operationalBuildings)
            {
                if (building.HasAvailableSlots)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check for sick NPCs that need treatment and try to assign them
        /// </summary>
        private void CheckForSickNPCsNeedingTreatment()
        {
            var operationalBuildings = GetOperationalMedicalBuildings();
            if (operationalBuildings.Count == 0)
                return;
                
            // Find sick NPCs not currently receiving treatment
            var allNPCs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);
            var sickNPCs = new List<SettlerNPC>();
            
            foreach (var npc in allNPCs)
            {
                if (npc.IsSick && npc.GetCurrentTaskType() != TaskType.MEDICAL_TREATMENT)
                {
                    // Check if NPC is not in critical states
                    if (npc.GetCurrentTaskType() != TaskType.FLEE && 
                        npc.GetCurrentTaskType() != TaskType.ATTACK)
                    {
                        sickNPCs.Add(npc);
                    }
                }
            }
            
            // Try to assign sick NPCs to available medical buildings
            foreach (var building in operationalBuildings)
            {
                if (building.HasAvailableSlots)
                {
                    AssignSickNPCsToMedicalBuilding(building, sickNPCs);
                }
            }
        }
        
        /// <summary>
        /// Try to assign sick NPCs to a specific medical building
        /// </summary>
        /// <param name="medicalBuilding">The medical building to assign to</param>
        /// <param name="candidateNPCs">Optional list of candidate NPCs (if null, will find sick NPCs)</param>
        private void AssignSickNPCsToMedicalBuilding(MedicalBuilding medicalBuilding, List<SettlerNPC> candidateNPCs = null)
        {
            if (!medicalBuilding.HasAvailableSlots)
                return;
                
            List<SettlerNPC> sickNPCs = candidateNPCs;
            
            // If no candidates provided, find sick NPCs in range
            if (sickNPCs == null)
            {
                sickNPCs = new List<SettlerNPC>();
                var allNPCs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);
                
                foreach (var npc in allNPCs)
                {
                    if (npc.IsSick && npc.GetCurrentTaskType() != TaskType.MEDICAL_TREATMENT)
                    {
                        sickNPCs.Add(npc);
                    }
                }
            }
            
            // Sort by distance to this medical building
            sickNPCs.Sort((a, b) =>
            {
                float distA = Vector3.Distance(a.transform.position, medicalBuilding.transform.position);
                float distB = Vector3.Distance(b.transform.position, medicalBuilding.transform.position);
                return distA.CompareTo(distB);
            });
            
            // Assign NPCs until building is full
            foreach (var npc in sickNPCs)
            {
                if (!medicalBuilding.HasAvailableSlots)
                    break;
                    
                if (medicalBuilding.CanAcceptPatient(npc))
                {
                    // Notify the NPC to seek medical treatment
                    npc.ChangeTask(TaskType.MEDICAL_TREATMENT);
                    OnPatientAssignedToTreatment?.Invoke(npc, medicalBuilding);
                    
                    Debug.Log($"[MedicalManager] Assigned {npc.name} to medical treatment at {medicalBuilding.name}");
                }
            }
        }
        
        /// <summary>
        /// Get statistics about the medical system
        /// </summary>
        /// <returns>Medical system statistics</returns>
        public MedicalSystemStats GetMedicalSystemStats()
        {
            var stats = new MedicalSystemStats();
            var operationalBuildings = GetOperationalMedicalBuildings();
            
            stats.TotalMedicalBuildings = registeredMedicalBuildings.Count;
            stats.OperationalMedicalBuildings = operationalBuildings.Count;
            
            foreach (var building in operationalBuildings)
            {
                stats.TotalTreatmentSlots += building.MaxPatients;
                stats.OccupiedTreatmentSlots += building.CurrentPatientCount;
            }
            
            stats.AvailableTreatmentSlots = stats.TotalTreatmentSlots - stats.OccupiedTreatmentSlots;
            
            // Count sick NPCs
            var allNPCs = FindObjectsByType<SettlerNPC>(FindObjectsSortMode.None);
            foreach (var npc in allNPCs)
            {
                if (npc.IsSick)
                {
                    stats.SickNPCs++;
                    if (npc.GetCurrentTaskType() == TaskType.MEDICAL_TREATMENT)
                    {
                        stats.NPCsReceivingTreatment++;
                    }
                }
            }
            
            return stats;
        }
    }
    
    /// <summary>
    /// Statistics about the medical system
    /// </summary>
    [System.Serializable]
    public class MedicalSystemStats
    {
        public int TotalMedicalBuildings;
        public int OperationalMedicalBuildings;
        public int TotalTreatmentSlots;
        public int OccupiedTreatmentSlots;
        public int AvailableTreatmentSlots;
        public int SickNPCs;
        public int NPCsReceivingTreatment;
        
        public override string ToString()
        {
            return $"Medical System Stats:\n" +
                   $"Buildings: {OperationalMedicalBuildings}/{TotalMedicalBuildings} operational\n" +
                   $"Treatment Slots: {OccupiedTreatmentSlots}/{TotalTreatmentSlots} occupied\n" +
                   $"Sick NPCs: {SickNPCs} ({NPCsReceivingTreatment} receiving treatment)";
        }
    }
}
