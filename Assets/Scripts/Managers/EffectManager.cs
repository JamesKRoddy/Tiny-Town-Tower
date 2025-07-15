using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Managers
{  
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
                PlayEffect(position, normal, Quaternion.LookRotation(normal), null, effects.bloodEffects[Random.Range(0, effects.bloodEffects.Length)]);
            }

            if (effects.impactEffects != null && effects.impactEffects.Length > 0)
            {
                PlayEffect(position, normal, Quaternion.LookRotation(normal), null, effects.impactEffects[Random.Range(0, effects.impactEffects.Length)]);
            }
        }

        public void PlayDeathEffect(Vector3 position, Vector3 normal, IDamageable damageable)
        {
            if (damageable == null) return;
            var effects = GetCharacterEffects(damageable.CharacterType);
            if (effects == null || effects.deathEffects == null || effects.deathEffects.Length == 0)
            {
                Debug.LogWarning($"No death effects found for character type: {damageable.CharacterType}");
                return;
            }

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, effects.deathEffects[Random.Range(0, effects.deathEffects.Length)]);
        }

        public void PlayFootstepEffect(Vector3 position, Vector3 normal, CharacterType characterType)
        {
            var effects = GetCharacterEffects(characterType);
            if (effects == null || effects.footstepEffects == null || effects.footstepEffects.Length == 0) return;

            PlayEffect(position, normal, Quaternion.LookRotation(normal), null, effects.footstepEffects[Random.Range(0, effects.footstepEffects.Length)]);
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

        public GameObject PlayEffect(Vector3 position, Vector3 normal, Quaternion rotation, Transform parent, EffectDefinition effect, float duration = 0f)
        {
            if (effect == null)
            {
                Debug.LogWarning("[EffectManager] Attempted to play null effect");
                return null;
            }

            // If the effect isn't in our pools yet, initialize it
            if (!effectPools.ContainsKey(effect))
            {
                InitializeEffectPool(new[] { effect });
            }

            GameObject vfx = GetPooledObject(effect);
            if (vfx == null)
            {
                // If we couldn't get a pooled object, create a new one
                if (effect.prefabs != null && effect.prefabs.Length > 0)
                {
                    GameObject prefab = effect.playMode == EffectDefinition.PlayMode.Random
                        ? effect.prefabs[Random.Range(0, effect.prefabs.Length)]
                        : effect.prefabs[0]; // For All mode, we'll create additional instances below

                    if (prefab != null)
                    {
                        vfx = Instantiate(prefab, parent ?? transform);
                        activeEffects[effect].Add(vfx);
                    }
                }
            }

            if (vfx == null)
            {
                Debug.LogError($"[EffectManager] Failed to create effect instance for: {effect.name}");
                return null;
            }

            // Set parent first to ensure proper local space calculations
            vfx.transform.SetParent(parent ?? transform, false);
            
            // Set position and rotation in world space
            vfx.transform.position = position;
            vfx.transform.rotation = rotation;

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
                if (audioSource == null)
                {
                    audioSource = vfx.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.loop = false;
                }

                AudioClip[] soundsToPlay = effect.playMode == EffectDefinition.PlayMode.Random
                    ? new[] { effect.sounds[Random.Range(0, effect.sounds.Length)] }
                    : effect.sounds;

                foreach (var sound in soundsToPlay)
                {
                    audioSource.clip = sound;
                    audioSource.pitch = Random.Range(effect.minPitch, effect.maxPitch);
                    audioSource.volume = effect.volume;
                    audioSource.spatialBlend = effect.spatialBlend;
                    audioSource.Play();
                    audioDuration = Mathf.Max(audioDuration, sound.length);
                }
            }

            // If in All mode, create additional instances for remaining prefabs
            if (effect.playMode == EffectDefinition.PlayMode.All && effect.prefabs != null && effect.prefabs.Length > 1)
            {
                for (int i = 1; i < effect.prefabs.Length; i++)
                {
                    GameObject additionalVfx = Instantiate(effect.prefabs[i], parent ?? transform);
                    additionalVfx.transform.position = position;
                    additionalVfx.transform.rotation = rotation;
                    activeEffects[effect].Add(additionalVfx);

                    var additionalPs = additionalVfx.GetComponent<ParticleSystem>();
                    if (additionalPs != null)
                    {
                        additionalPs.Play();
                        particleDuration = Mathf.Max(particleDuration, additionalPs.main.duration);
                    }
                }
            }

            if (duration <= 0)
                duration = effect.duration > 0 ? effect.duration : Mathf.Max(particleDuration, audioDuration);

            StartCoroutine(ReturnToPoolAfterDuration(vfx, effect, duration));

            return vfx;
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
                // Reset parent back to EffectManager before disabling
                obj.transform.SetParent(transform, false);
                obj.SetActive(false);
                activeEffects[effect].Remove(obj);
                if (effectPools.ContainsKey(effect))
                {
                    effectPools[effect].Enqueue(obj);
                }
            }
        }
    }
    
    /// <summary>
    /// Helper class for playing effects with delays and automatic positioning.
    /// Used primarily by boss attacks to manage effect timing and positioning.
    /// </summary>
    public class EffectPlayer
    {
        private readonly MonoBehaviour owner;
        private readonly EffectDefinition effect;
        private readonly float delay;
        private Coroutine activeCoroutine;

        /// <summary>
        /// Creates a new EffectPlayer instance.
        /// </summary>
        /// <param name="owner">The MonoBehaviour that owns this effect player (used for coroutines)</param>
        /// <param name="effect">The effect definition to play</param>
        /// <param name="delay">Delay in seconds before playing the effect</param>
        public EffectPlayer(MonoBehaviour owner, EffectDefinition effect, float delay)
        {
            this.owner = owner;
            this.effect = effect;
            this.delay = delay;
        }

        /// <summary>
        /// Plays the effect with optional position, normal, rotation and parent.
        /// If not specified, uses the owner's position and forward direction.
        /// </summary>
        public void Play(Vector3? position = null, Vector3? normal = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (effect == null) return;
            
            Vector3 effectPosition = position ?? owner.transform.position;
            Vector3 effectNormal = normal ?? owner.transform.forward;
            Quaternion effectRotation = rotation ?? Quaternion.LookRotation(effectNormal);
            
            activeCoroutine = owner.StartCoroutine(PlayWithDelay(effectPosition, effectNormal, effectRotation, parent));
        }

        /// <summary>
        /// Stops any currently playing delayed effect.
        /// </summary>
        public void Stop()
        {
            if (activeCoroutine != null)
            {
                owner.StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }

        private IEnumerator PlayWithDelay(Vector3 position, Vector3 normal, Quaternion rotation, Transform parent)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            EffectManager.Instance.PlayEffect(position, normal, rotation, parent, effect);
        }
    }
} 