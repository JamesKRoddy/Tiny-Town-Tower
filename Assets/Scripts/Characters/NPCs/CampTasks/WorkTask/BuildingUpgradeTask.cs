using UnityEngine;
using System.Collections;

public class BuildingUpgradeTask : WorkTask
{
    [SerializeField] private BuildingScriptableObj upgradeTarget;
    [SerializeField] private float upgradeTime = 30f;
    [SerializeField] private ResourceScriptableObj[] requiredResources;
    [SerializeField] private int[] resourceCosts;

    private float upgradeProgress = 0f;
    private SettlerNPC currentWorker;
    private Coroutine upgradeCoroutine;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.UPGRADE_BUILDING;
    }

    public override void PerformTask(SettlerNPC npc)
    {
        if (currentWorker == null && HasRequiredResources())
        {
            currentWorker = npc;
            ConsumeResources();
            upgradeCoroutine = StartCoroutine(UpgradeCoroutine());
        }
    }

    private IEnumerator UpgradeCoroutine()
    {
        while (upgradeProgress < upgradeTime)
        {
            upgradeProgress += Time.deltaTime;
            yield return null;
        }

        CompleteUpgrade();
    }

    private bool HasRequiredResources()
    {
        return CampInventory.Instance.HasResources(requiredResources, resourceCosts);
    }

    private void ConsumeResources()
    {
        CampInventory.Instance.ConsumeResources(requiredResources, resourceCosts);
    }

    private void CompleteUpgrade()
    {
        // Replace the current building with the upgraded version
        GameObject upgradedBuilding = Instantiate(upgradeTarget.prefab, transform.position, transform.rotation);
        upgradedBuilding.transform.parent = transform.parent;
        
        Debug.Log("Building upgrade completed!");
        
        // Reset state
        upgradeProgress = 0f;
        currentWorker = null;
        upgradeCoroutine = null;
        
        // Notify completion
        InvokeStopWork();
        
        // Destroy the old building
        Destroy(gameObject);
    }

    public override Transform WorkTaskTransform()
    {
        return transform;
    }

    private void OnDisable()
    {
        if (upgradeCoroutine != null)
        {
            StopCoroutine(upgradeCoroutine);
            upgradeCoroutine = null;
        }
    }
} 