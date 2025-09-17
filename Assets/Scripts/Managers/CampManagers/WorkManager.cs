using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Enum representing the different states of work task progress
/// </summary>
public enum WorkTaskProgressState
{
    Normal,   // Task is progressing normally
    Paused,   // Task is paused (worker can't work efficiently)
    Error     // Task has encountered an error
}

namespace Managers
{
    public class WorkManager : MonoBehaviour
    {
        private Queue<WorkTask> workQueue = new Queue<WorkTask>(); // Queue to hold available tasks
        
        // Centralized list of all SleepTasks (beds) for efficient access
        private List<SleepTask> availableSleepTasks = new List<SleepTask>();
        
        // Event to notify NPCs when a task is available
        public delegate void TaskAvailable(WorkTask task);
        public event TaskAvailable OnTaskAvailable;

        private HumanCharacterController npcForAssignment;
        private Dictionary<WorkTask, HumanCharacterController> previousWorkers = new Dictionary<WorkTask, HumanCharacterController>();

        public object buildingForAssignment; // Can be IPlaceableStructure or WorkTask (for construction sites)

        // Progress bar management is now handled by PlayerUIManager

        // Method to add a new work task to the queue
        public void AddWorkTask(WorkTask newTask)
        {
            workQueue.Enqueue(newTask);
            // Notify that a new task is available
            OnTaskAvailable?.Invoke(newTask);
        }

        // Method to remove a completed task from the queue
        public void RemoveTaskFromQueue(WorkTask taskToRemove)
        {
            if (taskToRemove == null) return;
            
            // Create a new queue without the completed task
            Queue<WorkTask> newQueue = new Queue<WorkTask>();
            while (workQueue.Count > 0)
            {
                WorkTask task = workQueue.Dequeue();
                if (task != taskToRemove)
                {
                    newQueue.Enqueue(task);
                }
            }
            workQueue = newQueue;
            
            Debug.Log($"[WorkManager] Removed {taskToRemove.GetType().Name} from work queue. Queue count: {workQueue.Count}");
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

            // Keep track of tasks we've already checked to prevent infinite loops
            HashSet<WorkTask> checkedTasks = new HashSet<WorkTask>();
            
            // Create a temporary queue to process tasks
            Queue<WorkTask> tempQueue = new Queue<WorkTask>();
            
            while (workQueue.Count > 0)
            {
                WorkTask nextTask = workQueue.Dequeue();
                if (nextTask == null) continue;
                
                // If we've already checked this task, don't process it again
                if (checkedTasks.Contains(nextTask))
                {
                    tempQueue.Enqueue(nextTask); // Keep it in queue for future attempts
                    continue;
                }
                
                checkedTasks.Add(nextTask);

                // Check if the task can be performed
                if (!nextTask.CanPerformTask())
                {
                    tempQueue.Enqueue(nextTask); // Keep it in queue
                    continue;
                }

                // Check if the player has required resources
                if (!nextTask.HasRequiredResources())
                {
                    tempQueue.Enqueue(nextTask); // Keep it in queue
                    continue;
                }

                // Check if task is already fully occupied
                if (nextTask.IsFullyOccupied)
                {
                    tempQueue.Enqueue(nextTask); // Keep it in queue, might have space later
                    continue;
                }

                // Found a suitable task - assign the NPC but keep the task in queue for other workers
                tempQueue.Enqueue(nextTask); // Keep task in queue for potential additional workers
                
                // Restore the queue with all tasks
                while (tempQueue.Count > 0)
                {
                    workQueue.Enqueue(tempQueue.Dequeue());
                }
                
                return AssignNPCToTask(npc, nextTask);
            }
            
            // If we get here, no suitable tasks were found - restore all tasks to queue
            while (tempQueue.Count > 0)
            {
                workQueue.Enqueue(tempQueue.Dequeue());
            }
            
            return false;
        }


        /// <summary>
        /// Helper method to assign an NPC to a specific task
        /// </summary>
        private bool AssignNPCToTask(HumanCharacterController npc, WorkTask task)
        {
            // Assign the task to the NPC
            if (npc is RobotCharacterController robot)
            {
                task.AssignNPC(robot);
                robot.StartWork(task);
            }
            else if (npc is SettlerNPC settler)
            {
                task.AssignNPC(settler);
                
                Debug.Log($"[WorkManager] Assigning task {task.GetType().Name} to {settler.name}. Current task type: {settler.GetCurrentTaskType()}");
                
                // If the NPC is already in work state, we need to update the work state directly
                if (settler.GetCurrentTaskType() == TaskType.WORK)
                {
                    var workState = settler.GetComponent<WorkState>();
                    if (workState != null)
                    {
                        workState.AssignTask(task);
                        workState.UpdateTaskDestination(); // Force update the destination
                        Debug.Log($"[WorkManager] Updated work state for {settler.name} to task {task.GetType().Name}");
                    }
                }
                else
                {
                    settler.StartWork(task);
                    Debug.Log($"[WorkManager] Started work for {settler.name} on task {task.GetType().Name}");
                }
            }
            else
            {
                Debug.LogError($"[WorkManager] Invalid character type for assignment: {npc.GetType().Name}");
                return false;
            }

            return true;
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



            // Special handling for SleepTask (bed assignment)
            if (workTask is SleepTask sleepTask)
            {
                AssignBedToSettler(sleepTask);
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
                    StructureConstructionTask => "Construction task cannot be performed",
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
                    workTask = null,
                    returnToGameControls = true
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
                // Handle SleepTask specially - show bed assignment options
                SleepTask sleepTask => ("Assign Bed", () => {
                    // For SleepTask, we want to show bed assignment options
                    // This will be handled by the building selection popup
                    onTaskSelected(task);
                    ClearNPCForAssignment(); // Clear assignment after work is started
                }),
                MedicalTask => ("Medical Treatment", () => {
                    onTaskSelected(task);
                    ClearNPCForAssignment(); // Clear assignment after work is started
                }),
                _ => (task.GetType().Name.Replace("Task", ""), () => {
                    onTaskSelected(task);
                    ClearNPCForAssignment(); // Clear assignment after work is started
                })
            };

            // Custom canSelect logic for specific task types
            Func<bool> canSelectTask = task switch
            {
                MedicalTask medicalTask => () => {
                    // Medical tasks can only be selected if the task can be performed AND the NPC is sick
                    return medicalTask.CanPerformTask() && 
                           characterToAssign is SettlerNPC settler && 
                           settler.IsSick;
                },
                _ => () => task.CanPerformTask()
            };

            return new SelectionPopup.SelectionOption
            {
                optionName = name,
                onSelected = action,
                canSelect = canSelectTask,
                workTask = task,
                returnToGameControls = !(task is ResearchTask || task is CookingTask || task is ResourceUpgradeTask || task is FarmingTask)
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
        
        /// <summary>
        /// Register a SleepTask (bed) with the WorkManager
        /// </summary>
        /// <param name="sleepTask">The SleepTask to register</param>
        public void RegisterSleepTask(SleepTask sleepTask)
        {
            if (sleepTask != null && !availableSleepTasks.Contains(sleepTask))
            {
                availableSleepTasks.Add(sleepTask);
            }
        }
        
        /// <summary>
        /// Unregister a SleepTask (bed) from the WorkManager
        /// </summary>
        /// <param name="sleepTask">The SleepTask to unregister</param>
        public void UnregisterSleepTask(SleepTask sleepTask)
        {
            if (sleepTask != null && availableSleepTasks.Contains(sleepTask))
            {
                availableSleepTasks.Remove(sleepTask);
            }
        }
        
        /// <summary>
        /// Get all available SleepTasks (beds) for NPCs to use
        /// </summary>
        /// <returns>List of available SleepTasks</returns>
        public List<SleepTask> GetAvailableSleepTasks()
        {
            // Return a copy to prevent external modification
            return new List<SleepTask>(availableSleepTasks);
        }
        
        /// <summary>
        /// Get the count of available SleepTasks
        /// </summary>
        /// <returns>Number of available SleepTasks</returns>
        public int GetSleepTaskCount()
        {
            return availableSleepTasks.Count;
        }
        
        /// <summary>
        /// Assign a bed to a settler for sleeping
        /// </summary>
        /// <param name="sleepTask">The SleepTask component representing the bed</param>
        private void AssignBedToSettler(SleepTask sleepTask)
        {
            if (npcForAssignment == null)
            {
                Debug.LogWarning("[WorkManager] No NPC set for bed assignment");
                PlayerUIManager.Instance.DisplayUIErrorMessage("No NPC selected for bed assignment");
                return;
            }
            
            if (npcForAssignment is not SettlerNPC settler)
            {
                Debug.LogWarning("[WorkManager] Only settlers can be assigned to beds");
                PlayerUIManager.Instance.DisplayUIErrorMessage("Only settlers can be assigned to beds");
                return;
            }
            
            // Assign the settler to the bed
            bool success = sleepTask.AssignSettlerToBed(settler);
            if (success)
            {
                PlayerUIManager.Instance.DisplayUIErrorMessage($"{settler.name} assigned to bed");
                
                // Close any open popups
                CloseSelectionPopup();
                
                // Clear the settler filter and close the menu
                var settlerMenu = PlayerUIManager.Instance.settlerNPCMenu;
                settlerMenu.ClearSettlerFilter();
                settlerMenu.SetScreenActive(false);
                
                // Clear assignments
                ClearNPCForAssignment();
            }
            else
            {
                Debug.LogWarning($"[WorkManager] Failed to assign {settler.name} to bed {sleepTask.name}");
                PlayerUIManager.Instance.DisplayUIErrorMessage("Failed to assign settler to bed");
            }
        }

        #region Progress Bar Management

        private void Start()
        {
            // Subscribe to scene loaded events
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Don't pre-create progress bars - create them only when needed
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Clear any existing progress bars since we're in a new scene
            ClearAllProgressBars();
        }

        /// <summary>
        /// Show a progress bar for the specified work task
        /// </summary>
        /// <param name="task">The work task to show progress for</param>
        public void ShowProgressBar(WorkTask task)
        {
            if (PlayerUIManager.Instance != null)
            {
                PlayerUIManager.Instance.ShowProgressBar(task);
            }
        }

        /// <summary>
        /// Update the progress for a specific work task
        /// </summary>
        /// <param name="task">The work task</param>
        /// <param name="progress">Progress value between 0 and 1</param>
        /// <param name="state">The current state of the progress</param>
        public void UpdateProgress(WorkTask task, float progress, WorkTaskProgressState state = WorkTaskProgressState.Normal)
        {
            if (PlayerUIManager.Instance != null)
            {
                PlayerUIManager.Instance.UpdateProgressBar(task, progress, state);
            }
        }

        /// <summary>
        /// Hide the progress bar for the specified work task
        /// </summary>
        /// <param name="task">The work task to hide progress for</param>
        public void HideProgressBar(WorkTask task)
        {
            if (PlayerUIManager.Instance != null)
            {
                PlayerUIManager.Instance.HideProgressBar(task);
            }
        }

        /// <summary>
        /// Check if a task currently has a progress bar showing
        /// </summary>
        /// <param name="task">The work task to check</param>
        /// <returns>True if progress bar is active</returns>
        public bool HasProgressBar(WorkTask task)
        {
            if (PlayerUIManager.Instance != null)
            {
                return PlayerUIManager.Instance.HasProgressBar(task);
            }
            return false;
        }

        /// <summary>
        /// Clean up all progress bars (useful when changing scenes)
        /// </summary>
        public void ClearAllProgressBars()
        {
            if (PlayerUIManager.Instance != null)
            {
                PlayerUIManager.Instance.ClearAllProgressBars();
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from scene loaded events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            ClearAllProgressBars();
        }
    }
}
