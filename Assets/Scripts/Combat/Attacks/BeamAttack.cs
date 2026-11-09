using UnityEngine;
using Managers;
using System.Collections;
using PolygonArsenal;

namespace Combat.Attacks
{
    /// <summary>
    /// Continuous beam attack that deals damage over time while active.
    /// Good for laser attacks, flame throwers, lightning beams, etc.
    /// </summary>
    public class BeamAttack : AttackBase
    {
        [Header("Beam Settings")]
        [Tooltip("Layer mask for beam hit detection")]
        public LayerMask beamHitLayers = -1;
        [Tooltip("Interval between damage checks (seconds)")]
        public float damageInterval = 0.1f;
        [Tooltip("Transform representing the beam fire point")]
        public Transform beamFirePoint;
        [Tooltip("Beam visual component")]
        public PolygonBeamStatic beamVisual;
        
        [Header("Head IK Settings")]
        [Tooltip("Weight for head IK targeting")]
        public float headIKWeight = 1f;
        [Tooltip("Rotation weight for head IK")]
        public float headIKRotationWeight = 1f;
        [Tooltip("Speed of head IK interpolation")]
        public float headIKLerpSpeed = 3f;
        [Tooltip("Maximum angle the head can turn")]
        public float maxHeadRotationAngle = 90f;

        // Beam attack state
        private bool isFiringBeam = false;
        private Coroutine beamDamageCoroutine;
        private Vector3 currentLookAtTarget;
        private bool isHeadIKActive = false;
        private float currentIKWeight = 0f;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for beam attacks
            if (attackType == 0)
            {
                attackType = 3; // Beam attack type
            }
            
            // Set default angle threshold for beam attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 15f; // Beam attacks need precise aiming
            }
        }

        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            // Set default elemental damage for beam attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            // Validate fire point
            if (beamFirePoint == null)
            {
                Debug.LogWarning($"[{attackOwner.gameObject.name}] BeamAttack: Beam fire point not assigned! Please assign beamFirePoint.");
            }
            
            // Initialize current look target to forward position
            currentLookAtTarget = attackOwner.transform.position + attackOwner.transform.forward * 5f + Vector3.up;
            
            Debug.Log($"[{attackOwner.gameObject.name}] BeamAttack initialized | Max: {maxRange} | Damage: {damage} | DamageInterval: {damageInterval}");
        }

        public override bool CanAttack()
        {
            if (!base.CanAttack()) return false;
            
            // Check if we're properly aligned for beam attack
            return IsReadyForBeamAttack();
        }

        /// <summary>
        /// Checks if the owner is properly aligned for beam attack
        /// </summary>
        private bool IsReadyForBeamAttack()
        {
            if (target == null || beamFirePoint == null) return false;
            
            // Calculate direction from fire point to target (with Y offset)
            Vector3 targetPosition = target.position + Vector3.up;
            Vector3 directionToTarget = (targetPosition - beamFirePoint.position).normalized;
            float angleToTarget = Vector3.Angle(beamFirePoint.forward, directionToTarget);
            
            return angleToTarget <= attackAngleThreshold;
        }

        /// <summary>
        /// Rotates owner towards target with enhanced precision for beam attacks
        /// </summary>
        public void RotateTowardsTargetForBeamAttack()
        {
            if (target == null) return;
            
            // Use centralized rotation utility with enhanced speed for beam attacks
            NavigationUtils.RotateTowardsTargetForAction(OwnerTransform, target, 2f, 2f, attackAngleThreshold, true);
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Beam attack started | Target: {target.name} | Distance: {Vector3.Distance(OwnerTransform.position, target.position):F2}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Beam attack called with no target!");
                return;
            }
            
            isFiringBeam = true;
            
            // Fire beam from fire point
            FireBeamFromPoint();
            
            // Start continuous damage checking
            beamDamageCoroutine = StartCoroutine(ContinuousBeamDamage());
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Beam attack executed | Continuous damage started");
        }

        public override void OnAttackEnd()
        {
            // Stop damage coroutine
            if (beamDamageCoroutine != null)
            {
                StopCoroutine(beamDamageCoroutine);
                beamDamageCoroutine = null;
            }
            
            // Disable beam visual
            if (beamVisual != null)
            {
                beamVisual.gameObject.SetActive(false);
            }
            
            // Reset beam state
            isFiringBeam = false;
            
            base.OnAttackEnd();
        }

        /// <summary>
        /// Fires a beam from the fire point
        /// </summary>
        private void FireBeamFromPoint()
        {
            if (beamFirePoint == null || target == null) return;
            
            Vector3 startPosition = beamFirePoint.position;
            
            // Position the beam visual at the fire point
            if (beamVisual != null)
            {
                beamVisual.transform.position = startPosition;
                
                // Enable the beam
                beamVisual.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Continuously checks for damageable targets in beam path
        /// </summary>
        private IEnumerator ContinuousBeamDamage()
        {
            while (isFiringBeam && owner != null && owner.IsAttacking)
            {
                // Check damage from fire point
                CheckBeamDamageFromPoint();
                
                yield return new WaitForSeconds(damageInterval);
            }
        }

        /// <summary>
        /// Checks for damageable targets in the path of a beam from the fire point
        /// </summary>
        private void CheckBeamDamageFromPoint()
        {
            if (beamFirePoint == null) return;
            
            Vector3 startPosition = beamFirePoint.position;
            Vector3 direction = beamFirePoint.forward; // Use fire point's forward direction
            
            // Raycast to find the actual hit point (same as the visual beam)
            RaycastHit hit;
            if (Physics.Raycast(startPosition, direction, out hit, maxRange, beamHitLayers))
            {
                // Check if hit object is damageable
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null && DamageUtils.IsValidTarget(DealerAllegiance, damageable))
                {
                    // Deal damage with poise damage
                    DamageUtils.DealDamageToTarget(damageable, damage, poiseDamage, OwnerTransform, attackElement);
                    
                    // Play hit effect
                    PlayHitEffect(hit.point, hit.normal);
                }
            }
        }

        public override bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            return !IsReadyForBeamAttack();
        }

        /// <summary>
        /// Called by Unity for IK updates - implements head tracking for beam attacks
        /// </summary>
        /// <param name="layerIndex">The IK layer index</param>
        public override void OnAnimatorIK(int layerIndex)
        {
            if (owner == null || animator == null) return;
            
            // Only use head IK when we have a target and are not dead
            if (target != null && owner.Health > 0)
            {
                // Calculate target position with Y offset
                Vector3 targetPosition = target.position + Vector3.up;
                
                // Calculate direction from head to target
                Vector3 directionToTarget = (targetPosition - OwnerTransform.position).normalized;
                
                // Check if target is within head rotation limits
                float angleToTarget = Vector3.Angle(OwnerTransform.forward, directionToTarget);
                if (angleToTarget <= maxHeadRotationAngle)
                {
                    // Enable head IK
                    isHeadIKActive = true;
                    
                    // Smoothly lerp the look-at target position
                    currentLookAtTarget = Vector3.Lerp(currentLookAtTarget, targetPosition, headIKLerpSpeed * Time.deltaTime);
                    
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
                animator.SetLookAtWeight(currentIKWeight, headIKRotationWeight, 0f, 0f, 0f);
                animator.SetLookAtPosition(currentLookAtTarget);
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(drawTransform.position, maxRange);
            
            // Draw beam fire point
            if (beamFirePoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(beamFirePoint.position, 0.2f);
                
                // Draw beam direction
                Gizmos.color = Color.red;
                Gizmos.DrawRay(beamFirePoint.position, beamFirePoint.forward * maxRange);
                
                // Draw attack angle
                Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * beamFirePoint.forward;
                Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * beamFirePoint.forward;
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(beamFirePoint.position, rightDir * maxRange);
                Gizmos.DrawRay(beamFirePoint.position, leftDir * maxRange);
            }
            
            // Draw head IK target
            if (isHeadIKActive)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentLookAtTarget, 0.3f);
                Gizmos.DrawLine(drawTransform.position + Vector3.up * 1.5f, currentLookAtTarget);
            }
        }
    }
}
