using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Combat;

/// <summary>
/// Unified combat system component that manages AttackBase components for any character.
/// 
/// USAGE:
/// 1. Add this component to any character (player, NPC, enemy)
/// 2. Character must implement IAttackOwner interface
/// 3. Add AttackBase components (AnimationAttack, WeaponAttack, etc.) to the character
/// 4. Combat system will automatically manage attacks
/// 
/// FEATURES:
/// - Works with any IAttackOwner (players, NPCs, enemies)
/// - Multiple attack selection strategies
/// - Cooldown management
/// - Attack rotation and timing
/// - Animation integration
/// 
/// This replaces the attack management code in ModularEnemy for shared use.
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterCombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Strategy for selecting which attack to use")]
    [SerializeField] private AttackSelectionStrategy selectionStrategy = AttackSelectionStrategy.DISTANCE_BASED;
    
    [Tooltip("Cooldown before switching to a different attack type")]
    [SerializeField] private float attackSwitchCooldown = 0.5f;
    
    [Tooltip("Time to wait before auto-resetting a stuck attack")]
    [SerializeField] private float attackTimeoutDuration = 3.0f;

    /// <summary>
    /// Different strategies for selecting which attack to use
    /// </summary>
    public enum AttackSelectionStrategy
    {
        PRIORITY,           // Use attacks in order of priority (first in list)
        RANDOM,             // Randomly select from available attacks
        DISTANCE_BASED,     // Select attack based on distance to target
        ROTATION_BASED,     // Select attack based on rotation requirements
        MANUAL              // Attack selection is controlled externally (for players)
    }

    // Component references
    private IAttackOwner owner;
    private Animator animator;
    private NavMeshAgent agent;
    
    // Attack management
    private AttackBase[] attackComponents;
    private AttackBase currentAttack;
    private AttackBase selectedAttack; // For MANUAL mode
    private float lastAttackSwitchTime;
    private bool isExecutingAttack = false;
    private float attackExecutionStartTime;
    
    // Public properties
    public AttackBase CurrentAttack => currentAttack;
    public AttackBase[] AttackComponents => attackComponents;
    public bool IsExecutingAttack => isExecutingAttack;
    public bool HasAttacks => attackComponents != null && attackComponents.Length > 0;

    #region Initialization

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        
        // Find all attack components
        InitializeAttackComponents();
    }

    private void Start()
    {
        // Get IAttackOwner from this or parent
        owner = GetComponent<IAttackOwner>();
        
        if (owner == null)
        {
            Debug.LogError($"[{gameObject.name}] CharacterCombatSystem requires IAttackOwner implementation!");
            enabled = false;
            return;
        }
        
        // Initialize all attack components
        foreach (var attack in attackComponents)
        {
            if (attack != null)
            {
                attack.Initialize(owner);
            }
        }
        
        Debug.Log($"[{gameObject.name}] CharacterCombatSystem initialized with {attackComponents.Length} attack(s)");
    }

    private void InitializeAttackComponents()
    {
        // Find all attack components on this GameObject
        attackComponents = GetComponents<AttackBase>();
        
        if (attackComponents.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No attack components found. Add AttackBase components to enable combat.");
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (owner == null || owner.Health <= 0) return;
        
        // Check for stuck attacks
        HandleAttackTimeout();
        
        // Update current attack during execution
        if (isExecutingAttack && currentAttack != null)
        {
            currentAttack.UpdateDuringAttack();
        }
    }

    /// <summary>
    /// Auto-reset stuck attack execution
    /// </summary>
    private void HandleAttackTimeout()
    {
        if (isExecutingAttack)
        {
            float timeSinceAttackStart = Time.time - attackExecutionStartTime;
            if (timeSinceAttackStart > attackTimeoutDuration)
            {
                Debug.LogWarning($"[{gameObject.name}] Auto-resetting stuck attack after timeout ({timeSinceAttackStart:F2}s)");
                ForceEndAttack();
            }
        }
    }

    #endregion

    #region Attack Selection (AI/Auto)

    /// <summary>
    /// Check if any attack can be used
    /// </summary>
    public bool CanAttack()
    {
        if (owner == null || owner.Health <= 0) return false;
        if (isExecutingAttack) return false;
        
        foreach (var attack in attackComponents)
        {
            if (attack != null && attack.CanAttack())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Try to start an attack using the current selection strategy
    /// </summary>
    /// <returns>True if an attack was started</returns>
    public bool TryStartAttack()
    {
        if (isExecutingAttack) return false;
        
        AttackBase attack = SelectAttack();
        if (attack != null && attack.CanAttack())
        {
            StartAttack(attack);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Select the best attack based on current strategy
    /// </summary>
    private AttackBase SelectAttack()
    {
        if (attackComponents == null || attackComponents.Length == 0) return null;
        
        switch (selectionStrategy)
        {
            case AttackSelectionStrategy.PRIORITY:
                return SelectAttackByPriority();
            case AttackSelectionStrategy.RANDOM:
                return SelectAttackRandom();
            case AttackSelectionStrategy.DISTANCE_BASED:
                return SelectAttackByDistance();
            case AttackSelectionStrategy.ROTATION_BASED:
                return SelectAttackByRotation();
            case AttackSelectionStrategy.MANUAL:
                return selectedAttack; // Use externally selected attack
            default:
                return SelectAttackByDistance();
        }
    }

    private AttackBase SelectAttackByPriority()
    {
        foreach (var attack in attackComponents)
        {
            if (attack != null && attack.CanAttack())
            {
                return attack;
            }
        }
        return null;
    }

    private AttackBase SelectAttackRandom()
    {
        List<AttackBase> availableAttacks = new List<AttackBase>();
        foreach (var attack in attackComponents)
        {
            if (attack != null && attack.CanAttack())
            {
                availableAttacks.Add(attack);
            }
        }
        
        if (availableAttacks.Count > 0)
        {
            return availableAttacks[Random.Range(0, availableAttacks.Count)];
        }
        return null;
    }

    private AttackBase SelectAttackByDistance()
    {
        Transform target = owner.AttackTarget;
        if (target == null) return SelectAttackByPriority();
        
        float distance = Vector3.Distance(transform.position, target.position);
        AttackBase bestAttack = null;
        float bestScore = float.MaxValue;
        
        foreach (var attack in attackComponents)
        {
            if (attack == null || !attack.CanAttack()) continue;
            
            // Score based on how well the distance fits the attack range
            float midRange = (attack.minRange + attack.maxRange) / 2f;
            float score = Mathf.Abs(distance - midRange);
            
            if (score < bestScore)
            {
                bestScore = score;
                bestAttack = attack;
            }
        }
        
        return bestAttack;
    }

    private AttackBase SelectAttackByRotation()
    {
        Transform target = owner.AttackTarget;
        if (target == null) return SelectAttackByPriority();
        
        // Prefer attacks that don't require rotation (already facing target)
        foreach (var attack in attackComponents)
        {
            if (attack != null && attack.CanAttack() && !attack.ShouldRotateToAttack())
            {
                return attack;
            }
        }
        
        // Fallback to any available attack
        return SelectAttackByPriority();
    }

    #endregion

    #region Attack Execution

    /// <summary>
    /// Start a specific attack
    /// </summary>
    /// <param name="attack">The attack to execute</param>
    public void StartAttack(AttackBase attack)
    {
        if (attack == null || isExecutingAttack) return;
        
        currentAttack = attack;
        isExecutingAttack = true;
        attackExecutionStartTime = Time.time;
        lastAttackSwitchTime = Time.time;
        
        // Start the attack
        attack.StartAttack();
        
        Debug.Log($"[{gameObject.name}] Started attack: {attack.GetType().Name}");
    }

    /// <summary>
    /// Called from animation events to trigger the damage frame
    /// </summary>
    public void OnAttackDamageFrame()
    {
        if (currentAttack != null && isExecutingAttack)
        {
            currentAttack.OnAttack();
        }
    }

    /// <summary>
    /// Called from animation events when attack animation ends
    /// </summary>
    public void OnAttackEnd()
    {
        if (currentAttack != null)
        {
            currentAttack.OnAttackEnd();
        }
        
        isExecutingAttack = false;
        currentAttack = null;
        
        // Reset animation parameters
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, 0);
        }
    }

    /// <summary>
    /// Force end the current attack (used for interrupts, death, etc.)
    /// </summary>
    public void ForceEndAttack()
    {
        if (currentAttack != null)
        {
            currentAttack.OnAttackEnd();
        }
        
        isExecutingAttack = false;
        currentAttack = null;
        
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, 0);
            animator.ResetTrigger("Attack");
        }
    }

    #endregion

    #region Manual Attack Control (for Players)

    /// <summary>
    /// Set the attack to use in MANUAL mode
    /// </summary>
    /// <param name="attack">The attack to select</param>
    public void SetSelectedAttack(AttackBase attack)
    {
        selectedAttack = attack;
    }

    /// <summary>
    /// Set the attack to use by index
    /// </summary>
    /// <param name="index">Index of the attack in the attacks array</param>
    public void SetSelectedAttack(int index)
    {
        if (index >= 0 && index < attackComponents.Length)
        {
            selectedAttack = attackComponents[index];
        }
    }

    /// <summary>
    /// Start the selected attack (for MANUAL mode or player-triggered)
    /// </summary>
    /// <returns>True if attack was started</returns>
    public bool TryStartSelectedAttack()
    {
        if (selectedAttack != null)
        {
            StartAttack(selectedAttack);
            return true;
        }
        else if (attackComponents.Length > 0)
        {
            // Fallback to first attack
            StartAttack(attackComponents[0]);
            return true;
        }
        return false;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get attacks that are currently usable
    /// </summary>
    public List<AttackBase> GetAvailableAttacks()
    {
        List<AttackBase> available = new List<AttackBase>();
        foreach (var attack in attackComponents)
        {
            if (attack != null && attack.CanAttack())
            {
                available.Add(attack);
            }
        }
        return available;
    }

    /// <summary>
    /// Check if a specific attack can be used
    /// </summary>
    public bool CanUseAttack(AttackBase attack)
    {
        return attack != null && attack.CanAttack() && !isExecutingAttack;
    }

    /// <summary>
    /// Get the minimum attack range across all attacks
    /// </summary>
    public float GetMinAttackRange()
    {
        float min = float.MaxValue;
        foreach (var attack in attackComponents)
        {
            if (attack != null && attack.minRange < min)
            {
                min = attack.minRange;
            }
        }
        return min < float.MaxValue ? min : 0f;
    }

    /// <summary>
    /// Get the maximum attack range across all attacks
    /// </summary>
    public float GetMaxAttackRange()
    {
        float max = 0f;
        foreach (var attack in attackComponents)
        {
            if (attack != null && attack.maxRange > max)
            {
                max = attack.maxRange;
            }
        }
        return max;
    }

    #endregion
}

