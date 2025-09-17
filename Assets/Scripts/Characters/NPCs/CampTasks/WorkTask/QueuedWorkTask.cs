using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Managers;

public abstract class QueuedWorkTask : WorkTask
{
    // Task queue
    public Queue<object> taskQueue = new Queue<object>();
    protected object currentTaskData;

    // Properties
    public override bool HasQueuedTasks => taskQueue.Count > 0;
    public bool HasCurrentWork => currentTaskData != null;

    protected override void Start()
    {
        base.Start();
        taskType = WorkTaskType.Queued; // This is a queued task
        maxWorkers = 1; // Queued tasks like cooking and researching are single-worker tasks
    }

    // Helper method for derived classes to set up tasks
    protected void SetupTask(object taskData)
    {
        if (taskData == null) return;
        
        // Queue the task
        QueueTask(taskData);

        // If no current task, set it up immediately
        if (currentTaskData == null)
        {
            currentTaskData = taskQueue.Dequeue();
            SetupNextTask();
        }
    }

    // Method to add a task to the queue
    public virtual void QueueTask(object taskData)
    {
        taskQueue.Enqueue(taskData);
        
        // If we have a previous worker and no current workers, assign them to the new task
        if (currentWorkers.Count == 0 && taskQueue.Count == 1)
        {
            // Find the previous worker through the WorkManager
            var previousWorker = CampManager.Instance.WorkManager.GetPreviousWorkerForTask(this);
            if (previousWorker != null)
            {
                // Set the NPC for assignment before assigning the task
                CampManager.Instance.WorkManager.SetNPCForAssignment(previousWorker);
                AssignNPC(previousWorker);
                if (previousWorker is SettlerNPC settler)
                {
                    settler.ChangeTask(TaskType.WORK);
                }
            }
        }
    }

    // Method to clear the task queue
    public virtual void ClearTaskQueue()
    {
        taskQueue.Clear();
    }

    // Virtual method to setup the next task in queue
    protected virtual void SetupNextTask()
    {
        // To be implemented by derived classes
    }

    protected override void CompleteWork()
    {
        // Store the first current worker as previous worker before clearing (for backward compatibility)
        if (currentWorkers.Count > 0)
        {
            CampManager.Instance.WorkManager.StorePreviousWorker(this, currentWorkers[0]);
        }
        
        // Reset state
        workProgress = 0f;
        
        // Process next task in queue if available
        if (taskQueue.Count > 0)
        {
            // Notify completion of current task without stopping work
            NotifyTaskCompletion();
            
            currentTaskData = taskQueue.Dequeue();
            SetupNextTask();
            
            // Update progress bar to show new task if it's active
            if (progressBarActive && CampManager.Instance?.WorkManager != null)
            {
                // Reset progress to 0 for the new task
                CampManager.Instance.WorkManager.UpdateProgress(this, 0f, WorkTaskProgressState.Normal);
            }
            
            // Workers will automatically continue working on the next task
            // since they're still assigned to this task
        }
        else
        {
            Debug.Log($"[QueuedWorkTask] No more tasks in queue, completing work for {GetType().Name}");
            
            // Only unassign and notify completion if there are no more tasks
            UnassignNPC();
            
            // Clear current task data since there are no more tasks
            currentTaskData = null;
            
            // Reset progress bar state before calling base CompleteWork
            if (progressBarActive && CampManager.Instance?.WorkManager != null)
            {
                Debug.Log($"[QueuedWorkTask] Hiding progress bar for completed task {GetType().Name}");
                CampManager.Instance.WorkManager.HideProgressBar(this);
                progressBarActive = false;
            }
            
            // For queued tasks, we don't want to call base.CompleteWork() when there are no more tasks
            // because that would remove the task from the work queue and make it unavailable for new tasks
            // Instead, we just stop the current work and let the task remain available for new tasks
            
            // Stop all workers from working on this task
            var workersToStop = new List<HumanCharacterController>(currentWorkers);
            foreach (var worker in workersToStop)
            {
                Debug.Log($"[QueuedWorkTask] Stopping worker {worker.name} from completed task {GetType().Name}");
                worker.StopWork();
            }
            
            // Clear all workers
            currentWorkers.Clear();
            
            // Switch back to idle effects when work is complete
            if (isOperational)
            {
                SetAmbientEffectsState(false); // Back to idle state
            }
            
            if (taskStructure != null)
            {
                taskStructure.SetCurrentWorkTask(null);
                Debug.Log($"[WorkTask] Set task structure current work task to null");
            }
            
            // Notify completion
            NotifyTaskCompletion();
            InvokeStopWork();
            Debug.Log($"[QueuedWorkTask] Work completion notifications sent for {GetType().Name}");
        }
    }
} 