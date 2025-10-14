using UnityEngine;

public class ThrowableWeapon : WeaponBase
{
    [Header("Throwable Weapon Stats")]
    public GameObject throwablePrefab;
    public float throwForce = 10f;

    public override void OnEquipped(Transform character)
    {
        // Store character transform in base class
        base.OnEquipped(character);
    }

    public override void StopUse()
    {
        throw new System.NotImplementedException();
    }

    public override void Use()
    {
        Throw();
    }

    private void Throw()
    {
        if (throwablePrefab == null)
        {
            Debug.LogError("Throwable prefab is not assigned!");
            return;
        }

        GameObject throwableInstance = Instantiate(throwablePrefab, transform.position, transform.rotation);
        Rigidbody rb = throwableInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(transform.forward * throwForce, ForceMode.VelocityChange);
        }

        // Add collision handler to deal damage
        var damageHandler = throwableInstance.AddComponent<ThrowableCollisionHandler>();
        damageHandler.SetWeaponData(this, characterTransform);
    }
}

public class ThrowableCollisionHandler : MonoBehaviour
{
    private WeaponBase weaponData;
    private Transform characterTransform; // Store who threw the projectile

    public void SetWeaponData(WeaponBase weapon, Transform character)
    {
        weaponData = weapon;
        characterTransform = character;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var target = collision.collider.GetComponent<IDamageable>();
        if (target != null && weaponData != null)
        {
            // Use the weapon's elemental damage system, passing the character's transform for proper camera shake
            weaponData.DealDamage(target, characterTransform);
            Debug.Log($"{collision.collider.name} took {weaponData.GetTotalDamage()} damage!");
        }

        // Destroy the throwable object after impact
        Destroy(gameObject);
    }
}
