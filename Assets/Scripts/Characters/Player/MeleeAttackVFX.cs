using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeleeAttackVFX : MonoBehaviour
{
    [SerializeField] ParticleSystem[] meleeVfx;

    private void Start()
    {
        meleeVfx = transform.GetComponentsInChildren<ParticleSystem>()
                        .Where(ps => ps.transform.parent == transform)
                        .ToArray();
    }

    internal void Play(AttackElement element, float attackSpeed = 1f)
    {
        //Adding 1 to compensate for NONE in enum
        var ps = meleeVfx[(int)element];
        var main = ps.main;
        main.simulationSpeed = attackSpeed;
        ps.Play();
    }

    internal void Stop(AttackElement element)
    {
        meleeVfx[(int)element].Stop();
    }
}
