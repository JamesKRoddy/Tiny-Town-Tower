using UnityEngine;
using System.Collections;
using Managers;

public class StructureRepairTask : WorkTask
{
    private float healthRestored = 50f;
    private PlaceableStructure targetStructure;

    protected override void Start()
    {
        base.Start();
        taskAnimation = TaskAnimation.HAMMER_STANDING;
        targetStructure = GetComponent<PlaceableStructure>();
        if (targetStructure == null)
        {
            Debug.LogError("StructureRepairTask requires a PlaceableStructure component on the same GameObject!");
            enabled = false;
        }
    }

    public void SetupRepairTask(float repairTime, float healthRestored)
    {
        baseWorkTime = repairTime;
        this.healthRestored = healthRestored;
        
        // Set up required resources from the scriptable object
        var scriptableObj = targetStructure?.GetStructureScriptableObj();
        if (scriptableObj != null)
        {
            requiredResources = scriptableObj.repairResources;
        }
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
        if (targetStructure != null)
        {
            targetStructure.Heal(healthRestored);
            Debug.Log($"Structure repair completed! Restored {healthRestored} health");
        }
        
        base.CompleteWork();
    }

    public override bool CanPerformTask()
    {
        if (targetStructure == null) return false;
        
        // Check if the structure needs repair (is not at full health)
        return targetStructure.GetCurrentHealth() < targetStructure.GetMaxHealth();
    }

    public override string GetTooltipText()
    {
        string tooltip = $"Repair Structure\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        tooltip += $"Health Restored: {healthRestored}\n";
        
        if (requiredResources != null)
        {
            tooltip += "Required Resources:\n";
            foreach (var resource in requiredResources)
            {
                tooltip += $"- {resource.resourceScriptableObj.objectName}: {resource.count}\n";
            }
        }
        
        return tooltip;
    }
} 