using UnityEngine;

public class TurretManager : GameModeManager<TurretEnemyWaveConfig>
{
    [Header("Turret Grid")]
    [SerializeField] private Vector2 xBounds = new Vector2(-25f, 25f);
    [SerializeField] private Vector2 zBounds = new Vector2(-25f, 25f);
    [SerializeField] private bool showGridBounds;

    public Vector2 GetXBounds() => xBounds;
    public Vector2 GetZBounds() => zBounds;

    // Singleton instance
    private static TurretManager _instance;

    // Singleton property to get the instance
    public static TurretManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<TurretManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("TurretManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            _instance = this; // Set the instance
        }
    }

    protected override void EnemySetupStateChanged(EnemySetupState newState)
    {
        switch (newState)
        {
            case EnemySetupState.NONE:
                break;
            case EnemySetupState.WAVE_START:
                EnemySpawnManager.Instance.ResetWaveCount();
                break;
            case EnemySetupState.PRE_ENEMY_SPAWNING:
                SetEnemySetupState(EnemySetupState.ENEMIES_SPAWNED);
                EnemySpawnManager.Instance.StartSpawningEnemies(GetTurretWaveConfig(GetCurrentWaveDifficulty()));
                break;
            case EnemySetupState.ENEMIES_SPAWNED:
                break;
            case EnemySetupState.ALL_WAVES_CLEARED:
                break;
            default:
                break;
        }
    }

    public override int GetCurrentWaveDifficulty()
    {
        Debug.LogError("TODO have to figure out how this is calculated");
        return 1;
    }

    public EnemyWaveConfig GetTurretWaveConfig(int difficulty)
    {
        return GetWaveConfig(difficulty);
    }

    private void OnDrawGizmos()
    {
        if (showGridBounds)
        {
            Gizmos.color = Color.green;
            Vector3 bottomLeft = new Vector3(xBounds.x, 0, zBounds.x);
            Vector3 bottomRight = new Vector3(xBounds.y, 0, zBounds.x);
            Vector3 topLeft = new Vector3(xBounds.x, 0, zBounds.y);
            Vector3 topRight = new Vector3(xBounds.y, 0, zBounds.y);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
