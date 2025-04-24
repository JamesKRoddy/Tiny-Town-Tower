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
            npcForAssignment = npc;
        }

        public void AssignWorkToBuilding(WorkTask workTask)
        {
            if (npcForAssignment == null || workTask == null)
            {
                PlayerUIManager.Instance.DisplayUIErrorMessage("No NPC selected for work assignment");
                return;
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
                currentNPC.ChangeTask(TaskType.WANDER);
                workTask.UnassignNPC();
            }

            // Assign the new NPC to the task
            workTask.AssignNPC(npcForAssignment);
            npcForAssignment.AssignWork(workTask);
            npcForAssignment = null; // Clear the assignment NPC
        }
    }
}
