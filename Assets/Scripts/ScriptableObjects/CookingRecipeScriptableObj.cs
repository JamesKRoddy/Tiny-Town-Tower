using UnityEngine;


[CreateAssetMenu(fileName = "CookingRecipeScriptableObj", menuName = "Scriptable Objects/Camp/CookingRecipeScriptableObj")]
public class CookingRecipeScriptableObj : WorldItemBase
{
    public bool isUnlocked = false;
    public ResourceItemCount[] requiredIngredients;
    [Min(5f)]
    public float cookingTime;
    public ResourceItemCount outputFood;
    public int outputAmount = 1;
    [Min(0f)]
    public float hungerRestoreAmount = 50f; // How much hunger this meal restores
}
