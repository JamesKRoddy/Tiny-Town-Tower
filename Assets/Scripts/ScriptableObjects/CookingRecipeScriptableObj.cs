using UnityEngine;


[CreateAssetMenu(fileName = "CookingRecipeScriptableObj", menuName = "Scriptable Objects/Camp/CookingRecipeScriptableObj")]
public class CookingRecipeScriptableObj : WorldItemBase
{
    public bool isUnlocked = false;
    public ResourceItemCount[] requiredIngredients;
    public float cookingTime;
    public ResourceScriptableObj outputFood;
    public int outputAmount = 1;
}
