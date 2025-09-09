using UnityEngine;
using Managers;
using System.Collections;

public class ElectricityGeneratorTask : WorkTask
{
    [Header("Electricity Generation")]
    [SerializeField] private float electricityGeneratedPerCycle = 100f;
    [SerializeField] private float generationInterval = 5f;
    private float generationTimer = 0f;



    protected override void Start()
    {
        base.Start();
        baseWorkTime = float.MaxValue; // Set to max value so it never completes automatically
    }

    public override bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        if (!isOperational || !currentWorkers.Contains(worker))
        {
            return false;
        }

        // Get final work speed (including cleanliness modifier) from base class
        float finalWorkSpeed = GetFinalWorkSpeed(worker);
        
        // If worker can't work (starving, etc.), stop
        if (finalWorkSpeed <= 0)
        {
            return false;
        }
        
        // Update generation timer (affected by work speed and cleanliness)
        generationTimer += deltaTime * finalWorkSpeed;
        
        // Generate electricity at regular intervals
        if (generationTimer >= generationInterval)
        {
            CampManager.Instance.ElectricityManager.AddElectricity(electricityGeneratedPerCycle);
            generationTimer = 0f;
        }
        
        // Call base DoWork for electricity consumption and dirt generation
        base.DoWork(worker, deltaTime);
        
        // Keep progress below max to prevent completion (this task runs indefinitely)
        if (workProgress >= baseWorkTime - 1f)
        {
            workProgress = baseWorkTime - 1f;
        }
        
        return true; // Always continue (generator never completes)
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Electricity Generator\n";
        tooltip += $"Generates: {electricityGeneratedPerCycle} units per {generationInterval} seconds\n";
        tooltip += $"Current Power: {CampManager.Instance.ElectricityManager.GetElectricityPercentage():F1}%\n";
        
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