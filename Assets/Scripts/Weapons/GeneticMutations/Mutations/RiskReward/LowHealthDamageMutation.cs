using UnityEngine;

public class LowHealthDamageMutation : BaseMutationEffect
{
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private float healthThreshold = 0.3f; // 30% health threshold

    private IDamageable damageable;
    private bool isLowHealth;
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
        // Get the required components from the possessed NPC
        Transform npcTransform = PlayerController.Instance._possessedNPC.GetTransform();
        damageable = npcTransform.GetComponent<IDamageable>();

        if (characterInventory == null || damageable == null)
        {
            Debug.LogError("Required components not found on possessed NPC!");
            return;
        }

        // Subscribe to damage taken event
        if (damageable is HumanCharacterController humanController)
        {
            humanController.OnDamageTaken += OnDamageTaken;
            humanController.OnHeal += OnDamageTaken; // Reuse the same handler since the logic is identical
        }
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        if (isLowHealth)
        {
            ApplyWeaponModifiers(damageMultiplier);
        }
        else
        {
            ApplyWeaponModifiers(1f); // Restore original damage
        }

        // Unsubscribe from damage taken event
        if (damageable is HumanCharacterController humanController)
        {
            humanController.OnDamageTaken -= OnDamageTaken;
            humanController.OnHeal -= OnDamageTaken;
        }

        damageable = null;
        isLowHealth = false;
    }

    private void OnDamageTaken(float currentHealth, float maxHealth)
    {
        if (!isActive || damageable == null || characterInventory == null) return;

        float currentHealthPercent = currentHealth / maxHealth;
        bool wasLowHealth = isLowHealth;
        isLowHealth = currentHealthPercent <= healthThreshold;

        // Only update weapon when crossing the threshold
        if (wasLowHealth != isLowHealth)
        {
            if (isLowHealth)
            {
                ApplyWeaponModifiers(damageMultiplier);
            }
            else
            {
                ApplyWeaponModifiers(1f); // Restore original damage
            }
        }
    }

    protected override void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        if (isActive)
        {
            // Reapply the effect
            base.HandleWeaponChange(newWeapon);
        }
    }
} 