using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;
using UnityEngine.AI;

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

            // Determine initial target based on game mode
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
                case GameMode.CAMP_ATTACK:
                    // In camp, find any available target (enemy will validate reachability)
                    enemyTarget = FindInitialCampTarget();
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
                Debug.LogWarning("No initial target found for enemy, enemy will find its own target");
                // Enemy will find its own target in its Update method
            }

            return enemy;
        }

        private Transform FindInitialCampTarget()
        {
            // Simple initial target finding - let the enemy handle reachability validation
            List<Transform> potentialTargets = new List<Transform>();
            
            // Find all buildings
            Building[] buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                if (building != null && building.Health > 0)
                {
                    potentialTargets.Add(building.transform);
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
            
            // Find the closest target (enemy will validate reachability)
            Transform closestTarget = null;
            float closestDistance = Mathf.Infinity;
            
            foreach (var target in potentialTargets)
            {
                if (target == null) continue;
                
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
