using UnityEngine;


[CreateAssetMenu(fileName = "CookingRecipeScriptableObj", menuName = "Scriptable Objects/Camp/CookingRecipeScriptableObj")]
public class CookingRecipeScriptableObj : ScriptableObject
{
    public string objectName;
    public string description;
    public Sprite sprite;
    public bool isUnlocked = false;
    public ResourceItemCount[] requiredIngredients;
    public float cookingTime;
    public ResourceScriptableObj outputFood;
    public int outputAmount = 1;
}
