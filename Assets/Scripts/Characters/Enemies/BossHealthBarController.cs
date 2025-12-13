using UnityEngine;
using Managers;

namespace Enemies
{
    /// <summary>
    /// Component that adds boss health bar UI to any enemy.
    /// Attach this to any enemy (HumanoidEnemy, Drone, etc.) to make them display as a boss.
    /// 
    /// This component:
    /// - Uses the singleton BossHealthBarUI instance in the scene
    /// - Updates the health bar when the enemy takes damage
    /// - Hides the UI when the boss dies
    /// 
    /// Usage:
    /// 1. Add this component to any enemy GameObject
    /// 2. Ensure BossHealthBarUI exists in your Canvas
    /// 3. The component will automatically hook into the enemy's health system
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class BossHealthBarController : MonoBehaviour
    {
        #region Private Fields
        
        /// <summary>
        /// Reference to the enemy this component is attached to
        /// </summary>
        private EnemyBase enemy;
        
        /// <summary>
        /// Track if we've already cleaned up (prevents double cleanup)
        /// </summary>
        private bool hasCleanedUp = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Get reference to the enemy component
            enemy = GetComponent<EnemyBase>();
            
            if (enemy == null)
            {
                Debug.LogError($"[{gameObject.name}] BossHealthBarController requires an EnemyBase component!");
                enabled = false;
                return;
            }
            
            // Initialize the boss health bar UI
            InitializeBossUI();
        }
        
        private void OnEnable()
        {
            // Subscribe to enemy events
            if (enemy != null)
            {
                enemy.OnDamageTaken += OnEnemyDamageTaken;
                enemy.OnDeath += OnEnemyDeath;
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            if (enemy != null)
            {
                enemy.OnDamageTaken -= OnEnemyDamageTaken;
                enemy.OnDeath -= OnEnemyDeath;
            }
            CleanupHealthBar();
        }
        
        private void OnDestroy()
        {
            // Ensure cleanup happens even if enemy is destroyed directly
            CleanupHealthBar();
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Called when the enemy takes damage
        /// </summary>
        private void OnEnemyDamageTaken(float currentHealth, float maxHealth)
        {
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.UpdateHealth(currentHealth, maxHealth);
            }
        }
        
        /// <summary>
        /// Called when the enemy dies
        /// </summary>
        private void OnEnemyDeath()
        {
            CleanupHealthBar();
        }
        
        #endregion
        
        #region Health Bar Management
        
        /// <summary>
        /// Initialize the boss health bar UI using the singleton instance
        /// </summary>
        private void InitializeBossUI()
        {
            if (BossHealthBarUI.Instance == null)
            {
                FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include).Initialize();
            }
            
            // Initialize the singleton instance with this boss's info
            BossHealthBarUI.Instance.EnableBossHealthUI(enemy.gameObject.name, enemy.Health, enemy.MaxHealth);
            Debug.Log($"[{gameObject.name}] Boss health bar initialized for {enemy.name}");
        }
        
        /// <summary>
        /// Clean up the boss health bar UI
        /// </summary>
        private void CleanupHealthBar()
        {
            if (hasCleanedUp) return;
            
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.Hide();
                Debug.Log($"[{gameObject.name}] Boss health bar hidden");
            }
            
            hasCleanedUp = true;
        }

        #endregion
    }
}

