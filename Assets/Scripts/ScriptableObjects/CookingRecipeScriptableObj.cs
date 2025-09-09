using UnityEngine;


[CreateAssetMenu(fileName = "CookingRecipeScriptableObj", menuName = "Scriptable Objects/Camp/CookingRecipeScriptableObj")]
public class CookingRecipeScriptableObj : CraftableScriptableObj
{
    [Min(0f)]
    public float hungerRestoreAmount = 50f; // How much hunger this meal restores
}
