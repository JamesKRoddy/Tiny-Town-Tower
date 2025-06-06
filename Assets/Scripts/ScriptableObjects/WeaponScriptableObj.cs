using UnityEngine;

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "Scriptable Objects/Roguelite/WeaponScriptableObject")]
public class WeaponScriptableObj : ResourceScriptableObj
{
    public WeaponAnimationType animationType;
    public int damage = 10;
    public WeaponElement weaponElement = WeaponElement.NONE;
    public float attackSpeed = 1f;
}
