using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    public GameObject finalBuildingPrefab;
    public float constructionTime = 10f;
    private float currentProgress = 0f;

    public void Update()
    {
        if (currentProgress < constructionTime)
        {
            currentProgress += Time.deltaTime;
        }
        else
        {
            CompleteConstruction();
        }
    }

    private void CompleteConstruction()
    {
        Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
