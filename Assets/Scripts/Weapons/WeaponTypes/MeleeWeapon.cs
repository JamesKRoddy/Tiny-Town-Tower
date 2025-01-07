using UnityEngine;
using System.Collections.Generic;

public class MeleeWeapon : WeaponBase
{
    [Header("Melee Weapon Stats")]
    public LayerMask targetLayer;

    private bool isAttacking = false;
    private HashSet<Collider> hitTargets = new HashSet<Collider>();

    public override void OnEquipped()
    {
        // Cache components
        Collider collider = GetComponent<Collider>();
        Rigidbody rigidbody = GetComponent<Rigidbody>();

        // Ensure components exist
        if (collider == null || rigidbody == null)
        {
            Debug.LogError("OnEquipped failed: Missing required components (Collider or Rigidbody).");
            return;
        }

        // Configure Collider and Rigidbody
        collider.isTrigger = true;
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }


    public override void Use()
    {
        // Begin the attack phase
        isAttacking = true;
        hitTargets.Clear();

        // Optionally set a timed window to automatically disable the attack after a duration
        Invoke(nameof(StopUse), 0.5f); // Safety just to make sure the collider is disabled
    }

    public override void StopUse()
    {
        // End the attack phase
        isAttacking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isAttacking) return;
        // Check if the collider is in the target layer
        if ((1 << other.gameObject.layer & targetLayer) != 0)
        {
            if (!hitTargets.Contains(other))
            {
                hitTargets.Add(other);

                var target = other.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(weaponScriptableObj.damage);
                    Debug.Log($"Hit {other.name}!");
                }
            }
        }
    }
}
