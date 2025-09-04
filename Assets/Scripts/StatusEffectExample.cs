using UnityEngine;
using System.Collections;
using Managers;

/// <summary>
/// Example script demonstrating how to use the integrated status effect system
/// Attach to any character that implements IStatusEffectTarget to test the system
/// </summary>
public class StatusEffectExample : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private float testDuration = 5f;
    
    [Header("Test Controls")]
    [SerializeField] private bool testHealthEffects = true;
    [SerializeField] private bool testEnvironmentalEffects = true;
    [SerializeField] private bool testBuffEffects = true;
    [SerializeField] private bool testTaskEffects = true;
    
    private IStatusEffectTarget statusTarget;
    private Coroutine testRoutine;
    
    private void Start()
    {
        // Get the status effect target interface
        statusTarget = GetComponent<IStatusEffectTarget>();
        if (statusTarget == null)
        {
            Debug.LogWarning($"[StatusEffectExample] {gameObject.name} does not implement IStatusEffectTarget interface");
            return;
        }
        
        // Start tests if enabled
        if (runTestsOnStart)
        {
            testRoutine = StartCoroutine(RunStatusEffectTests());
        }
    }
    
    /// <summary>
    /// Run a series of test demonstrations
    /// </summary>
    private IEnumerator RunStatusEffectTests()
    {
        if (EffectManager.Instance == null)
        {
            Debug.LogError("[StatusEffectExample] EffectManager.Instance is null");
            yield break;
        }
        
        Debug.Log($"[StatusEffectExample] Starting status effect tests on {gameObject.name}");
        
        // Test health effects
        if (testHealthEffects)
        {
            yield return StartCoroutine(TestHealthEffects());
            yield return new WaitForSeconds(1f);
        }
        
        // Test environmental effects
        if (testEnvironmentalEffects)
        {
            yield return StartCoroutine(TestEnvironmentalEffects());
            yield return new WaitForSeconds(1f);
        }
        
        // Test buff/debuff effects
        if (testBuffEffects)
        {
            yield return StartCoroutine(TestBuffEffects());
            yield return new WaitForSeconds(1f);
        }
        
        // Test task-related effects
        if (testTaskEffects)
        {
            yield return StartCoroutine(TestTaskEffects());
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log($"[StatusEffectExample] Status effect tests completed on {gameObject.name}");
    }
    
    /// <summary>
    /// Test health-related status effects
    /// </summary>
    private IEnumerator TestHealthEffects()
    {
        Debug.Log("[StatusEffectExample] Testing health effects...");
        
        // Test hunger progression
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.HUNGRY, testDuration);
        yield return new WaitForSeconds(testDuration / 2);
        
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.HUNGRY);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.STARVING, testDuration);
        yield return new WaitForSeconds(testDuration / 2);
        
        // Test sickness
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.STARVING);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.SICK, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        // Test healing
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.SICK);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.HEALING, testDuration / 2);
        yield return new WaitForSeconds(testDuration / 2);
        
        // Return to healthy
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.HEALING);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.HEALTHY, testDuration / 2);
        yield return new WaitForSeconds(testDuration / 2);
        
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.HEALTHY);
    }
    
    /// <summary>
    /// Test environmental status effects
    /// </summary>
    private IEnumerator TestEnvironmentalEffects()
    {
        Debug.Log("[StatusEffectExample] Testing environmental effects...");
        
        // Test fire effect (with damage over time)
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.ON_FIRE, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        // Test frozen effect (with movement slow)
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.ON_FIRE);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.FROZEN, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        // Test electrocution effect
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.FROZEN);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.ELECTROCUTED, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.ELECTROCUTED);
    }
    
    /// <summary>
    /// Test buff and debuff effects
    /// </summary>
    private IEnumerator TestBuffEffects()
    {
        Debug.Log("[StatusEffectExample] Testing buff/debuff effects...");
        
        // Test buff effect
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.BUFFED, testDuration);
        yield return new WaitForSeconds(testDuration / 2);
        
        // Test speed effects
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.HASTENED, testDuration);
        yield return new WaitForSeconds(testDuration / 2);
        
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.BUFFED);
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.HASTENED);
        
        // Test debuff effects
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.DEBUFFED, testDuration);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.SLOWED, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.DEBUFFED);
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.SLOWED);
    }
    
    /// <summary>
    /// Test task-related status effects
    /// </summary>
    private IEnumerator TestTaskEffects()
    {
        Debug.Log("[StatusEffectExample] Testing task effects...");
        
        // Test working
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.WORKING, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        // Test eating
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.WORKING);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.EATING, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        // Test sleeping
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.EATING);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.SLEEPING, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        // Test medical treatment
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.SLEEPING);
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.RECEIVING_MEDICAL_TREATMENT, testDuration);
        yield return new WaitForSeconds(testDuration);
        
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.RECEIVING_MEDICAL_TREATMENT);
    }
    
    #region Manual Test Controls (Context Menu)
    
    [ContextMenu("Test Fire Effect")]
    public void TestFireEffect()
    {
        if (statusTarget != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.ON_FIRE, 5f);
        }
    }
    
    [ContextMenu("Test Sick Effect")]
    public void TestSickEffect()
    {
        if (statusTarget != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.SICK, 5f);
        }
    }
    
    [ContextMenu("Test Hungry Effect")]
    public void TestHungryEffect()
    {
        if (statusTarget != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.HUNGRY, 5f);
        }
    }
    
    [ContextMenu("Test Frozen Effect")]
    public void TestFrozenEffect()
    {
        if (statusTarget != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.FROZEN, 5f);
        }
    }
    
    [ContextMenu("Test Buff Effect")]
    public void TestBuffEffect()
    {
        if (statusTarget != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.BUFFED, 5f);
        }
    }
    
    [ContextMenu("Test Stun Effect")]
    public void TestStunEffect()
    {
        if (statusTarget != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.STUNNED, 3f);
        }
    }
    
    [ContextMenu("Clear All Effects")]
    public void ClearAllEffects()
    {
        if (statusTarget != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.RemoveAllStatusEffects(statusTarget);
        }
    }
    
    #endregion
    
    #region Runtime Test Controls
    
    /// <summary>
    /// Apply a random status effect for testing
    /// </summary>
    public void ApplyRandomStatusEffect()
    {
        if (statusTarget == null || EffectManager.Instance == null) return;
        
        var statusTypes = System.Enum.GetValues(typeof(StatusEffectType));
        var randomStatus = (StatusEffectType)statusTypes.GetValue(Random.Range(0, statusTypes.Length));
        
        EffectManager.Instance.ApplyStatusEffect(statusTarget, randomStatus, Random.Range(3f, 8f));
        Debug.Log($"[StatusEffectExample] Applied random effect: {randomStatus}");
    }
    
    /// <summary>
    /// Simulate environmental damage (fire zone)
    /// </summary>
    public void SimulateFireZone()
    {
        if (statusTarget == null || EffectManager.Instance == null) return;
        
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.ON_FIRE, 10f);
        Debug.Log("[StatusEffectExample] Simulated fire zone - character is on fire!");
    }
    
    /// <summary>
    /// Simulate magical freeze spell
    /// </summary>
    public void SimulateFreezeSpell()
    {
        if (statusTarget == null || EffectManager.Instance == null) return;
        
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.FROZEN, 8f);
        Debug.Log("[StatusEffectExample] Simulated freeze spell - character is frozen!");
    }
    
    /// <summary>
    /// Simulate healing potion
    /// </summary>
    public void SimulateHealingPotion()
    {
        if (statusTarget == null || EffectManager.Instance == null) return;
        
        // Remove negative effects
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.SICK);
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.POISONED);
        EffectManager.Instance.RemoveStatusEffect(statusTarget, StatusEffectType.BLEEDING);
        
        // Apply healing effect
        EffectManager.Instance.ApplyStatusEffect(statusTarget, StatusEffectType.HEALING, 5f);
        Debug.Log("[StatusEffectExample] Simulated healing potion - character is healing!");
    }
    
    /// <summary>
    /// Check current status effects
    /// </summary>
    public void CheckCurrentEffects()
    {
        if (statusTarget == null || EffectManager.Instance == null) return;
        
        Debug.Log("[StatusEffectExample] Checking current status effects:");
        
        var testEffects = new StatusEffectType[]
        {
            StatusEffectType.ON_FIRE, StatusEffectType.FROZEN, StatusEffectType.SICK,
            StatusEffectType.HUNGRY, StatusEffectType.TIRED, StatusEffectType.BUFFED,
            StatusEffectType.WORKING, StatusEffectType.SLEEPING
        };
        
        foreach (var effect in testEffects)
        {
            bool hasEffect = EffectManager.Instance.HasStatusEffect(statusTarget, effect);
            if (hasEffect)
            {
                Debug.Log($"  - {effect}: ACTIVE");
            }
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (testRoutine != null)
        {
            StopCoroutine(testRoutine);
        }
    }
}
