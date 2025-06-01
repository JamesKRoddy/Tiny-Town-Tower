using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DamageArea : MonoBehaviour
{
    private float damage;
    private float damageInterval = 1f; // Time between damage applications
    private float lastDamageTime; // When we last applied damage

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<IDamageable>() != null)
        {
            // Only apply damage if enough time has passed since last damage
            if (Time.time >= lastDamageTime + damageInterval)
            {
                other.gameObject.GetComponent<IDamageable>().TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void SetDamageInterval(float interval)
    {
        damageInterval = interval;
    }
}

