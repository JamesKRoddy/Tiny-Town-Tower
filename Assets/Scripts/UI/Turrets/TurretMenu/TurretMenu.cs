using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class TurretMenu : PreviewListMenuBase<TurretCategory, TurretScriptableObject>, IControllerInput
{
    [Header("Turret Menu Preview UI")]
    [SerializeField] GameObject previewResourceCostPrefab;
    [SerializeField] RectTransform previewResourceCostParent;

    [Header("Camp Turret Wave UI")]
    [SerializeField] Button startCampWaveButton;
    [SerializeField] TMP_Text startCampWaveButtonText;

    protected void Start()
    {        
        // Setup camp wave button
        if (startCampWaveButton != null)
        {
            startCampWaveButton.onClick.AddListener(StartCampTurretWave);
            UpdateCampWaveButtonVisibility();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (startCampWaveButton != null)
        {
            startCampWaveButton.onClick.RemoveAllListeners();
        }
    }

    public override IEnumerable<TurretScriptableObject> GetItems()
    {
        return TurretManager.Instance.GetTurretScriptableObjs(); // Get turrets from TurretManager
    }

    public override TurretCategory GetItemCategory(TurretScriptableObject item)
    {
        return TurretCategory.NONE; // Placeholder if no specific category exists
    }

    public override void SetupItemButton(TurretScriptableObject item, GameObject button)
    {
        var buttonComponent = button.GetComponent<TurretPreviewBtn>();
        buttonComponent.SetupButton(item);
    }

    public override string GetPreviewName(TurretScriptableObject item)
    {
        return item.objectName;
    }

    public override Sprite GetPreviewSprite(TurretScriptableObject item)
    {
        return item.sprite;
    }

    public override string GetPreviewDescription(TurretScriptableObject item)
    {
        return item.description;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(TurretScriptableObject item)
    {
        foreach (var resourceCost in item._resourceCost)
        {
            yield return (
                resourceCost.resourceScriptableObj.objectName,
                resourceCost.count,
                PlayerInventory.Instance.GetItemCount(resourceCost.resourceScriptableObj)
            );
        }
    }

    public override void UpdatePreviewSpecifics(TurretScriptableObject item)
    {
        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var (resourceName, requiredCount, playerCount) in GetPreviewResourceCosts(item))
        {
            GameObject resourceCostUI = Instantiate(previewResourceCostPrefab, previewResourceCostParent);
            resourceCostUI.GetComponentInChildren<TMP_Text>().text = $"{resourceName} : {requiredCount} ({playerCount})";
        }
    }

    public override void DestroyPreviewSpecifics()
    {
        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void StartTurretWave()
    {
        TurretManager.Instance.StartWave();
    }

    public void StartCampTurretWave()
    {
        TurretManager.Instance.StartCampTurretWave();
        if (startCampWaveButtonText != null)
        {
            startCampWaveButtonText.text = "Wave In Progress...";
            startCampWaveButton.interactable = false;
        }
    }

    private void UpdateCampWaveButtonVisibility()
    {
        if (startCampWaveButton != null)
        {
            bool isInCamp = GameManager.Instance.CurrentGameMode == GameMode.CAMP;
            startCampWaveButton.gameObject.SetActive(isInCamp);
            
            if (isInCamp && startCampWaveButtonText != null)
            {
                startCampWaveButtonText.text = "Start Zombie Wave";
            }
        }
    }

    public override void SetScreenActive(bool active, float delay = 0f, Action onComplete = null)
    {
        base.SetScreenActive(active, delay, onComplete);
        
        if (active)
        {
            UpdateCampWaveButtonVisibility();
        }
    }
}
