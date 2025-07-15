using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Managers;

namespace Enemies
{
    public class SplitterZombie : Zombie
    {
        #region Constants
        
        private const int DEFAULT_SPLIT_COUNT = 3;
        private const float SPLIT_SPAWN_RADIUS = 2f;
        private const float SPLIT_SPAWN_HEIGHT = 0.5f;
        
        #endregion
        
        [Header("Splitter Settings")]
        [SerializeField] private GameObject[] splitPrefabs; // Array of smaller zombie prefabs to spawn
        [SerializeField] private int splitCount = DEFAULT_SPLIT_COUNT;
        [SerializeField] private float splitSpawnRadius = SPLIT_SPAWN_RADIUS;
        [SerializeField] private float splitSpawnHeight = SPLIT_SPAWN_HEIGHT;
        [SerializeField] private EffectDefinition splitEffect; // Visual effect when splitting
        
        [Header("Splitter Behavior")]
        [SerializeField] private bool splitsOnDeath = true; // Whether to split when killed
        [SerializeField] private int maxSplitLevel = 2; // Maximum number of times a splitter can split
        
        private int currentSplitLevel = 0; // Track how many times this zombie has split
        private bool hasSplit = false;

        protected override void Awake()
        {
            base.Awake();
            
            // Validate split prefabs
            if (splitPrefabs == null || splitPrefabs.Length == 0)
            {
                Debug.LogError("Split prefabs are not assigned to SplitterZombie on " + gameObject.name);
            }
        }

        protected override void Update()
        {
            base.Update();
        }

        private void Split()
        {
            if (hasSplit || splitPrefabs == null || splitPrefabs.Length == 0) return;
            
            hasSplit = true;
            Debug.Log($"[SplitterZombie] {gameObject.name} splitting into {splitCount} smaller zombies!");
            
            // Play split effect
            if (splitEffect != null)
            {
                EffectManager.Instance.PlayEffect(
                    transform.position,
                    Vector3.zero,
                    Quaternion.identity,
                    null,
                    splitEffect
                );
            }
            
            // Spawn smaller zombies
            for (int i = 0; i < splitCount; i++)
            {
                SpawnSplitZombie();
            }
        }

        private void SpawnSplitZombie()
        {
            // Choose a random split prefab
            GameObject splitPrefab = splitPrefabs[Random.Range(0, splitPrefabs.Length)];
            if (splitPrefab == null) return;
            
            // Calculate spawn position around the splitter
            Vector3 spawnDirection = Random.insideUnitSphere.normalized;
            spawnDirection.y = 0; // Keep on ground level
            Vector3 spawnPosition = transform.position + spawnDirection * splitSpawnRadius;
            spawnPosition.y += splitSpawnHeight; // Slight height offset
            
            // Spawn the split zombie
            GameObject splitZombie = Instantiate(splitPrefab, spawnPosition, Quaternion.identity);
            
            // Configure the split zombie
            ConfigureSplitZombie(splitZombie);
        }

        private void ConfigureSplitZombie(GameObject splitZombie)
        {
            // Set the target to the same target as the original
            EnemyBase enemyComponent = splitZombie.GetComponent<EnemyBase>();
            if (enemyComponent != null && navMeshTarget != null)
            {
                enemyComponent.Setup(navMeshTarget);
            }
            
            // If it's also a splitter, configure its split level
            SplitterZombie splitterComponent = splitZombie.GetComponent<SplitterZombie>();
            if (splitterComponent != null)
            {
                splitterComponent.currentSplitLevel = currentSplitLevel + 1;
                
                // Reduce split count for deeper splits
                splitterComponent.splitCount = Mathf.Max(1, splitCount - 1);
                
                // Disable splitting if at max level
                if (splitterComponent.currentSplitLevel >= maxSplitLevel)
                {
                    splitterComponent.splitsOnDeath = false;
                }
            }
            

            
            // Scale the zombie down slightly
            float scaleMultiplier = 0.8f - (currentSplitLevel * 0.1f); // Get smaller with each split
            scaleMultiplier = Mathf.Max(0.5f, scaleMultiplier); // Don't get too small
            splitZombie.transform.localScale *= scaleMultiplier;
        }

        // Override death to handle splitting
        public override void Die()
        {
            // Split before dying if configured to do so
            if (splitsOnDeath && !hasSplit && currentSplitLevel < maxSplitLevel)
            {
                Split();
            }
            
            // Call base death behavior
            base.Die();
        }

        // Optional: Add visual indicator for split radius
        private void OnDrawGizmosSelected()
        {
            // Draw split spawn radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, splitSpawnRadius);
        }

        // Public method to force a split (useful for testing or special events)
        public void ForceSplit()
        {
            if (!hasSplit && currentSplitLevel < maxSplitLevel)
            {
                Split();
            }
        }
    }
} 