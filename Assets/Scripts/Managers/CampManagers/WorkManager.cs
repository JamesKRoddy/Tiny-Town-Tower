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

        private HumanCharacterController npcForAssignment;
        private Dictionary<WorkTask, HumanCharacterController> previousWorkers = new Dictionary<WorkTask, HumanCharacterController>();

        public object buildingForAssignment; // Can be IPlaceableStructure or WorkTask (for construction sites)

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

            // Method to automatically assign the next available task to an NPC
            public bool AssignNextAvailableTask(HumanCharacterController npc)
    {
        if (npc == null) return false;

        // Check if it's night time and NPC should sleep instead of work
        if (GameManager.Instance?.TimeManager != null && 
            GameManager.Instance.TimeManager.IsNight && 
            GameManager.Instance.TimeManager.ShouldNPCSleep())
        {
            if (npc is SettlerNPC settler)
            {
                settler.ChangeTask(TaskType.SLEEP);
                return true; // Task "assigned" (sleep)
            }
        }

        WorkTask nextTask = GetNextTask();
        if (nextTask != null)
        {
            // Check if the task can be performed
            if (!nextTask.CanPerformTask())
            {
                // Put the task back in the queue if it can't be performed
                workQueue.Enqueue(nextTask);
                return false;
            }

            // Check if the player has required resources
            if (!nextTask.HasRequiredResources())
            {
                // Put the task back in the queue if resources are missing
                workQueue.Enqueue(nextTask);
                return false;
            }

            // Check if task is already fully occupied
            if (nextTask.IsFullyOccupied)
            {
                // Put the task back in the queue if it's full
                workQueue.Enqueue(nextTask);
                return false;
            }

            // Assign the task to the NPC
            if (npc is RobotCharacterController robot)
            {
                nextTask.AssignNPC(robot);
                robot.StartWork(nextTask);
            }
            else if (npc is SettlerNPC settler)
            {
                nextTask.AssignNPC(settler);
                
                Debug.Log($"[WorkManager] Assigning task {nextTask.GetType().Name} to {settler.name}. Current task type: {settler.GetCurrentTaskType()}");
                
                // If the NPC is already in work state, we need to update the work state directly
                if (settler.GetCurrentTaskType() == TaskType.WORK)
                {
                    var workState = settler.GetComponent<WorkState>();
                    if (workState != null)
                    {
                        workState.AssignTask(nextTask);
                        workState.UpdateTaskDestination(); // Force update the destination
                        Debug.Log($"[WorkManager] Updated work state for {settler.name} to task {nextTask.GetType().Name}");
                    }
                }
                else
                {
                    settler.StartWork(nextTask);
                        Debug.Log($"[WorkManager] Started work for {settler.name} on task {nextTask.GetType().Name}");
                    }
                }
                else
                {
                    Debug.LogError($"[WorkManager] Invalid character type for assignment: {npc.GetType().Name}");
                    workQueue.Enqueue(nextTask);
                    return false;
                }

                return true;
            }
            return false;
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

        public bool IsNPCForAssignmentSet()
        {
            return npcForAssignment != null;
        }

        public void SetNPCForAssignment(HumanCharacterController npc)
        {
            npcForAssignment = npc;
        }

        public void ClearNPCForAssignment()
        {
            Debug.Log($"[WorkManager] Clearing NPC for assignment. Was: {npcForAssignment?.name ?? "null"}");
            Debug.Log($"[WorkManager] Clearing buildingForAssignment. Was: {buildingForAssignment?.ToString() ?? "null"}");
            npcForAssignment = null;
            buildingForAssignment = null; // Also clear building assignment
        }

        public void AssignWorkToBuilding(WorkTask workTask)
        {            
            if (workTask == null)
            {
                Debug.LogWarning("[WorkManager] Attempted to assign null work task");
                PlayerUIManager.Instance.DisplayUIErrorMessage("Invalid work task");
                return;
            }

            // Check if the task can be performed
            if (!workTask.CanPerformTask())
            {
                string errorMessage = workTask switch
                {
                    StructureRepairTask => "Structure is already at full health",
                    StructureUpgradeTask => "Structure cannot be upgraded further",
                    CleaningTask => "Camp is already clean",
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


            // If we have no NPC for assignment, check if we can get a previous worker            
            if (npcForAssignment == null)
            {
                var previousWorker = GetPreviousWorkerForTask(workTask);
                if (previousWorker == null)
                {
                    Debug.LogWarning("[WorkManager] No previous worker found for task");
                    PlayerUIManager.Instance.DisplayUIErrorMessage("No NPC available for work assignment");
                    return;
                }
                SetNPCForAssignment(previousWorker);
            }

            // If the task already has an NPC assigned, unassign them
            if (workTask.IsAssigned())
            {
                HumanCharacterController currentNPC = workTask.AssignedNPC;
                if (currentNPC is SettlerNPC settler)
                {
                    settler.ChangeTask(TaskType.WANDER);
                }
                workTask.UnassignNPC();
            }

            // Store the previous worker
            if (workTask.AssignedNPC != null)
            {
                previousWorkers[workTask] = workTask.AssignedNPC;
            }

            // Assign the new NPC to the task
            if (npcForAssignment is RobotCharacterController robot)
            {
                workTask.AssignNPC(robot);
                robot.StartWork(workTask);
            }
            else if (npcForAssignment is SettlerNPC settler)
            {
                workTask.AssignNPC(settler);
                settler.StartWork(workTask);
            }
            else
            {
                Debug.LogError($"[WorkManager] Invalid character type for assignment: {npcForAssignment?.GetType().Name ?? "null"}");
            }
            
            npcForAssignment = null; // Clear the assignment NPC
        }

        public void StorePreviousWorker(WorkTask task, HumanCharacterController worker)
        {
            previousWorkers[task] = worker;
        }

        public HumanCharacterController GetPreviousWorkerForTask(WorkTask task)
        {            
            Debug.Log($"Getting previous worker for task: {task.GetType().Name}");
            if (previousWorkers.TryGetValue(task, out HumanCharacterController previousWorker))
            {
                return previousWorker;
            }
            return null;
        }
        
        public void ShowWorkTaskOptions(IPlaceableStructure structure, HumanCharacterController characterToAssign, Action<WorkTask> onTaskSelected)
        {
            var workTasks = (structure as MonoBehaviour)?.GetComponents<WorkTask>();
            if (workTasks == null)
            {
                Debug.LogError("Structure is not a MonoBehaviour or has no WorkTask components");
                return;
            }

            var options = new List<SelectionPopup.SelectionOption>();

            // Add Destroy Structure option first (only for buildings and turrets, not construction sites)
            if (structure is Building || structure is BaseTurret)
            {
                options.Add(new SelectionPopup.SelectionOption
                {
                    optionName = "Destroy Structure",
                    onSelected = () => {
                        if (structure is MonoBehaviour mb)
                        {
                            // Call StartDestruction through the interface or cast to the appropriate type
                            if (mb is Building building)
                            {
                        building.StartDestruction();
                            }
                            else if (mb is BaseTurret turret)
                            {
                                turret.StartDestruction();
                            }
                        }
                        CloseSelectionPopup();
                        PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
                        ClearNPCForAssignment(); // Clear assignment when destroying structure
                    },
                    canSelect = () => !structure.IsUnderConstruction(),
                    workTask = null
                });
            }

            ShowWorkTaskOptions(workTasks, characterToAssign, onTaskSelected, options);
        }

        // Overload for construction sites that are WorkTasks but not IPlaceableStructure
        public void ShowWorkTaskOptions(StructureConstructionTask constructionTask, HumanCharacterController characterToAssign, Action<WorkTask> onTaskSelected)
        {
            var workTasks = constructionTask.GetComponents<WorkTask>();
            var options = new List<SelectionPopup.SelectionOption>();

            // Construction sites don't get a destroy option - they're completed or abandoned
            ShowWorkTaskOptions(workTasks, characterToAssign, onTaskSelected, options);
        }


        public void ShowWorkTaskOptions(WorkTask[] workTasks, HumanCharacterController characterToAssign, Action<WorkTask> onTaskSelected, List<SelectionPopup.SelectionOption> options = null)
        {
            if (characterToAssign == null)
            {
                characterToAssign = npcForAssignment;
            }

            options ??= new List<SelectionPopup.SelectionOption>();

            // Create selection options for each work task
            foreach (var task in workTasks)
            {
                var option = CreateWorkTaskOption(task, characterToAssign, onTaskSelected);
                if (option != null)
                {
                    options.Add(option);
                }
            }

            // Show the selection popup with cleanup callback to clear NPC assignment
            PlayerUIManager.Instance.selectionPopup.Setup(options, null, () => {
                // Clear NPC assignment when popup closes
                ClearNPCForAssignment();
            });
        }

        private SelectionPopup.SelectionOption CreateWorkTaskOption(WorkTask task, HumanCharacterController characterToAssign, Action<WorkTask> onTaskSelected)
        {
            // Common action for tasks that use the selection preview list
            Action previewListAction = () => {
                if (!task.IsTaskCompleted)
                {
                    characterToAssign.StartWork(task);
                }
                PlayerUIManager.Instance.selectionPreviewList.Setup(task, characterToAssign);
                PlayerUIManager.Instance.selectionPreviewList.SetScreenActive(true);
                ClearNPCForAssignment(); // Clear assignment after work is started
            };

            // Get option name and action based on task type
            (string name, Action action) = task switch
            {
                ResearchTask => ("Research", previewListAction),
                CookingTask => ("Cook", previewListAction),
                ResourceUpgradeTask => ("Upgrade Resource", previewListAction),
                StructureUpgradeTask => ("Upgrade", () => {
                    // Execute upgrade immediately - consume resources, destroy building, create construction site
                    (task as StructureUpgradeTask)?.ExecuteUpgrade();
                    CloseSelectionPopup();
                    PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
                    ClearNPCForAssignment(); // Clear assignment after work is started
                }),
                StructureConstructionTask => ("Build", () => {
                    onTaskSelected(task);
                    ClearNPCForAssignment(); // Clear assignment after work is started
                }),
                FarmingTask => ("Farm", () => {
                    if ((task as FarmingTask)?.IsOccupiedWithCrop() ?? false)
                    {
                        characterToAssign.StartWork(task);
                        ClearNPCForAssignment(); // Clear assignment after work is started
                    }
                    else
                    {
                        previewListAction();
                    }
                }),
                _ => (task.GetType().Name.Replace("Task", ""), () => {
                    onTaskSelected(task);
                    ClearNPCForAssignment(); // Clear assignment after work is started
                })
            };

            return new SelectionPopup.SelectionOption
            {
                optionName = name,
                onSelected = action,
                canSelect = () => task.CanPerformTask(),
                workTask = task
            };
        }

        public void CloseSelectionPopup()
        {
            PlayerUIManager.Instance.selectionPopup.OnCloseClicked();
        }

        // Debug method to show work queue status
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugWorkQueueStatus()
        {
            Debug.Log($"[WorkManager] Work queue status: {workQueue.Count} tasks in queue");
            if (workQueue.Count > 0)
            {
                int index = 0;
                foreach (var task in workQueue)
                {
                    Debug.Log($"[WorkManager] Task {index}: {task.GetType().Name} at {task.transform.position}");
                    index++;
                }
            }
        }

        // Debug method to get work queue count
        public int GetWorkQueueCount()
        {
            return workQueue.Count;
        }
    }
}
