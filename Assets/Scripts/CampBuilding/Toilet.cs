using UnityEngine;
using Managers;

[RequireComponent(typeof(ToiletCleaningTask))]
public class Toilet : Building
{
    [Header("Toilet Settings")]
    [SerializeField] private float maxCapacity = 100f;
    [SerializeField] private float currentCapacity = 0f;
    [SerializeField] private float fillRate = 1f;
    [SerializeField] private float emptyTime = 10f;
    private bool isBeingEmptied = false;
    private ToiletCleaningTask cleaningTask;

    protected override void Start()
    {
        base.Start();
        CampManager.Instance.CleanlinessManager.RegisterToilet(this);
        cleaningTask = GetComponent<ToiletCleaningTask>();
        cleaningTask.SetupTask(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampManager.Instance != null)
        {
            CampManager.Instance.CleanlinessManager.UnregisterToilet(this);
        }
    }

    private void Update()
    {
        if (!isOperational || isBeingEmptied) return;

        currentCapacity = Mathf.Min(maxCapacity, currentCapacity + fillRate * Time.deltaTime);
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

    public ToiletCleaningTask GetCleaningTask()
    {
        return cleaningTask;
    }
} 