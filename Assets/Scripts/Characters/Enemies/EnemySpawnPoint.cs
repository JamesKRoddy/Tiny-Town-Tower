using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Enemies
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        public EnemyTargetType enemyTargetType;
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

            Transform enemyTarget;
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();

            switch (enemyTargetType)
            {
                case EnemyTargetType.NONE:
                    Debug.LogError("EnemySpawnPoint incorrectly setup");
                    return null;
                case EnemyTargetType.PLAYER:
                    enemyTarget = PlayerController.Instance._possessedNPC.GetTransform();
                    enemyBase.Setup(enemyTarget);
                    break;
                case EnemyTargetType.CLOSEST_NPC:
                    return null;
                case EnemyTargetType.TURRET_END:
                    enemyTarget = TurretManager.Instance.baseTarget.transform;
                    break;
                default:
                    return null;
            }

            // For camp enemies, find a target building or turret instead of just using the default target
            if (GameManager.Instance.CurrentGameMode == GameMode.CAMP)
            {
                Transform campTarget = FindCampTarget();
                if (campTarget != null)
                {
                    enemyTarget = campTarget;
                }
            }

            if (enemyTarget != null)
                enemyBase.SetEnemyDestination(enemyTarget.position);

            return enemy;
        }

        private Transform FindCampTarget()
        {
            // Find the closest building or turret to attack
            List<Transform> potentialTargets = new List<Transform>();
            
            // Find all buildings
            Building[] buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                if (building != null && building.IsOperational())
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
            
            // Find the closest target
            Transform closestTarget = null;
            float closestDistance = Mathf.Infinity;
            
            foreach (var target in potentialTargets)
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
