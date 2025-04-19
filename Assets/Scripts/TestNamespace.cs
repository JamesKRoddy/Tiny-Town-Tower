using UnityEngine;
using Managers;

public class TestNamespace : MonoBehaviour
{
    private void Start()
    {
        // Test references to managers
        var gameManager = GameManager.Instance;
        var rogueLiteManager = RogueLiteManager.Instance;
        var turretManager = TurretManager.Instance;
        var enemySpawnManager = EnemySpawnManager.Instance;
        var buildManager = BuildManager.Instance;
    }
} 