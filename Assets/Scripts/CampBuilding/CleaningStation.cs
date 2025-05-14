using UnityEngine;
using Managers;

[RequireComponent(typeof(CleaningTask))]
public class CleaningStation : Building
{
    private CleaningTask cleaningTask;

    protected override void Start()
    {
        base.Start();
        CampManager.Instance.CleanlinessManager.RegisterCleaningStation(this);
        cleaningTask = GetComponent<CleaningTask>();
        cleaningTask.SetupTask(null);
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

        // If we have an assigned worker and they're not cleaning anything, find a new dirt pile
        if (cleaningTask.IsAssigned() && cleaningTask.GetCurrentTarget() == null)
        {
            var activeDirtPiles = CampManager.Instance.CleanlinessManager.GetActiveDirtPiles();
            foreach (var dirtPile in activeDirtPiles)
            {
                if (!dirtPile.IsBeingCleaned())
                {
                    cleaningTask.SetupTask(dirtPile);
                    break;
                }
            }
        }
    }
} 