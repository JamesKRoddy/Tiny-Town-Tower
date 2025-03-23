using UnityEngine;

public enum MutationType
{
    Combat,
    Survival,
    RiskReward
}

[CreateAssetMenu(fileName = "GeneticMutation", menuName = "Scriptable Objects/GeneticMutation")]
public class GeneticMutationObj : ResourceScriptableObj
{
    public Vector2Int size = new Vector2Int(2, 2);
    public MutationType mutationType;
    public GameObject mutationEffectPrefab; // Reference to the prefab containing the mutation effect component
    public Sprite mutationIcon;
}
