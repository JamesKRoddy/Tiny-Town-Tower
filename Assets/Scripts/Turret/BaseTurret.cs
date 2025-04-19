using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enemies;

[System.Serializable]
public class UpgradeData
{
    public float damageIncrease = 5;
    public float rangeIncrease = 2f;
    public float fireRateIncrease = 0.5f;
    public float turretTurnSpeedIncrease = 0.25f;
    public int upgradeCost = 50;
    public float upgradeTime = 5f;
    public int costIncrease = 50;
    public string description = "Upgrade the turret for increased stats.";
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class BaseTurret : MonoBehaviour
{
    [Header("Turret Settings")]
    public float damage = 10f;
    public float range = 10f; //TODO use this for the size of the collider
    public float fireRate = 1f;
    public float turretTurnSpeed = 5f;
    public Transform turretTop;
    public Transform firePoint;
    public UpgradeData upgradeData;

    private float fireCooldown = 0f;
    private EnemyBase target;
    private List<EnemyBase> enemiesInRange = new List<EnemyBase>();
    private bool isUpgrading = false;

    private void Start()
    {
        //upgradeButton.onClick.AddListener(StartUpgrade);
    }

    private void Update()
    {
        if (isUpgrading) return;

        if(enemiesInRange.Count > 0 )
            UpdateTarget();

        if (target != null)
        {
            RotateToTarget();
            HandleFiring();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out EnemyBase enemy))
        {
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out EnemyBase enemy))
        {
            enemiesInRange.Remove(enemy);
            if (target == other.GetComponent<EnemyBase>())
                target = null;
        }
    }

    private void UpdateTarget()
    {
        if (target == null || !enemiesInRange.Contains(target))
        {
            enemiesInRange.RemoveAll(enemy => enemy == null);

            float shortestDistance = Mathf.Infinity;
            foreach (var enemy in enemiesInRange)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
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
        Vector3 direction = target.transform.position - turretTop.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 rotation = Quaternion.Lerp(turretTop.rotation, lookRotation, Time.deltaTime * turretTurnSpeed).eulerAngles;
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

        DeductResource(upgradeData.upgradeCost);
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

    protected virtual void DeductResource(int amount) 
    {
        //TODO figure out cost system, maybe use specific resources to upgrade specific turrets eg. electric components for tech turrets
        //GoldManager.Instance.DeductGold(amount);
    }

    internal void SetupTurret()
    {
        //TODO setup turret
    }
}
