using UnityEngine;
using System.Collections;
using Managers;

public class CleaningTask : WorkTask
{
    [SerializeField] private float cleaningTime = 5f;
    [SerializeField] private float cleanlinessIncrease = 10f;
    
    private float cleaningProgress = 0f;
    private SettlerNPC currentWorker;
    private Coroutine cleaningCoroutine;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.CLEANING;
    }

    public override void PerformTask(SettlerNPC npc)
    {
        if (currentWorker == null)
        {
            currentWorker = npc;
            cleaningCoroutine = StartCoroutine(CleaningCoroutine());
        }
    }

    private IEnumerator CleaningCoroutine()
    {
        while (cleaningProgress < cleaningTime)
        {
            cleaningProgress += Time.deltaTime;
            yield return null;
        }

        CompleteCleaning();
    }

    private void CompleteCleaning()
    {
        // Increase the camp's cleanliness
        CampManager.Instance.CleanlinessManager.IncreaseCleanliness(cleanlinessIncrease);
        Debug.Log($"Cleaning completed! Increased cleanliness by {cleanlinessIncrease}");
        
        // Reset state
        cleaningProgress = 0f;
        currentWorker = null;
        cleaningCoroutine = null;
        
        // Notify completion
        InvokeStopWork();
    }

    public override Transform WorkTaskTransform()
    {
        return transform;
    }

    private void OnDisable()
    {
        if (cleaningCoroutine != null)
        {
            StopCoroutine(cleaningCoroutine);
            cleaningCoroutine = null;
        }
    }
} 