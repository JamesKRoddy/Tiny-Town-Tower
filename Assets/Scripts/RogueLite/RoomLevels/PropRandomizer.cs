using UnityEngine;

public class PropRandomizer : MonoBehaviour
{
    [Header("Randomization Settings")]
    [Range(0, 1)] public float enableChance = 0.5f; // Chance of enabling each prop

    private string physicsLayer = "ObstacleLayer";

    public void RandomizeProps()
    {
        // Disable all props first
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        // Randomly enable props based on the enableChance
        foreach (Transform child in transform)
        {
            if (Random.value < enableChance)
            {
                child.gameObject.SetActive(true);
                if (LayerMask.LayerToName(child.gameObject.layer) != "Vaultable")
                {
                    child.gameObject.layer = LayerMask.NameToLayer(physicsLayer);
                }

                // Ensure NavMeshObstacle with carving is present
                var obstacle = child.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (obstacle == null)
                    obstacle = child.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.carving = true;
            }
        }
    }
}
