using UnityEngine;

public class ConstructionSite : WorkTask
{
    public GameObject finalBuildingPrefab;
    public float constructionTime = 10f;
    private float currentProgress = 0f;

    public override void PerformTask(SettlerNPC npc)
    {
        throw new System.NotImplementedException();
    }

    public override Transform WorkTaskTransform()
    {
        return this.transform;
    }

    //TODO move this somewhere else and start construction
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

    public void SetupConstruction(BuildingScriptableObj buildingScriptableObj)
    {
        finalBuildingPrefab = buildingScriptableObj.buildingPrefab;
        constructionTime = buildingScriptableObj.constructionTime;
    }

    private void CompleteConstruction()
    {
        Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }


}
