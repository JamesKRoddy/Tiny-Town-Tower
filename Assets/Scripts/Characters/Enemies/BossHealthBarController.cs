using UnityEngine;
using Managers;

namespace Enemies
{
    /// <summary>
    /// Component that adds boss health bar UI to any enemy.
    /// Attach this to any enemy (HumanoidEnemy, Drone, etc.) to make them display as a boss.
    /// 
    /// This component:
    /// - Spawns and manages the boss health bar UI
    /// - Updates the health bar when the enemy takes damage
    /// - Cleans up the UI when the boss dies
    /// 
    /// Usage:
    /// 1. Add this component to any enemy GameObject
    /// 2. Assign the boss health bar prefab in the inspector
    /// 3. The component will automatically hook into the enemy's health system
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class BossHealthBarController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Boss Health Bar")]
        [Tooltip("Prefab containing the BossHealthBarUI component")]
        [SerializeField] private GameObject bossHealthBarPrefab;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Reference to the enemy this component is attached to
        /// </summary>
        private EnemyBase enemy;
        
        /// <summary>
        /// Instance of the boss health bar UI
        /// </summary>
        private BossHealthBarUI healthBarUI;
        
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
                // Note: EnemyBase doesn't have events, so we'll poll in Update
                // If you add events to EnemyBase later, subscribe here
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            CleanupHealthBar();
        }
        
        private void Update()
        {
            // Update health bar if enemy health changed
            // This is a polling approach since EnemyBase doesn't have health change events
            if (healthBarUI != null && enemy != null)
            {
                healthBarUI.UpdateHealth(enemy.Health, enemy.MaxHealth);
            }
            
            // Check if enemy died (health <= 0)
            if (enemy != null && enemy.Health <= 0 && !hasCleanedUp)
            {
                CleanupHealthBar();
            }
        }
        
        private void OnDestroy()
        {
            // Ensure cleanup happens even if enemy is destroyed directly
            CleanupHealthBar();
        }
        
        #endregion
        
        #region Health Bar Management
        
        /// <summary>
        /// Initialize and spawn the boss health bar UI
        /// </summary>
        private void InitializeBossUI()
        {
            if (bossHealthBarPrefab == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Boss health bar prefab not assigned to BossHealthBarController");
                return;
            }
            
            // Spawn the health bar UI
            GameObject healthBarObj = Instantiate(bossHealthBarPrefab);
            healthBarUI = healthBarObj.GetComponent<BossHealthBarUI>();
            
            if (healthBarUI != null)
            {
                // Initialize with enemy reference
                healthBarUI.Initialize(enemy);
                Debug.Log($"[{gameObject.name}] Boss health bar initialized for {enemy.name}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] BossHealthBarUI component not found on boss health bar prefab");
                Destroy(healthBarObj);
            }
        }
        
        /// <summary>
        /// Clean up the boss health bar UI
        /// </summary>
        private void CleanupHealthBar()
        {
            if (hasCleanedUp) return;
            
            if (healthBarUI != null)
            {
                Destroy(healthBarUI.gameObject);
                healthBarUI = null;
                Debug.Log($"[{gameObject.name}] Boss health bar cleaned up");
            }
            
            hasCleanedUp = true;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get the current health bar UI instance (useful for testing/debugging)
        /// </summary>
        public BossHealthBarUI GetHealthBarUI()
        {
            return healthBarUI;
        }
        
        /// <summary>
        /// Force update the health bar (useful if health changes outside of Update loop)
        /// </summary>
        public void ForceUpdateHealthBar()
        {
            if (healthBarUI != null && enemy != null)
            {
                healthBarUI.UpdateHealth(enemy.Health, enemy.MaxHealth);
            }
        }
        
        #endregion
    }
}

