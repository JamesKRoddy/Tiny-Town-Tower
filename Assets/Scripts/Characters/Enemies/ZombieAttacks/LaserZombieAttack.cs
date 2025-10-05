using UnityEngine;
using System.Collections;
using Managers;
using PolygonArsenal;

namespace Enemies.ZombieAttacks
{
    /// <summary>
    /// Laser attack component for zombies.
    /// Handles continuous laser beam attacks with precise aiming.
    /// </summary>
    public class LaserZombieAttack : ZombieAttackBase
    {
        [Header("Laser Settings")]
        [Tooltip("Layer mask for laser hit detection")]
        public LayerMask laserHitLayers = -1;
        [Tooltip("Interval between damage checks (seconds)")]
        public float damageInterval = 0.1f;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for laser attacks
            if (attackType == 0)
            {
                attackType = 3; // Laser attack type
            }
            
            // Set default angle threshold for laser attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 15f; // Laser attacks need precise aiming
            }
        }
        
        [Header("Laser Visual Settings")]
        [Tooltip("Transform representing the laser fire point")]
        public Transform laserFirePoint;
        [Tooltip("Laser beam visual component")]
        public PolygonBeamStatic laserBeam;
        
        [Header("Head IK Settings")]
        [Tooltip("Weight for head IK targeting")]
        public float headIKWeight = 1f;
        [Tooltip("Rotation weight for head IK")]
        public float headIKRotationWeight = 1f;
        [Tooltip("Speed of head IK interpolation")]
        public float headIKLerpSpeed = 3f;
        [Tooltip("Maximum angle the head can turn")]
        public float maxHeadRotationAngle = 90f;

        // Laser attack state
        private bool isFiringLaser = false;
        private Coroutine laserDamageCoroutine;
        private Vector3 currentLookAtTarget;
        private bool isHeadIKActive = false;
        private float currentIKWeight = 0f;

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Zombie zombieEnemy)
            {
                // Set default elemental damage for laser attacks (can be overridden in inspector)
                if (attackElement == AttackElement.NONE)
                {
                    attackElement = AttackElement.FIRE; // Laser could be fire damage
                }
                
                // Validate fire point
                if (laserFirePoint == null)
                {
                    Debug.LogWarning($"[{zombieEnemy.gameObject.name}] LaserZombieAttack: Laser fire point not assigned! Please assign laserFirePoint.");
                }
                
                // Initialize current look target to forward position
                currentLookAtTarget = zombieEnemy.transform.position + zombieEnemy.transform.forward * 5f + Vector3.up;
                
                Debug.Log($"[{zombieEnemy.gameObject.name}] LaserZombieAttack initialized | Max: {maxRange} | Damage: {damage} | DamageInterval: {damageInterval}");
            }
        }

        public override bool CanAttack()
        {
            if (!base.CanAttack()) return false;
            
            // Check if we're properly aligned for laser attack
            return IsReadyForLaserAttack();
        }

        /// <summary>
        /// Checks if the zombie is properly aligned for laser attack
        /// </summary>
        private bool IsReadyForLaserAttack()
        {
            if (target == null || laserFirePoint == null) return false;
            
            // Calculate direction from fire point to target (with Y offset)
            Vector3 targetPosition = target.position + Vector3.up;
            Vector3 directionToTarget = (targetPosition - laserFirePoint.position).normalized;
            float angleToTarget = Vector3.Angle(laserFirePoint.forward, directionToTarget);
            
            return angleToTarget <= attackAngleThreshold;
        }

        /// <summary>
        /// Rotates body towards target with enhanced precision for laser attacks
        /// </summary>
        public void RotateTowardsTargetForLaserAttack()
        {
            if (target == null) return;
            
            // Use centralized rotation utility with enhanced speed for laser attacks
            NavigationUtils.RotateTowardsTargetForAction(zombie.transform, target, zombie.rotationSpeed, 2f, attackAngleThreshold, true);
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            // Play start effect
            PlayStartEffect(laserFirePoint.position, laserFirePoint.forward, laserFirePoint.rotation, laserFirePoint);
            
            Debug.Log($"[{zombie.gameObject.name}] Laser attack started | Target: {target.name} | Distance: {Vector3.Distance(zombie.transform.position, target.position):F2}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{zombie.gameObject.name}] Laser attack called with no target!");
                return;
            }
            
            isFiringLaser = true;
            
            // Fire laser from fire point
            FireLaserFromPoint();
            
            // Start continuous damage checking
            laserDamageCoroutine = StartCoroutine(ContinuousLaserDamage());
            
            // Play attack effect
            PlayAttackEffect(laserFirePoint.position, laserFirePoint.forward, laserFirePoint.rotation, laserFirePoint);
            
            Debug.Log($"[{zombie.gameObject.name}] Laser attack executed | Continuous damage started");
        }

        public override void OnAttackEnd()
        {
            // Stop damage coroutine
            if (laserDamageCoroutine != null)
            {
                StopCoroutine(laserDamageCoroutine);
                laserDamageCoroutine = null;
            }
            
            // Disable laser beam
            if (laserBeam != null)
            {
                laserBeam.gameObject.SetActive(false);
            }
            
            // Reset laser state
            isFiringLaser = false;
            
            base.OnAttackEnd();
            
            // Play end effect
            PlayEndEffect(laserFirePoint.position, laserFirePoint.forward, laserFirePoint.rotation, laserFirePoint);
        }

        /// <summary>
        /// Fires a laser from the fire point
        /// </summary>
        private void FireLaserFromPoint()
        {
            if (laserFirePoint == null || target == null) return;
            
            Vector3 startPosition = laserFirePoint.position;
            
            // Position the laser beam at the fire point
            if (laserBeam != null)
            {
                laserBeam.transform.position = startPosition;
                
                // Enable the beam
                laserBeam.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Continuously checks for damageable targets in laser path
        /// </summary>
        private IEnumerator ContinuousLaserDamage()
        {
            while (isFiringLaser && zombie != null && zombie.isAttacking)
            {
                // Check damage from fire point
                CheckLaserDamageFromPoint();
                
                yield return new WaitForSeconds(damageInterval);
            }
        }

        /// <summary>
        /// Checks for damageable targets in the path of a laser from the fire point
        /// </summary>
        private void CheckLaserDamageFromPoint()
        {
            if (laserFirePoint == null) return;
            
            Vector3 startPosition = laserFirePoint.position;
            Vector3 direction = laserFirePoint.forward; // Use fire point's forward direction
            
            // Raycast to find the actual hit point (same as the visual beam)
            RaycastHit hit;
            if (Physics.Raycast(startPosition, direction, out hit, maxRange, laserHitLayers))
            {
                // Check if hit object is damageable
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    // Deal damage with poise damage
                    DealDamageToTarget(damageable, damage, poiseDamage);
                    
                    // Play hit effect
                    PlayHitEffect(hit.point, hit.normal);
                }
            }
        }

        /// <summary>
        /// Check if the zombie should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public override bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            return !IsReadyForLaserAttack();
        }

        /// <summary>
        /// Get the current attack range
        /// </summary>
        /// <returns>Current effective attack range</returns>
        public override float GetCurrentAttackRange()
        {
            return maxRange;
        }

        /// <summary>
        /// Called by Unity for IK updates - implements head tracking for laser attacks
        /// </summary>
        /// <param name="layerIndex">The IK layer index</param>
        public override void OnAnimatorIK(int layerIndex)
        {
            if (zombie == null || zombie.animator == null) return;
            
            // Only use head IK when we have a target and are not dead
            if (target != null && zombie.Health > 0)
            {
                // Calculate target position with Y offset
                Vector3 targetPosition = target.position + Vector3.up;
                
                // Calculate direction from head to target
                Vector3 directionToTarget = (targetPosition - zombie.transform.position).normalized;
                
                // Check if target is within head rotation limits
                float angleToTarget = Vector3.Angle(zombie.transform.forward, directionToTarget);
                if (angleToTarget <= maxHeadRotationAngle)
                {
                    // Enable head IK
                    isHeadIKActive = true;
                    currentLookAtTarget = targetPosition;
                    currentIKWeight = Mathf.Lerp(currentIKWeight, headIKWeight, headIKLerpSpeed * Time.deltaTime);
                }
                else
                {
                    // Target is outside head rotation limits, disable head IK
                    isHeadIKActive = false;
                    currentIKWeight = Mathf.Lerp(currentIKWeight, 0f, headIKLerpSpeed * Time.deltaTime);
                }
            }
            else
            {
                // No target or dead, disable head IK
                isHeadIKActive = false;
                currentIKWeight = Mathf.Lerp(currentIKWeight, 0f, headIKLerpSpeed * Time.deltaTime);
            }
            
            // Apply head IK weights
            if (currentIKWeight > 0.01f)
            {
                zombie.animator.SetLookAtWeight(currentIKWeight, headIKRotationWeight, 0f, 0f, 0f);
                zombie.animator.SetLookAtPosition(currentLookAtTarget);
            }
            else
            {
                zombie.animator.SetLookAtWeight(0f);
            }
        }

        /// <summary>
        /// Draw debug gizmos for laser attack
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            if (zombie == null) return;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(zombie.transform.position, maxRange);
            
            // Draw laser fire point
            if (laserFirePoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(laserFirePoint.position, 0.2f);
                
                // Draw laser direction
                Gizmos.color = Color.red;
                Gizmos.DrawRay(laserFirePoint.position, laserFirePoint.forward * maxRange);
                
                // Draw attack angle
                Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * laserFirePoint.forward;
                Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * laserFirePoint.forward;
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(laserFirePoint.position, rightDir * maxRange);
                Gizmos.DrawRay(laserFirePoint.position, leftDir * maxRange);
            }
            
            // Draw head IK target
            if (isHeadIKActive)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentLookAtTarget, 0.3f);
                Gizmos.DrawLine(zombie.transform.position + Vector3.up * 1.5f, currentLookAtTarget);
            }
        }
    }
}
