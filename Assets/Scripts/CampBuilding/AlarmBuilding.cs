using UnityEngine;
using System.Collections.Generic;
using Managers;

/// <summary>
/// An alarm building that alerts sleeping NPCs when enemy waves start.
/// When operational, automatically wakes up all sleeping settlers when a wave begins,
/// giving them time to prepare for the incoming attack.
/// </summary>
public class AlarmBuilding : Building
{
    [Header("Alarm Settings")]
    [Tooltip("Range within which NPCs will be alerted (0 = infinite range)")]
    [SerializeField] private float alarmRange = 0f; // 0 = alert all NPCs regardless of distance
    
    [Tooltip("Alarm effect with spawn configuration")]
    [SerializeField] private EffectSpawnData alarmEffect;
    
    private bool isAlarmActive = false;
    private GameObject currentAlarmEffect;
    
    #region Unity Lifecycle
    
    protected override void Start()
    {
        base.Start();
        
        // Subscribe to wave events when the building starts
        SubscribeToWaveEvents();
    }
    
    protected override void OnDestroy()
    {
        // Unsubscribe from wave events when destroyed
        UnsubscribeFromWaveEvents();
        
        base.OnDestroy();
    }
    
    #endregion
    
    #region Building Setup
    
    public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        base.SetupBuilding(buildingScriptableObj);
    }
    
    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        
        // Subscribe to wave events once construction is complete
        SubscribeToWaveEvents();
    }
    
    #endregion
    
    #region Wave Event Handling
    
    /// <summary>
    /// Subscribe to CampManager wave events
    /// </summary>
    private void SubscribeToWaveEvents()
    {
        if (CampManager.Instance != null)
        {
            CampManager.Instance.OnCampWaveStarted += OnWaveStarted;
            Debug.Log($"[AlarmBuilding] {gameObject.name} subscribed to wave events");
        }
        else
        {
            Debug.LogWarning($"[AlarmBuilding] {gameObject.name} - CampManager not found, cannot subscribe to wave events");
        }
    }
    
    /// <summary>
    /// Unsubscribe from CampManager wave events
    /// </summary>
    private void UnsubscribeFromWaveEvents()
    {
        if (CampManager.Instance != null)
        {
            CampManager.Instance.OnCampWaveStarted -= OnWaveStarted;
        }
    }
    
    /// <summary>
    /// Called when a wave starts - triggers the alarm
    /// </summary>
    private void OnWaveStarted()
    {
        // Only trigger alarm if building is operational and not under construction
        if (isOperational && !isUnderConstruction)
        {
            TriggerAlarm();
        }
        else
        {
            Debug.Log($"[AlarmBuilding] {gameObject.name} cannot trigger alarm - Operational: {isOperational}, Under Construction: {isUnderConstruction}");
        }
    }
    
    #endregion
    
    #region Alarm Logic
    
    /// <summary>
    /// Trigger the alarm to wake up sleeping NPCs
    /// </summary>
    private void TriggerAlarm()
    {
        if (isAlarmActive)
        {
            Debug.Log($"[AlarmBuilding] {gameObject.name} alarm already active");
            return;
        }
        
        Debug.Log($"[AlarmBuilding] {gameObject.name} triggering alarm! Waking up sleeping NPCs...");
        
        isAlarmActive = true;
        
        // Play alarm effect
        PlayAlarmEffect();
        
        // Wake up all sleeping NPCs
        WakeUpSleepingNPCs();
        
        // Deactivate alarm after duration (use effect's configured duration)
        float duration = alarmEffect != null ? alarmEffect.GetEffectDuration() : 3f;
        Invoke(nameof(DeactivateAlarm), duration);
    }
    
    /// <summary>
    /// Wake up all sleeping NPCs within range
    /// </summary>
    private void WakeUpSleepingNPCs()
    {
        // Get all registered NPCs from NPCManager
        if (NPCManager.Instance == null)
        {
            Debug.LogWarning("[AlarmBuilding] NPCManager not found!");
            return;
        }
        
        List<SettlerNPC> allSettlers = NPCManager.Instance.GetAllNPCs();
        int npcCount = allSettlers.Count;
        int wokeUpCount = 0;
        
        foreach (var settler in allSettlers)
        {
            if (settler == null || settler.Health <= 0)
                continue;
            
            // Check if NPC is sleeping
            if (settler.GetCurrentTaskType() == TaskType.SLEEP)
            {
                // Check range if specified (0 = infinite range)
                if (alarmRange <= 0f || Vector3.Distance(transform.position, settler.transform.position) <= alarmRange)
                {
                    Debug.Log($"[AlarmBuilding] Waking up {settler.name} due to alarm");
                    
                    // Wake up the NPC and send them to wander state (they'll detect threats from there)
                    settler.ChangeTask(TaskType.WANDER);
                    wokeUpCount++;
                }
            }
        }
        
        Debug.Log($"[AlarmBuilding] Alarm woke up {wokeUpCount} out of {npcCount} NPCs");
    }
    
    /// <summary>
    /// Play the alarm visual/audio effect using EffectManager
    /// </summary>
    private void PlayAlarmEffect()
    {
        if (alarmEffect != null && alarmEffect.IsValid())
        {
            // Spawn the effect using the configured spawn data
            currentAlarmEffect = alarmEffect.SpawnEffect(transform);
            
            if (currentAlarmEffect != null)
            {
                Debug.Log($"[AlarmBuilding] Playing alarm effect: {alarmEffect.effectDefinition.name} at {currentAlarmEffect.transform.position}");
            }
        }
        else
        {
            Debug.LogWarning($"[AlarmBuilding] {gameObject.name} - No alarm effect assigned or invalid!");
        }
    }
    
    /// <summary>
    /// Deactivate the alarm after the effect duration
    /// </summary>
    private void DeactivateAlarm()
    {
        isAlarmActive = false;
        currentAlarmEffect = null; // Clear reference (EffectManager handles cleanup)
        Debug.Log($"[AlarmBuilding] {gameObject.name} alarm deactivated");
    }
    
    #endregion
    
    #region Manual Alarm Trigger
    
    /// <summary>
    /// Manually trigger the alarm (can be called by player interaction or debug menu)
    /// </summary>
    [ContextMenu("Trigger Alarm Manually")]
    public void TriggerAlarmManually()
    {
        if (isOperational && !isUnderConstruction)
        {
            Debug.Log($"[AlarmBuilding] Manually triggering alarm");
            TriggerAlarm();
        }
        else
        {
            Debug.LogWarning($"[AlarmBuilding] Cannot trigger alarm - building not operational or under construction");
        }
    }
    
    #endregion
    
    #region Interaction
    
    public override string GetInteractionText()
    {
        string baseText = base.GetInteractionText();
        
        string alarmText = $"\nAlarm Status:";
        alarmText += $"\nOperational: {(isOperational ? "Yes" : "No")}";
        alarmText += $"\nRange: {(alarmRange <= 0 ? "Unlimited" : $"{alarmRange}m")}";
        
        if (alarmEffect != null && alarmEffect.IsValid())
        {
            alarmText += $"\nEffect: {alarmEffect.effectDefinition.name}";
        }
        else
        {
            alarmText += "\nEffect: None assigned";
        }
        
        if (isAlarmActive)
        {
            alarmText += "\n[ALARM ACTIVE]";
        }
        else if (isOperational && !isUnderConstruction)
        {
            alarmText += "\n- Test Alarm";
        }
        
        return baseText + alarmText;
    }
    
    #endregion
    
    #region Debug Visualization
    
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Determine effect spawn position
        Vector3 effectSpawnPos = transform.position + Vector3.up * 2f;
        Quaternion effectSpawnRot = Quaternion.identity;
        
        if (alarmEffect != null)
        {
            effectSpawnPos = alarmEffect.GetSpawnPosition(transform);
            effectSpawnRot = alarmEffect.GetSpawnRotation(transform);
        }
        
        // Draw alarm range if specified
        if (alarmRange > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, alarmRange);
            
            // Draw solid sphere at effect spawn point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(effectSpawnPos, 0.5f);
        }
        else
        {
            // Draw icon for infinite range
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(effectSpawnPos, 0.5f);
            
            // Draw expanding circles to indicate infinite range
            for (int i = 1; i <= 3; i++)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f / i);
                Gizmos.DrawWireSphere(transform.position, i * 10f);
            }
        }
        
        // Draw axis arrows at spawn point if effect is configured
        if (alarmEffect != null && alarmEffect.IsValid())
        {
            // Draw forward direction (blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(effectSpawnPos, effectSpawnPos + effectSpawnRot * Vector3.forward * 1f);
            
            // Draw up direction (green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(effectSpawnPos, effectSpawnPos + effectSpawnRot * Vector3.up * 1f);
            
            // Draw right direction (red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(effectSpawnPos, effectSpawnPos + effectSpawnRot * Vector3.right * 1f);
        }
    }
    #endif
    
    #endregion
}


