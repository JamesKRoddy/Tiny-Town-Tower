using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Helper component that applies intensity scaling to particle effects via transform scale
    /// Automatically ensures particle systems use Hierarchy scaling mode for proper scaling behavior
    /// Attached automatically to pooled particle effects
    /// </summary>
    public class ParticleEffectIntensity : MonoBehaviour
    {
        private bool isInitialized = false;
        
        /// <summary>
        /// Initialize - ensures all particle systems use Hierarchy scaling mode
        /// This makes particles scale properly with transform.localScale
        /// Only runs once per pooled object
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            
            // Set all particle systems to Hierarchy scaling mode
            // This makes them respect the GameObject's transform scale
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in systems)
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Apply intensity scaling via transform scale
        /// Works perfectly with Size over Lifetime curves
        /// </summary>
        /// <param name="intensity">Intensity value (0.1-2.0)</param>
        public void ApplyIntensity(float intensity)
        {
            if (!isInitialized) Initialize();
            
            float clampedIntensity = Mathf.Clamp(intensity, 0.1f, 2.0f);
            transform.localScale = Vector3.one * clampedIntensity;
        }
        
        /// <summary>
        /// Reset transform scale to original
        /// Called when returning to pool
        /// </summary>
        public void ResetToOriginal()
        {
            transform.localScale = Vector3.one;
        }
    }
}
