using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretMenu : PreviewListMenuBase<TurretCategory, TurretScriptableObject>, IControllerInput
{
    private static TurretMenu _instance;

    public static TurretMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<TurretMenu>();
                if (_instance == null)
                {
                    Debug.LogError("TurretMenu instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("Turret Menu UI")]
    [SerializeField] GameObject previewResourceCostPrefab; // Optional if resources apply to turrets
    [SerializeField] RectTransform previewResourceCostParent; // Optional if resources apply to turrets
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonText;

    [Header("Full list of Turret Scriptable Objects")]
    public TurretScriptableObject[] turretScriptableObjs;

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
        if (PlayerInput.Instance != null)
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

    public override IEnumerable<TurretScriptableObject> GetItems()
    {
        return turretScriptableObjs; // Array of turrets from inspector
    }

    public override TurretCategory GetItemCategory(TurretScriptableObject item)
    {
        // Assuming TurretCategory is part of the TurretScriptableObject or deduced elsewhere
        return TurretCategory.NONE; // Placeholder if no specific category exists
    }

    public override void SetupItemButton(TurretScriptableObject item, GameObject button)
    {
        var buttonComponent = button.GetComponent<TurretPreviewBtn>();
        buttonComponent.SetupButton(item);
    }

    public override string GetPreviewName(TurretScriptableObject item)
    {
        return item.turretName;
    }

    public override Sprite GetPreviewSprite(TurretScriptableObject item)
    {
        return item.turretSprite;
    }

    public override string GetPreviewDescription(TurretScriptableObject item)
    {
        return item.turretDescription;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(TurretScriptableObject item)
    {
        // Placeholder: Implement if turrets have a resource cost
        yield break;
    }

    public override void UpdatePreviewSpecifics(TurretScriptableObject item)
    {
        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }

        // Populate UI elements with turret-specific details
        if (previewResourceCostPrefab != null && previewResourceCostParent != null)
        {
            // Example: Populate resource costs if relevant
            foreach (var (resourceName, requiredCount, playerCount) in GetPreviewResourceCosts(item))
            {
                GameObject resourceCostUI = Instantiate(previewResourceCostPrefab, previewResourceCostParent);
                resourceCostUI.GetComponentInChildren<TMP_Text>().text = $"{resourceName} : {requiredCount} ({playerCount})";
            }
        }

        // Update upgrade button specifics (if applicable)
        upgradeButton.interactable = CanUpgrade(item); // Implement CanUpgrade logic
        upgradeButtonText.text = GetUpgradeText(item); // Implement GetUpgradeText logic
    }

    public override void DestroyPreviewSpecifics()
    {
        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }
    }

    private bool CanUpgrade(TurretScriptableObject item)
    {
        // Add logic to determine if the turret can be upgraded
        return true; // Placeholder
    }

    private string GetUpgradeText(TurretScriptableObject item)
    {
        // Add logic to get appropriate upgrade text
        return "Upgrade Available"; // Placeholder
    }
}
