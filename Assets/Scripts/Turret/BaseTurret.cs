using System.Collections.Generic;
using UnityEngine;
using Enemies;
using Managers;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class BaseTurret : PlaceableStructure<TurretScriptableObject>
{
    [Header("Turret Settings")]
    public float damage = 10f;
    public float range = 10f; //TODO use this for the size of the collider
    public float fireRate = 1f;
    public float turretTurnSpeed = 5f;
    public Transform turretTop;
    public Transform firePoint;

    private float fireCooldown = 0f;
    private EnemyBase target;
    private List<EnemyBase> enemiesInRange = new List<EnemyBase>();

    protected override void Start()
    {
        base.Start();
        
        // Register with CampManager for target tracking
        if (CampManager.Instance != null)
        {
            CampManager.Instance.RegisterTarget(this);
        }
    }

    private void Update()
    {
        if (enemiesInRange.Count > 0)
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
        if (target == null || turretTop == null) return;
        
        // Use centralized rotation utility for turret rotation
        NavigationUtils.RotateTowardsTarget(turretTop, target.transform, turretTurnSpeed, true);
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

    public virtual void SetupStructure(TurretScriptableObject scriptableObj)
    {
        base.SetupStructure(scriptableObj);
        
        // Set turret stats from scriptable object
        if (scriptableObj != null)
        {
            damage = scriptableObj.damage;
            range = scriptableObj.range;
            fireRate = scriptableObj.fireRate;
            turretTurnSpeed = scriptableObj.turretTurnSpeed;
        }
    }

    protected override void OnStructureSetup()
    {
        // Base class handles repair and upgrade task setup
        base.OnStructureSetup();
    }

    public override string GetInteractionText()
    {
        if (IsUnderConstruction()) return "Turret under construction";
        if (!IsOperational()) return "Turret not operational";
        
        string text = "Turret Options:\n";
        if (repairTask != null && repairTask.CanPerformTask())
            text += "- Repair\n";
        if (upgradeTask != null && upgradeTask.CanPerformTask())
            text += "- Upgrade\n";
        if (CanUpgrade())
            text += "- Upgrade to Next Level\n";
        text += $"- Damage: {damage}\n";
        text += $"- Range: {range}\n";
        text += $"- Fire Rate: {fireRate}\n";
        return text;
    }

    public string GetTurretStatsText()
    {
        string stats = $"Turret Stats:\n\n";
        stats += $"Health: {GetCurrentHealth():F0}/{GetMaxHealth():F0} ({(GetHealthPercentage() * 100):F1}%)\n";
        stats += $"Damage: {damage:F1}\n";
        stats += $"Range: {range:F1}\n";
        stats += $"Fire Rate: {fireRate:F1} shots/sec\n";
        stats += $"Turn Speed: {turretTurnSpeed:F1}\n";
        stats += $"Status: {(IsOperational() ? "Operational" : "Not Operational")}\n";
        
        if (IsUnderConstruction())
        {
            stats += $"Construction: Under Construction\n";
        }
        
        if (StructureScriptableObj != null)
        {
            stats += $"\nTurret Type: {StructureScriptableObj.objectName}\n";
            if (StructureScriptableObj.upgradeTarget != null)
            {
                stats += $"Can Upgrade To: {StructureScriptableObj.upgradeTarget.objectName}\n";
            }
        }
        
        return stats;
    }

    public override void StartDestruction()
    {
        base.StartDestruction();
    }
} 