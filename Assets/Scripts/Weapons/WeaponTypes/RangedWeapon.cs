using UnityEngine;

public class RangedWeapon : WeaponBase
{
    [Header("Ranged Weapon Stats")]
    public float range = 50f;
    public float fireRate = 0.5f;
    public int maxAmmo = 30;
    public int currentAmmo;

    [Header("References")]
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;

    private float nextFireTime = 0f;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    public override void OnEquipped(Transform character)
    {
        Debug.Log("UNIMPLEMENTED FUNCTION");
    }

    public override void Use()
    {
        if (Time.time >= nextFireTime)
        {
            Fire();
        }
    }

    private void Fire()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        currentAmmo--;
        nextFireTime = Time.time + fireRate;

        Shoot();
    }

    private void Shoot()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, range))
        {
            Debug.Log($"Hit {hit.collider.name}!");

            var target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(WeaponData.damage);
            }

            if (impactEffect != null)
            {
                Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }

    public void Reload()
    {
        currentAmmo = maxAmmo;
    }

    public override void StopUse()
    {
        throw new System.NotImplementedException();
    }
}
