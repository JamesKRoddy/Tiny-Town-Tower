using UnityEngine;
using System.Collections.Generic;

public class MeleeWeapon : WeaponBase
{
    [Header("Melee Weapon Stats")]
    public LayerMask targetLayer;
    public float boxCastDistance = 2f; // Distance in front of the player for the BoxCast
    public Vector3 boxSize = new Vector3(1f, 1f, 1f); // Public size of the box for visualization and adjustment
    public Vector3 boxOffset = new Vector3(0f, 1f, 0f); // Offset for the box origin (relative to player)

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

        // Use the unified box cast damage system
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

        // Use the unified gizmo drawing utility
        DamageUtils.DrawBoxCastGizmo(boxOrigin, boxSize, characterTransform.rotation, Color.red);
    }
}
