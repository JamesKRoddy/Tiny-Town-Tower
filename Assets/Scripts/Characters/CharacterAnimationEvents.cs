using UnityEngine;

public class CharacterAnimationEvents : MonoBehaviour
{
    public CharacterCombat characterCombat;

    private void Start()
    {
        characterCombat = GetComponentInParent<CharacterCombat>();
    }

    public void AttackVFX(int attackDirection)
    {
        if(characterCombat != null)
            characterCombat.AttackVFX(attackDirection);
    }
}
