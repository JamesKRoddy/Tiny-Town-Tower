using UnityEngine;
using UnityEngine.AI;

namespace Enemies.BossAttacks
{
    public class BossJumpAttack : BossAttackBase
    {
        [Header("Jump Settings")]
        [Range(1f, 10f)]
        public float jumpRadius = 3f;
        [Range(1f, 30f)]
        public float jumpHeight = 5f;
        [Range(0.1f, 3f)]
        public float jumpDuration = 1f;
        [Tooltip("How much to predict player movement (0 = no prediction, 1 = full prediction)")]
        [Range(0f, 1f)]
        public float predictionFactor = 0.5f;
        [Tooltip("Stop tracking player during the final portion of the jump (0-1)")]
        [Range(0f, 1f)]
        public float finalJumpLockPercentage = 0.1f;
        [Tooltip("How fast the boss rotates to face the player (degrees per second)")]
        [Range(90f, 720f)]
        public float rotationSpeed = 360f;

        private Vector3 jumpStartPosition;
        private Vector3 jumpTargetPosition;
        private float jumpStartTime;
        private bool isJumping = false;
        private bool wasRootMotionEnabled;
        private Vector3 playerVelocity;
        private Quaternion targetRotation;
        private float originalStoppingDistance;
        private NavMeshAgent agent;

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
            // Store the original stopping distance and agent reference
            if (boss != null)
            {
                agent = boss.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    originalStoppingDistance = agent.stoppingDistance;
                }
            }
        }

        /// <summary>
        /// Starts the jump attack from animator event
        /// </summary>
        public void StartJump()
        {
            if (target == null)
            {
                Debug.LogWarning("[BossJumpAttack] StartJump failed: target is null");
                return;
            }

            PlayStartEffect();

            jumpStartPosition = transform.position;
            
            // Get player's current velocity for prediction
            if (target.GetComponent<Rigidbody>() != null)
            {
                playerVelocity = target.GetComponent<Rigidbody>().linearVelocity;
            }
            else
            {
                playerVelocity = Vector3.zero;
            }

            // Calculate predicted landing position
            Vector3 predictedPosition = target.position + (playerVelocity * jumpDuration * predictionFactor);
            
            // Ensure the predicted position is on the NavMesh
            if (NavMesh.SamplePosition(predictedPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                jumpTargetPosition = hit.position;
            }
            else
            {
                jumpTargetPosition = target.position;
            }

            // Set initial target rotation
            UpdateTargetRotation();

            jumpStartTime = Time.time;
            isJumping = true;

            // Disable NavMeshAgent and root motion during jump
            if (boss != null)
            {
                if (agent != null)
                {
                    agent.enabled = false;
                }
                else
                {
                    Debug.LogWarning("[BossJumpAttack] No NavMeshAgent found on boss");
                }

                var animator = boss.GetComponent<Animator>();
                if (animator != null)
                {
                    wasRootMotionEnabled = animator.applyRootMotion;
                    animator.applyRootMotion = false;
                }
            }
            else
            {
                Debug.LogWarning("[BossJumpAttack] Boss reference is null");
            }
        }

        private void UpdateTargetRotation()
        {
            if (target != null)
            {
                Vector3 directionToTarget = target.position - transform.position;
                directionToTarget.y = 0; // Keep rotation on the horizontal plane
                if (directionToTarget != Vector3.zero)
                {
                    targetRotation = Quaternion.LookRotation(directionToTarget);
                }
            }
        }

        private void Update()
        {
            if (!isJumping) return;

            float elapsedTime = Time.time - jumpStartTime;
            float progress = elapsedTime / jumpDuration;

            if (progress >= 1f)
            {
                EndJump();
                return;
            }

            // Only update target position if we're not in the final portion of the jump
            if (progress < (1f - finalJumpLockPercentage))
            {
                // Update target position during jump to account for player movement
                if (target != null)
                {
                    Vector3 currentTargetPos = target.position;
                    if (NavMesh.SamplePosition(currentTargetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        // Blend between original predicted position and current position
                        float blendFactor = progress * 0.5f; // Gradually increase influence of current position
                        jumpTargetPosition = Vector3.Lerp(jumpTargetPosition, hit.position, blendFactor);
                    }
                }
            }

            // Update rotation to face the player
            UpdateTargetRotation();
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Calculate the current position in the jump arc
            Vector3 currentPosition = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, progress);
            
            // Add vertical movement using a single sine wave
            // Using PI * progress gives us a full sine wave from 0 to 1
            currentPosition.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            // Apply the movement directly
            transform.position = currentPosition;
        }

        private void EndJump()
        {
            isJumping = false;

            if (boss != null && agent != null)
            {
                // First, ensure we're on the NavMesh
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {                    
                    // Warp to the nearest valid NavMesh position
                    transform.position = hit.position;
                    agent.enabled = true;
                    
                    // Reset the agent's destination to the current target
                    if (target != null)
                    {
                        // Reset stopping distance to original value
                        agent.stoppingDistance = originalStoppingDistance;
                        agent.isStopped = false;
                        agent.ResetPath();
                        
                        // Ensure the target position is on the NavMesh
                        if (NavMesh.SamplePosition(target.position, out NavMeshHit targetHit, 5f, NavMesh.AllAreas))
                        {
                            agent.SetDestination(targetHit.position);
                        }
                        else
                        {
                            Debug.LogWarning("[BossJumpAttack] Could not find valid NavMesh position for target");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[BossJumpAttack] Target is null when trying to set destination");
                    }
                }
                else
                {
                    Debug.LogWarning("[BossJumpAttack] Could not find valid NavMesh position for boss");
                }

                var animator = boss.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.applyRootMotion = wasRootMotionEnabled;
                }

                // Reset the attack state
                boss.AttackEnd();
            }
            else
            {
                Debug.LogWarning("[BossJumpAttack] Boss or NavMeshAgent reference is null");
            }

            // Deal damage on landing
            DealDamageInRadius(jumpRadius, damage, transform.position);

            PlayEndEffect();
        }

        public override void OnAttackStart()
        {
            // The actual damage is dealt when landing
        }

        private void OnDrawGizmos()
        {
            if (isJumping)
            {
                // Draw jump path
                Gizmos.color = Color.yellow;
                int segments = 20;
                for (int i = 0; i < segments; i++)
                {
                    float progress = i / (float)segments;
                    Vector3 point = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, progress);
                    point.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                    Gizmos.DrawSphere(point, 0.2f);
                }

                // Draw target position
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(jumpTargetPosition, 0.5f);

                // Draw the point where tracking stops
                float lockProgress = 1f - finalJumpLockPercentage;
                Vector3 lockPoint = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, lockProgress);
                lockPoint.y += Mathf.Sin(lockProgress * Mathf.PI) * jumpHeight;
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(lockPoint, 0.3f);

                // Draw rotation direction
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, transform.forward * 2f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, (target.position - transform.position).normalized * 2f);
            }
        }
    }
} 