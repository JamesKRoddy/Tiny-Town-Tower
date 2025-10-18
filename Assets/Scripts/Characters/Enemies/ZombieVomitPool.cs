using UnityEngine;
using System.Collections;

/// <summary>
/// Zombie vomit pool - a temporary damage area with scaling animation
/// Grows from small to full size over time
/// </summary>
public class ZombieVomitPool : TemporaryDamageArea
    {
        [Header("Visual Settings")]
        [SerializeField] private float scaleAnimationDuration = 0.5f;
        [SerializeField] private Vector3 targetScale = new Vector3(2f, 0.3f, 2f);
        
        private Coroutine scaleCoroutine;

        /// <summary>
        /// Setup with just scale parameters (uses base damage settings)
        /// </summary>
        public void Setup(float scaleDuration, Vector3 scale)
        {
            scaleAnimationDuration = scaleDuration;
            targetScale = scale;
            
            // Set initial scale
            transform.localScale = new Vector3(0f, 0.3f, 0f);
            
            // Start scaling animation
            scaleCoroutine = StartCoroutine(ScaleAnimation());
        }
        
        /// <summary>
        /// Full setup with damage, duration, and scaling
        /// </summary>
        public void Setup(float damage, float poiseDamage, float duration, float scaleDuration, Vector3 scale, 
            AttackElement element = AttackElement.NONE, int elementalBonus = 0, Transform source = null, 
            Allegiance allegiance = Allegiance.HOSTILE)
        {
            // Call base setup for damage and duration (vomit pools are HOSTILE by default - enemy-created)
            base.Setup(damage, poiseDamage, duration, element, elementalBonus, source, 1f, allegiance);
            
            scaleAnimationDuration = scaleDuration;
            targetScale = scale;
            
            Debug.Log($"[ZombieVomitPool] Setup called - targetScale set to: {targetScale}, scaleDuration: {scaleDuration}, allegiance: {allegiance}");
            
            // Set initial scale
            transform.localScale = new Vector3(0f, 0.3f, 0f);
            
            Debug.Log($"[ZombieVomitPool] Initial scale set to: {transform.localScale}, will lerp to: {targetScale}");
            
            // Start scaling animation
            scaleCoroutine = StartCoroutine(ScaleAnimation());
        }

        private IEnumerator ScaleAnimation()
        {
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;
            
            Debug.Log($"[ZombieVomitPool] ScaleAnimation started - from {startScale} to {targetScale} over {scaleAnimationDuration}s");

            while (elapsedTime < scaleAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scaleAnimationDuration;
                
                // Use smooth step for more natural scaling
                t = t * t * (3f - 2f * t);
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            // Ensure final scale is exact
            transform.localScale = targetScale;
            
            Debug.Log($"[ZombieVomitPool] ScaleAnimation completed - final scale: {transform.localScale}");
        }
        
        protected override void OnBeforeDestroy()
        {
            // Stop scaling animation if still running
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
        }
    } 