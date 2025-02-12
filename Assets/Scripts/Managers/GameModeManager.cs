using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameModeManager<TWaveConfig> : MonoBehaviour where TWaveConfig : EnemyWaveConfig
{
    [SerializeField] protected List<TWaveConfig> waveConfigs;

    protected EnemySetupState enemySetupState;
    public Action<EnemySetupState> OnRoomSetupStateChanged;

    protected virtual void Start()
    {
        OnRoomSetupStateChanged += EnemySetupStateChanged;
        enemySetupState = EnemySetupState.ALL_WAVES_CLEARED;
    }

    protected virtual void OnDestroy()
    {
        OnRoomSetupStateChanged -= EnemySetupStateChanged;
    }

    protected abstract void EnemySetupStateChanged(EnemySetupState newState);
    public abstract int GetCurrentWaveDifficulty();

    public void SetEnemySetupState(EnemySetupState newState)
    {
        if (enemySetupState != newState)
        {
            enemySetupState = newState;
            OnRoomSetupStateChanged?.Invoke(enemySetupState);
        }
    }

    public EnemySetupState GetEnemySetupState()
    {
        return enemySetupState;
    }

    protected EnemyWaveConfig GetWaveConfig(int difficulty)
    {
        foreach (var config in waveConfigs)
        {
            if (config is EnemyWaveConfig enemyWaveConfig && enemyWaveConfig.enemyWaveDifficulty <= difficulty)
            {
                return enemyWaveConfig;
            }
        }
        Debug.LogWarning($"No matching waveConfig found for difficulty {difficulty}. Returning null.");
        return null;
    }
}
