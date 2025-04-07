using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class WeaponComparisonMenu : MenuBase
{
    [Header("Current Weapon UI")]
    [SerializeField] private Image currentWeaponImage;
    [SerializeField] private TextMeshProUGUI currentWeaponName;
    [SerializeField] private TextMeshProUGUI currentWeaponDescription;
    [SerializeField] private TextMeshProUGUI currentWeaponStats;

    [Header("New Weapon UI")]
    [SerializeField] private Image newWeaponImage;
    [SerializeField] private TextMeshProUGUI newWeaponName;
    [SerializeField] private TextMeshProUGUI newWeaponDescription;
    [SerializeField] private TextMeshProUGUI newWeaponStats;

    [Header("Buttons")]
    [SerializeField] private Button keepCurrentButton;
    [SerializeField] private Button equipNewButton;

    private WeaponScriptableObj currentWeapon;
    private WeaponScriptableObj newWeapon;
    private Action<bool> onChoiceMade;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        keepCurrentButton.onClick.AddListener(() => OnChoiceMade(false));
        equipNewButton.onClick.AddListener(() => OnChoiceMade(true));
    }

    public void ShowWeaponComparison(WeaponScriptableObj current, WeaponScriptableObj newWeapon, Action<bool> callback)
    {
        this.currentWeapon = current;
        this.newWeapon = newWeapon;
        this.onChoiceMade = callback;

        // Update current weapon UI
        UpdateWeaponUI(current, currentWeaponImage, currentWeaponName, currentWeaponDescription, currentWeaponStats);

        // Update new weapon UI
        UpdateWeaponUI(newWeapon, newWeaponImage, newWeaponName, newWeaponDescription, newWeaponStats);

        // Show the menu with a small delay
        SetScreenActive(true, 0.1f);
    }

    private void UpdateWeaponUI(WeaponScriptableObj weapon, Image image, TextMeshProUGUI nameText, 
        TextMeshProUGUI descriptionText, TextMeshProUGUI statsText)
    {
        if (weapon == null) return;

        image.sprite = weapon.sprite;
        nameText.text = weapon.objectName;
        descriptionText.text = weapon.description;

        // Format stats text
        string stats = $"Damage: {weapon.damage}\n" +
                      $"Attack Speed: {weapon.attackSpeed}\n" +
                      $"Element: {weapon.weaponElement}";

        statsText.text = stats;
    }

    private void OnChoiceMade(bool equipNew)
    {
        onChoiceMade?.Invoke(equipNew);
        ReturnToGame();
    }

    public void ReturnToGame()
    {
        SetScreenActive(false, 0.1f);
        PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
    }
}
