using UnityEngine;
using System.Collections;

namespace Enemies
{
    public class ZombieVomitPool : DamageArea
    {
        private Coroutine scaleCoroutine;

        public void Setup(float damage, float poiseDamage, float scaleDuration, Vector3 targetScale)
        {
            SetDamage(damage, poiseDamage);
            
            // Set initial scale
            transform.localScale = new Vector3(0f, 0.3f, 0f);
            
            // Start scaling animation
            scaleCoroutine = StartCoroutine(ScaleAnimation(scaleDuration, targetScale));
        }

        private IEnumerator ScaleAnimation(float scaleDuration, Vector3 targetScale)
        {
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsedTime < scaleDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scaleDuration;
                
                // Use smooth step for more natural scaling
                t = t * t * (3f - 2f * t);
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            // Ensure final scale is exact
            transform.localScale = targetScale;
        }
    }
} 