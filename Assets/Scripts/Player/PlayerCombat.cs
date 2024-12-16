using UnityEngine;

public class PlayerCombat : CharacterCombat
{
    [Header("Dash VFX")]
    public DashVFX dashVfx;
    public Transform dashVfxPoint;

    public override void Start()
    {
        base.Start();
    }

    public void DashVFX()
    {
        dashVfx.Play(PlayerInventory.Instance.dashElement, dashVfxPoint);
    }
}


public enum GeneticMutation
{
    NONE
}
