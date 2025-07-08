using UnityEngine;
using Managers;

/// <summary>
/// Debug script to help track work queue and NPC movement issues
/// </summary>
public class WorkQueueDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private KeyCode debugKey = KeyCode.F2;
    [SerializeField] private bool showOnScreenDebug = true;

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            DebugWorkQueueAndNPCs();
        }
    }

    private void DebugWorkQueueAndNPCs()
    {
        if (!enableDebugLogging) return;

        Debug.Log("=== WORK QUEUE DEBUG ===");
        
        if (CampManager.Instance?.WorkManager != null)
        {
            int queueCount = CampManager.Instance.WorkManager.GetWorkQueueCount();
            Debug.Log($"Tasks in work queue: {queueCount}");
            
            var npcs = FindObjectsOfType<SettlerNPC>();
            Debug.Log($"Total SettlerNPCs: {npcs.Length}");
            
            foreach (var npc in npcs)
            {
                string currentTask = npc.GetCurrentTaskType().ToString();
                string hasWork = npc.HasAssignedWork() ? "Has Work" : "No Work";
                string workTaskName = npc.GetAssignedWork()?.GetType().Name ?? "None";
                
                Debug.Log($"NPC {npc.name}:");
                Debug.Log($"  - Current Task: {currentTask}");
                Debug.Log($"  - Has Assigned Work: {hasWork}");
                Debug.Log($"  - Assigned Work Task: {workTaskName}");
                
                // Check work state details
                var workState = npc.GetComponent<WorkState>();
                if (workState != null)
                {
                    string workStateTask = workState.assignedTask?.GetType().Name ?? "None";
                    Debug.Log($"  - WorkState Task: {workStateTask}");
                }
                
                // Check NavMeshAgent status
                var agent = npc.GetAgent();
                if (agent != null)
                {
                    Debug.Log($"  - Agent Destination: {agent.destination}");
                    Debug.Log($"  - Agent isStopped: {agent.isStopped}");
                    Debug.Log($"  - Agent Velocity: {agent.velocity.magnitude:F2}");
                }
            }
        }
        else
        {
            Debug.LogWarning("CampManager or WorkManager not found!");
        }
        
        Debug.Log("=== END WORK QUEUE DEBUG ===");
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug) return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Label("Work Queue Debugger");
        
        if (CampManager.Instance?.WorkManager != null)
        {
            int queueCount = CampManager.Instance.WorkManager.GetWorkQueueCount();
            GUILayout.Label($"Tasks in queue: {queueCount}");
            
            var npcs = FindObjectsOfType<SettlerNPC>();
            GUILayout.Label($"SettlerNPCs: {npcs.Length}");
            
            int workingNPCs = 0;
            int wanderingNPCs = 0;
            int stuckNPCs = 0;
            
            foreach (var npc in npcs)
            {
                if (npc.GetCurrentTaskType() == TaskType.WORK)
                {
                    workingNPCs++;
                    var agent = npc.GetAgent();
                    if (agent != null && agent.velocity.magnitude < 0.1f && !agent.isStopped)
                    {
                        stuckNPCs++;
                    }
                }
                else if (npc.GetCurrentTaskType() == TaskType.WANDER)
                {
                    wanderingNPCs++;
                }
            }
            
            GUILayout.Label($"Working NPCs: {workingNPCs}");
            GUILayout.Label($"Wandering NPCs: {wanderingNPCs}");
            GUILayout.Label($"Stuck NPCs: {stuckNPCs}");
        }
        else
        {
            GUILayout.Label("CampManager not found!");
        }
        
        GUILayout.Label($"Press {debugKey} for detailed debug info");
        GUILayout.EndArea();
    }
} 