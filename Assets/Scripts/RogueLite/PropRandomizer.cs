using UnityEngine;

public class PropRandomizer : MonoBehaviour
{
    [Header("Randomization Settings")]
    [Range(0, 1)] public float enableChance = 0.5f; // Chance of enabling each prop

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
            }
        }
    }
}
