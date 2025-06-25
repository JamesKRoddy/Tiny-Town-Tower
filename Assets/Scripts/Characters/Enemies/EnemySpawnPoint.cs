using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Enemies
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private ParticleSystem spawnEffect;
        private bool isAvailable = true;
        public float cooldownDuration = 2f;

        public bool IsAvailable()
        {
            return isAvailable;
        }

        public GameObject SpawnEnemy(GameObject enemyPrefab)
        {
            if (!isAvailable)
            {
                Debug.LogWarning($"Spawn point at {transform.position} is on cooldown.");
                return null;
            }

            if (enemyPrefab == null)
            {
                Debug.LogError("Enemy prefab is null!");
                return null;
            }

            isAvailable = false; // Mark spawn point as unavailable
            StartCoroutine(StartCooldown()); // Start cooldown

            // Instantiate the enemy at this spawn point's position and rotation
            GameObject enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);

            if (spawnEffect != null)
            {
                spawnEffect.Play();
            }

            Transform enemyTarget = null;
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();

            // Determine target based on game mode
            switch (GameManager.Instance.CurrentGameMode)
            {
                case GameMode.ROGUE_LITE:
                    // In rogue lite, target the possessed NPC
                    if (PlayerController.Instance._possessedNPC != null)
                    {
                        enemyTarget = PlayerController.Instance._possessedNPC.GetTransform();
                    }
                    break;
                    
                case GameMode.CAMP:
                    // In camp, find appropriate camp targets
                    enemyTarget = FindCampTarget();
                    break;
                    
                case GameMode.CAMP_ATTACK:
                    // In camp attack, also use camp targeting
                    enemyTarget = FindCampTarget();
                    break;
                    
                default:
                    Debug.LogWarning($"No targeting logic for game mode: {GameManager.Instance.CurrentGameMode}");
                    break;
            }

            // Setup the enemy with the target
            if (enemyTarget != null)
            {
                enemyBase.Setup(enemyTarget);
                enemyBase.SetEnemyDestination(enemyTarget.position);
            }
            else
            {
                Debug.LogWarning("No target found for enemy, using default behavior");
                // Fallback to possessed NPC if available
                if (PlayerController.Instance._possessedNPC != null)
                {
                    enemyTarget = PlayerController.Instance._possessedNPC.GetTransform();
                    enemyBase.Setup(enemyTarget);
                }
            }

            return enemy;
        }

        private Transform FindCampTarget()
        {
            // Find the closest building, wall, turret, or NPC to attack
            List<Transform> potentialTargets = new List<Transform>();
            List<Transform> wallTargets = new List<Transform>();
            
            // Find all buildings
            Building[] buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                if (building != null && building.IsOperational())
                {
                    // Check if it's a wall building
                    if (building.GetType() == typeof(WallBuilding))
                    {
                        // Use reflection to check if the wall is destroyed
                        var isDestroyedProperty = building.GetType().GetProperty("IsDestroyed");
                        if (isDestroyedProperty != null)
                        {
                            bool isDestroyed = (bool)isDestroyedProperty.GetValue(building);
                            if (!isDestroyed)
                            {
                                wallTargets.Add(building.transform);
                            }
                        }
                    }
                    else
                    {
                        potentialTargets.Add(building.transform);
                    }
                }
            }
            
            // Find all turrets
            var turrets = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var turret in turrets)
            {
                if (turret != null && turret.GetType().Name.Contains("Turret"))
                {
                    potentialTargets.Add(turret.transform);
                }
            }
            
            // Find all NPCs (excluding the player if they're possessed)
            HumanCharacterController[] npcs = FindObjectsByType<HumanCharacterController>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc != null && npc != PlayerController.Instance._possessedNPC)
                {
                    potentialTargets.Add(npc.transform);
                }
            }
            
            // First try to find a non-wall target
            Transform closestTarget = FindClosestTarget(potentialTargets);
            
            // If no non-wall targets found, use walls as fallback
            if (closestTarget == null && wallTargets.Count > 0)
            {
                closestTarget = FindClosestTarget(wallTargets);
                Debug.Log($"No other targets found, targeting wall at {closestTarget?.position}");
            }
            
            // If still no targets, log a warning
            if (closestTarget == null)
            {
                Debug.LogWarning("No camp targets found! No buildings, turrets, walls, or NPCs available for enemies to attack.");
            }
            
            return closestTarget;
        }
        
        private Transform FindClosestTarget(List<Transform> targets)
        {
            Transform closestTarget = null;
            float closestDistance = Mathf.Infinity;
            
            foreach (var target in targets)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }
            
            return closestTarget;
        }

        private IEnumerator StartCooldown()
        {
            yield return new WaitForSeconds(cooldownDuration);
            isAvailable = true;
        }
    }
}
