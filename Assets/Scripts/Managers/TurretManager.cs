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

    [Header("Turret Level Vars")]
    public TurretBaseTarget baseTarget;

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

    protected override void Start()
    {
        base.Start();
        //SetEnemySetupState(EnemySetupState.PRE_ENEMY_SPAWNING);
    }

    public void StartWave() //************************************************** TODO!!!!!!! HAVE TO CALL THIS
    {
        SetEnemySetupState(EnemySetupState.WAVE_START);
    }

    protected override void EnemySetupStateChanged(EnemySetupState newState)
    {
        Debug.Log($"Enemy Setup Changed: <color=red> {newState} </color>");
        switch (newState)
        {
            case EnemySetupState.NONE:
                break;
            case EnemySetupState.WAVE_START:
                EnemySpawnManager.Instance.ResetWaveCount();
                SetEnemySetupState(EnemySetupState.PRE_ENEMY_SPAWNING);
                break;
            case EnemySetupState.PRE_ENEMY_SPAWNING:
                EnemySpawnManager.Instance.StartSpawningEnemies(GetTurretWaveConfig(GetCurrentWaveDifficulty()));
                SetEnemySetupState(EnemySetupState.ENEMIES_SPAWNED);
                break;
            case EnemySetupState.ENEMIES_SPAWNED:    
                //This is the point in which the enemies are moving toward the end point so I dont think we need to do anything here
                break;
            case EnemySetupState.ALL_WAVES_CLEARED:
                //TODO turret wave section complete, i think here we check to see how many waves the player is supposed to complete on this night so either move onto the next wave or finish the night
                break;
            default:
                break;
        }
    }

    public override int GetCurrentWaveDifficulty()
    {
        Debug.LogError("TODO have to figure out how this is calculated");
        return 0;
    }

    private EnemyWaveConfig GetTurretWaveConfig(int difficulty)
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
