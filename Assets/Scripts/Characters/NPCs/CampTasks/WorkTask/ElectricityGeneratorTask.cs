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

    protected override IEnumerator WorkCoroutine()
    {
        workProgress = 0f;
        generationTimer = 0f;

        while (true) // Continue indefinitely until stopped
        {
            workProgress += Time.deltaTime;
            generationTimer += Time.deltaTime;

            // Generate electricity at regular intervals
            if (generationTimer >= generationInterval)
            {
                CampManager.Instance.AddElectricity(electricityGeneratedPerCycle);
                generationTimer = 0f;
            }

            yield return null;
        }
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Electricity Generator\n";
        tooltip += $"Generates: {electricityGeneratedPerCycle} units per {generationInterval} seconds\n";
        tooltip += $"Current Power: {CampManager.Instance.GetElectricityPercentage():F1}%\n";
        
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