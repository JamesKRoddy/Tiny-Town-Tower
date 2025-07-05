using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretUpgradeMenu : MenuBase, IControllerInput
{
    [Header("Upgrade Menu UI")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private RectTransform previewResourceCostParent;
    [SerializeField] private GameObject previewResourceCostPrefab;

    private BaseTurret currentTurret;

    public void ShowUpgradeOptions(BaseTurret turret)
    {
        currentTurret = turret;
        var turretSO = turret.GetStructureScriptableObj();

        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }

        if (turretSO?.upgradeTarget != null)
        {
            foreach (var (resourceName, requiredCount, playerCount) in GetPreviewResourceCosts(turretSO.upgradeTarget))
            {
                var resourceUI = Instantiate(previewResourceCostPrefab, previewResourceCostParent);
                resourceUI.GetComponentInChildren<TMP_Text>().text = $"{resourceName}: {requiredCount} ({playerCount})";
            }

            upgradeButton.interactable = CanUpgrade(turret);
            upgradeButtonText.text = GetUpgradeText(turret);
        }
        else
        {
            upgradeButton.interactable = false;
            upgradeButtonText.text = "No Upgrade Available";
        }
    }

    private bool CanUpgrade(BaseTurret turret)
    {
        return turret.CanUpgrade();
    }

    private string GetUpgradeText(BaseTurret turret)
    {
        var turretSO = turret.GetStructureScriptableObj();
        if (turretSO?.upgradeTarget != null)
        {
            return $"Upgrade to {turretSO.upgradeTarget.objectName}";
        }
        return "No Upgrade Available";
    }

    private IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(PlaceableObjectParent upgradeTarget)
    {
        if (upgradeTarget?.upgradeResources == null) yield break;

        foreach (var resourceCost in upgradeTarget.upgradeResources)
        {
            if (resourceCost.resourceScriptableObj != null)
            {
                int playerCount = PlayerInventory.Instance?.GetItemCount(resourceCost.resourceScriptableObj) ?? 0;
                yield return (resourceCost.resourceScriptableObj.objectName, resourceCost.count, playerCount);
            }
        }
    }

    public void OnUpgradeButtonClicked()
    {
        if (currentTurret != null && currentTurret.CanUpgrade())
        {
            //currentTurret.StartUpgrade();
            SetScreenActive(false);
        }
    }
}
