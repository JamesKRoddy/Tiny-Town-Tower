using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildMenu : PreviewListMenuBase<BuildingCategory, BuildingScriptableObj>, IControllerInput
{
    private static BuildMenu _instance;

    public static BuildMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BuildMenu>();
                if (_instance == null)
                {
                    Debug.LogError("BuildMenu instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    public override void Setup()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    [Header("Build Menu Preview UI")]
    [SerializeField] GameObject previewResourceCostPrefab;
    [SerializeField] RectTransform previewResourceCostParent;

    [Header("Full list of Building Scriptable Objs")]
    public BuildingScriptableObj[] buildingScriptableObjs;

    public override void OnEnable()
    {
        base.OnEnable();        

        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public void OnDestroy()
    {
        if(PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;

        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnRBPressed += rightScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnLBPressed += leftScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnBPressed += () => UtilityMenu.Instance.EnableUtilityMenu();
                break;
            default:
                break;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay);     
    }

    public override IEnumerable<BuildingScriptableObj> GetItems()
    {
        return buildingScriptableObjs; // Array of buildings from inspector
    }

    public override BuildingCategory GetItemCategory(BuildingScriptableObj item)
    {
        return item.buildingCategory; // Category of the building
    }

    public override void SetupItemButton(BuildingScriptableObj item, GameObject button)
    {
        var buttonComponent = button.GetComponent<BuildingPreviewBtn>();
        buttonComponent.SetupButton(item);
    }

    public override string GetPreviewName(BuildingScriptableObj item)
    {
        return item.buildingName;
    }

    public override Sprite GetPreviewSprite(BuildingScriptableObj item)
    {
        return item.buildingSprite;
    }

    public override string GetPreviewDescription(BuildingScriptableObj item)
    {
        return item.buildingDescription;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(BuildingScriptableObj item)
    {
        foreach (var resourceCost in item.buildingResourceCost)
        {
            yield return (
                resourceCost.resource.resourceName,
                resourceCost.count,
                PlayerInventory.Instance.GetItemCount(resourceCost.resource)
            );
        }
    }

    public override void UpdatePreviewSpecifics(BuildingScriptableObj item)
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
}
