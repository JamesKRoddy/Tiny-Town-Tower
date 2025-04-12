using UnityEngine;
using UnityEngine.UI;

namespace Enemies
{
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text bossNameText;

        private Boss boss;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        public void Initialize(Boss boss)
        {
            this.boss = boss;
            if (bossNameText != null)
            {
                bossNameText.text = boss.gameObject.name;
            }
            UpdateHealth(boss.Health, boss.MaxHealth);
        }

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
            if (boss != null && mainCamera != null)
            {
                // Position the health bar above the boss
                Vector3 screenPos = mainCamera.WorldToScreenPoint(boss.transform.position + Vector3.up * 3f);
                transform.position = screenPos;
            }
        }
    }
} 