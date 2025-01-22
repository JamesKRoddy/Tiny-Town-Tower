using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UpgradeData
{
    public float rangeIncrease = 2f;
    public float fireRateIncrease = 0.5f;
    public int upgradeCost = 50;
    public float upgradeTime = 5f;
    public int costIncrease = 50;
    public string description = "Upgrade the turret for increased stats.";
}

[RequireComponent(typeof(Collider))]
public abstract class BaseTurret : MonoBehaviour
{
    [Header("Turret Settings")]
    public float range = 10f;
    public float fireRate = 1f;
    public Transform turretTop;
    public Transform firePoint;
    public UpgradeData upgradeData;

    private float fireCooldown = 0f;
    private Transform target;
    private List<Transform> enemiesInRange = new List<Transform>();
    private bool isUpgrading = false;

    private void Start()
    {
        //upgradeButton.onClick.AddListener(StartUpgrade);
    }

    private void Update()
    {
        if (isUpgrading) return;

        UpdateTarget();
        if (target != null)
        {
            RotateToTarget();
            HandleFiring();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Remove(other.transform);
            if (target == other.transform)
                target = null;
        }
    }

    private void UpdateTarget()
    {
        if (target == null || !enemiesInRange.Contains(target))
        {
            target = null;
            float shortestDistance = Mathf.Infinity;
            foreach (var enemy in enemiesInRange)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, enemy.position);
                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    target = enemy;
                }
            }
        }
    }

    private void RotateToTarget()
    {
        Vector3 direction = target.position - turretTop.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 rotation = Quaternion.Lerp(turretTop.rotation, lookRotation, Time.deltaTime * 5f).eulerAngles;
        turretTop.rotation = Quaternion.Euler(0f, rotation.y, 0f);
    }

    private void HandleFiring()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            Fire();
            fireCooldown = 1f / fireRate;
        }
    }

    protected abstract void Fire();

    private void StartUpgrade()
    {
        if (isUpgrading || !CanAffordUpgrade()) return;

        DeductGold(upgradeData.upgradeCost);
        StartCoroutine(UpgradeRoutine());
    }

    private IEnumerator UpgradeRoutine()
    {
        isUpgrading = true;
        //upgradeButton.interactable = false;
        //upgradeButtonText.text = "Upgrading...";

        yield return new WaitForSeconds(upgradeData.upgradeTime);

        UpgradeTurret();
        isUpgrading = false;
        //upgradeButton.interactable = true;
        //upgradeButtonText.text = "Upgrade";
    }

    protected virtual void UpgradeTurret()
    {
        range += upgradeData.rangeIncrease;
        fireRate += upgradeData.fireRateIncrease;
        upgradeData.upgradeCost += upgradeData.costIncrease;
    }

    protected virtual bool CanAffordUpgrade()
    {
        //TODO figure out cost system, maybe use specific resources to upgrade specific turrets eg. electric components for tech turrets
        return true;
        //return GoldManager.Instance.HasEnoughGold(upgradeData.upgradeCost);
    }

    protected virtual void DeductGold(int amount)
    {
        //TODO figure out cost system, maybe use specific resources to upgrade specific turrets eg. electric components for tech turrets
        //GoldManager.Instance.DeductGold(amount);
    }

    internal void SetupTurret()
    {
        throw new NotImplementedException();
    }
}
