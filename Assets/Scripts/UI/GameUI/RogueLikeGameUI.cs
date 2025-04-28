using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
public class RogueLikeGameUI : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;

    public void Setup()
    {
        // Subscribe to game mode changes
        GameManager.Instance.OnGameModeChanged += OnGameModeChanged;
        
        // Subscribe to the NPC possessed event
        PlayerController.Instance.OnNPCPossessed += OnNPCPossessed;
        
        // If there's already a possessed NPC, subscribe to its health events
        if (PlayerController.Instance._possessedNPC != null)
        {
            SubscribeToHealthEvents(PlayerController.Instance._possessedNPC);
        }

        // Set initial visibility based on current game mode
        gameObject.SetActive(GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE);
    }

    private void OnGameModeChanged(GameMode newGameMode)
    {
        // Only show the UI in ROGUE_LITE mode
        gameObject.SetActive(newGameMode == GameMode.ROGUE_LITE);
    }

    private void OnNPCPossessed(IPossessable npc)
    {
        // Unsubscribe from previous NPC's health events if it was damageable
        if (PlayerController.Instance._possessedNPC is IDamageable previousDamageable)
        {
            previousDamageable.OnDamageTaken -= UpdateHealthUI;
            previousDamageable.OnHeal -= UpdateHealthUI;
        }

        // Subscribe to new NPC's health events
        SubscribeToHealthEvents(npc);
    }

    private void SubscribeToHealthEvents(IPossessable npc)
    {
        if (npc is IDamageable damageable)
        {
            damageable.OnDamageTaken += UpdateHealthUI;
            damageable.OnHeal += UpdateHealthUI;
            UpdateHealthUI(0, damageable.Health);
        }
    }

    private void UpdateHealthUI(float amount, float currentHealth)
    {
        if (healthSlider != null && PlayerController.Instance._possessedNPC is IDamageable damageable)
        {
            healthSlider.maxValue = damageable.MaxHealth;
            healthSlider.value = currentHealth;
            
            // Update health text
            if (healthText != null)
            {
                healthText.text = $"{currentHealth:F0}/{damageable.MaxHealth:F0}";
            }

            // Update health bar color based on health percentage
            if (healthFillImage != null)
            {
                float healthPercentage = currentHealth / damageable.MaxHealth;
                healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
            }
        }
    }

    public void OnDestroy()
    {
        // Unsubscribe from game mode changes
        if (GameManager.Instance != null) GameManager.Instance.OnGameModeChanged -= OnGameModeChanged;
        
        // Unsubscribe from NPC possessed event
        if (PlayerController.Instance != null) PlayerController.Instance.OnNPCPossessed -= OnNPCPossessed;
        else return;

        // Unsubscribe from current NPC's health events if it's damageable
        if (PlayerController.Instance._possessedNPC is IDamageable damageable)
        {
            damageable.OnDamageTaken -= UpdateHealthUI;
            damageable.OnHeal -= UpdateHealthUI;
        }
    }
}
