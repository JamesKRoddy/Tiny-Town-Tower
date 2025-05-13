using UnityEngine;
using Managers;

[RequireComponent(typeof(CleaningTask))]
public class CleaningStation : Building
{
    [Header("Cleaning Station Settings")]
    [SerializeField] private float checkInterval = 5f;
    private float lastCheckTime;
    private CleaningTask cleaningTask;

    protected override void Start()
    {
        base.Start();
        CampManager.Instance.CleanlinessManager.RegisterCleaningStation(this);
        cleaningTask = GetComponent<CleaningTask>();
        cleaningTask.SetupTask(null); // Initialize with no target
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampManager.Instance != null)
        {
            CampManager.Instance.CleanlinessManager.UnregisterCleaningStation(this);
        }
    }

    private void Update()
    {
        if (!isOperational) return;

        if (Time.time - lastCheckTime >= checkInterval)
        {
            CheckForCleaningTasks();
            lastCheckTime = Time.time;
        }
    }

    private void CheckForCleaningTasks()
    {
        // First check if we need to assign a cleaner
        if (!cleaningTask.IsAssigned())
        {
            CampManager.Instance.WorkManager.AddWorkTask(cleaningTask);
            return;
        }

        // If we have an assigned cleaner, check for tasks that need cleaning
        var dirtPiles = CampManager.Instance.CleanlinessManager.GetActiveDirtPiles();
        foreach (var dirtPile in dirtPiles)
        {
            if (!dirtPile.IsBeingCleaned())
            {
                AssignCleaningTask(dirtPile);
                return;
            }
        }

        // Check for full toilets
        var fullToilets = CampManager.Instance.CleanlinessManager.GetFullToilets();
        foreach (var toilet in fullToilets)
        {
            if (!toilet.IsBeingEmptied())
            {
                AssignToiletTask(toilet);
                return;
            }
        }

        // Check for full waste bins
        var fullBins = CampManager.Instance.CleanlinessManager.GetFullWasteBins();
        foreach (var bin in fullBins)
        {
            if (!bin.IsBeingEmptied())
            {
                AssignBinTask(bin);
                return;
            }
        }
    }

    private void AssignCleaningTask(DirtPile dirtPile)
    {
        if (cleaningTask != null)
        {
            cleaningTask.SetupTask(dirtPile);
        }
    }

    private void AssignToiletTask(Toilet toilet)
    {
        var task = toilet.GetCleaningTask();
        if (task != null)
        {
            CampManager.Instance.WorkManager.AddWorkTask(task);
        }
    }

    private void AssignBinTask(WasteBin bin)
    {
        var task = bin.GetCleaningTask();
        if (task != null)
        {
            CampManager.Instance.WorkManager.AddWorkTask(task);
        }
    }
} 