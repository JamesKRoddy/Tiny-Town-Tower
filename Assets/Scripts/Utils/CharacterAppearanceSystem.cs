using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
/// 
/// Spawn Weights:
/// - Higher weight = more common (e.g., 100 for common items)
/// - Lower weight = more rare (e.g., 10 for rare items)
/// - Weight of 0 = disabled (never spawns)
/// </summary>
[System.Serializable]
public class CharacterAppearanceSystem
{
    [Header("Model Options")]
    [SerializeField] private AppearanceOption[] bodyModels; // Different body/mesh options
    [SerializeField] private AppearanceOption[] hairModels; // Different hair styles
    
    [Header("Clothing Options")]
    [SerializeField] private AppearanceOption[] topClothing; // Shirts, jackets, etc.
    [SerializeField] private AppearanceOption[] bottomClothing; // Pants, skirts, etc.
    [SerializeField] private AppearanceOption[] footwear; // Shoes, boots, etc.
    
    [Header("Head Accessories")]
    [SerializeField] private AppearanceOption[] hats; // Casual headwear (caps, beanies, etc.)
    [SerializeField] private AppearanceOption[] helmets; // Protective headwear (helmets, masks, etc.)
    [SerializeField] private AppearanceOption[] faceAccessories; // Glasses, masks, visors, etc.
    
    [Header("Shoulder Accessories")]
    [SerializeField] private AppearanceOption[] leftShoulderAccessories; // Left pauldrons, shoulder pads
    [SerializeField] private AppearanceOption[] rightShoulderAccessories; // Right pauldrons, shoulder pads
    
    [Header("Forearm Accessories")]
    [SerializeField] private AppearanceOption[] leftForearmAccessories; // Left bracers, arm guards
    [SerializeField] private AppearanceOption[] rightForearmAccessories; // Right bracers, arm guards
    
    [Header("Hand Accessories")]
    [SerializeField] private AppearanceOption[] leftHandAccessories; // Left gloves, bracelets
    [SerializeField] private AppearanceOption[] rightHandAccessories; // Right gloves, bracelets
    
    [Header("Upper Leg Accessories")]
    [SerializeField] private AppearanceOption[] leftUpperLegAccessories; // Left upper leg pouches, holsters, thigh armor
    [SerializeField] private AppearanceOption[] rightUpperLegAccessories; // Right upper leg pouches, holsters, thigh armor
    
    [Header("Calf Accessories")]
    [SerializeField] private AppearanceOption[] leftCalfAccessories; // Left shin guards, leg wraps
    [SerializeField] private AppearanceOption[] rightCalfAccessories; // Right shin guards, leg wraps
    
    [Header("Foot Accessories")]
    [SerializeField] private AppearanceOption[] leftFootAccessories; // Left ankle accessories, spurs
    [SerializeField] private AppearanceOption[] rightFootAccessories; // Right ankle accessories, spurs
    
    [Header("Other Accessories")]
    [SerializeField] private AppearanceOption[] backAccessories; // Backpacks, cloaks
    
    [Header("Material Variants")]
    [SerializeField] private Material[] skinMaterials; // Different skin tones
    [SerializeField] private Material[] hairMaterials; // Different hair colors
    [SerializeField] private Material[] clothingMaterials; // Different clothing colors
    
    [Header("Accessory Spawn Chances")]
    [Range(0f, 1f)] [SerializeField] private float hatChance = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float helmetChance = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float faceAccessoryChance = 0.15f;
    [Range(0f, 1f)] [SerializeField] private float leftShoulderAccessoryChance = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float rightShoulderAccessoryChance = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float leftForearmAccessoryChance = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float rightForearmAccessoryChance = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float leftHandAccessoryChance = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float rightHandAccessoryChance = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float leftUpperLegAccessoryChance = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float rightUpperLegAccessoryChance = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float leftCalfAccessoryChance = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float rightCalfAccessoryChance = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float leftFootAccessoryChance = 0.15f;
    [Range(0f, 1f)] [SerializeField] private float rightFootAccessoryChance = 0.15f;
    [Range(0f, 1f)] [SerializeField] private float backAccessoryChance = 0.4f;
    
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
        bool hasAnyModels = (bodyModels?.Length > 0) || (hairModels?.Length > 0) || 
                           (topClothing?.Length > 0) || (bottomClothing?.Length > 0) || (footwear?.Length > 0);
        
        if (!hasAnyModels)
        {
            Debug.LogError($"[CharacterAppearanceSystem] No appearance models found for {characterName}! Check prefab setup.");
            return;
        }
        
        // Track all exclusions from activated models
        AppearanceExclusions activeExclusions = AppearanceExclusions.None;
        
        // Randomize body parts
        if (bodyModels != null && bodyModels.Length > 0)
        {
            var selected = ActivateRandomModel(bodyModels, "Body");
            if (selected != null) activeExclusions |= selected.exclusions;
        }
        else
        {
            Debug.LogWarning($"[CharacterAppearanceSystem] No body models available for {characterName}");
        }
        
        // Randomize hair
        if (hairModels != null && hairModels.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.Hair))
            {
                var selected = ActivateRandomModel(hairModels, "Hair");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(hairModels); // Deactivate if excluded
            }
        }
        
        // Randomize clothing (check exclusions)
        if (topClothing != null && topClothing.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.TopClothing))
            {
                var selected = ActivateRandomModel(topClothing, "Top Clothing");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(topClothing); // Deactivate if excluded
            }
        }
        
        if (bottomClothing != null && bottomClothing.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.BottomClothing))
            {
                var selected = ActivateRandomModel(bottomClothing, "Bottom Clothing");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(bottomClothing); // Deactivate if excluded
            }
        }
        
        if (footwear != null && footwear.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.Footwear))
            {
                var selected = ActivateRandomModel(footwear, "Footwear");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(footwear); // Deactivate if excluded
            }
        }
        
        // Randomize head accessories based on spawn chances and exclusions
        if (hats != null && hats.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.Hats) && 
                UnityEngine.Random.value <= hatChance)
            {
                var selected = ActivateRandomModel(hats, "Hat");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(hats); // Deactivate if excluded or not spawned
            }
        }
        
        if (helmets != null && helmets.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.Helmets) && 
                UnityEngine.Random.value <= helmetChance)
            {
                var selected = ActivateRandomModel(helmets, "Helmet");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(helmets); // Deactivate if excluded or not spawned
            }
        }
        
        if (faceAccessories != null && faceAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.FaceAccessories) && 
                UnityEngine.Random.value <= faceAccessoryChance)
            {
                var selected = ActivateRandomModel(faceAccessories, "Face Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(faceAccessories); // Deactivate if excluded or not spawned
            }
        }
        
        if (backAccessories != null && backAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.BackAccessories) && 
                UnityEngine.Random.value <= backAccessoryChance)
            {
                var selected = ActivateRandomModel(backAccessories, "Back Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(backAccessories); // Deactivate if excluded or not spawned
            }
        }
        
        // Left shoulder
        if (leftShoulderAccessories != null && leftShoulderAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.LeftShoulderAccessories) && 
                UnityEngine.Random.value <= leftShoulderAccessoryChance)
            {
                var selected = ActivateRandomModel(leftShoulderAccessories, "Left Shoulder Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(leftShoulderAccessories);
            }
        }
        
        // Right shoulder
        if (rightShoulderAccessories != null && rightShoulderAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.RightShoulderAccessories) && 
                UnityEngine.Random.value <= rightShoulderAccessoryChance)
            {
                var selected = ActivateRandomModel(rightShoulderAccessories, "Right Shoulder Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(rightShoulderAccessories);
            }
        }
        
        // Left forearm
        if (leftForearmAccessories != null && leftForearmAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.LeftForearmAccessories) && 
                UnityEngine.Random.value <= leftForearmAccessoryChance)
            {
                var selected = ActivateRandomModel(leftForearmAccessories, "Left Forearm Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(leftForearmAccessories);
            }
        }
        
        // Right forearm
        if (rightForearmAccessories != null && rightForearmAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.RightForearmAccessories) && 
                UnityEngine.Random.value <= rightForearmAccessoryChance)
            {
                var selected = ActivateRandomModel(rightForearmAccessories, "Right Forearm Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(rightForearmAccessories);
            }
        }
        
        // Left hand
        if (leftHandAccessories != null && leftHandAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.LeftHandAccessories) && 
                UnityEngine.Random.value <= leftHandAccessoryChance)
            {
                var selected = ActivateRandomModel(leftHandAccessories, "Left Hand Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(leftHandAccessories);
            }
        }
        
        // Right hand
        if (rightHandAccessories != null && rightHandAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.RightHandAccessories) && 
                UnityEngine.Random.value <= rightHandAccessoryChance)
            {
                var selected = ActivateRandomModel(rightHandAccessories, "Right Hand Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(rightHandAccessories);
            }
        }
        
        // Left upper leg
        if (leftUpperLegAccessories != null && leftUpperLegAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.LeftUpperLegAccessories) && 
                UnityEngine.Random.value <= leftUpperLegAccessoryChance)
            {
                var selected = ActivateRandomModel(leftUpperLegAccessories, "Left Upper Leg Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(leftUpperLegAccessories);
            }
        }
        
        // Right upper leg
        if (rightUpperLegAccessories != null && rightUpperLegAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.RightUpperLegAccessories) && 
                UnityEngine.Random.value <= rightUpperLegAccessoryChance)
            {
                var selected = ActivateRandomModel(rightUpperLegAccessories, "Right Upper Leg Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(rightUpperLegAccessories);
            }
        }
        
        // Left calf
        if (leftCalfAccessories != null && leftCalfAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.LeftCalfAccessories) && 
                UnityEngine.Random.value <= leftCalfAccessoryChance)
            {
                var selected = ActivateRandomModel(leftCalfAccessories, "Left Calf Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(leftCalfAccessories);
            }
        }
        
        // Right calf
        if (rightCalfAccessories != null && rightCalfAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.RightCalfAccessories) && 
                UnityEngine.Random.value <= rightCalfAccessoryChance)
            {
                var selected = ActivateRandomModel(rightCalfAccessories, "Right Calf Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(rightCalfAccessories);
            }
        }
        
        // Left foot
        if (leftFootAccessories != null && leftFootAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.LeftFootAccessories) && 
                UnityEngine.Random.value <= leftFootAccessoryChance)
            {
                var selected = ActivateRandomModel(leftFootAccessories, "Left Foot Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(leftFootAccessories);
            }
        }
        
        // Right foot
        if (rightFootAccessories != null && rightFootAccessories.Length > 0)
        {
            if (!activeExclusions.HasFlag(AppearanceExclusions.RightFootAccessories) && 
                UnityEngine.Random.value <= rightFootAccessoryChance)
            {
                var selected = ActivateRandomModel(rightFootAccessories, "Right Foot Accessory");
                if (selected != null) activeExclusions |= selected.exclusions;
            }
            else
            {
                DeactivateAllInCategory(rightFootAccessories);
            }
        }        
        
        // Apply random materials (respect exclusions)
        ApplyRandomMaterials(activeExclusions);
    }
    
    /// <summary>
    /// Activate a random model from the given array using weighted selection.
    /// Items with higher spawn weights are more likely to be selected.
    /// Returns the selected AppearanceOption so exclusions can be tracked.
    /// </summary>
    /// <summary>
    /// Deactivate all models in a category (used when category is excluded or needs to be cleared)
    /// </summary>
    private void DeactivateAllInCategory(AppearanceOption[] modelArray)
    {
        if (modelArray == null || modelArray.Length == 0) return;
        
        foreach (var option in modelArray)
        {
            if (option != null && option.model != null)
            {
                option.model.SetActive(false);
            }
        }
    }
    
    private AppearanceOption ActivateRandomModel(AppearanceOption[] modelArray, string categoryName)
    {
        if (modelArray == null || modelArray.Length == 0) return null;
        
        // FIRST: Deactivate ALL models in this category (important for spawned characters where all start active)
        DeactivateAllInCategory(modelArray);
        
        // Filter out null models and those with 0 weight
        var validOptions = modelArray.Where(opt => opt != null && opt.model != null && opt.spawnWeight > 0).ToList();
        if (validOptions.Count == 0)
        {
            Debug.LogWarning($"[CharacterAppearanceSystem] No valid options for {categoryName}");
            return null;
        }
        
        // Select based on weighted probability
        AppearanceOption selectedOption = SelectWeightedRandomOption(validOptions);
        if (selectedOption != null && selectedOption.model != null)
        {
            // THEN: Activate only the selected model
            selectedOption.model.SetActive(true);
            activeModels.Add(selectedOption.model);
            
            // Apply random material variant if the model has the AppearanceMaterialVariants component
            AppearanceMaterialVariants materialVariants = selectedOption.model.GetComponent<AppearanceMaterialVariants>();
            if (materialVariants != null && materialVariants.VariantCount > 0)
            {
                materialVariants.ApplyRandomVariant();
            }
            
            return selectedOption;
        }
        
        return null;
    }
    
    /// <summary>
    /// Select a random model based on spawn weights.
    /// Higher weight = higher chance of selection.
    /// Returns the full AppearanceOption for exclusion tracking.
    /// </summary>
    private AppearanceOption SelectWeightedRandomOption(List<AppearanceOption> options)
    {
        // Calculate total weight
        float totalWeight = options.Sum(opt => opt.spawnWeight);
        if (totalWeight <= 0) return null;
        
        // Pick a random value within the total weight
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        
        // Find which option this value falls into
        float currentWeight = 0f;
        foreach (var option in options)
        {
            currentWeight += option.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return option;
            }
        }
        
        // Fallback (shouldn't happen, but just in case)
        return options[0];
    }
    
    /// <summary>
    /// Apply random materials to the active models.
    /// </summary>
    private void ApplyRandomMaterials(AppearanceExclusions exclusions = AppearanceExclusions.None)
    {
        foreach (GameObject model in activeModels)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // Apply random skin material if available and not excluded
                if (!exclusions.HasFlag(AppearanceExclusions.SkinMaterials) &&
                    skinMaterials != null && skinMaterials.Length > 0 && 
                    (model.name.Contains("Body") || model.name.Contains("Head")))
                {
                    Material randomSkinMaterial = skinMaterials[UnityEngine.Random.Range(0, skinMaterials.Length)];
                    renderer.material = randomSkinMaterial;
                }
                // Apply random hair material if available and not excluded
                else if (!exclusions.HasFlag(AppearanceExclusions.HairMaterials) &&
                         hairMaterials != null && hairMaterials.Length > 0 && model.name.Contains("Hair"))
                {
                    Material randomHairMaterial = hairMaterials[UnityEngine.Random.Range(0, hairMaterials.Length)];
                    renderer.material = randomHairMaterial;
                }
                // Apply random clothing material if available and not excluded
                else if (!exclusions.HasFlag(AppearanceExclusions.ClothingMaterials) &&
                         clothingMaterials != null && clothingMaterials.Length > 0)
                {
                    Material randomClothingMaterial = clothingMaterials[UnityEngine.Random.Range(0, clothingMaterials.Length)];
                    renderer.material = randomClothingMaterial;
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all currently active appearance models.
    /// Deactivates all models in all categories to ensure clean state.
    /// </summary>
    public void ClearCurrentAppearance()
    {
        if (activeModels == null)
        {
            Debug.LogError("CharacterAppearanceSystem: Cannot clear current appearance - activeModels is null");
            return;
        }

        // Deactivate all categories to ensure nothing is left active
        DeactivateAllInCategory(bodyModels);
        DeactivateAllInCategory(hairModels);
        DeactivateAllInCategory(topClothing);
        DeactivateAllInCategory(bottomClothing);
        DeactivateAllInCategory(footwear);
        DeactivateAllInCategory(hats);
        DeactivateAllInCategory(helmets);
        DeactivateAllInCategory(faceAccessories);
        DeactivateAllInCategory(leftShoulderAccessories);
        DeactivateAllInCategory(rightShoulderAccessories);
        DeactivateAllInCategory(leftForearmAccessories);
        DeactivateAllInCategory(rightForearmAccessories);
        DeactivateAllInCategory(leftHandAccessories);
        DeactivateAllInCategory(rightHandAccessories);
        DeactivateAllInCategory(leftUpperLegAccessories);
        DeactivateAllInCategory(rightUpperLegAccessories);
        DeactivateAllInCategory(leftCalfAccessories);
        DeactivateAllInCategory(rightCalfAccessories);
        DeactivateAllInCategory(leftFootAccessories);
        DeactivateAllInCategory(rightFootAccessories);
        DeactivateAllInCategory(backAccessories);
        
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
        ActivateModelByName(hairModels, appearanceData.hairModelName, "Hair");

        // Set clothing
        ActivateModelByName(topClothing, appearanceData.topClothingName, "Top Clothing");
        ActivateModelByName(bottomClothing, appearanceData.bottomClothingName, "Bottom Clothing");
        ActivateModelByName(footwear, appearanceData.footwearName, "Footwear");

        // Set accessories (only if they have values)
        if (!string.IsNullOrEmpty(appearanceData.hatName))
        {
            ActivateModelByName(hats, appearanceData.hatName, "Hat");
        }
        if (!string.IsNullOrEmpty(appearanceData.helmetName))
        {
            ActivateModelByName(helmets, appearanceData.helmetName, "Helmet");
        }
        if (!string.IsNullOrEmpty(appearanceData.faceAccessoryName))
        {
            ActivateModelByName(faceAccessories, appearanceData.faceAccessoryName, "Face Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.leftShoulderAccessoryName))
        {
            ActivateModelByName(leftShoulderAccessories, appearanceData.leftShoulderAccessoryName, "Left Shoulder Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.rightShoulderAccessoryName))
        {
            ActivateModelByName(rightShoulderAccessories, appearanceData.rightShoulderAccessoryName, "Right Shoulder Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.leftForearmAccessoryName))
        {
            ActivateModelByName(leftForearmAccessories, appearanceData.leftForearmAccessoryName, "Left Forearm Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.rightForearmAccessoryName))
        {
            ActivateModelByName(rightForearmAccessories, appearanceData.rightForearmAccessoryName, "Right Forearm Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.leftHandAccessoryName))
        {
            ActivateModelByName(leftHandAccessories, appearanceData.leftHandAccessoryName, "Left Hand Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.rightHandAccessoryName))
        {
            ActivateModelByName(rightHandAccessories, appearanceData.rightHandAccessoryName, "Right Hand Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.leftUpperLegAccessoryName))
        {
            ActivateModelByName(leftUpperLegAccessories, appearanceData.leftUpperLegAccessoryName, "Left Upper Leg Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.rightUpperLegAccessoryName))
        {
            ActivateModelByName(rightUpperLegAccessories, appearanceData.rightUpperLegAccessoryName, "Right Upper Leg Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.leftCalfAccessoryName))
        {
            ActivateModelByName(leftCalfAccessories, appearanceData.leftCalfAccessoryName, "Left Calf Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.rightCalfAccessoryName))
        {
            ActivateModelByName(rightCalfAccessories, appearanceData.rightCalfAccessoryName, "Right Calf Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.leftFootAccessoryName))
        {
            ActivateModelByName(leftFootAccessories, appearanceData.leftFootAccessoryName, "Left Foot Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.rightFootAccessoryName))
        {
            ActivateModelByName(rightFootAccessories, appearanceData.rightFootAccessoryName, "Right Foot Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.backAccessoryName))
        {
            ActivateModelByName(backAccessories, appearanceData.backAccessoryName, "Back Accessory");
        }

        // Apply saved materials
        ApplySavedMaterials(appearanceData);
    }
    
    /// <summary>
    /// Get all models from the appearance option array as GameObjects.
    /// </summary>
    private GameObject[] GetModelsFromOptions(AppearanceOption[] options)
    {
        if (options == null) return null;
        return options.Where(opt => opt != null && opt.model != null).Select(opt => opt.model).ToArray();
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
            else if (IsModelInArray(activeModel, hats))
            {
                appearanceData.hatName = modelName;
            }
            else if (IsModelInArray(activeModel, helmets))
            {
                appearanceData.helmetName = modelName;
            }
            else if (IsModelInArray(activeModel, faceAccessories))
            {
                appearanceData.faceAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, leftShoulderAccessories))
            {
                appearanceData.leftShoulderAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, rightShoulderAccessories))
            {
                appearanceData.rightShoulderAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, leftForearmAccessories))
            {
                appearanceData.leftForearmAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, rightForearmAccessories))
            {
                appearanceData.rightForearmAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, leftHandAccessories))
            {
                appearanceData.leftHandAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, rightHandAccessories))
            {
                appearanceData.rightHandAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, leftUpperLegAccessories))
            {
                appearanceData.leftUpperLegAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, rightUpperLegAccessories))
            {
                appearanceData.rightUpperLegAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, leftCalfAccessories))
            {
                appearanceData.leftCalfAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, rightCalfAccessories))
            {
                appearanceData.rightCalfAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, leftFootAccessories))
            {
                appearanceData.leftFootAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, rightFootAccessories))
            {
                appearanceData.rightFootAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, backAccessories))
            {
                appearanceData.backAccessoryName = modelName;
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
    private bool IsModelInArray(GameObject model, AppearanceOption[] modelArray)
    {
        if (modelArray == null || model == null) return false;
        
        foreach (var option in modelArray)
        {
            if (option != null && option.model == model) return true;
        }
        return false;
    }

    /// <summary>
    /// Activate a specific model by name from the given array.
    /// </summary>
    private void ActivateModelByName(AppearanceOption[] modelArray, string modelName, string categoryName)
    {
        if (modelArray == null || string.IsNullOrEmpty(modelName)) return;

        foreach (var option in modelArray)
        {
            if (option != null && option.model != null && option.model.name == modelName)
            {
                option.model.SetActive(true);
                activeModels.Add(option.model);
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
/// Appearance categories that can be excluded by certain options.
/// For example, a full helmet might exclude hair, hats, and face accessories.
/// </summary>
[System.Flags]
public enum AppearanceExclusions
{
    None = 0,
    Hair = 1 << 0,                      // Blocks hair models
    Hats = 1 << 1,                      // Blocks hats (casual headwear)
    Helmets = 1 << 2,                   // Blocks helmets (protective headwear)
    FaceAccessories = 1 << 3,           // Blocks face accessories (glasses, masks, visors)
    LeftShoulderAccessories = 1 << 4,   // Blocks left shoulder accessories
    RightShoulderAccessories = 1 << 5,  // Blocks right shoulder accessories
    LeftForearmAccessories = 1 << 6,    // Blocks left forearm accessories
    RightForearmAccessories = 1 << 7,   // Blocks right forearm accessories
    LeftHandAccessories = 1 << 8,       // Blocks left hand accessories
    RightHandAccessories = 1 << 9,      // Blocks right hand accessories
    LeftUpperLegAccessories = 1 << 10,       // Blocks left upper leg accessories
    RightUpperLegAccessories = 1 << 11,      // Blocks right upper leg accessories
    LeftCalfAccessories = 1 << 12,      // Blocks left calf accessories
    RightCalfAccessories = 1 << 13,     // Blocks right calf accessories
    LeftFootAccessories = 1 << 14,      // Blocks left foot accessories
    RightFootAccessories = 1 << 15,     // Blocks right foot accessories
    BackAccessories = 1 << 16,          // Blocks back accessories (backpacks, cloaks)
    TopClothing = 1 << 17,              // Blocks top clothing (for full body suits)
    BottomClothing = 1 << 18,           // Blocks bottom clothing (for full body suits)
    Footwear = 1 << 19,                 // Blocks footwear (for full body suits)
    SkinMaterials = 1 << 20,            // Blocks skin material variation (for full coverage)
    HairMaterials = 1 << 21,            // Blocks hair material variation
    ClothingMaterials = 1 << 22,        // Blocks clothing material variation
    
    // Convenience combinations - Arms
    AllShoulderAccessories = LeftShoulderAccessories | RightShoulderAccessories,
    AllForearmAccessories = LeftForearmAccessories | RightForearmAccessories,
    AllHandAccessories = LeftHandAccessories | RightHandAccessories,
    AllArmAccessories = AllShoulderAccessories | AllForearmAccessories | AllHandAccessories,
    
    // Convenience combinations - Legs
    AllUpperLegAccessories = LeftUpperLegAccessories | RightUpperLegAccessories,
    AllCalfAccessories = LeftCalfAccessories | RightCalfAccessories,
    AllFootAccessories = LeftFootAccessories | RightFootAccessories,
    AllLegAccessories = AllUpperLegAccessories | AllCalfAccessories | AllFootAccessories
}

/// <summary>
/// Represents a single appearance option with its associated spawn weight.
/// Higher weight = more common, lower weight = more rare.
/// Weight of 0 = disabled (never spawns).
/// Can also specify which accessories/options this blocks.
/// </summary>
[System.Serializable]
public class AppearanceOption
{
    [Tooltip("The GameObject model for this appearance option")]
    public GameObject model;
    
    [Tooltip("Spawn weight - higher values are more common (default: 100 = common, 50 = uncommon, 10 = rare, 1 = very rare)")]
    [Range(0f, 100f)]
    public float spawnWeight = 100f;
    
    [Tooltip("Which appearance categories this option excludes (e.g., full body armor excludes hair and head accessories)")]
    public AppearanceExclusions exclusions = AppearanceExclusions.None;
}

/// <summary>
/// Data class to store character appearance information for saving/loading.
/// Works with any character type that uses CharacterAppearanceSystem.
/// </summary>
[System.Serializable]
public class CharacterAppearanceData
{
    public string bodyModelName;
    public string hairModelName;
    public string topClothingName;
    public string bottomClothingName;
    public string footwearName;
    public string hatName;
    public string helmetName;
    public string faceAccessoryName;
    public string leftShoulderAccessoryName;
    public string rightShoulderAccessoryName;
    public string leftForearmAccessoryName;
    public string rightForearmAccessoryName;
    public string leftHandAccessoryName;
    public string rightHandAccessoryName;
    public string leftUpperLegAccessoryName;
    public string rightUpperLegAccessoryName;
    public string leftCalfAccessoryName;
    public string rightCalfAccessoryName;
    public string leftFootAccessoryName;
    public string rightFootAccessoryName;
    public string backAccessoryName;
    public string skinMaterialName;
    public string hairMaterialName;
    public string clothingMaterialName;
}
