using UnityEngine;
using System.Collections;
using Managers;

public class ResourceUpgradeTask : QueuedWorkTask
{
    public ResourceUpgradeScriptableObj currentUpgrade;

    protected override void Start()
    {
        base.Start();
    }

    public void SetUpgrade(ResourceUpgradeScriptableObj upgrade)
    {
        SetupTask(upgrade);
    }

    protected override void SetupNextTask()
    {
        if (currentTaskData is ResourceUpgradeScriptableObj nextUpgrade)
        {
            currentUpgrade = nextUpgrade;
            // Convert game hours to real seconds using TimeManager
            baseWorkTime = Managers.TimeManager.ConvertGameHoursToSecondsStatic(nextUpgrade.craftTimeInGameHours);
            requiredResources = nextUpgrade.requiredResources;
        }
    }

    public override bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        // Start by consuming resources if we haven't started work yet
        if (workProgress == 0f && currentUpgrade != null)
        {
            ConsumeResources();
        }
        
        // Call base DoWork to handle electricity and progress
        return base.DoWork(worker, deltaTime);
    }

    protected override void CompleteWork()
    {
        if (currentUpgrade != null)
        {
            // Create the upgraded resources
            for (int i = 0; i < currentUpgrade.outputAmount; i++)
            {
                AddResourceToInventory(currentUpgrade.outputResources);
            }
        }
        
        // Clear the current upgrade before completing the work
        currentUpgrade = null;
        
        base.CompleteWork();
    }
} 