using UnityEngine;
using System.Collections.Generic;
using System;

namespace Managers
{
    public class WorkManager : MonoBehaviour
    {
        private Queue<WorkTask> workQueue = new Queue<WorkTask>(); // Queue to hold available tasks
        
        // Event to notify NPCs when a task is available
        public delegate void TaskAvailable(WorkTask task);
        public event TaskAvailable OnTaskAvailable;

        private SettlerNPC npcForAssignment;
        private Dictionary<WorkTask, SettlerNPC> previousWorkers = new Dictionary<WorkTask, SettlerNPC>();

        // Method to add a new work task to the queue
        public void AddWorkTask(WorkTask newTask)
        {
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
                    PlayerUIManager.Instance.DisplayUIErrorMessage("No NPC available for work assignment");
                    return;
                }
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
                    int playerCount = PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj);
                    if (playerCount < resource.count)
                    {
                        if (!firstResource) resourceMessage += ", ";
                        resourceMessage += $"{resource.resourceScriptableObj.objectName} ({playerCount}/{resource.count})";
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

            // Store the previous worker
            if (workTask.AssignedNPC != null)
            {
                previousWorkers[workTask] = workTask.AssignedNPC;
            }

            // Assign the new NPC to the task
            workTask.AssignNPC(npcForAssignment);
            npcForAssignment.AssignWork(workTask);
            npcForAssignment = null; // Clear the assignment NPC
        }

        public void StorePreviousWorker(WorkTask task, SettlerNPC worker)
        {
            previousWorkers[task] = worker;
        }

        public SettlerNPC GetPreviousWorkerForTask(WorkTask task)
        {            
            if (previousWorkers.TryGetValue(task, out SettlerNPC previousWorker))
            {
                return previousWorker;
            }
            return null;
        }

        public void ShowWorkTaskOptions(Building building, Action<WorkTask> onTaskSelected)
        {
            // Create selection options for each work task
            var workTasks = building.GetComponents<WorkTask>();

            var options = new List<SelectionPopup.SelectionOption>();

            // Add Destroy Building option first
            options.Add(new SelectionPopup.SelectionOption
            {
                optionName = "Destroy Building",
                onSelected = () => {
                    building.StartDestruction();
                    CloseSelectionPopup();
                },
                canSelect = () => !building.IsUnderConstruction(),
                workTask = null
            });

            foreach (var task in workTasks)
            {
                if (task.workType == WorkType.RESEARCH)
                {
                    // For research tasks, show the research selection screen
                    options.Add(new SelectionPopup.SelectionOption
                    {
                        optionName = "Research",
                        onSelected = () => {
                            PlayerUIManager.Instance.selectionPreviewList.Setup(task, building);
                            PlayerUIManager.Instance.selectionPreviewList.SetScreenActive(true);
                        },
                        canSelect = () => true,
                        workTask = task
                    });
                }
                else if (task.workType == WorkType.COOKING)
                {
                    // For cooking tasks, show the cooking selection screen
                    options.Add(new SelectionPopup.SelectionOption
                    {
                        optionName = "Cook",
                        onSelected = () => {
                            PlayerUIManager.Instance.selectionPreviewList.Setup(task, building);
                            PlayerUIManager.Instance.selectionPreviewList.SetScreenActive(true);
                        },
                        canSelect = () => true,
                        workTask = task
                    });
                }
                else if (task.workType == WorkType.UPGRADE_RESOURCE)
                {
                    // For resource upgrade tasks, show the resource upgrade selection screen
                    options.Add(new SelectionPopup.SelectionOption
                    {
                        optionName = "Upgrade Resource",
                        onSelected = () => {
                            PlayerUIManager.Instance.selectionPreviewList.Setup(task, building);
                            PlayerUIManager.Instance.selectionPreviewList.SetScreenActive(true);
                        },
                        canSelect = () => true,
                        workTask = task
                    });
                }
                else
                {
                    // For other tasks, show the normal work assignment
                    options.Add(new SelectionPopup.SelectionOption
                    {
                        optionName = task.workType.ToString(),
                        onSelected = () => {
                            onTaskSelected(task);
                        },
                        canSelect = () => task.CanPerformTask(),
                        workTask = task
                    });
                }
            }

            // Show the selection popup
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, null);
        }

        public void CloseSelectionPopup()
        {
            PlayerUIManager.Instance.selectionPopup.OnCloseClicked();
        }
    }
}
