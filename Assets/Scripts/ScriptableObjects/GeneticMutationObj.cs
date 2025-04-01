using UnityEngine;

[CreateAssetMenu(fileName = "GeneticMutation", menuName = "Scriptable Objects/GeneticMutation")]
public class GeneticMutationObj : ResourceScriptableObj
{
    public Vector2Int size = new Vector2Int(2, 2);
    public GameObject mutationEffectPrefab; // Reference to the prefab containing the mutation effect component
    public Sprite mutationIcon;
}
