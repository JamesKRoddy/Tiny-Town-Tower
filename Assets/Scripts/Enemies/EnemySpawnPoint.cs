using System.Collections;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    public EnemyTargetType enemyTargetType;
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
            case EnemyTargetType.CLOSEST_NPC: //TODO set this up for the enemy to attack whatevers closest
                return null;
            case EnemyTargetType.TURRET_END:
                enemyTarget = TurretManager.Instance.baseTarget.transform;
                return null;
            default:
                return null;
        }

        return enemy;
    }

    private IEnumerator StartCooldown()
    {
        yield return new WaitForSeconds(cooldownDuration);
        isAvailable = true;
    }
}
