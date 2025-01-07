using UnityEngine;

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "Scriptable Objects/WeaponScriptableObject")]
public class WeaponScriptableObj : ResourceScriptableObj //TODO create a bunch of different types of scriptable weapons eg. MeleeWeapon, RangeedWeapon
{
    public GameObject weaponPrefab;
    public WeaponAnimationType animationType;
    public int damage = 10;
    public WeaponElement weaponElement = WeaponElement.NONE;
}
