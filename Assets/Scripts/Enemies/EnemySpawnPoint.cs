using UnityEngine;

public enum SpawnPointType
{
    ROGUELITE,
    TURRETMAZE
}

public class EnemySpawnPoint : MonoBehaviour
{
    public SpawnPointType spawnPointType;

    public GameObject SpawnEnemy(GameObject enemyPrefab)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab is null!");
            return null;
        }

        // Instantiate the enemy at this spawn point's position and rotation
        GameObject enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);

        EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();

        if (enemyBase)
        {
            switch (spawnPointType)
            {
                case SpawnPointType.ROGUELITE:
                    enemyBase.Setup(PlayerController.Instance.possesedNPC.transform);
                    break;
                case SpawnPointType.TURRETMAZE: //TODO set this up too
                    break;
                default:
                    break;
            }
        }
        else
        {
            Debug.LogError($"{enemy.name} doesnt have an EnemyBase");
            return null;
        }

        // Return the instantiated enemy
        return enemy;
    }
}
