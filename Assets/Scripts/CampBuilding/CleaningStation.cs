using UnityEngine;
using Managers;

public class CleaningStation : Building
{
    [Header("Cleaning Station Settings")]
    [SerializeField] private float cleaningRange = 10f;
    [SerializeField] private float checkInterval = 5f;
    private float lastCheckTime;

    protected override void Start()
    {
        base.Start();
        CampManager.Instance.CleanlinessManager.RegisterCleaningStation(this);
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
        // Check for dirt piles
        var dirtPiles = CampManager.Instance.CleanlinessManager.GetActiveDirtPiles();
        foreach (var dirtPile in dirtPiles)
        {
            if (Vector3.Distance(transform.position, dirtPile.transform.position) <= cleaningRange)
            {
                if (!dirtPile.IsBeingCleaned())
                {
                    CreateCleaningTask(dirtPile);
                    return;
                }
            }
        }

        // Check for full toilets
        var fullToilets = CampManager.Instance.CleanlinessManager.GetFullToilets();
        foreach (var toilet in fullToilets)
        {
            if (Vector3.Distance(transform.position, toilet.transform.position) <= cleaningRange)
            {
                if (!toilet.IsBeingEmptied())
                {
                    CreateToiletTask(toilet);
                    return;
                }
            }
        }

        // Check for full waste bins
        var fullBins = CampManager.Instance.CleanlinessManager.GetFullWasteBins();
        foreach (var bin in fullBins)
        {
            if (Vector3.Distance(transform.position, bin.transform.position) <= cleaningRange)
            {
                if (!bin.IsBeingEmptied())
                {
                    CreateBinTask(bin);
                    return;
                }
            }
        }
    }

    private void CreateCleaningTask(DirtPile dirtPile)
    {
        CleaningTask task = gameObject.AddComponent<CleaningTask>();
        task.SetupTask(dirtPile);
        CampManager.Instance.WorkManager.AddWorkTask(task);
    }

    private void CreateToiletTask(Toilet toilet)
    {
        ToiletCleaningTask task = gameObject.AddComponent<ToiletCleaningTask>();
        task.SetupTask(toilet);
        CampManager.Instance.WorkManager.AddWorkTask(task);
    }

    private void CreateBinTask(WasteBin bin)
    {
        BinCleaningTask task = gameObject.AddComponent<BinCleaningTask>();
        task.SetupTask(bin);
        CampManager.Instance.WorkManager.AddWorkTask(task);
    }
} 