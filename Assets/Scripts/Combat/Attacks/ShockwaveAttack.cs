using UnityEngine;
using Managers;

namespace Combat.Attacks
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

        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            // Set default elemental damage for shockwave attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            Debug.Log($"[{attackOwner.gameObject.name}] ShockwaveAttack initialized | MaxRadius: {maxShockwaveRadius} | Damage: {damage}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Shockwave attack called with no target!");
                return;
            }
            
            // Create the shockwave effect
            CreateShockwave();
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Shockwave attack executed | Radius: {maxShockwaveRadius} | Damage: {damage}");
        }

        /// <summary>
        /// Creates the expanding shockwave
        /// </summary>
        private void CreateShockwave()
        {
            Vector3 shockwaveCenter = OwnerTransform.position;
            
            // Play attack effect if provided
            PlayAttackEffect(shockwaveCenter, Vector3.up);
            
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
                        if (damageable != null && DamageUtils.IsValidTarget(DealerAllegiance, damageable))
                        {
                            DamageUtils.DealDamageToTarget(damageable, damage, poiseDamage, OwnerTransform, attackElement);
                            hitTargets.Add(collider);
                        }
                    }
                }
                
                yield return null;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(drawTransform.position, maxRange);
            
            // Draw shockwave radius
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
            Gizmos.DrawWireSphere(drawTransform.position, maxShockwaveRadius);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * drawTransform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * drawTransform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(drawTransform.position, rightDir * maxRange);
            Gizmos.DrawRay(drawTransform.position, leftDir * maxRange);
        }
    }
}
