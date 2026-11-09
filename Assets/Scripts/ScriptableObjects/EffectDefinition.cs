using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "New Effect Definition", menuName = "Scriptable Objects/Effects/Effect Definition")]
public class EffectDefinition : ScriptableObject
{
    public enum PlayMode
    {
        Random,
        All
    }

    [System.Serializable]
    public class EffectPrefabEntry
    {
        [Tooltip("Particle system prefab to spawn when this entry is selected")]
        public GameObject prefab;

        [Tooltip("Delay in seconds before this prefab is played")]
        [Min(0f)]
        public float delay = 0f;
    }

    [System.Serializable]
    public class EffectSoundEntry
    {
        [Tooltip("Audio clip to play when this entry is selected")]
        public AudioClip clip;

        [Tooltip("Delay in seconds before this audio clip is played")]
        [Min(0f)]
        public float delay = 0f;
    }

    [Tooltip("Array of possible particle system prefabs. One will be randomly selected")]
    public EffectPrefabEntry[] prefabs;

    [Tooltip("Array of possible sound effects to play. One will be randomly selected")]
    public EffectSoundEntry[] sounds;

#if UNITY_EDITOR
    [SerializeField, HideInInspector, FormerlySerializedAs("prefabs")]
    private GameObject[] legacyPrefabs;

    [SerializeField, HideInInspector, FormerlySerializedAs("sounds")]
    private AudioClip[] legacySounds;

    public bool MigrateLegacyData()
    {
        bool migrated = false;

        if ((prefabs == null || prefabs.Length == 0) && legacyPrefabs != null && legacyPrefabs.Length > 0)
        {
            prefabs = new EffectPrefabEntry[legacyPrefabs.Length];
            for (int i = 0; i < legacyPrefabs.Length; i++)
            {
                prefabs[i] = new EffectPrefabEntry
                {
                    prefab = legacyPrefabs[i],
                    delay = 0f
                };
            }
            legacyPrefabs = null;
            migrated = true;
        }

        if ((sounds == null || sounds.Length == 0) && legacySounds != null && legacySounds.Length > 0)
        {
            sounds = new EffectSoundEntry[legacySounds.Length];
            for (int i = 0; i < legacySounds.Length; i++)
            {
                sounds[i] = new EffectSoundEntry
                {
                    clip = legacySounds[i],
                    delay = 0f
                };
            }
            legacySounds = null;
            migrated = true;
        }

        return migrated;
    }

    private void OnValidate()
    {
        if (MigrateLegacyData())
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
    
    [Tooltip("Minimum pitch variation for the sound effect (0.9 = 10% lower)")]
    public float minPitch = 0.9f;
    
    [Tooltip("Maximum pitch variation for the sound effect (1.1 = 10% higher)")]
    public float maxPitch = 1.1f;
    
    [Tooltip("Volume level for the sound effect (0-1)")]
    public float volume = 1f;

    [Tooltip("Duration of the effect in seconds. If 0, uses the particle system duration")]
    public float duration = 0f;
    
    [Tooltip("Should this effect loop indefinitely? Useful for persistent status effects like sleeping, working, etc.")]
    public bool looping = false;
    
    [Tooltip("Loop interval in seconds. How long to wait between loop cycles (only used if looping is true)")]
    public float loopInterval = 5f;

    [Tooltip("Controls how much the sound is affected by 3D positioning (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;

    [Tooltip("Whether to play a random effect or all effects")]
    public PlayMode playMode = PlayMode.Random;
}
