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
        [SerializeField] protected float laserDamage = 25f;
        [SerializeField] protected LayerMask laserHitLayers = -1; // Default to everything
        
        [Header("Laser Visual Settings")]
        [SerializeField] protected Transform laserFirePoint;
        [SerializeField] protected PolygonBeamStatic laserBeam;
        
        [Header("Head IK Settings")]
        [SerializeField] protected float headIKWeight = 1f;
        [SerializeField] protected float headIKRotationWeight = 1f;
        [SerializeField] protected float headIKLerpSpeed = 3f;
        private float maxHeadRotationAngle = 90f; // Maximum angle the head can turn
        

        
        // Laser attack state
        protected bool isFiringLaser = false;
        protected Coroutine laserDamageCoroutine;
        protected Vector3 currentLookAtTarget;
        protected bool isHeadIKActive = false;
        protected float currentIKWeight = 0f;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Validate fire point
            if (laserFirePoint == null)
            {
                Debug.LogWarning($"[{gameObject.name}] LaserZombie: Laser fire point not assigned! Please assign laserFirePoint.");
            }
            
            // Initialize current look target to forward position
            currentLookAtTarget = transform.position + transform.forward * 5f + Vector3.up;
        }
        
        protected override void Update()
        {
            if (Health <= 0) return;

            base.Update(); // Call base Update to handle destination setting

            if (navMeshTarget == null) return;

            // Cache distance calculation
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float effectiveLaserDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, attackRange, obstacleBoundsOffset);
            
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
                Time.time < lastAttackTime + attackCooldown)
            {
                return false;
            }

            // Check if we're properly aligned for laser attack
            if (!IsReadyForLaserAttack())
            {
                // Rotate body towards target for laser attack
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
            
            // Calculate direction from fire point to target (with Y offset)
            Vector3 targetPosition = navMeshTarget.position + Vector3.up;
            Vector3 directionToTarget = (targetPosition - laserFirePoint.position).normalized;
            float angleToTarget = Vector3.Angle(laserFirePoint.forward, directionToTarget);
            
            return angleToTarget <= LASER_ATTACK_ANGLE_THRESHOLD;
        }
        
        /// <summary>
        /// Rotates body towards target with enhanced precision for laser attacks
        /// </summary>
        private void RotateTowardsTargetForLaserAttack()
        {
            if (navMeshTarget == null) return;
            
            // Use centralized rotation utility with enhanced speed for laser attacks
            NavigationUtils.RotateTowardsTargetForAction(transform, navMeshTarget, rotationSpeed, 2f, LASER_ATTACK_ANGLE_THRESHOLD, true);
        }
        
        /// <summary>
        /// Called by animator for laser charge effects - uses base AttackWarning
        /// </summary>
        public override void AttackWarning()
        {
            // Call base method for material flash
            base.AttackWarning();
            
            // Add any laser-specific charge effects here if needed
            // For now, just use the base material flash
        }
        
        /// <summary>
        /// Called by the animator to fire the laser
        /// </summary>
        public override void Attack()
        {
            if (!isAttacking || navMeshTarget == null) return;
            
            isFiringLaser = true;
            
            // Fire laser from fire point
            FireLaserFromPoint();
            
            lastAttackTime = Time.time;
            
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
            float damageInterval = 0.1f; // Check for damage every 0.1 seconds
            
            while (isFiringLaser && isAttacking)
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
            if (Physics.Raycast(startPosition, direction, out hit, attackRange, laserHitLayers))
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
        
        /// <summary>
        /// Unity IK callback for head targeting
        /// </summary>
        protected virtual void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;
            
            // Only apply head IK when not dead
            if (Health <= 0) return;
            
            // Calculate neutral position (always forward relative to current rotation)
            Vector3 neutralPosition = transform.position + transform.forward * 5f + Vector3.up;
            
            bool shouldLookAtPlayer = false;
            Vector3 targetPosition = neutralPosition;
            
            // Check if we should be looking at the player
            if (navMeshTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                float effectiveLaserDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, attackRange, obstacleBoundsOffset);
                
                if (distanceToTarget <= effectiveLaserDistance)
                {
                    // Calculate target position with Y offset (aim at upper body/head)
                    Vector3 targetPos = navMeshTarget.position + Vector3.up;
                    
                    // Check if target is within head rotation limits
                    Vector3 directionToTarget = (targetPos - transform.position).normalized;
                    float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                    
                    if (angleToTarget <= maxHeadRotationAngle)
                    {
                        // Target is within head rotation range - look at player
                        targetPosition = targetPos;
                        shouldLookAtPlayer = true;
                    }
                }
            }
            
            // Update look target position
            currentLookAtTarget = Vector3.Lerp(currentLookAtTarget, targetPosition, headIKLerpSpeed * Time.deltaTime);
            
            // Smoothly lerp IK weight based on whether we should be looking at the player
            float targetWeight = shouldLookAtPlayer ? headIKWeight : 0f;
            currentIKWeight = Mathf.Lerp(currentIKWeight, targetWeight, headIKLerpSpeed * Time.deltaTime);
            
            // Apply IK with smooth weight transition
            animator.SetLookAtWeight(currentIKWeight, headIKRotationWeight, 0f, 0f, 0f);
            animator.SetLookAtPosition(currentLookAtTarget);
        }
        
        #region Debug Visualization
        
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            if (navMeshTarget == null) return;
            
            // Draw laser attack range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
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
                Gizmos.DrawRay(transform.position, direction * attackRange);
            }
        }
        
        #endregion
    }
} 