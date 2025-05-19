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
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampManager.Instance != null)
        {
            CampManager.Instance.CleanlinessManager.UnregisterCleaningStation(this);
        }
    }
} 