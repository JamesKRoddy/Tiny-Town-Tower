using UnityEngine;
using System.Collections;
using Managers;

public class BuildingRepairTask : WorkTask
{
    private float healthRestored = 50f;
    private Building targetBuilding;

    protected override void Start()
    {
        base.Start();
        targetBuilding = GetComponent<Building>();
        if (targetBuilding == null)
        {
            Debug.LogError("BuildingRepairTask requires a Building component on the same GameObject!");
            enabled = false;
        }
    }

    public void SetupRepairTask(float repairTime, float healthRestored)
    {
        baseWorkTime = repairTime;
        this.healthRestored = healthRestored;
    }

    protected override IEnumerator WorkCoroutine()
    {
        // Consume resources
        ConsumeResources();

        // Process the repair
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    protected override void CompleteWork()
    {
        if (targetBuilding != null)
        {
            targetBuilding.Repair(healthRestored);
            Debug.Log($"Building repair completed! Restored {healthRestored} health");
        }
        
        base.CompleteWork();
    }

    public override bool CanPerformTask()
    {
        if (targetBuilding == null) return false;
        
        // Check if the building needs repair (is not at full health)
        return targetBuilding.GetCurrentHealth() < targetBuilding.GetMaxHealth();
    }

    public override string GetAnimationClipName()
    {
        return TaskAnimation.REPAIR_BUILDING.ToString();
    }
} 