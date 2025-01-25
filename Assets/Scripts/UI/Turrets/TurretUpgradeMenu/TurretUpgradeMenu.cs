using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretUpgradeMenu : MenuBase, IControllerInput
{
    private static TurretUpgradeMenu _instance;

    public static TurretUpgradeMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<TurretUpgradeMenu>();
                if (_instance == null)
                {
                    Debug.LogError("TurretUpgradeMenu instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("Upgrade Menu UI")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private RectTransform previewResourceCostParent;
    [SerializeField] private GameObject previewResourceCostPrefab;

    private TurretScriptableObject currentTurret;

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

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay);
    }

    public void ShowUpgradeOptions(TurretScriptableObject turret)
    {
        currentTurret = turret;

        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var (resourceName, requiredCount, playerCount) in GetPreviewResourceCosts(turret))
        {
            var resourceUI = Instantiate(previewResourceCostPrefab, previewResourceCostParent);
            resourceUI.GetComponentInChildren<TMP_Text>().text = $"{resourceName}: {requiredCount} ({playerCount})";
        }

        upgradeButton.interactable = CanUpgrade(turret);
        upgradeButtonText.text = GetUpgradeText(turret);
    }

    private bool CanUpgrade(TurretScriptableObject turret)
    {
        // Add logic to determine if the turret can be upgraded
        return true; // Placeholder
    }

    private string GetUpgradeText(TurretScriptableObject turret)
    {
        // Add logic to determine upgrade button text
        return "Upgrade Available"; // Placeholder
    }

    private IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(TurretScriptableObject turret)
    {
        // Replace with actual logic to fetch resource costs
        yield break;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;

        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnBPressed += () => PlayerUIManager.Instance.SetScreenActive(this, false);
                break;
            default:
                break;
        }
    }
}
