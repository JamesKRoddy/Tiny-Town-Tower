using UnityEngine;

namespace Managers
{
    public class TurretManager : GameModeManager<TurretEnemyWaveConfig>
    {
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

        public void StartWave()
        {   
            SetEnemySetupState(EnemySetupState.WAVE_START);
            baseTarget.gameObject.SetActive(true);
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
                    baseTarget.gameObject.SetActive(false);
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
    }
}
