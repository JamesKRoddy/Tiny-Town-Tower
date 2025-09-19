using UnityEngine;

namespace Enemies
{
    public class MeleeZombie : Zombie
    {
        #region Constants
        
        // Use the attack-ready threshold from base class for validation
        private const float MELEE_ATTACK_ANGLE_THRESHOLD = 45f; // Now using stricter threshold
        
        #endregion
        
        [Header("Melee Settings")]
        [SerializeField] protected float meleeDamage = 2f;
        [SerializeField] protected float meleeAttackRadius = 1.5f; // Radius of the attack sphere
        [SerializeField] protected LayerMask targetLayer; // Layer mask for valid targets
        [SerializeField] protected float buildingAttackRange = 5f; // Special attack range for buildings (separate from NPC combat)
        
        [Header("Melee Attack Distance")]
        [SerializeField] protected float minAttackDistance = 1.0f; // Minimum distance for attacks
        [SerializeField] protected float maxAttackDistance = 1.5f; // Maximum distance for attacks
        [SerializeField] protected float idealAttackDistance = 1.2f; // Preferred attack distance

        protected override void Awake()
        {
            base.Awake();
            
            // Set the melee-specific distance settings
            Debug.Log($"[{gameObject.name}] MELEE ATTACK DISTANCES | Min: {minAttackDistance} | Ideal: {idealAttackDistance} | Max: {maxAttackDistance}");
        }

        protected override void Update()
        {
            if (Health <= 0) return;

            // Call EnemyBase Update to handle destination setting (skip Zombie.Update)
            base.Update();

            if (navMeshTarget == null) return;

            // Handle melee-specific attack logic
            HandleMeleeAttackLogic();
        }

        /// <summary>
        /// Melee-specific attack logic using this class's distance settings
        /// </summary>
        private void HandleMeleeAttackLogic()
        {
            if (Health <= 0) return;

            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, maxAttackDistance, obstacleBoundsOffset);

            // Check if we're in the melee attack range
            if (distanceToTarget >= minAttackDistance && 
                distanceToTarget <= effectiveAttackDistance && 
                !isAttacking && 
                Time.time >= lastAttackTime + attackCooldown)
            {
                // Phase 1: Rotation phase - rotate towards target until properly aligned
                if (!IsReadyToAttack())
                {
                    isRotatingToAttack = true;
                    RotateTowardsTargetForAttack();
                    
                    if (Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"[{gameObject.name}] ROTATING to attack | Distance: {distanceToTarget:F2} | Range: {minAttackDistance:F2}-{maxAttackDistance:F2}");
                    }
                    return;
                }

                // Phase 2: Attack phase - we're properly aligned, execute attack
                if (isRotatingToAttack || IsReadyToAttack())
                {
                    Debug.Log($"[{gameObject.name}] EXECUTING MELEE ATTACK | Distance: {distanceToTarget:F2}");
                    BeginAttackSequence();
                    lastAttackTime = Time.time;
                    isRotatingToAttack = false;
                }
            }
            else
            {
                // Stop rotation phase if conditions not met
                isRotatingToAttack = false;
            }
        }

        // Called by animation event or timing logic
        public override void Attack()
        {
            string logPrefix = $"[{gameObject.name}] MeleeZombie.Attack";
            
            // Don't attack if dead
            if (Health <= 0) 
            {
                Debug.LogWarning($"{logPrefix} - Called while dead! Health: {Health}");
                return;
            }
            
            if (navMeshTarget == null)
            {
                Debug.LogWarning($"{logPrefix} - No target available!");
                return;
            }
            
            // Use building-specific attack range if attacking a building, otherwise use normal attack range
            float currentAttackRange = attackRange;
            bool isAttackingBuilding = navMeshTarget.GetComponent<Building>() != null;
            if (isAttackingBuilding)
            {
                currentAttackRange = buildingAttackRange;
            }
            
            Debug.Log($"{logPrefix} - Attempting attack | Target: {navMeshTarget.name} | IsBuilding: {isAttackingBuilding} | AttackRange: {currentAttackRange:F2} | Position: {transform.position} | TargetPos: {navMeshTarget.position}");
            
            // Use base class validation
            if (!ValidateAttack(currentAttackRange, MELEE_ATTACK_ANGLE_THRESHOLD, out float distanceToTarget, out float angleToTarget))
            {
                float effectiveDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, currentAttackRange, 1.0f);
                string reason = distanceToTarget > effectiveDistance ? "RANGE_FAIL" : "ANGLE_FAIL";

                Debug.LogWarning($"{logPrefix} - ATTACK VALIDATION FAILED | Reason: {reason} | Distance: {distanceToTarget:F2} | EffectiveDistance: {effectiveDistance:F2} | Angle: {angleToTarget:F1}° | AngleThreshold: {MELEE_ATTACK_ANGLE_THRESHOLD}°");
                return;
            }

            // Execute the attack
            IDamageable target = navMeshTarget.GetComponent<IDamageable>();
            if (target != null)
            {
                float poiseDamage = GetAttackPoiseDamage();
                Debug.Log($"{logPrefix} - EXECUTING ATTACK | Target: {target} | Damage: {meleeDamage} | PoiseDamage: {poiseDamage} | TargetHealth: {target.Health} | TargetPoise: {target.Poise} | Distance: {distanceToTarget:F2} | Angle: {angleToTarget:F1}°");
                target.TakeDamage(meleeDamage, poiseDamage, transform);
                Debug.Log($"{logPrefix} - Attack completed | TargetHealthAfter: {target.Health} | TargetPoiseAfter: {target.Poise}");
            }
            else
            {
                Debug.LogWarning($"{logPrefix} - Target has no IDamageable component! Target: {navMeshTarget.name}");
            }
        }



        // Optional: Visualize attack range in editor
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, 45, 0) * transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -45, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, rightDir * attackRange);
            Gizmos.DrawRay(transform.position, leftDir * attackRange);
        }
    }
}
