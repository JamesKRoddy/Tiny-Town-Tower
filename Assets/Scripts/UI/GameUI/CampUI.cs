using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;

public class CampUI : MonoBehaviour
{
    [Header("Electricity UI")]
    [SerializeField] private Slider electricitySlider;
    [SerializeField] private TextMeshProUGUI electricityText;
    [SerializeField] private Image electricityFillImage;
    [SerializeField] private Color normalElectricityColor = Color.green;
    [SerializeField] private Color lowElectricityColor = Color.red;
    [SerializeField] private float lowElectricityThreshold = 20f; // Percentage

    [Header("Cleanliness UI")]
    [SerializeField] private Slider cleanlinessSlider;
    [SerializeField] private TextMeshProUGUI cleanlinessText;
    [SerializeField] private Image cleanlinessFillImage;
    [SerializeField] private Color normalCleanlinessColor = Color.green;
    [SerializeField] private Color lowCleanlinessColor = Color.red;
    [SerializeField] private float lowCleanlinessThreshold = 20f; // Percentage

    void Start()
    {
        UpdateElectricityUI(CampManager.Instance.GetElectricityPercentage());
        UpdateCleanlinessUI(CampManager.Instance.GetCleanlinessPercentage());
    }

    public void Setup()
    {
        // Subscribe to game mode changes
        GameManager.Instance.OnGameModeChanged += OnGameModeChanged;
        
        // Subscribe to camp events
        CampManager.Instance.OnElectricityChanged += UpdateElectricityUI;
        CampManager.Instance.OnCleanlinessChanged += UpdateCleanlinessUI;

        // Initialize UI values
        if (electricitySlider != null)
        {
            electricitySlider.maxValue = 100f;
            electricitySlider.value = CampManager.Instance.GetElectricityPercentage();
        }

        if (cleanlinessSlider != null)
        {
            cleanlinessSlider.maxValue = 100f;
            cleanlinessSlider.value = CampManager.Instance.GetCleanlinessPercentage();
        }

        // Set initial visibility based on current game mode
        gameObject.SetActive(GameManager.Instance.CurrentGameMode == GameMode.CAMP);
    }

    private void OnGameModeChanged(GameMode newGameMode)
    {
        // Only show the UI in CAMP mode
        gameObject.SetActive(newGameMode == GameMode.CAMP);
    }

    private void UpdateElectricityUI(float percentage)
    {
        if (electricitySlider != null)
        {
            electricitySlider.value = percentage;
        }

        if (electricityText != null)
        {
            electricityText.text = $"Power: {percentage:F1}%";
        }

        if (electricityFillImage != null)
        {
            electricityFillImage.color = percentage <= lowElectricityThreshold ? 
                lowElectricityColor : normalElectricityColor;
        }
    }

    private void UpdateCleanlinessUI(float percentage)
    {
        if (cleanlinessSlider != null)
        {
            cleanlinessSlider.value = percentage;
        }

        if (cleanlinessText != null)
        {
            cleanlinessText.text = $"Cleanliness: {percentage:F1}%";
        }

        if (cleanlinessFillImage != null)
        {
            cleanlinessFillImage.color = percentage <= lowCleanlinessThreshold ? 
                lowCleanlinessColor : normalCleanlinessColor;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from game mode changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameModeChanged -= OnGameModeChanged;
        }

        // Unsubscribe from camp events
        if (CampManager.Instance != null)
        {
            CampManager.Instance.OnElectricityChanged -= UpdateElectricityUI;
            CampManager.Instance.OnCleanlinessChanged -= UpdateCleanlinessUI;
        }
    }
} 