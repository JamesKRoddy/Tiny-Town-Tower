using UnityEngine;
using Managers;

/// <summary>
/// Base class for projectile initialization parameters
/// Contains all common parameters shared by all projectile types
/// </summary>
[System.Serializable]
public class ProjectileParams
{
    // Common projectile parameters
    public float damage;
    public float poiseDamage;
    public Transform attacker;
    public AttackElement element;
    public EffectDefinition impactEffect;
    public bool createDamageArea;
    public float damageAreaRadius;
    public float damageAreaDuration;
    public bool useTriggerBasedDamage;
    public float armingDelay = 0.2f;
    public bool explodeOnAnyHit = false;

    /// <summary>
    /// Default constructor
    /// </summary>
    public ProjectileParams()
    {
        damage = 1f;
        poiseDamage = 0f;
        attacker = null;
        element = AttackElement.PHYSICAL;
        impactEffect = null;
        createDamageArea = false;
        damageAreaRadius = 0f;
        damageAreaDuration = 5f;
        useTriggerBasedDamage = false;
        armingDelay = 0.2f;
        explodeOnAnyHit = false;
    }

    /// <summary>
    /// Full constructor with all common parameters
    /// </summary>
    public ProjectileParams(float dmg, float poiseDmg, Transform attackTransform, AttackElement elem,
        EffectDefinition impactEff, bool createArea, float areaRadius, float areaDuration,
        bool triggerBased, float armingDelay = 0.2f, bool explodeOnAnyHit = false)
    {
        damage = dmg;
        poiseDamage = poiseDmg;
        attacker = attackTransform;
        element = elem;
        impactEffect = impactEff;
        createDamageArea = createArea;
        damageAreaRadius = areaRadius;
        damageAreaDuration = areaDuration;
        useTriggerBasedDamage = triggerBased;
        this.armingDelay = armingDelay;
        this.explodeOnAnyHit = explodeOnAnyHit;
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    public ProjectileParams(ProjectileParams other)
    {
        if (other == null) return;
        
        damage = other.damage;
        poiseDamage = other.poiseDamage;
        attacker = other.attacker;
        element = other.element;
        impactEffect = other.impactEffect;
        createDamageArea = other.createDamageArea;
        damageAreaRadius = other.damageAreaRadius;
        damageAreaDuration = other.damageAreaDuration;
        useTriggerBasedDamage = other.useTriggerBasedDamage;
        armingDelay = other.armingDelay;
        explodeOnAnyHit = other.explodeOnAnyHit;
    }
}

/// <summary>
/// Parameters for straight-line projectiles (bullets, lasers, arrows)
/// </summary>
[System.Serializable]
public class StraightProjectileParams : ProjectileParams
{
    public Vector3 direction;
    public float speed = 20f;

    public StraightProjectileParams() : base()
    {
        direction = Vector3.forward;
        speed = 20f;
    }

    public StraightProjectileParams(Vector3 dir, float spd, ProjectileParams baseParams) : base(baseParams)
    {
        direction = dir;
        speed = spd;
    }

    public StraightProjectileParams(Vector3 dir, float spd, float dmg, float poiseDmg, Transform attackTransform,
        AttackElement elem, EffectDefinition impactEff, bool createArea, float areaRadius,
        float areaDuration, bool triggerBased, float armingDelay = 0.2f) 
        : base(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased, armingDelay)
    {
        direction = dir;
        speed = spd;
    }
}

/// <summary>
/// Parameters for arc projectiles (vomit, grenades, mortar shells)
/// </summary>
[System.Serializable]
public class ArcProjectileParams : ProjectileParams
{
    public Vector3 targetPosition;
    public float speed = 10f;
    public float maxHeight = 5f;

    public ArcProjectileParams() : base()
    {
        targetPosition = Vector3.zero;
        speed = 10f;
        maxHeight = 5f;
    }

    public ArcProjectileParams(Vector3 targetPos, float spd, float maxHt, ProjectileParams baseParams) : base(baseParams)
    {
        targetPosition = targetPos;
        speed = spd;
        maxHeight = maxHt;
    }

    public ArcProjectileParams(Vector3 targetPos, float spd, float maxHt, float dmg, float poiseDmg, Transform attackTransform,
        AttackElement elem, EffectDefinition impactEff, bool createArea, float areaRadius,
        float areaDuration, bool triggerBased, float armingDelay = 0.2f, bool explodeOnAnyHit = false)
        : base(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased, armingDelay, explodeOnAnyHit)
    {
        targetPosition = targetPos;
        speed = spd;
        maxHeight = maxHt;
    }
}

/// <summary>
/// Parameters for homing projectiles (guided missiles, tracking spells)
/// </summary>
[System.Serializable]
public class HomingProjectileParams : ProjectileParams
{
    public Transform targetTransform;
    public float speed = 15f;
    public float homingDuration = 3f;
    public float turnSpeed = 180f;
    public bool explodeOnTimeout = true;

    public HomingProjectileParams() : base()
    {
        targetTransform = null;
        speed = 15f;
        homingDuration = 3f;
        turnSpeed = 180f;
        explodeOnTimeout = true;
    }

    public HomingProjectileParams(Transform target, float spd, float duration, float turnSpd, bool explodeOnExpiry, ProjectileParams baseParams) : base(baseParams)
    {
        targetTransform = target;
        speed = spd;
        homingDuration = duration;
        turnSpeed = turnSpd;
        explodeOnTimeout = explodeOnExpiry;
    }

    public HomingProjectileParams(Transform target, float spd, float duration, float turnSpd, bool explodeOnExpiry,
        float dmg, float poiseDmg, Transform attackTransform, AttackElement elem, EffectDefinition impactEff,
        bool createArea, float areaRadius, float areaDuration, bool triggerBased, float armingDelay = 0.2f)
        : base(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased, armingDelay)
    {
        targetTransform = target;
        speed = spd;
        homingDuration = duration;
        turnSpeed = turnSpd;
        explodeOnTimeout = explodeOnExpiry;
    }
}

