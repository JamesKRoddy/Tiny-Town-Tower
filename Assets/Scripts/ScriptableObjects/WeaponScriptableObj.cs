using UnityEngine;

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "Scriptable Objects/WeaponScriptableObject")]
public class WeaponScriptableObj : ResourceScriptableObj
{
    public GameObject weaponPrefab;
    public WeaponAnimationType animationType;
    public int damage = 10;
    public WeaponElement weaponElement = WeaponElement.NONE;
}
