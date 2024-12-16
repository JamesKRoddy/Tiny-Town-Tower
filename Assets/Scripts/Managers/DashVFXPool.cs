using System.Collections.Generic;
using UnityEngine;

public class DashVFXPool : MonoBehaviour
{
    public DashVFX dashVfx; // Reference to your DashVFX prefab
    public int poolSize = 10; // Number of particle systems to pool per WeaponElement
    private Queue<ParticleSystem>[] particleSystemPools; // Pool for each WeaponElement

    private void Start()
    {
        // Initialize the particle system pools for each WeaponElement
        int enumLength = System.Enum.GetValues(typeof(WeaponElement)).Length;
        particleSystemPools = new Queue<ParticleSystem>[enumLength];

        // Create a pool for each WeaponElement (excluding NONE)
        for (int i = 0; i < enumLength; i++)
        {
            particleSystemPools[i] = new Queue<ParticleSystem>();

            // Initialize the pool with ParticleSystems from the DashVFX prefab
            for (int j = 0; j < poolSize; j++)
            {
                // Instantiate a new DashVFX prefab to get its ParticleSystems
                DashVFX vfxInstance = Instantiate(dashVfx, transform); // Set as child of the pool
                ParticleSystem ps = vfxInstance.transform.GetChild(i).GetComponent<ParticleSystem>(); // Get the i-th ParticleSystem

                // Deactivate the particle system initially
                ps.gameObject.SetActive(false);

                // Add it to the corresponding pool
                particleSystemPools[i].Enqueue(ps);
            }
        }
    }

    public ParticleSystem GetPooledParticleSystem(WeaponElement element)
    {
        // Ensure valid element (if it's NONE, just return null)
        if (element == WeaponElement.NONE)
        {
            return null;
        }

        // Get the corresponding particle system pool
        int index = (int)element;
        if (particleSystemPools[index].Count > 0)
        {
            ParticleSystem ps = particleSystemPools[index].Dequeue();
            ps.gameObject.SetActive(true); // Activate the particle system when it's in use
            return ps;
        }
        else
        {
            Debug.LogWarning("No particle system available for element: " + element);
            return null;
        }
    }

    public void ReturnToPool(ParticleSystem ps, WeaponElement element)
    {
        // Ensure valid element (if it's NONE, just return)
        if (element == WeaponElement.NONE)
        {
            return;
        }

        int index = (int)element;
        ps.gameObject.SetActive(false); // Deactivate when returned
        particleSystemPools[index].Enqueue(ps); // Return the particle system to the pool
    }
}
