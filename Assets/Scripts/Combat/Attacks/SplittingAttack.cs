using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace Combat.Attacks
{
    /// <summary>
    /// Splitting attack that spawns smaller enemies when triggered.
    /// Inherits from ExplosionAttack to get explosion VFX and damage.
    /// Can be triggered on death, on damage, or as a manual attack.
    /// 
    /// This is a modular attack component that can be attached to ANY enemy type:
    /// - Splitting Zombies (explode and spawn mini zombies)
    /// - Splitting Robots (explode and spawn repair bots)
    /// - Splitting Drones (explode and spawn mini drones)
    /// - Any other enemy you want to split!
    /// 
    /// Usage:
    /// 1. Attach this component to your enemy GameObject
    /// 2. Assign the smallEnemyPrefab in the inspector
    /// 3. Configure splitCount (how many small enemies to spawn)
    /// 4. Set minSplitSize to prevent infinite splitting
    /// 5. Configure explosion settings from ExplosionAttack base class
    /// 6. Optionally disable dieAfterExplosion if you want the enemy to survive splitting
    /// </summary>
    public class SplittingAttack : ExplosionAttack
    {
        #region Inspector Fields

        [Header("Splitting Settings")]
        [Tooltip("Prefab of the smaller enemy to spawn. Can be any enemy type with SplittingAttack for multi-level splitting.")]
        [SerializeField] private GameObject smallEnemyPrefab;
        
        [Tooltip("Number of smaller enemies to spawn when triggered")]
        [SerializeField] private int splitCount = 2;
        
        [Tooltip("Minimum scale before enemies stop splitting (prevents infinite splitting)")]
        [SerializeField] private float minSplitSize = 0.5f;
        
        [Tooltip("Scale multiplier for spawned enemies (1.0 = same size, 0.5 = half size)")]
        [SerializeField] private float splitScaleMultiplier = 0.7f;
        
        [Tooltip("Health multiplier for spawned enemies (1.0 = same health, 0.5 = half health)")]
        [SerializeField] private float splitHealthMultiplier = 0.5f;
        
        [Tooltip("Damage multiplier for spawned enemies (1.0 = same damage, 0.5 = half damage)")]
        [SerializeField] private float splitDamageMultiplier = 0.7f;
        
        [Tooltip("Spawn radius around the split position")]
        [SerializeField] private float spawnRadius = 2f;
        
        [Tooltip("Effect to play when splitting (in addition to explosion effect)")]
        [SerializeField] private EffectSpawnData splitEffect;
        
        [Tooltip("Delay before spawning small enemies (allows explosion animation to play)")]
        [SerializeField] private float spawnDelay = 0.3f;

        #endregion

        #region Private Fields

        /// <summary>
        /// Track if this enemy has already split to prevent multiple splits
        /// </summary>
        private bool hasSplit = false;
        
        /// <summary>
        /// Cached MonoBehaviour for coroutines (owner may not be a MonoBehaviour)
        /// </summary>
        private MonoBehaviour coroutineRunner;

        #endregion

        #region Initialization

        protected override void Awake()
        {
            base.Awake();
            
            // Splitting attacks typically use explosion attack type
            if (attackType == 0)
            {
                attackType = 4; // Explosion attack type
            }
            
            coroutineRunner = this;
        }

        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            Debug.Log($"[{attackOwner.gameObject.name}] SplittingAttack initialized | SplitCount: {splitCount} | SpawnPrefab: {smallEnemyPrefab?.name ?? "None"}");
        }

        #endregion

        #region Attack Behavior

        /// <summary>
        /// Override OnAttackEnd to spawn smaller enemies after explosion
        /// </summary>
        public override void OnAttackEnd()
        {
            // Call base to handle explosion and dying
            base.OnAttackEnd();
            
            // Spawn smaller enemies if we should split
            if (!hasSplit && ShouldSplit())
            {
                hasSplit = true;
                coroutineRunner.StartCoroutine(SpawnSmallEnemiesCoroutine());
            }
        }

        /// <summary>
        /// Override ForceExplode to also trigger splitting
        /// </summary>
        public new void ForceExplode()
        {
            // Call base explosion
            base.ForceExplode();
            
            // Spawn smaller enemies if we should split
            if (!hasSplit && ShouldSplit())
            {
                hasSplit = true;
                coroutineRunner.StartCoroutine(SpawnSmallEnemiesCoroutine());
            }
        }

        #endregion

        #region Splitting Logic

        /// <summary>
        /// Check if this enemy should split based on size constraints
        /// </summary>
        private bool ShouldSplit()
        {
            // Don't split if no prefab assigned
            if (smallEnemyPrefab == null)
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] No small enemy prefab assigned, cannot split!");
                return false;
            }
            
            // Don't split if too small
            float currentScale = OwnerTransform.localScale.x;
            float nextScale = currentScale * splitScaleMultiplier;
            
            if (nextScale < minSplitSize)
            {
                Debug.Log($"[{OwnerTransform.gameObject.name}] Too small to split (current: {currentScale}, next: {nextScale}, min: {minSplitSize})");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Coroutine to handle spawning smaller enemies after split
        /// </summary>
        private IEnumerator SpawnSmallEnemiesCoroutine()
        {
            Debug.Log($"[{OwnerTransform.gameObject.name}] Starting split spawn coroutine, waiting {spawnDelay} seconds...");
            
            // Wait for explosion animation to play
            yield return new WaitForSeconds(spawnDelay);
            
            // Play split effect (in addition to explosion effect)
            if (splitEffect != null && splitEffect.IsValid())
            {
                splitEffect.SpawnEffect(OwnerTransform);
            }
            
            // Spawn smaller enemies
            for (int i = 0; i < splitCount; i++)
            {
                SpawnSmallEnemy(i);
            }
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Split complete, spawned {splitCount} smaller enemies");
        }

        /// <summary>
        /// Spawn a single small enemy
        /// </summary>
        private void SpawnSmallEnemy(int index)
        {
            // Calculate spawn position in a circle around the split position
            float angle = (360f / splitCount) * index;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * spawnRadius;
            Vector3 spawnPosition = OwnerTransform.position + offset;
            
            // Try to find a valid NavMesh position
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, spawnRadius * 2f, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
            }
            else
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Could not find valid NavMesh position for small enemy {index}, using original position");
            }
            
            // Spawn the small enemy
            GameObject smallEnemy = Object.Instantiate(smallEnemyPrefab, spawnPosition, OwnerTransform.rotation);
            
            // Scale down the spawned enemy
            float newScale = OwnerTransform.localScale.x * splitScaleMultiplier;
            smallEnemy.transform.localScale = Vector3.one * newScale;
            
            // Get the IDamageable to modify health
            IDamageable damageable = smallEnemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Reduce health based on owner's health
                float ownerMaxHealth = owner?.Health ?? 100f;
                damageable.MaxHealth = ownerMaxHealth * splitHealthMultiplier;
                damageable.Health = damageable.MaxHealth;
            }
            
            // Reduce damage for all attacks
            AttackBase[] attacks = smallEnemy.GetComponents<AttackBase>();
            foreach (var attack in attacks)
            {
                attack.damage *= splitDamageMultiplier;
                attack.poiseDamage *= splitDamageMultiplier;
            }
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Spawned small enemy {index} with scale: {newScale}");
            
            // If the spawned enemy also has SplittingAttack, it can split again (if large enough)
            SplittingAttack splittingComponent = smallEnemy.GetComponent<SplittingAttack>();
            if (splittingComponent != null)
            {
                Debug.Log($"[{OwnerTransform.gameObject.name}] Small enemy {index} can also split (scale check will determine if it actually does)");
            }
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw gizmos to show split spawn radius
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Draw split spawn radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(drawTransform.position, spawnRadius);
            
            // Draw spawn positions
            Gizmos.color = Color.green;
            for (int i = 0; i < splitCount; i++)
            {
                float angle = (360f / splitCount) * i;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * spawnRadius;
                Vector3 spawnPosition = drawTransform.position + offset;
                Gizmos.DrawWireSphere(spawnPosition, 0.5f);
            }
        }

        #endregion
    }
}
