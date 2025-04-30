using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class WorkManager : MonoBehaviour
    {
        private static WorkManager _instance;
        public static WorkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<WorkManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("WorkManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        private Queue<WorkTask> workQueue = new Queue<WorkTask>(); // Queue to hold available tasks
        
        // Event to notify NPCs when a task is available
        public delegate void TaskAvailable(WorkTask task);
        public event TaskAvailable OnTaskAvailable;

        private SettlerNPC npcForAssignment;
        private Dictionary<WorkTask, SettlerNPC> previousWorkers = new Dictionary<WorkTask, SettlerNPC>();

        // Method to add a new work task to the queue
        public void AddWorkTask(WorkTask newTask)
        {
            Debug.Log($"<color=green>[WorkManager]</color> Adding work task: {newTask.name}");
            workQueue.Enqueue(newTask);
            // Notify that a new task is available
            OnTaskAvailable?.Invoke(newTask);
        }

        // Method to get the next available task
        public WorkTask GetNextTask()
        {
            if (workQueue.Count > 0)
            {
                return workQueue.Dequeue();
            }
            return null;
        }

        // Method to assign a task to a specific NPC
        public void AssignTask(SettlerNPC npc, WorkTask task)
        {
            if (npc == null || task == null) return;

            // If the task is in the queue, remove it
            if (workQueue.Contains(task))
            {
                var tempQueue = new Queue<WorkTask>();
                while (workQueue.Count > 0)
                {
                    var queuedTask = workQueue.Dequeue();
                    if (queuedTask != task)
                    {
                        tempQueue.Enqueue(queuedTask);
                    }
                }
                workQueue = tempQueue;
            }

            // Assign the task to the NPC
            task.PerformTask(npc);
        }

        public void SetNPCForAssignment(SettlerNPC npc)
        {
            Debug.Log($"[WorkManager] Setting NPC for assignment: {(npc != null ? npc.name : "null")}");
            npcForAssignment = npc;
        }

        public void AssignWorkToBuilding(WorkTask workTask)
        {
            Debug.Log($"[WorkManager] Attempting to assign work to building. NPC: {(npcForAssignment != null ? npcForAssignment.name : "null")}, Task: {workTask.name}");
            
            if (workTask == null)
            {
                PlayerUIManager.Instance.DisplayUIErrorMessage("Invalid work task");
                return;
            }

            // If we have no NPC for assignment, check if we can get a previous worker
            if (npcForAssignment == null)
            {
                var previousWorker = GetPreviousWorkerForTask(workTask);
                if (previousWorker == null)
                {
                    Debug.Log($"[WorkManager] No NPC available for task {workTask.name}");
                    PlayerUIManager.Instance.DisplayUIErrorMessage("No NPC available for work assignment");
                    return;
                }
                Debug.Log($"[WorkManager] Using previous worker {previousWorker.name} for task {workTask.name}");
                SetNPCForAssignment(previousWorker);
            }

            // Check if the task can be performed
            if (!workTask.CanPerformTask())
            {
                string errorMessage = workTask.workType switch
                {
                    WorkType.REPAIR_BUILDING => "Building is already at full health",
                    WorkType.UPGRADE_BUILDING => "Building cannot be upgraded further",
                    WorkType.CLEANING => "Camp is already clean",
                    _ => "Task cannot be performed at this time"
                };
                PlayerUIManager.Instance.DisplayUIErrorMessage(errorMessage);
                return;
            }

            // Check if the player has required resources
            if (!workTask.HasRequiredResources())
            {
                string resourceMessage = "Missing required resources: ";
                bool firstResource = true;
                foreach (var resource in workTask.requiredResources)
                {
                    int playerCount = PlayerInventory.Instance.GetItemCount(resource.resource);
                    if (playerCount < resource.count)
                    {
                        if (!firstResource) resourceMessage += ", ";
                        resourceMessage += $"{resource.resource.objectName} ({playerCount}/{resource.count})";
                        firstResource = false;
                    }
                }
                PlayerUIManager.Instance.DisplayUIErrorMessage(resourceMessage);
                return;
            }

            // If the task already has an NPC assigned, unassign them
            if (workTask.IsAssigned())
            {
                SettlerNPC currentNPC = workTask.AssignedNPC;
                Debug.Log($"[WorkManager] Unassigning current NPC: {currentNPC.name}");
                currentNPC.ChangeTask(TaskType.WANDER);
                workTask.UnassignNPC();
            }

            // Store the previous worker
            if (workTask.AssignedNPC != null)
            {
                Debug.Log($"[WorkManager] Storing previous worker: {workTask.AssignedNPC.name} for task {workTask.name}");
                previousWorkers[workTask] = workTask.AssignedNPC;
            }
            else
            {
                Debug.Log($"[WorkManager] No worker to store as previous worker for task {workTask.name}");
            }

            // Assign the new NPC to the task
            Debug.Log($"[WorkManager] Assigning new NPC: {npcForAssignment.name} to task: {workTask.name}");
            workTask.AssignNPC(npcForAssignment);
            npcForAssignment.AssignWork(workTask);
            npcForAssignment = null; // Clear the assignment NPC
        }

        public void StorePreviousWorker(WorkTask task, SettlerNPC worker)
        {
            Debug.Log($"[WorkManager] Storing previous worker {worker.name} for task {task.name}");
            previousWorkers[task] = worker;
        }

        public SettlerNPC GetPreviousWorkerForTask(WorkTask task)
        {
            Debug.Log($"[WorkManager] Looking for previous worker for task {task.name}. Previous workers count: {previousWorkers.Count}");
            foreach (var kvp in previousWorkers)
            {
                Debug.Log($"[WorkManager] Checking task {kvp.Key.name} with worker {kvp.Value.name}");
            }
            
            if (previousWorkers.TryGetValue(task, out SettlerNPC previousWorker))
            {
                Debug.Log($"[WorkManager] Found previous worker for task {task.name}: {previousWorker.name}");
                return previousWorker;
            }
            Debug.Log($"[WorkManager] No previous worker found for task {task.name}");
            return null;
        }
    }
}
