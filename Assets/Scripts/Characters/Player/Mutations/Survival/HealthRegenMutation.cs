using UnityEngine;

public class HealthRegenMutation : BaseMutationEffect
{
    [SerializeField] private float healthRegenPerSecond = 1f;
    [SerializeField] private float regenInterval = 1f; // How often to apply the regeneration
    private float timeSinceLastRegen;
    private IDamageable damageable;
    private static int activeInstancesCount = 0;

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    protected override void ApplyEffect()
    {
        if (!isActive) return;

        ActiveInstances++;
        // Get the damageable component from the possessed NPC
        damageable = PlayerController.Instance._possessedNPC.GetTransform().GetComponent<IDamageable>();
        if (damageable == null)
        {
            Debug.LogError("No IDamageable component found on possessed NPC!");
            return;
        }
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        damageable = null;
    }

    private void Update()
    {
        if (!isActive || damageable == null) return;

        timeSinceLastRegen += Time.deltaTime;
        if (timeSinceLastRegen >= regenInterval)
        {
            timeSinceLastRegen = 0f;
            damageable.Heal(healthRegenPerSecond * regenInterval * ActiveInstances);
        }
    }
} 