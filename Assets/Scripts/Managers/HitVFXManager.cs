using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    [System.Serializable]
    public class HitEffect
    {
        public GameObject prefab;
        public AudioClip[] hitSounds;
        public float minPitch = 0.9f;
        public float maxPitch = 1.1f;
        public float volume = 1f;
    }

    public class HitVFXManager : MonoBehaviour
    {
        public static HitVFXManager Instance { get; private set; }

        [Header("Blood Effects")]
        public HitEffect[] bloodEffects;
        public int poolSize = 20;

        private Dictionary<HitEffect, Queue<GameObject>> effectPools = new Dictionary<HitEffect, Queue<GameObject>>();
        private Dictionary<HitEffect, List<GameObject>> activeEffects = new Dictionary<HitEffect, List<GameObject>>();

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
            foreach (var effect in bloodEffects)
            {
                Queue<GameObject> pool = new Queue<GameObject>();
                List<GameObject> active = new List<GameObject>();

                for (int i = 0; i < poolSize; i++)
                {
                    GameObject obj = Instantiate(effect.prefab, transform);
                    obj.SetActive(false);
                    pool.Enqueue(obj);
                }

                effectPools[effect] = pool;
                activeEffects[effect] = active;
            }
        }

        public void PlayHitEffect(Vector3 position, Vector3 normal, Allegiance allegiance)
        {
            // Select appropriate effect based on allegiance
            HitEffect effect = bloodEffects[Random.Range(0, bloodEffects.Length)];
            
            // Get pooled object
            GameObject vfx = GetPooledObject(effect);
            if (vfx == null) return;

            // Position and rotate the effect
            vfx.transform.position = position;
            vfx.transform.rotation = Quaternion.LookRotation(normal);

            // Play particle system
            var particleSystem = vfx.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
            }

            // Play random sound
            if (effect.hitSounds.Length > 0)
            {
                AudioSource audioSource = vfx.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.clip = effect.hitSounds[Random.Range(0, effect.hitSounds.Length)];
                    audioSource.pitch = Random.Range(effect.minPitch, effect.maxPitch);
                    audioSource.volume = effect.volume;
                    audioSource.Play();
                }
            }

            // Return to pool after duration
            StartCoroutine(ReturnToPoolAfterDuration(vfx, effect, particleSystem.main.duration));
        }

        private GameObject GetPooledObject(HitEffect effect)
        {
            Queue<GameObject> pool = effectPools[effect];
            List<GameObject> active = activeEffects[effect];

            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                active.Add(obj);
                return obj;
            }
            else if (active.Count > 0)
            {
                // Reuse the oldest active effect
                GameObject obj = active[0];
                active.RemoveAt(0);
                active.Add(obj);
                return obj;
            }

            return null;
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDuration(GameObject obj, HitEffect effect, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            obj.SetActive(false);
            activeEffects[effect].Remove(obj);
            effectPools[effect].Enqueue(obj);
        }
    }
} 