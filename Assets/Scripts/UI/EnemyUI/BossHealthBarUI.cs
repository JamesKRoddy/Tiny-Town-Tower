using UnityEngine;
using UnityEngine.UI;

namespace Enemies
{
    /// <summary>
    /// UI component for displaying boss health bars.
    /// Works with any EnemyBase (no longer tied to Boss class).
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text bossNameText;

        private EnemyBase enemy;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Initialize the boss health bar with an enemy reference
        /// </summary>
        public void Initialize(EnemyBase enemy)
        {
            this.enemy = enemy;
            if (bossNameText != null)
            {
                bossNameText.text = enemy.gameObject.name;
            }
            UpdateHealth(enemy.Health, enemy.MaxHealth);
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

        private void Update()
        {
            if (enemy != null && mainCamera != null)
            {
                // Position the health bar above the enemy
                Vector3 screenPos = mainCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 3f);
                transform.position = screenPos;
            }
        }
    }
} 