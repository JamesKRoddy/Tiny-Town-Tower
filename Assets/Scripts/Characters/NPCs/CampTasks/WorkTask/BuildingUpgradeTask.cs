using UnityEngine;
using System.Collections;
using Managers;

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
        for (int i = 0; i < requiredResources.Length; i++)
        {
            if (CampManager.Instance.PlayerInventory.GetItemCount(requiredResources[i]) < resourceCosts[i])
            {
                return false;
            }
        }
        return true;
    }

    private void ConsumeResources()
    {
        for (int i = 0; i < requiredResources.Length; i++)
        {
            CampManager.Instance.PlayerInventory.RemoveItem(requiredResources[i], resourceCosts[i]);
        }
    }

    private void CompleteUpgrade()
    {
        // Get the current building's position and rotation
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Destroy the current building
        Destroy(gameObject);

        // Create the upgraded building
        GameObject upgradedBuilding = Instantiate(upgradeTarget.prefab, position, rotation);
        
        Debug.Log($"Building upgrade completed!");
        
        // Reset state
        upgradeProgress = 0f;
        currentWorker = null;
        upgradeCoroutine = null;
        
        // Notify completion
        InvokeStopWork();
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