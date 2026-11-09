using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Enemies
{
    /// <summary>
    /// UI component for displaying boss health bars.
    /// This is a static UI element that should be placed in the scene (Canvas).
    /// Uses singleton pattern since there can only be one boss health bar.
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text bossNameText;

        public static BossHealthBarUI Instance { get; private set; }

        public void Initialize(){
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Initialize the boss health bar with boss name and health
        /// </summary>
        public void EnableBossHealthUI(string bossName, float currentHealth, float maxHealth)
        {
            gameObject.SetActive(true);
            
            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }
            
            UpdateHealth(currentHealth, maxHealth);
        }

        /// <summary>
        /// Update the health bar display
        /// </summary>
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }

        /// <summary>
        /// Hide the boss health bar
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
} 