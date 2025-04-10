using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Managers
{
    [CreateAssetMenu(fileName = "NewEffect", menuName = "Scriptable Objects/Roguelite/Enemies/Effects/Effect Definition")]
    public class EffectDefinition : ScriptableObject
    {
        [Tooltip("Array of possible particle system prefabs. One will be randomly selected")]
        public GameObject[] prefabs;
        
        [Tooltip("Array of possible sound effects to play. One will be randomly selected")]
        public AudioClip[] sounds;
        
        [Tooltip("Minimum pitch variation for the sound effect (0.9 = 10% lower)")]
        public float minPitch = 0.9f;
        
        [Tooltip("Maximum pitch variation for the sound effect (1.1 = 10% higher)")]
        public float maxPitch = 1.1f;
        
        [Tooltip("Volume level for the sound effect (0-1)")]
        public float volume = 1f;

        [Tooltip("Duration of the effect in seconds. If 0, uses the particle system duration")]
        public float duration = 0f;
    }

    [CreateAssetMenu(fileName = "NewCharacterEffects", menuName = "Scriptable Objects/Roguelite/Enemies/Effects/Character Effects")]
    public class CharacterEffects : ScriptableObject
    {
        [Tooltip("The type of character these effects are for")]
        public CharacterType characterType;

        [Header("Combat Effects")]
        [Tooltip("Blood/gore effects for organic characters, or fluid/particle effects for machines")]
        public EffectDefinition[] bloodEffects = new EffectDefinition[0];

        [Tooltip("Impact effects showing the force of the hit (dust, sparks, debris)")]
        public EffectDefinition[] impactEffects = new EffectDefinition[0];

        [Tooltip("Special effects played when the character dies (explosions, disintegration, etc.)")]
        public EffectDefinition[] deathEffects = new EffectDefinition[0];

        [Header("Movement Effects")]
        [Tooltip("Effects played when the character takes a step")]
        public EffectDefinition[] footstepEffects = new EffectDefinition[0];

        [Header("Idle Effects")]
        [Tooltip("Random effects played while the character is idle")]
        public EffectDefinition[] idleEffects = new EffectDefinition[0];

        [Tooltip("Minimum time between idle effects")]
        public float minIdleInterval = 5f;

        [Tooltip("Maximum time between idle effects")]
        public float maxIdleInterval = 15f;

        private void OnEnable()
        {
            // Initialize arrays if they're null
            if (bloodEffects == null) bloodEffects = new EffectDefinition[0];
            if (impactEffects == null) impactEffects = new EffectDefinition[0];
            if (deathEffects == null) deathEffects = new EffectDefinition[0];
            if (footstepEffects == null) footstepEffects = new EffectDefinition[0];
            if (idleEffects == null) idleEffects = new EffectDefinition[0];
        }
    }

    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [Header("Character Effects")]
        [Tooltip("Array of effect sets for different character types")]
        public CharacterEffects[] characterEffects;

        [Tooltip("Number of instances of each effect to keep in the object pool")]
        public int poolSize = 20;

        private Dictionary<EffectDefinition, Queue<GameObject>> effectPools = new Dictionary<EffectDefinition, Queue<GameObject>>();
        private Dictionary<EffectDefinition, List<GameObject>> activeEffects = new Dictionary<EffectDefinition, List<GameObject>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePools()
        {
            if (characterEffects == null) return;

            foreach (var charEffect in characterEffects)
            {
                if (charEffect == null) continue;

                InitializeEffectPool(charEffect.bloodEffects);
                InitializeEffectPool(charEffect.impactEffects);
                InitializeEffectPool(charEffect.deathEffects);
                InitializeEffectPool(charEffect.footstepEffects);
                InitializeEffectPool(charEffect.idleEffects);
            }
        }

        private void InitializeEffectPool(EffectDefinition[] effects)
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                if (effect != null && !effectPools.ContainsKey(effect) && effect.prefabs != null && effect.prefabs.Length > 0)
                {
                    Queue<GameObject> pool = new Queue<GameObject>();
                    List<GameObject> active = new List<GameObject>();

                    for (int i = 0; i < poolSize; i++)
                    {
                        GameObject prefab = effect.prefabs[Random.Range(0, effect.prefabs.Length)];
                        if (prefab != null)
                        {
                            GameObject obj = Instantiate(prefab, transform);
                            obj.SetActive(false);
                            pool.Enqueue(obj);
                        }
                    }

                    effectPools[effect] = pool;
                    activeEffects[effect] = active;
                }
            }
        }

        public void PlayHitEffect(Vector3 position, Vector3 normal, IDamageable damageable)
        {
            if (damageable == null) return;
            var effects = GetCharacterEffects(damageable.CharacterType);
            if (effects == null) return;

            if (effects.bloodEffects != null && effects.bloodEffects.Length > 0)
            {
                PlayEffect(position, normal, effects.bloodEffects[Random.Range(0, effects.bloodEffects.Length)]);
            }

            if (effects.impactEffects != null && effects.impactEffects.Length > 0)
            {
                PlayEffect(position, normal, effects.impactEffects[Random.Range(0, effects.impactEffects.Length)]);
            }
        }

        public void PlayDeathEffect(Vector3 position, Vector3 normal, IDamageable damageable)
        {
            if (damageable == null) return;
            var effects = GetCharacterEffects(damageable.CharacterType);
            if (effects == null || effects.deathEffects == null || effects.deathEffects.Length == 0) return;

            PlayEffect(position, normal, effects.deathEffects[Random.Range(0, effects.deathEffects.Length)]);
        }

        public void PlayFootstepEffect(Vector3 position, Vector3 normal, CharacterType characterType)
        {
            var effects = GetCharacterEffects(characterType);
            if (effects == null || effects.footstepEffects == null || effects.footstepEffects.Length == 0) return;

            PlayEffect(position, normal, effects.footstepEffects[Random.Range(0, effects.footstepEffects.Length)]);
        }

        private CharacterEffects GetCharacterEffects(CharacterType characterType)
        {
            if (characterEffects == null) return null;

            foreach (var effect in characterEffects)
            {
                if (effect != null && effect.characterType == characterType)
                {
                    return effect;
                }
            }
            Debug.LogWarning($"No effects found for character type: {characterType}");
            return null;
        }

        private void PlayEffect(Vector3 position, Vector3 normal, EffectDefinition effect)
        {
            if (effect == null) return;

            GameObject vfx = GetPooledObject(effect);
            if (vfx == null) return;

            vfx.transform.position = position;
            vfx.transform.rotation = Quaternion.LookRotation(normal);

            float particleDuration = 0f;
            float audioDuration = 0f;

            var particleSystem = vfx.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
                particleDuration = particleSystem.main.duration;
            }

            if (effect.sounds != null && effect.sounds.Length > 0)
            {
                AudioSource audioSource = vfx.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.clip = effect.sounds[Random.Range(0, effect.sounds.Length)];
                    audioSource.pitch = Random.Range(effect.minPitch, effect.maxPitch);
                    audioSource.volume = effect.volume;
                    audioSource.Play();
                    audioDuration = audioSource.clip.length;
                }
            }

            float duration = effect.duration > 0 ? effect.duration : Mathf.Max(particleDuration, audioDuration);
            StartCoroutine(ReturnToPoolAfterDuration(vfx, effect, duration));
        }

        private GameObject GetPooledObject(EffectDefinition effect)
        {
            if (effect == null || !effectPools.ContainsKey(effect)) return null;

            Queue<GameObject> pool = effectPools[effect];
            List<GameObject> active = activeEffects[effect];

            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                obj.SetActive(true);
                active.Add(obj);
            }
            else if (active.Count > 0)
            {
                obj = active[0];
                active.RemoveAt(0);
                active.Add(obj);
            }
            else
            {
                return null;
            }

            // Ensure AudioSource component exists if the effect has sounds
            if (effect.sounds != null && effect.sounds.Length > 0)
            {
                AudioSource audioSource = obj.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = obj.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.loop = false;
                }
            }

            return obj;
        }

        private IEnumerator ReturnToPoolAfterDuration(GameObject obj, EffectDefinition effect, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (obj != null && effect != null && activeEffects.ContainsKey(effect))
            {
                obj.SetActive(false);
                activeEffects[effect].Remove(obj);
                if (effectPools.ContainsKey(effect))
                {
                    effectPools[effect].Enqueue(obj);
                }
            }
        }
    }
} 