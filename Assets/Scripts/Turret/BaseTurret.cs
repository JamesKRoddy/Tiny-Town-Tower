using System.Collections.Generic;
using UnityEngine;
using Enemies;
using Managers;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class BaseTurret : PlaceableStructure
{
    [Header("Turret Settings")]
    public float damage = 10f;
    public float range = 10f; //TODO use this for the size of the collider
    public float fireRate = 1f;
    public float turretTurnSpeed = 5f;
    public Transform turretTop;
    public Transform firePoint;

    [Header("Turret Configuration")]
    [SerializeField] protected TurretScriptableObject turretScriptableObj;

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

    public void SetupTurret(TurretScriptableObject turretSO)
    {
        this.turretScriptableObj = turretSO;
        
        // Set turret stats from scriptable object
        damage = turretSO.damage;
        range = turretSO.range;
        fireRate = turretSO.fireRate;
        turretTurnSpeed = turretSO.turretTurnSpeed;
        
        base.SetupStructure(turretSO);
    }

    protected override void OnStructureSetup()
    {
        // Additional turret-specific setup can be done here
        if (turretScriptableObj != null)
        {
            // Set turret stats from scriptable object
            damage = turretScriptableObj.damage;
            range = turretScriptableObj.range;
            fireRate = turretScriptableObj.fireRate;
            turretTurnSpeed = turretScriptableObj.turretTurnSpeed;
        }
    }

    public void SetTurretScriptableObject(TurretScriptableObject turretSO)
    {
        turretScriptableObj = turretSO;
    }

    public override string GetInteractionText()
    {
        if (IsUnderConstruction()) return "Turret under construction";
        if (!IsOperational()) return "Turret not operational";
        
        string text = "Turret Options:\n";
        if (CanUpgrade())
            text += "- Upgrade to Next Level\n";
        text += $"- Damage: {damage}\n";
        text += $"- Range: {range}\n";
        text += $"- Fire Rate: {fireRate}\n";
        return text;
    }

    public TurretScriptableObject GetTurretScriptableObject()
    {
        return turretScriptableObj;
    }
} 