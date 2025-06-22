using UnityEngine;
using System.Collections;

public class HealthRegenMutation : BaseMutationEffect
{
    [SerializeField] private float healthRegenPerSecond = 1f;
    [SerializeField] private float regenInterval = 1f; // How often to apply the regeneration
    private float timeSinceLastRegen;
    private IDamageable damageable;
    private static int activeInstancesCount = 0;
    private Coroutine regenCoroutine;

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

        // Start the regeneration coroutine
        regenCoroutine = StartCoroutine(RegenerateHealth());
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
        damageable = null;
    }

    private System.Collections.IEnumerator RegenerateHealth()
    {
        while (isActive && damageable != null)
        {
            damageable.Heal(healthRegenPerSecond * regenInterval * ActiveInstances);
            yield return new WaitForSeconds(regenInterval);
        }
    }

    protected override void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        // HealthRegenMutation doesn't modify weapons, so we don't need to do anything
        // Just let the base class handle it
        base.HandleWeaponChange(newWeapon);
    }

    public override string GetStatsDescription()
    {
        return $"Health Regen: +{healthRegenPerSecond:F1} HP/s";
    }
} 