using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingEffects", menuName = "Scriptable Objects/Effects/Building Effects")]
public class BuildingEffects : BaseEffects
{
    [Tooltip("The category of building these effects are for")]
    public CampBuildingCategory buildingCategory;

    [Header("Building-Specific Effects")]
    [Tooltip("Effects played when the building is under construction")]
    public EffectDefinition[] constructionEffects = new EffectDefinition[0];

    [Tooltip("Effects played when construction is completed")]
    public EffectDefinition[] constructionCompleteEffects = new EffectDefinition[0];

    [Tooltip("Effects played when the building is being repaired")]
    public EffectDefinition[] repairEffects = new EffectDefinition[0];

    [Tooltip("Effects played when the building is being upgraded")]
    public EffectDefinition[] upgradeEffects = new EffectDefinition[0];

    [Header("Operational Effects")]
    [Tooltip("Ambient effects played while the building is operational")]
    public EffectDefinition[] ambientEffects = new EffectDefinition[0];

    [Tooltip("Minimum time between ambient effects")]
    public float minAmbientInterval = 10f;

    [Tooltip("Maximum time between ambient effects")]
    public float maxAmbientInterval = 30f;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Initialize building-specific arrays if they're null
        if (constructionEffects == null) constructionEffects = new EffectDefinition[0];
        if (constructionCompleteEffects == null) constructionCompleteEffects = new EffectDefinition[0];
        if (repairEffects == null) repairEffects = new EffectDefinition[0];
        if (upgradeEffects == null) upgradeEffects = new EffectDefinition[0];
        if (ambientEffects == null) ambientEffects = new EffectDefinition[0];
    }
} 