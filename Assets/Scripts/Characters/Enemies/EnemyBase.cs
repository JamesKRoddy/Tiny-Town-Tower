using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

namespace Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Character Type")]
        [SerializeField] protected CharacterType characterType = CharacterType.ZOMBIE_MELEE;

        [Header("Movement Settings")]
        [SerializeField] protected bool useRootMotion = false;
        [SerializeField] protected float stoppingDistance = 1.5f;
        [SerializeField] protected float rotationSpeed = 10f;
        [SerializeField] protected float movementSpeed = 3.5f;
        [SerializeField] protected float acceleration = 8f;
        [SerializeField] protected float angularSpeed = 120f;

        protected NavMeshAgent agent;
        protected Animator animator;
        protected Transform navMeshTarget;        
        public Transform NavMeshTarget => navMeshTarget;
        protected bool isAttacking = false;

        [SerializeField] private float health = 100f;
        [SerializeField] private float maxHealth = 100f;

        // Material flash effect
        protected SkinnedMeshRenderer skinnedMeshRenderer;
        protected Material originalMaterial;
        protected Material flashMaterial;
        protected float flashDuration = 0.5f;
        protected Color flashColor = new Color(1f, 0.1f, 0.1f); // Bright red color for flash

        public float Health
        {
            get => health;
            set => health = Mathf.Clamp(value, 0, maxHealth);
        }

        public float MaxHealth
        {
            get => maxHealth;
            set => maxHealth = value;
        }

        protected float damage;
        public event Action<float, float> OnDamageTaken;
        public event Action<float, float> OnHeal;
        public event Action OnDeath;

        public CharacterType CharacterType => characterType;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            
            // Configure NavMeshAgent
            agent.stoppingDistance = stoppingDistance;
            agent.speed = movementSpeed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.updateRotation = true;
            agent.updateUpAxis = false;
            
            // Get the SkinnedMeshRenderer and store original material
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                originalMaterial = skinnedMeshRenderer.material;
                // Create a new instance of the material for flashing
                flashMaterial = new Material(originalMaterial);
                flashMaterial.color = flashColor;
            }

            if (useRootMotion)
            {
                SetupRootMotion();
            }
        }

        IEnumerator Start(){

            if(MaxHealth == 0)
                MaxHealth = 10f;

            Health = MaxHealth;

            yield return new WaitForSeconds(0.5f);

            if(navMeshTarget == null){
                Debug.LogWarning($"Enemy {gameObject.name} has no target set");
                navMeshTarget = PlayerController.Instance._possessedNPC.GetTransform();
            }           
        }

        protected virtual void SetupRootMotion()
        {
            agent.updatePosition = false;
            agent.updateRotation = true; // Enable rotation by default
            
            // Ensure the enemy is on the NavMesh when spawned
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"Enemy {gameObject.name} could not be placed on NavMesh at spawn position {transform.position}");
            }
        }

        protected virtual void Update()
        {
            // Don't do anything if dead or no target
            if (Health <= 0 || navMeshTarget == null)
                return;

            if (!isAttacking)
            {
                // Update the destination continuously
                agent.SetDestination(navMeshTarget.position);
                
                // Handle rotation towards target
                if (agent.velocity.magnitude > 0.1f)
                {
                    Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                }
            }
        }

        protected virtual void UpdateRootMotion()
        {
            // Don't do anything if dead or no target
            if (Health <= 0 || navMeshTarget == null)
                return;

            if (!isAttacking)
            {
                // Update the destination continuously
                agent.SetDestination(navMeshTarget.position);
                
                // Handle rotation towards target
                if (agent.velocity.magnitude > 0.1f)
                {
                    Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                }
            }
        }

        // This method is called by the Animator when root motion is being applied
        protected virtual void OnAnimatorMove()
        {
            if (useRootMotion && Health > 0 && agent.isOnNavMesh)
            {
                // Get the root motion delta (movement from animation)
                Vector3 rootMotion = animator.deltaPosition;
                rootMotion.y = 0; // We don't want to apply any vertical movement (gravity, etc.)

                // Calculate the new position
                Vector3 newPosition = transform.position + rootMotion;

                // Sample the NavMesh with a larger radius to ensure we stay on it
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    // Move the enemy using root motion
                    transform.position = hit.position;
                    
                    // Update agent's position to match with root motion
                    agent.nextPosition = hit.position;
                }
                else
                {
                    // If we can't find a valid position, try to find the nearest valid position
                    if (NavMesh.FindClosestEdge(transform.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
                    {
                        // Move to the nearest valid position
                        transform.position = edgeHit.position;
                        agent.Warp(edgeHit.position);
                    }
                    else
                    {
                        // If we can't find any valid position, disable root motion temporarily
                        useRootMotion = false;
                        agent.updatePosition = true;
                        agent.updateRotation = true;
                    }
                }
            }
        }

        internal void Setup(Transform navAgentTarget)
        {
            navMeshTarget = navAgentTarget;
        }

        public void SetEnemyDestination(Vector3 navMeshTarget)
        {
            agent.SetDestination(navMeshTarget);
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(Health + amount, MaxHealth);
        }    

        /// <summary>
        /// Called by the child classes to start the attack animation and disable the nav mesh agent rotation
        /// </summary>
        protected virtual void BeginAttackSequence()
        {
            // Trigger attack animation, this should transition to attack animations via root motion
            animator.SetBool("Attack", true);
            isAttacking = true;

            if (useRootMotion)
            {
                agent.updateRotation = false; // Disable NavMeshAgent rotation during attack
            }
        }

        /// <summary>
        /// Called by the child classes to end the attack animation and re-enable the nav mesh agent rotation
        /// </summary>
        protected virtual void EndAttack()
        {
            // Reset isAttacking flag after the attack animation finishes
            animator.SetBool("Attack", false);
            isAttacking = false;

            if (useRootMotion)
            {
                agent.updateRotation = true; // Re-enable NavMeshAgent rotation after attack
            }
        }

        public Allegiance GetAllegiance() => Allegiance.HOSTILE;

        public void TakeDamage(float amount, Transform damageSource = null)
        {
            float previousHealth = Health;
            Health -= amount;

            // Interrupt attack animation and play damage animation
            if (animator != null)
            {
                // Reset attack trigger and return to default state
                animator.ResetTrigger("Attack");
                animator.Play("Default", 1, 0); // Play the default animation on attack layer
                // Play damage animation
                animator.SetTrigger("Damaged");
            }

            OnDamageTaken?.Invoke(amount, Health);

            // Play hit VFX
            if (damageSource != null)
            {
                Vector3 hitPoint = transform.position + Vector3.up * 1.5f;
                Vector3 hitNormal = (transform.position - damageSource.position).normalized;
                EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, this);
                
                // Handle knockback and rotation
                HandleDamageReaction(damageSource);
            }

            if (Health <= 0)
            {
                Die();
            }
        }

        protected virtual void HandleDamageReaction(Transform damageSource)
        {
            //if (!isAttacking)
            //{
                Vector3 direction = (damageSource.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f);

                    // Add knockback effect
                    Vector3 knockbackDirection = -direction;
                    float maxKnockbackDistance = 1.0f;
                    float distanceFromSource = Vector3.Distance(transform.position, damageSource.position);
                    float knockbackDistance = Mathf.Lerp(maxKnockbackDistance, maxKnockbackDistance * 0.3f, distanceFromSource / 5f);
                    Vector3 newPosition = transform.position + knockbackDirection * knockbackDistance;

                    if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
                    {
                        StartCoroutine(KnockbackRoutine(hit.position));
                    }
                }
            //}
        }

        private IEnumerator KnockbackRoutine(Vector3 targetPosition)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startPosition = transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
                
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                
                yield return null;
            }
        }

        public void Die()
        {
            OnDeath?.Invoke();
            if (animator != null)
            {
                animator.SetTrigger("Dead");
            }
            isAttacking = false;
            agent.enabled = false;
            GetComponent<Collider>().enabled = false;

            int shouldDropLoot = UnityEngine.Random.Range(0, 100);
            if (shouldDropLoot < 50)
            {
                GameManager.Instance.ResourceManager.SpawnCharacterLoot(characterType, RogueLiteManager.Instance.GetCurrentWaveDifficulty(), transform.position + Vector3.up * 1.0f);
            }

            // Play death VFX
            Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
            Vector3 deathNormal = Vector3.up;
            EffectManager.Instance.PlayDeathEffect(deathPoint, deathNormal, this);

            Destroy(gameObject, 10f);
        }

        internal float GetDamageValue()
        {
            return damage;
        }

        // Animation event function that can be called from the animator
        public void AttackWarning()
        {
            if (skinnedMeshRenderer != null)
            {
                StartCoroutine(FlashMaterialCoroutine());
            }
        }

        private IEnumerator FlashMaterialCoroutine()
        {
            // Apply flash material
            skinnedMeshRenderer.material = flashMaterial;
            
            // Wait for flash duration
            yield return new WaitForSeconds(flashDuration);
            
            // Restore original material
            skinnedMeshRenderer.material = originalMaterial;
        }
    }
}
