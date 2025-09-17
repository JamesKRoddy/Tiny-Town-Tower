using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Managers
{
    /// <summary>
    /// Central manager for tracking game progression milestones and global flags.
    /// Tracks achievements like building types reached, story milestones, etc.
    /// Integrates with the narrative system for dialogue progression gates.
    /// </summary>
    public class GameProgressionManager : MonoBehaviour
    {
        [Header("Progression Tracking")]
        [SerializeField] private bool debugLogging = true;
        [SerializeField] private List<ProgressionMilestone> milestones = new List<ProgressionMilestone>();
        
        // Runtime tracking
        private Dictionary<string, ProgressionMilestone> milestoneMap = new Dictionary<string, ProgressionMilestone>();
        private HashSet<string> completedMilestones = new HashSet<string>();
        private Dictionary<string, string> globalFlags = new Dictionary<string, string>();
        
        // Events
        public event Action<ProgressionMilestone> OnMilestoneCompleted;
        public event Action<string, string> OnGlobalFlagChanged;

        private void InitializeProgressionSystem()
        {
            // Build milestone lookup map
            milestoneMap.Clear();
            foreach (var milestone in milestones)
            {
                milestoneMap[milestone.milestoneId] = milestone;
            }

            Debug.Log($"[GameProgressionManager] Initialized with {milestones.Count} milestones");
        }

        #region Milestone Tracking

        /// <summary>
        /// Mark a milestone as completed
        /// </summary>
        public void CompleteMilestone(string milestoneId)
        {
            if (string.IsNullOrEmpty(milestoneId))
                return;

            if (completedMilestones.Contains(milestoneId))
            {
                if (debugLogging)
                    Debug.Log($"[GameProgressionManager] Milestone already completed: {milestoneId}");
                return;
            }

            completedMilestones.Add(milestoneId);

            if (milestoneMap.TryGetValue(milestoneId, out var milestone))
            {
                if (debugLogging)
                    Debug.Log($"[GameProgressionManager] Milestone completed: {milestone.displayName} ({milestoneId})");

                // Set any associated flags
                if (milestone.flagsToSet != null)
                {
                    foreach (var flag in milestone.flagsToSet)
                    {
                        SetGlobalFlag(flag.flagName, flag.flagValue);
                    }
                }

                OnMilestoneCompleted?.Invoke(milestone);
            }
            else
            {
                if (debugLogging)
                    Debug.LogWarning($"[GameProgressionManager] Unknown milestone completed: {milestoneId}");
            }
        }

        /// <summary>
        /// Check if a milestone has been completed
        /// </summary>
        public bool IsMilestoneCompleted(string milestoneId)
        {
            return completedMilestones.Contains(milestoneId);
        }

        /// <summary>
        /// Get all completed milestones
        /// </summary>
        public List<string> GetCompletedMilestones()
        {
            return completedMilestones.ToList();
        }

        #endregion

        #region Building Type Tracking

        /// <summary>
        /// Track when player reaches a new building type in roguelike mode
        /// </summary>
        public void OnBuildingTypeReached(RogueLikeBuildingType buildingType)
        {
            if (buildingType == RogueLikeBuildingType.NONE)
                return;

            string milestoneId = $"building_reached_{buildingType.ToString().ToLower()}";
            CompleteMilestone(milestoneId);

            // Also set a direct flag for easy dialogue checking
            SetGlobalFlag($"reached_{buildingType.ToString().ToLower()}", "true");

            if (debugLogging)
                Debug.Log($"[GameProgressionManager] Building type reached: {buildingType}");
        }

        /// <summary>
        /// Track when player clears a building type
        /// </summary>
        public void OnBuildingTypeCleared(RogueLikeBuildingType buildingType)
        {
            if (buildingType == RogueLikeBuildingType.NONE)
                return;

            string milestoneId = $"building_cleared_{buildingType.ToString().ToLower()}";
            CompleteMilestone(milestoneId);

            // Also set a direct flag for easy dialogue checking
            SetGlobalFlag($"cleared_{buildingType.ToString().ToLower()}", "true");

            if (debugLogging)
                Debug.Log($"[GameProgressionManager] Building type cleared: {buildingType}");
        }

        #endregion

        #region Global Flag System

        /// <summary>
        /// Set a global progression flag
        /// </summary>
        public void SetGlobalFlag(string flagName, string value = "true")
        {
            if (string.IsNullOrEmpty(flagName))
                return;

            string oldValue = globalFlags.ContainsKey(flagName) ? globalFlags[flagName] : null;
            globalFlags[flagName] = value;

            if (debugLogging && oldValue != value)
                Debug.Log($"[GameProgressionManager] Global flag set: {flagName} = {value}");

            OnGlobalFlagChanged?.Invoke(flagName, value);
        }

        /// <summary>
        /// Get a global progression flag value
        /// </summary>
        public string GetGlobalFlag(string flagName)
        {
            if (string.IsNullOrEmpty(flagName))
                return null;

            globalFlags.TryGetValue(flagName, out string value);
            return value;
        }

        /// <summary>
        /// Check if a global flag exists and is not empty
        /// </summary>
        public bool HasGlobalFlag(string flagName)
        {
            return !string.IsNullOrEmpty(GetGlobalFlag(flagName));
        }

        /// <summary>
        /// Remove a global flag
        /// </summary>
        public void RemoveGlobalFlag(string flagName)
        {
            if (string.IsNullOrEmpty(flagName))
                return;

            if (globalFlags.Remove(flagName))
            {
                if (debugLogging)
                    Debug.Log($"[GameProgressionManager] Global flag removed: {flagName}");

                OnGlobalFlagChanged?.Invoke(flagName, null);
            }
        }

        /// <summary>
        /// Get all global flags (for save/load)
        /// </summary>
        public Dictionary<string, string> GetAllGlobalFlags()
        {
            return new Dictionary<string, string>(globalFlags);
        }

        #endregion

        #region Save/Load Integration

        /// <summary>
        /// Get progression data for saving
        /// </summary>
        public GameProgressionData GetProgressionData()
        {
            return new GameProgressionData
            {
                completedMilestones = completedMilestones.ToList(),
                globalFlags = new Dictionary<string, string>(globalFlags)
            };
        }

        /// <summary>
        /// Load progression data from save
        /// </summary>
        public void LoadProgressionData(GameProgressionData data)
        {
            if (data == null)
                return;

            // Load completed milestones
            completedMilestones.Clear();
            if (data.completedMilestones != null)
            {
                foreach (var milestone in data.completedMilestones)
                {
                    completedMilestones.Add(milestone);
                }
            }

            // Load global flags
            globalFlags.Clear();
            if (data.globalFlags != null)
            {
                foreach (var flag in data.globalFlags)
                {
                    globalFlags[flag.Key] = flag.Value;
                }
            }

            if (debugLogging)
                Debug.Log($"[GameProgressionManager] Loaded progression data: {completedMilestones.Count} milestones, {globalFlags.Count} flags");
        }

        #endregion

        #region Narrative Integration

        /// <summary>
        /// Check if narrative conditions are met based on progression
        /// This method can be called by NarrativeManager for global flag checks
        /// </summary>
        public bool CheckProgressionConditions(List<string> requiredFlags, List<string> blockedByFlags)
        {
            // Check required flags
            if (requiredFlags != null && requiredFlags.Count > 0)
            {
                foreach (string flag in requiredFlags)
                {
                    if (!HasGlobalFlag(flag))
                    {
                        if (debugLogging)
                            Debug.Log($"[GameProgressionManager] Progression check failed: missing flag {flag}");
                        return false;
                    }
                }
            }

            // Check blocked flags
            if (blockedByFlags != null && blockedByFlags.Count > 0)
            {
                foreach (string flag in blockedByFlags)
                {
                    if (HasGlobalFlag(flag))
                    {
                        if (debugLogging)
                            Debug.Log($"[GameProgressionManager] Progression check failed: blocked by flag {flag}");
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Debug method to list all milestones and their status
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugListMilestones()
        {
            Debug.Log("=== Game Progression Status ===");
            foreach (var milestone in milestones)
            {
                bool completed = IsMilestoneCompleted(milestone.milestoneId);
                Debug.Log($"[{(completed ? "✓" : "✗")}] {milestone.displayName} ({milestone.milestoneId})");
            }
            
            Debug.Log("=== Global Flags ===");
            foreach (var flag in globalFlags)
            {
                Debug.Log($"  {flag.Key} = {flag.Value}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Data structure for progression milestones
    /// </summary>
    [System.Serializable]
    public class ProgressionMilestone
    {
        public string milestoneId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public MilestoneCategory category;
        public List<ProgressionFlag> flagsToSet;
    }

    /// <summary>
    /// Flag to set when a milestone is completed
    /// </summary>
    [System.Serializable]
    public class ProgressionFlag
    {
        public string flagName;
        public string flagValue = "true";
    }

    /// <summary>
    /// Categories for organizing milestones
    /// </summary>
    public enum MilestoneCategory
    {
        Story,
        Buildings,
        Combat,
        Research,
        NPCs,
        Exploration,
        Resources
    }

    /// <summary>
    /// Serializable data for save/load
    /// </summary>
    [System.Serializable]
    public class GameProgressionData
    {
        public List<string> completedMilestones = new List<string>();
        public Dictionary<string, string> globalFlags = new Dictionary<string, string>();
    }
}