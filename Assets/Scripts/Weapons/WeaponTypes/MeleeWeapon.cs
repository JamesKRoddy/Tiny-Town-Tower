using UnityEngine;
using System.Collections.Generic;

public class MeleeWeapon : WeaponBase
{
    [Header("Melee Weapon Stats")]
    public LayerMask targetLayer;
    public float boxCastDistance = 2f; // Distance in front of the player for the BoxCast
    public Vector3 boxSize = new Vector3(1f, 1f, 1f); // Public size of the box for visualization and adjustment
    public Vector3 boxOffset = new Vector3(0f, 1f, 0f); // Offset for the box origin (relative to player)

    [Header("Projectile Reflection")]
    [Tooltip("Enable separate, larger box cast for projectile detection (more forgiving for fast projectiles)")]
    public bool enableProjectileDetection = true;
    [Tooltip("Size multiplier for projectile detection box XZ (horizontal reach)")]
    public float projectileBoxSizeMultiplier = 3f;
    [Tooltip("Size multiplier for projectile detection box Y (vertical reach - higher for arc projectiles)")]
    public float projectileBoxHeightMultiplier = 5f;

    private bool isAttacking = false;
    private HashSet<Collider> hitTargets = new HashSet<Collider>();

    public override void OnEquipped(Transform character)
    {
        // Store character transform in base class
        base.OnEquipped(character);

        if (characterTransform == null)
        {
            Debug.LogError("Character Transform is null! Ensure the OnEquipped function receives a valid Transform.");
            return;
        }
    }

    public override void Use()
    {
        if (characterTransform == null)
        {
            Debug.LogError("Character Transform is not set!");
            return;
        }

        isAttacking = true;
        hitTargets.Clear(); // Clear hit tracking for this attack
    }

    public override void StopUse()
    {
        isAttacking = false;
    }

    private void FixedUpdate()
    {
        if (isAttacking)
        {
            PerformBoxCast();
        }
    }

    private void PerformBoxCast()
    {
        // Calculate the BoxCast origin, applying the offset relative to the character's local space
        Vector3 boxOrigin = characterTransform.position +
                            characterTransform.forward * boxCastDistance +
                            characterTransform.TransformDirection(boxOffset);

        Vector3 boxDirection = characterTransform.forward;

        // First, check for projectiles with a larger, more forgiving detection box
        // Y is scaled more to catch arc projectiles flying overhead
        if (enableProjectileDetection)
        {
            Vector3 projectileBoxSize = new Vector3(
                boxSize.x * projectileBoxSizeMultiplier,
                boxSize.y * projectileBoxHeightMultiplier,
                boxSize.z * projectileBoxSizeMultiplier
            );
            DamageUtils.PerformProjectileReflectionDetection(
                this,
                boxOrigin,
                projectileBoxSize,
                characterTransform.rotation,
                hitTargets
            );
        }

        // Then perform the normal box cast for damageable targets
        int targetsHit = DamageUtils.PerformBoxCastDamage(
            this,
            boxOrigin,
            boxSize,
            boxDirection,
            characterTransform.rotation,
            0f, // Distance (0 for immediate area)
            targetLayer,
            hitTargets
        );
    }

    private void OnDrawGizmos()
    {
        if (characterTransform == null) return;

        // Calculate the BoxCast origin
        Vector3 boxOrigin = characterTransform.position +
                            characterTransform.forward * boxCastDistance +
                            characterTransform.TransformDirection(boxOffset);

        // Draw projectile detection box (larger, more forgiving) in yellow if enabled
        if (enableProjectileDetection)
        {
            Vector3 projectileBoxSize = new Vector3(
                boxSize.x * projectileBoxSizeMultiplier,
                boxSize.y * projectileBoxHeightMultiplier,
                boxSize.z * projectileBoxSizeMultiplier
            );
            DamageUtils.DrawBoxCastGizmo(boxOrigin, projectileBoxSize, characterTransform.rotation, Color.yellow);
        }

        // Draw main damage box in red
        DamageUtils.DrawBoxCastGizmo(boxOrigin, boxSize, characterTransform.rotation, Color.red);
    }
}
