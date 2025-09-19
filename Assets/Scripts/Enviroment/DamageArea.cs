using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DamageArea : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private float poiseDamage;
    [SerializeField] private float damageInterval = 1f; // Time between damage applications
    private Dictionary<IDamageable, float> lastDamageTimes = new Dictionary<IDamageable, float>(); // Track damage times per IDamageable

    void OnTriggerStay(Collider other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Check if we have a record for this damageable
            if (!lastDamageTimes.ContainsKey(damageable))
            {
                lastDamageTimes[damageable] = 0f; // Initialize with 0 so it can take damage immediately
            }
            
            // Only apply damage if enough time has passed since last damage for this specific damageable
            if (Time.time >= lastDamageTimes[damageable] + damageInterval)
            {
                damageable.TakeDamage(damage, poiseDamage);
                lastDamageTimes[damageable] = Time.time;
            }
        }
    }

    public void SetDamage(float dmg, float pdm)
    {
        damage = dmg;
        poiseDamage = pdm;
    }

    public void SetDamageInterval(float interval)
    {
        damageInterval = interval;
    }

    void OnTriggerExit(Collider other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (damageable != null && lastDamageTimes.ContainsKey(damageable))
        {
            // Remove the damageable from our tracking when it leaves the trigger
            lastDamageTimes.Remove(damageable);
        }
    }
}

