using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashVFX : MonoBehaviour
{
    [System.Serializable]
    public class ElementVfxPrefab
    {
        public WeaponElement element;
        public ParticleSystem particleSystemPrefab;
    }

    [SerializeField] private List<ElementVfxPrefab> dashElementVfxPrefabs;
    [SerializeField] private Dictionary<WeaponElement, Queue<ParticleSystem>> dashVfxPool;

    private void Start()
    {
        dashVfxPool = new Dictionary<WeaponElement, Queue<ParticleSystem>>();

        // Initialize the pool for each WeaponElement type
        foreach (var elementPrefab in dashElementVfxPrefabs)
        {
            WeaponElement element = elementPrefab.element;
            var particleSystemPrefab = elementPrefab.particleSystemPrefab;

            // Initialize the pool for each element
            dashVfxPool[element] = new Queue<ParticleSystem>();

            // Instantiate a number of particle systems for each element (e.g., 5 per element)
            for (int i = 0; i < 5; i++)
            {
                var ps = Instantiate(particleSystemPrefab, transform);
                ps.gameObject.SetActive(false); // Pooling requires inactive objects initially
                dashVfxPool[element].Enqueue(ps);
            }
        }
    }

    internal void Play(WeaponElement element, Transform spawnTransform)
    {
        // Get a ParticleSystem from the pool for the specific element
        if (dashVfxPool[element].Count > 0)
        {
            var ps = dashVfxPool[element].Dequeue();
            ps.gameObject.SetActive(true); // Activate the particle system

            // Set the position and rotation to match the spawnTransform
            ps.transform.position = spawnTransform.position;
            ps.transform.rotation = spawnTransform.rotation;

            ps.Play();

            // Start the coroutine to stop the particle system after 3 seconds
            StartCoroutine(StopAfterDelay(ps, 3f, element));
        }
        else
        {
            Debug.LogWarning($"No available particle systems in pool for {element}");
        }
    }

    private IEnumerator StopAfterDelay(ParticleSystem ps, float delay, WeaponElement element)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Stop the particle system after the delay
        if (ps.isPlaying)
        {
            ps.Stop();
            ps.gameObject.SetActive(false); // Deactivate it when not in use
            dashVfxPool[element].Enqueue(ps); // Return it to the pool
        }
    }

    internal void Stop(WeaponElement element)
    {
        foreach (var ps in dashVfxPool[element])
        {
            if (ps.isPlaying)
            {
                ps.Stop();
                ps.gameObject.SetActive(false); // Deactivate it when not in use
                dashVfxPool[element].Enqueue(ps); // Return it to the pool
            }
        }
    }
}