using UnityEngine;

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "Scriptable Objects/Roguelite/WeaponScriptableObject")]
public class WeaponScriptableObj : ResourceScriptableObj //TODO create a bunch of different types of scriptable weapons eg. MeleeWeapon, RangeedWeapon
{
    public WeaponAnimationType animationType;
    public int damage = 10;
    public WeaponElement weaponElement = WeaponElement.NONE;
}
