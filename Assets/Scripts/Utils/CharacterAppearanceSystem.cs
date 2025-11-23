using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reusable appearance system for procedurally generating character looks.
/// Can be used by NPCs, enemies, or any character that needs randomized appearance.
/// 
/// How to use:
/// 1. Add [SerializeField] CharacterAppearanceSystem appearanceSystem to your character class
/// 2. Call appearanceSystem.Initialize(transform) in Awake/Start
/// 3. Call appearanceSystem.RandomizeAppearance() to generate random look
/// 4. Use appearanceSystem.GetCurrentAppearanceData() to save appearance
/// 5. Use appearanceSystem.SetAppearance(data) to restore saved appearance
/// </summary>
[System.Serializable]
public class CharacterAppearanceSystem
{
    [Header("Model Options")]
    [SerializeField] private GameObject[] bodyModels; // Different body/mesh options
    [SerializeField] private GameObject[] headModels; // Different head options
    [SerializeField] private GameObject[] hairModels; // Different hair styles
    
    [Header("Clothing Options")]
    [SerializeField] private GameObject[] topClothing; // Shirts, jackets, etc.
    [SerializeField] private GameObject[] bottomClothing; // Pants, skirts, etc.
    [SerializeField] private GameObject[] footwear; // Shoes, boots, etc.
    
    [Header("Accessories")]
    [SerializeField] private GameObject[] headAccessories; // Hats, helmets, glasses
    [SerializeField] private GameObject[] backAccessories; // Backpacks, cloaks
    [SerializeField] private GameObject[] handAccessories; // Gloves, bracelets
    
    [Header("Material Variants")]
    [SerializeField] private Material[] skinMaterials; // Different skin tones
    [SerializeField] private Material[] hairMaterials; // Different hair colors
    [SerializeField] private Material[] clothingMaterials; // Different clothing colors
    
    [Header("Accessory Spawn Chances")]
    [Range(0f, 1f)] [SerializeField] private float headAccessoryChance = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float backAccessoryChance = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float handAccessoryChance = 0.2f;
    
    private Transform characterTransform;
    private string characterName;
    private List<GameObject> activeModels = new List<GameObject>();

    /// <summary>
    /// Initialize the appearance system with a character transform.
    /// Call this in Awake() or Start() before using the system.
    /// </summary>
    /// <param name="characterTransform">The transform of the character using this system</param>
    /// <param name="characterName">Optional name for debug logging</param>
    public void Initialize(Transform characterTransform, string characterName = "Unknown Character")
    {
        this.characterTransform = characterTransform;
        this.characterName = characterName;
        activeModels = new List<GameObject>();
    }
    
    /// <summary>
    /// Randomize the character's appearance using the available options.
    /// </summary>
    public void RandomizeAppearance()
    {
        if (characterTransform == null)
        {
            Debug.LogError("CharacterAppearanceSystem: Cannot randomize appearance - not initialized (call Initialize first)");
            return;
        }
        
        // Clear any existing appearance models
        ClearCurrentAppearance();
        
        // Check if we have any models to work with
        bool hasAnyModels = (bodyModels?.Length > 0) || (headModels?.Length > 0) || (hairModels?.Length > 0) || 
                           (topClothing?.Length > 0) || (bottomClothing?.Length > 0) || (footwear?.Length > 0);
        
        if (!hasAnyModels)
        {
            Debug.LogError($"[CharacterAppearanceSystem] No appearance models found for {characterName}! Check prefab setup.");
            return;
        }
        
        // Randomize body parts
        if (bodyModels != null && bodyModels.Length > 0)
        {
            ActivateRandomModel(bodyModels, "Body");
        }
        else
        {
            Debug.LogWarning($"[CharacterAppearanceSystem] No body models available for {characterName}");
        }
        
        if (headModels != null && headModels.Length > 0)
        {
            ActivateRandomModel(headModels, "Head");
        }
        
        if (hairModels != null && hairModels.Length > 0)
        {
            ActivateRandomModel(hairModels, "Hair");
        }
        
        // Randomize clothing
        if (topClothing != null && topClothing.Length > 0)
        {
            ActivateRandomModel(topClothing, "Top Clothing");
        }
        
        if (bottomClothing != null && bottomClothing.Length > 0)
        {
            ActivateRandomModel(bottomClothing, "Bottom Clothing");
        }
        
        if (footwear != null && footwear.Length > 0)
        {
            ActivateRandomModel(footwear, "Footwear");
        }
        
        // Randomize accessories based on spawn chances
        if (headAccessories != null && headAccessories.Length > 0 && UnityEngine.Random.value <= headAccessoryChance)
        {
            ActivateRandomModel(headAccessories, "Head Accessory");
        }
        
        if (backAccessories != null && backAccessories.Length > 0 && UnityEngine.Random.value <= backAccessoryChance)
        {
            ActivateRandomModel(backAccessories, "Back Accessory");
        }
        
        if (handAccessories != null && handAccessories.Length > 0 && UnityEngine.Random.value <= handAccessoryChance)
        {
            ActivateRandomModel(handAccessories, "Hand Accessory");
        }        
        
        // Apply random materials
        ApplyRandomMaterials();
    }
    
    /// <summary>
    /// Activate a random model from the given array.
    /// </summary>
    private void ActivateRandomModel(GameObject[] modelArray, string categoryName)
    {
        if (modelArray == null || modelArray.Length == 0) return;
        
        GameObject selectedModel = modelArray[UnityEngine.Random.Range(0, modelArray.Length)];
        if (selectedModel != null)
        {
            selectedModel.SetActive(true);
            activeModels.Add(selectedModel);
        }
    }
    
    /// <summary>
    /// Apply random materials to the active models.
    /// </summary>
    private void ApplyRandomMaterials()
    {
        foreach (GameObject model in activeModels)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // Apply random skin material if available
                if (skinMaterials != null && skinMaterials.Length > 0 && 
                    (model.name.Contains("Body") || model.name.Contains("Head")))
                {
                    Material randomSkinMaterial = skinMaterials[UnityEngine.Random.Range(0, skinMaterials.Length)];
                    renderer.material = randomSkinMaterial;
                }
                // Apply random hair material if available
                else if (hairMaterials != null && hairMaterials.Length > 0 && model.name.Contains("Hair"))
                {
                    Material randomHairMaterial = hairMaterials[UnityEngine.Random.Range(0, hairMaterials.Length)];
                    renderer.material = randomHairMaterial;
                }
                // Apply random clothing material if available
                else if (clothingMaterials != null && clothingMaterials.Length > 0)
                {
                    Material randomClothingMaterial = clothingMaterials[UnityEngine.Random.Range(0, clothingMaterials.Length)];
                    renderer.material = randomClothingMaterial;
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all currently active appearance models.
    /// </summary>
    public void ClearCurrentAppearance()
    {
        if (activeModels == null)
        {
            Debug.LogError("CharacterAppearanceSystem: Cannot clear current appearance - activeModels is null");
            return;
        }

        foreach (GameObject model in activeModels)
        {
            if (model != null)
            {
                model.SetActive(false);
            }
        }
        activeModels.Clear();
    }
    
    /// <summary>
    /// Set specific appearance options (for saved/predefined appearances).
    /// </summary>
    public void SetAppearance(CharacterAppearanceData appearanceData)
    {
        if (characterTransform == null)
        {
            Debug.LogError("CharacterAppearanceSystem: Cannot set appearance - not initialized (call Initialize first)");
            return;
        }

        if (appearanceData == null)
        {
            Debug.LogWarning($"[CharacterAppearanceSystem] Appearance data is null for {characterName}");
            return;
        }

        // Clear current appearance
        ClearCurrentAppearance();

        // Set body parts
        ActivateModelByName(bodyModels, appearanceData.bodyModelName, "Body");
        ActivateModelByName(headModels, appearanceData.headModelName, "Head");
        ActivateModelByName(hairModels, appearanceData.hairModelName, "Hair");

        // Set clothing
        ActivateModelByName(topClothing, appearanceData.topClothingName, "Top Clothing");
        ActivateModelByName(bottomClothing, appearanceData.bottomClothingName, "Bottom Clothing");
        ActivateModelByName(footwear, appearanceData.footwearName, "Footwear");

        // Set accessories (only if they have values)
        if (!string.IsNullOrEmpty(appearanceData.headAccessoryName))
        {
            ActivateModelByName(headAccessories, appearanceData.headAccessoryName, "Head Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.backAccessoryName))
        {
            ActivateModelByName(backAccessories, appearanceData.backAccessoryName, "Back Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.handAccessoryName))
        {
            ActivateModelByName(handAccessories, appearanceData.handAccessoryName, "Hand Accessory");
        }

        // Apply saved materials
        ApplySavedMaterials(appearanceData);
    }
    
    /// <summary>
    /// Get current appearance data for saving.
    /// </summary>
    public CharacterAppearanceData GetCurrentAppearanceData()
    {
        CharacterAppearanceData appearanceData = new CharacterAppearanceData();
        
        if (activeModels == null || activeModels.Count == 0)
        {
            Debug.LogWarning($"[CharacterAppearanceSystem] No active models found for {characterName}");
            return appearanceData;
        }

        foreach (GameObject activeModel in activeModels)
        {
            if (activeModel == null) continue;

            string modelName = activeModel.name;
            
            // Determine which type of model this is and store its name
            if (IsModelInArray(activeModel, bodyModels))
            {
                appearanceData.bodyModelName = modelName;
            }
            else if (IsModelInArray(activeModel, headModels))
            {
                appearanceData.headModelName = modelName;
            }
            else if (IsModelInArray(activeModel, hairModels))
            {
                appearanceData.hairModelName = modelName;
            }
            else if (IsModelInArray(activeModel, topClothing))
            {
                appearanceData.topClothingName = modelName;
            }
            else if (IsModelInArray(activeModel, bottomClothing))
            {
                appearanceData.bottomClothingName = modelName;
            }
            else if (IsModelInArray(activeModel, footwear))
            {
                appearanceData.footwearName = modelName;
            }
            else if (IsModelInArray(activeModel, headAccessories))
            {
                appearanceData.headAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, backAccessories))
            {
                appearanceData.backAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, handAccessories))
            {
                appearanceData.handAccessoryName = modelName;
            }

            // Get material names from the active model
            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.material != null)
                {
                    string materialName = renderer.material.name.Replace(" (Instance)", "");
                    
                    // Categorize materials by model type
                    if (modelName.Contains("Body") || modelName.Contains("Head"))
                    {
                        appearanceData.skinMaterialName = materialName;
                    }
                    else if (modelName.Contains("Hair"))
                    {
                        appearanceData.hairMaterialName = materialName;
                    }
                    else
                    {
                        appearanceData.clothingMaterialName = materialName;
                    }
                }
            }
        }
        
        return appearanceData;
    }

    /// <summary>
    /// Helper method to check if a model exists in a given array.
    /// </summary>
    private bool IsModelInArray(GameObject model, GameObject[] modelArray)
    {
        if (modelArray == null || model == null) return false;
        
        foreach (GameObject arrayModel in modelArray)
        {
            if (arrayModel == model) return true;
        }
        return false;
    }

    /// <summary>
    /// Activate a specific model by name from the given array.
    /// </summary>
    private void ActivateModelByName(GameObject[] modelArray, string modelName, string categoryName)
    {
        if (modelArray == null || string.IsNullOrEmpty(modelName)) return;

        foreach (GameObject model in modelArray)
        {
            if (model != null && model.name == modelName)
            {
                model.SetActive(true);
                activeModels.Add(model);
                return;
            }
        }
        
        Debug.LogWarning($"[CharacterAppearanceSystem] Could not find {categoryName} model with name: {modelName}");
    }

    /// <summary>
    /// Apply saved materials to active models.
    /// </summary>
    private void ApplySavedMaterials(CharacterAppearanceData appearanceData)
    {
        foreach (GameObject model in activeModels)
        {
            if (model == null) continue;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material targetMaterial = null;
                string modelName = model.name;

                // Determine which material to apply based on model type
                if ((modelName.Contains("Body") || modelName.Contains("Head")) && !string.IsNullOrEmpty(appearanceData.skinMaterialName))
                {
                    targetMaterial = FindMaterialByName(skinMaterials, appearanceData.skinMaterialName);
                }
                else if (modelName.Contains("Hair") && !string.IsNullOrEmpty(appearanceData.hairMaterialName))
                {
                    targetMaterial = FindMaterialByName(hairMaterials, appearanceData.hairMaterialName);
                }
                else if (!string.IsNullOrEmpty(appearanceData.clothingMaterialName))
                {
                    targetMaterial = FindMaterialByName(clothingMaterials, appearanceData.clothingMaterialName);
                }

                if (targetMaterial != null)
                {
                    renderer.material = targetMaterial;
                }
            }
        }
    }

    /// <summary>
    /// Find a material by name in the given material array.
    /// </summary>
    private Material FindMaterialByName(Material[] materialArray, string materialName)
    {
        if (materialArray == null || string.IsNullOrEmpty(materialName)) return null;

        foreach (Material material in materialArray)
        {
            if (material != null && material.name == materialName)
            {
                return material;
            }
        }
        
        return null;
    }
}

/// <summary>
/// Data class to store character appearance information for saving/loading.
/// Works with any character type that uses CharacterAppearanceSystem.
/// </summary>
[System.Serializable]
public class CharacterAppearanceData
{
    public string bodyModelName;
    public string headModelName;
    public string hairModelName;
    public string topClothingName;
    public string bottomClothingName;
    public string footwearName;
    public string headAccessoryName;
    public string backAccessoryName;
    public string handAccessoryName;
    public string skinMaterialName;
    public string hairMaterialName;
    public string clothingMaterialName;
}
