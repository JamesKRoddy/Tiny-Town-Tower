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
    public override bool IsTaskCompleted => currentTaskData == null && !HasQueuedTasks;
    public bool HasCurrentWork => currentTaskData != null;

    protected override void Start()
    {
        base.Start();
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
            // Only unassign and notify completion if there are no more tasks
            UnassignNPC();
            
            // Clear current task data since there are no more tasks
            currentTaskData = null;
            
            // Notify completion and stop work
            base.CompleteWork();
        }
    }
} 