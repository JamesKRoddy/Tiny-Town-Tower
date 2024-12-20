using UnityEngine;

public class MeleeWeapon : WeaponBase
{
    [Header("Melee Weapon Stats")]
    public float attackRange = 2f;

    public override void OnEquipped()
    {
        Debug.Log("UNIMPLEMENTED FUNCTION");
    }

    public override void Use()
    {
        PerformAttack();
    }

    private void PerformAttack()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange))
        {
            Debug.Log($"Hit {hit.collider.name}!");

            var target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(weaponScriptableObj.damage);
            }
        }
    }
}
