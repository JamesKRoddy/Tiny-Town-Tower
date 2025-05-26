using UnityEngine;
using Managers;

[RequireComponent(typeof(BinCleaningTask))]
public class WasteBin : Building
{
    [Header("Waste Bin Settings")]
    [SerializeField] private float maxCapacity = 100f;
    [SerializeField] private float currentCapacity = 0f;
    [SerializeField] private float emptyTime = 5f;
    private bool isBeingEmptied = false;
    private BinCleaningTask cleaningTask;

    protected override void Start()
    {
        base.Start();
        CampManager.Instance.CleanlinessManager.RegisterWasteBin(this);
        cleaningTask = GetComponent<BinCleaningTask>();
        cleaningTask.SetupTask(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampManager.Instance != null)
        {
            CampManager.Instance.CleanlinessManager.UnregisterWasteBin(this);
        }
    }

    public void AddWaste(float amount)
    {
        if (!isOperational || isBeingEmptied) return;
        
        float previousCapacity = currentCapacity;
        currentCapacity = Mathf.Min(maxCapacity, currentCapacity + amount);
        
        // Notify if the bin just became full
        if (previousCapacity < maxCapacity && currentCapacity >= maxCapacity)
        {
            CampManager.Instance.CleanlinessManager.NotifyWasteBinFull(this);
        }
    }

    public void StartEmptying()
    {
        isBeingEmptied = true;
    }

    public void StopEmptying()
    {
        isBeingEmptied = false;
    }

    public void AddEmptyProgress(float progress)
    {
        if (!isBeingEmptied) return;

        currentCapacity = Mathf.Max(0, currentCapacity - (maxCapacity * progress / emptyTime));
        if (currentCapacity <= 0)
        {
            CompleteEmptying();
        }
    }

    private void CompleteEmptying()
    {
        currentCapacity = 0f;
        isBeingEmptied = false;
    }

    public bool IsFull()
    {
        return currentCapacity >= maxCapacity;
    }

    public float GetFillPercentage()
    {
        return currentCapacity / maxCapacity;
    }

    public bool IsBeingEmptied()
    {
        return isBeingEmptied;
    }

    public BinCleaningTask GetCleaningTask()
    {
        return cleaningTask;
    }
} 