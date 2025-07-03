using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enemies;
using Managers;

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
public abstract class BaseTurret : MonoBehaviour, IDamageable
{
    [Header("Turret Settings")]
    public float damage = 10f;
    public float range = 10f; //TODO use this for the size of the collider
    public float fireRate = 1f;
    public float turretTurnSpeed = 5f;
    public Transform turretTop;
    public Transform firePoint;
    public UpgradeData upgradeData;

    [Header("Turret Configuration")]
    [SerializeField] protected TurretScriptableObject turretScriptableObj;

    [Header("Health System")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;

    // Events for damage system
    public event Action<float, float> OnDamageTaken;
    public event Action<float, float> OnHeal;
    public event Action OnDeath;

    private float fireCooldown = 0f;
    private EnemyBase target;
    private List<EnemyBase> enemiesInRange = new List<EnemyBase>();
    private bool isUpgrading = false;

    private void Start()
    {
        //upgradeButton.onClick.AddListener(StartUpgrade);
        
        // Register with CampManager for target tracking
        if (Managers.CampManager.Instance != null)
        {
            Managers.CampManager.Instance.RegisterTarget(this);
        }
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

    public void SetTurretScriptableObject(TurretScriptableObject turretSO)
    {
        turretScriptableObj = turretSO;
    }

    protected virtual void OnDestroy()
    {
        // Unregister from CampManager target tracking
        if (Managers.CampManager.Instance != null)
        {
            Managers.CampManager.Instance.UnregisterTarget(this);
        }
        
        // Free up grid slots when turret is destroyed
        if (turretScriptableObj != null && Managers.CampManager.Instance != null)
        {
            Managers.CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, turretScriptableObj.size);
        }
    }

    #region IDamageable Interface Implementation

    public float Health 
    { 
        get => health; 
        set => health = Mathf.Clamp(value, 0, maxHealth); 
    }
    
    public float MaxHealth 
    { 
        get => maxHealth; 
        set => maxHealth = value; 
    }
    
    public CharacterType CharacterType => CharacterType.NONE;
    
    public Allegiance GetAllegiance() => Allegiance.FRIENDLY;

    public virtual void TakeDamage(float amount, Transform damageSource = null)
    {
        float previousHealth = health;
        health = Mathf.Max(0, health - amount);
        
        OnDamageTaken?.Invoke(amount, health);
        
        // Play hit VFX
        Vector3 hitPoint = transform.position + Vector3.up * 1.5f;
        Vector3 hitNormal = damageSource != null 
            ? (transform.position - damageSource.position).normalized 
            : Vector3.up;
        EffectManager.Instance?.PlayHitEffect(hitPoint, hitNormal, this);
        
        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        float previousHealth = health;
        health = Mathf.Min(maxHealth, health + amount);
        
        OnHeal?.Invoke(amount, health);
    }

    public virtual void Die()
    {
        Debug.Log($"{gameObject.name} turret has been destroyed!");
        OnDeath?.Invoke();
        
        // Notify all enemies that this turret was destroyed
        EnemyBase.NotifyTargetDestroyed(transform);
        
        // Destroy the turret
        Destroy(gameObject);
    }

    #endregion
}
