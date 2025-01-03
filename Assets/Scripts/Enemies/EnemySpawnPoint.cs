using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    private bool isAvailable = true; // Tracks if the spawn point is available
    public float cooldownDuration = 2f; // Cooldown duration in seconds

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

        // Mark spawn point as unavailable
        isAvailable = false;

        // Start cooldown timer
        StartCoroutine(StartCooldown());

        // Instantiate the enemy at this spawn point's position and rotation
        GameObject enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);

        return enemy;
    }

    private IEnumerator StartCooldown()
    {
        yield return new WaitForSeconds(cooldownDuration);
        isAvailable = true;
    }
}
