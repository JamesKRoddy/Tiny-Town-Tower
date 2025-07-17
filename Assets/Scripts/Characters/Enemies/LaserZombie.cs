using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;
using PolygonArsenal;

namespace Enemies
{
    public class LaserZombie : Zombie
    {
        #region Constants
        
        private const float LASER_ATTACK_ANGLE_THRESHOLD = 15f; // More precise aiming for ranged attacks
        
        #endregion
        
        [Header("Laser Attack Settings")]
        [SerializeField] protected float laserAttackRange = 8f; // Longer range than melee
        [SerializeField] protected float laserAttackCooldown = 3f;
        [SerializeField] protected float laserDamage = 25f;
        [SerializeField] protected LayerMask laserHitLayers = -1; // Default to everything
        
        [Header("Laser Visual Settings")]
        [SerializeField] protected Transform laserFirePoint;
        [SerializeField] protected PolygonBeamStatic laserBeam;
        

        
        // Laser attack state
        protected bool isFiringLaser = false;
        protected float lastLaserAttackTime;
        protected Coroutine laserDamageCoroutine;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Validate fire point
            if (laserFirePoint == null)
            {
                Debug.LogWarning($"[{gameObject.name}] LaserZombie: Laser fire point not assigned! Please assign laserFirePoint.");
            }
        }
        
        protected override void Update()
        {
            if (Health <= 0) return;

            base.Update(); // Call base Update to handle destination setting

            if (navMeshTarget == null) return;

            // Cache distance calculation
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float effectiveLaserDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, laserAttackRange, obstacleBoundsOffset);
            
            // Debug distance and attack state
            Debug.DrawLine(transform.position, navMeshTarget.position, Color.yellow);
            
            // Handle laser attack logic first (prioritize ranged attacks)
            if (HandleLaserAttackLogic(distanceToTarget, effectiveLaserDistance))
            {
                return; // Laser attack handled, skip melee
            }
            
            // Fall back to melee attack logic (call base Zombie logic)
            base.Update();
        }
        
        /// <summary>
        /// Handles laser attack logic with charging and firing phases
        /// </summary>
        private bool HandleLaserAttackLogic(float distanceToTarget, float effectiveLaserDistance)
        {
            if (Health <= 0) return false;

            // Check for laser attack range and cooldown
            if (distanceToTarget > effectiveLaserDistance || 
                isAttacking || 
                isFiringLaser || 
                Time.time < lastLaserAttackTime + laserAttackCooldown)
            {
                return false;
            }

            // Check if we're properly aligned for laser attack
            if (!IsReadyForLaserAttack())
            {
                // Rotate towards target for laser attack
                RotateTowardsTargetForLaserAttack();
                return true;
            }

            // Start laser attack sequence
            BeginAttackSequence();
            return true;
        }
        
        /// <summary>
        /// Checks if the zombie is properly aligned for laser attack
        /// </summary>
        private bool IsReadyForLaserAttack()
        {
            if (navMeshTarget == null) return false;
            
            Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            return angleToTarget <= LASER_ATTACK_ANGLE_THRESHOLD;
        }
        
        /// <summary>
        /// Rotates towards target with enhanced precision for laser attacks
        /// </summary>
        private void RotateTowardsTargetForLaserAttack()
        {
            if (navMeshTarget == null) return;
            
            Vector3 direction = (navMeshTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float enhancedRotationSpeed = rotationSpeed * 2f; // Faster rotation for ranged attacks
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enhancedRotationSpeed * Time.deltaTime);
        }
        
        /// <summary>
        /// Called by animator for laser charge effects - uses base AttackWarning
        /// </summary>
        public void LaserAttackWarning()
        {
            // Call base method for material flash
            AttackWarning();
            
            // Add any laser-specific charge effects here if needed
            // For now, just use the base material flash
        }
        
        /// <summary>
        /// Called by the animator to fire the laser
        /// </summary>
        public void FireLaser()
        {
            if (!isAttacking || navMeshTarget == null) return;
            
            isFiringLaser = true;
            
            // Fire laser from fire point
            FireLaserFromPoint();
            
            lastLaserAttackTime = Time.time;
            
            // Start continuous damage checking
            laserDamageCoroutine = StartCoroutine(ContinuousLaserDamage());
        }
                
        /// <summary>
        /// Fires a laser from the fire point
        /// </summary>
        private void FireLaserFromPoint()
        {
            if (laserFirePoint == null || navMeshTarget == null) return;
            
            Vector3 startPosition = laserFirePoint.position;
            Vector3 direction = (navMeshTarget.position - startPosition).normalized;
            
            // Position and orient the laser beam
            if (laserBeam != null)
            {
                laserBeam.transform.position = startPosition;
                laserBeam.transform.rotation = Quaternion.LookRotation(direction);
                
                // Enable the beam
                laserBeam.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Continuously checks for damageable targets in laser path
        /// </summary>
        private IEnumerator ContinuousLaserDamage()
        {
            float damageInterval = 0.1f; // Check for damage every 0.1 seconds
            
            while (isFiringLaser)
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
            if (laserFirePoint == null || navMeshTarget == null) return;
            
            Vector3 startPosition = laserFirePoint.position;
            Vector3 direction = (navMeshTarget.position - startPosition).normalized;
            
            // Raycast to find targets
            RaycastHit hit;
            if (Physics.Raycast(startPosition, direction, out hit, laserAttackRange, laserHitLayers))
            {
                // Check if hit object is damageable
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    // Deal damage
                    damageable.TakeDamage(laserDamage, transform);
                    
                    // Play hit effect
                    Vector3 hitPoint = hit.point;
                    Vector3 hitNormal = hit.normal;
                    EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, this);
                }
            }
        }
        
        /// <summary>
        /// Override EndAttack to clean up laser effects
        /// </summary>
        protected override void EndAttack()
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
            
            // Call base method
            base.EndAttack();
        }
        
        public override void Die()
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
            
            base.Die();
        }
        
        #region Debug Visualization
        
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            if (navMeshTarget == null) return;
            
            // Draw laser attack range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, laserAttackRange);
            
            // Draw fire point position
            if (laserFirePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(laserFirePoint.position, 0.1f);
            }
            
            // Draw laser direction
            if (navMeshTarget != null)
            {
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, direction * laserAttackRange);
            }
        }
        
        #endregion
    }
} 