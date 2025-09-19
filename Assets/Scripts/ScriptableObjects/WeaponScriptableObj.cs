using UnityEngine;

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "Scriptable Objects/Roguelite/WeaponScriptableObject")]
public class WeaponScriptableObj : ResourceScriptableObj
{
    public WeaponAnimationType animationType;
    public int damage = 10;
    public float poiseDamage = 10f; // Poise damage dealt by this weapon
    public WeaponElement weaponElement = WeaponElement.NONE;
    public float attackSpeed = 1f;
}
