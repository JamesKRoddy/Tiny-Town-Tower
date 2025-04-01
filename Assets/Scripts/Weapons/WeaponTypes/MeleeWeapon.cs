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
    private Transform characterTransform; //Used for the box trigger for hitting

    public override void OnEquipped(Transform character)
    {
        // Initialize the character's Transform
        characterTransform = character;

        if (characterTransform == null)
        {
            Debug.LogError("Character Transform is null! Ensure the OnEquipped function receives a valid Transform.");
            return;
        }

        Debug.Log("MeleeWeapon equipped for character: " + characterTransform.name);
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
        Debug.Log("Attack started.");
    }

    public override void StopUse()
    {
        isAttacking = false;
        Debug.Log("Attack stopped.");
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

        // Perform the BoxCast
        RaycastHit[] hits = Physics.BoxCastAll(boxOrigin, boxSize * 0.5f, boxDirection, characterTransform.rotation, 0, targetLayer);

        foreach (RaycastHit hit in hits)
        {
            if (!hitTargets.Contains(hit.collider))
            {
                hitTargets.Add(hit.collider);

                var target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    Debug.Log($"Hit target: {hit.collider.name}");
                    target.TakeDamage(WeaponData.damage);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (characterTransform == null) return;

        // Visualize the BoxCast in the Scene view
        Gizmos.color = Color.red;

        Vector3 boxOrigin = characterTransform.position +
                            characterTransform.forward * boxCastDistance +
                            characterTransform.TransformDirection(boxOffset);

        Gizmos.matrix = Matrix4x4.TRS(boxOrigin, characterTransform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }
}
