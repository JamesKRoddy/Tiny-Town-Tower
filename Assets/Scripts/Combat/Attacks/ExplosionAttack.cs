using UnityEngine;
using Managers;

namespace Combat.Attacks
{
    /// <summary>
    /// Explosion attack that deals area damage and may kill the attacker.
    /// Good for suicide bombers, explosive barrels, grenades, etc.
    /// 
    /// ATTACK DESCRIPTION FIELD:
    /// Use the "Attack Description" field at the top to document your specific setup.
    /// Example: "Suicide bomber that charges at player and explodes at 2m range. 
    /// Deals 50 fire damage in 5m radius. Dies after explosion."
    /// 
    /// Configuration Tips:
    /// - Set explosionRadius to the desired damage area (e.g., 5m)
    /// - Set detonationDistanceFraction to control when explosion triggers:
    ///   * 0.3 (default) = detonate at 30% of explosion radius (aggressive suicide bomber)
    ///   * 0.5 = detonate at 50% of explosion radius (balanced)
    ///   * 0.8 = detonate at 80% of explosion radius (proximity mine style)
    /// - Works with both root motion and non-root motion movement systems
    /// - Automatically prevents strafing for drones (charges directly at target)
    /// 
    /// VISUAL SYSTEM:
    /// Use BOTH visual arrays for complete effect:
    /// 
    /// PERMANENT EQUIPMENT (attackEquipment):
    /// - Create dynamite stick models as children of the character
    /// - Add them to "Equipment GameObjects" array
    /// - Keep them enabled in the scene
    /// - Players see the dynamite and know this character explodes!
    /// 
    /// TEMPORARY EFFECTS (attackEffectObjects):
    /// - Create explosion trails, sparking fuse particles, glow effects
    /// - Add them to "Effect GameObjects" array
    /// - Start them disabled in the scene
    /// - They'll light up/activate when the character is about to explode!
    /// </summary>
    public class ExplosionAttack : AttackBase
    {
        [Header("Explosion Settings")]
        [Tooltip("Radius of the explosion")]
        public float explosionRadius = 5f;
        [Tooltip("How close to get before detonating (as a fraction of explosion radius). 0.3 = detonate at 30% of explosion radius")]
        [Range(0.1f, 0.8f)]
        public float detonationDistanceFraction = 0.3f;
        [Tooltip("Poise damage multiplier for explosion (multiplied by base damage)")]
        public float poiseDamageMultiplier = 1.2f;
        [Tooltip("Whether the attacker should die after exploding")]
        public bool dieAfterExplosion = true;
        
        
        [Header("Explosion Triggers")]
        [Tooltip("Whether the explosion should trigger when the attacker dies")]
        public bool explodeOnDeath = true;
        [Tooltip("Whether the explosion should trigger when the attacker takes damage")]
        public bool explodeOnDamage = false;
        [Tooltip("Minimum damage required to trigger explosion on damage")]
        public float minDamageToExplode = 10f;

        private bool hasExploded = false;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for explosion attacks
            if (attackType == 0)
            {
                attackType = 4; // Explosion attack type
            }
        }

        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            // Set default elemental damage for explosion attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.FIRE; // Explosions could be fire damage
            }
            
            // Calculate detonation range based on explosion radius
            float detonationRange = explosionRadius * detonationDistanceFraction;
            maxRange = detonationRange;
            
            // Validate attack effect
            if (attackEffect == null || !attackEffect.IsValid())
            {
                Debug.LogError("Attack effect (explosion) definition is not assigned or invalid on ExplosionAttack on " + attackOwner.gameObject.name);
            }
            
            Debug.Log($"[{attackOwner.gameObject.name}] ExplosionAttack initialized | DetonationRange: {detonationRange:F2} ({detonationDistanceFraction * 100}% of radius) | ExplosionRadius: {explosionRadius} | ExplosionDamage: {damage}");
        }

        public override bool CanAttack()
        {
            if (!base.CanAttack()) return false;
            if (hasExploded) return false;
            
            float distanceToTarget = Vector3.Distance(OwnerTransform.position, target.position);
            
            // Can attack when close enough to target
            return distanceToTarget <= maxRange;
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Explosion attack started | Target: {target.name} | Distance: {Vector3.Distance(OwnerTransform.position, target.position):F2}");
        }

        public override void OnAttack()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            Debug.Log($"[{OwnerTransform.gameObject.name}] Explosion triggered!");
            
            // Create instant damage area (explosion)
            int targetsDamaged = DamageUtils.CreateInstantDamageArea(
                OwnerTransform.position, 
                explosionRadius, 
                damage, 
                damage * poiseDamageMultiplier, 
                OwnerTransform, 
                attackElement, 
                attackEffect?.effectDefinition
            );
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Explosion executed | Damage: {damage} | Radius: {explosionRadius} | Targets: {targetsDamaged}");
        }

        public override void OnAttackEnd()
        {
            base.OnAttackEnd();
            
            // Attacker dies after exploding
            if (dieAfterExplosion && owner != null)
            {
                // Kill the attacker with environmental damage (no VFX needed for self-destruction)
                var ownerDamageable = owner as IDamageable;
                if (ownerDamageable != null)
                {
                    var damageInfo = DamageInfo.Environmental(ownerDamageable.Health, playHitVFX: false);
                    ownerDamageable.TakeDamage(damageInfo);
                }
            }
        }

        /// <summary>
        /// Check if the explosion should trigger when taking damage
        /// </summary>
        /// <param name="damageAmount">Amount of damage taken</param>
        /// <returns>True if should explode</returns>
        public bool ShouldExplodeOnDamage(float damageAmount)
        {
            return explodeOnDamage && damageAmount >= minDamageToExplode && !hasExploded;
        }

        /// <summary>
        /// Check if the explosion should trigger on death
        /// </summary>
        /// <returns>True if should explode</returns>
        public bool ShouldExplodeOnDeath()
        {
            return explodeOnDeath && !hasExploded;
        }

        /// <summary>
        /// Force the explosion to trigger (called from external sources)
        /// </summary>
        public void ForceExplode()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            
            // Create instant damage area (explosion)
            DamageUtils.CreateInstantDamageArea(
                OwnerTransform.position, 
                explosionRadius, 
                damage, 
                damage * poiseDamageMultiplier, 
                OwnerTransform, 
                attackElement, 
                attackEffect?.effectDefinition
            );
            
            // Kill the attacker if configured to do so
            if (dieAfterExplosion && owner != null)
            {
                var ownerDamageable = owner as IDamageable;
                if (ownerDamageable != null)
                {
                    var damageInfo = DamageInfo.Environmental(ownerDamageable.Health, playHitVFX: false);
                    ownerDamageable.TakeDamage(damageInfo);
                }
            }
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Explosion force triggered!");
        }

        public override bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            // Explosions don't need precise aiming - just need to get close
            return false;
        }

        /// <summary>
        /// Check if the explosion has already triggered
        /// </summary>
        /// <returns>True if already exploded</returns>
        public bool HasExploded()
        {
            return hasExploded;
        }

        protected override void OnDrawGizmosSelected()
        {
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Calculate detonation range for visualization
            float detonationRange = explosionRadius * detonationDistanceFraction;
            
            // Draw explosion radius (damage area) - red
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
            Gizmos.DrawWireSphere(drawTransform.position, explosionRadius);
            
            // Draw detonation range (trigger distance) - yellow
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(drawTransform.position, detonationRange);
            Gizmos.DrawWireSphere(drawTransform.position, detonationRange * 0.8f);
            
            // Draw explosion center
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
            Gizmos.DrawWireSphere(drawTransform.position + Vector3.up * 1.0f, 0.5f);
        }
    }
}
