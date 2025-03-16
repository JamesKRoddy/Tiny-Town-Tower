using UnityEngine;

[CreateAssetMenu(fileName = "NewGeneticMutation", menuName = "Mutations/GeneticMutation")]
public class GeneticMutationData : ScriptableObject
{
    public string mutationName;
    public Vector2Int size = new Vector2Int(2, 2); // Default size, can be modified
    public bool isContaminated;
    public Sprite mutationSprite; // Now added to use in UI!
}
