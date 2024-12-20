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

    internal void Play(WeaponElement element)
    {
        //Adding 1 to compensate for NONE in enum
        meleeVfx[(int)element].Play();
    }

    internal void Stop(WeaponElement element)
    {
        meleeVfx[(int)element].Stop();
    }
}
