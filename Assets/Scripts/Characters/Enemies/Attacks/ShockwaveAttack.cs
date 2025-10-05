using UnityEngine;
using Managers;

namespace Enemies.Attacks
{
    /// <summary>
    /// Shockwave attack that creates a circular wave of damage expanding outward.
    /// Good for boss ground slams, environmental explosions, etc.
    /// </summary>
    public class ShockwaveAttack : AttackBase
    {
        [Header("Shockwave Settings")]
        [Tooltip("Maximum radius of the shockwave")]
        public float maxShockwaveRadius = 10f;
        [Tooltip("Speed of the shockwave expansion")]
        public float shockwaveSpeed = 15f;
        [Tooltip("Width of the shockwave ring")]
        public float shockwaveWidth = 1f;
        [Tooltip("Damage dealt by the shockwave")]
        public float shockwaveDamage = 25f;
        [Tooltip("Poise damage dealt by the shockwave")]
        public float shockwavePoiseDamage = 15f;
        
        [Header("Visual Effects")]
        [Tooltip("Visual effect for the shockwave")]
        public EffectDefinition shockwaveEffect;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for shockwave attacks
            if (attackType == 0)
            {
                attackType = 6; // Shockwave attack type
            }
            
            // Set default range values for shockwave attacks
            if (minRange == 0 && maxRange == 5) // Only set defaults if they haven't been customized
            {
                minRange = 1f;   // Minimum range for shockwave attacks
                maxRange = maxShockwaveRadius;   // Maximum range for shockwave attacks
            }
            
            // Set default angle threshold for shockwave attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 15f; // Shockwave attacks need moderate aiming
            }
        }

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            // Set default elemental damage for shockwave attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            Debug.Log($"[{enemy.gameObject.name}] ShockwaveAttack initialized | MaxRadius: {maxShockwaveRadius} | Damage: {shockwaveDamage}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{enemy.gameObject.name}] Shockwave attack called with no target!");
                return;
            }
            
            // Create the shockwave effect
            CreateShockwave();
            
            Debug.Log($"[{enemy.gameObject.name}] Shockwave attack executed | Radius: {maxShockwaveRadius} | Damage: {shockwaveDamage}");
        }

        /// <summary>
        /// Creates the expanding shockwave
        /// </summary>
        private void CreateShockwave()
        {
            Vector3 shockwaveCenter = enemy.transform.position;
            
            // Play visual effect if provided
            if (shockwaveEffect != null)
            {
                EffectManager.Instance.PlayEffect(shockwaveCenter, Vector3.up, Quaternion.identity, null, shockwaveEffect);
            }
            
            // Create expanding shockwave using coroutine
            StartCoroutine(ExpandShockwave(shockwaveCenter));
        }

        /// <summary>
        /// Coroutine that handles the expanding shockwave
        /// </summary>
        private System.Collections.IEnumerator ExpandShockwave(Vector3 center)
        {
            float currentRadius = 0f;
            float expansionTime = maxShockwaveRadius / shockwaveSpeed;
            float elapsedTime = 0f;
            
            // Track which targets have been hit to avoid multiple hits
            var hitTargets = new System.Collections.Generic.HashSet<Collider>();
            
            while (currentRadius < maxShockwaveRadius)
            {
                elapsedTime += Time.deltaTime;
                currentRadius = elapsedTime * shockwaveSpeed;
                
                // Find colliders in the shockwave ring
                Collider[] colliders = Physics.OverlapSphere(center, currentRadius);
                
                foreach (var collider in colliders)
                {
                    // Skip if already hit
                    if (hitTargets.Contains(collider)) continue;
                    
                    // Check if within the shockwave ring
                    float distanceFromCenter = Vector3.Distance(center, collider.transform.position);
                    if (distanceFromCenter >= currentRadius - shockwaveWidth && distanceFromCenter <= currentRadius)
                    {
                        // Deal damage to the target
                        IDamageable damageable = collider.GetComponent<IDamageable>();
                        if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                        {
                            DamageUtils.DealDamageToTarget(damageable, shockwaveDamage, shockwavePoiseDamage, enemy.transform, attackElement);
                            hitTargets.Add(collider);
                        }
                    }
                }
                
                yield return null;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            if (enemy == null) return;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, maxRange);
            
            // Draw shockwave radius
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
            Gizmos.DrawWireSphere(enemy.transform.position, maxShockwaveRadius);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * enemy.transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * enemy.transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(enemy.transform.position, rightDir * maxRange);
            Gizmos.DrawRay(enemy.transform.position, leftDir * maxRange);
        }
    }
}
