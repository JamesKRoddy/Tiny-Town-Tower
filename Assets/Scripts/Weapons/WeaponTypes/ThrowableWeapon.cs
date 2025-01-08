using UnityEngine;

public class ThrowableWeapon : WeaponBase
{
    [Header("Throwable Weapon Stats")]
    public GameObject throwablePrefab;
    public float throwForce = 10f;

    public override void OnEquipped(Transform character)
    {
        Debug.Log("UNIMPLEMENTED FUNCTION");
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
        damageHandler.SetDamage(weaponScriptableObj.damage);
    }
}

public class ThrowableCollisionHandler : MonoBehaviour
{
    private float damage;

    public void SetDamage(float damageAmount)
    {
        damage = damageAmount;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var target = collision.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            Debug.Log($"{collision.collider.name} took {damage} damage!");
        }

        // Destroy the throwable object after impact
        Destroy(gameObject);
    }
}
