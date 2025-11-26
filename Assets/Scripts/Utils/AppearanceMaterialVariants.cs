using UnityEngine;

/// <summary>
/// Component to store alternative material variants for appearance options.
/// Attach this to appearance GameObjects that have multiple material variants (colors, patterns, etc.)
/// The system will randomly pick one material and apply it to the renderer on this object.
/// </summary>
public class AppearanceMaterialVariants : MonoBehaviour
{
    [Header("Material Variants")]
    [Tooltip("Different materials to choose from. The appearance system will randomly select one when spawning.")]
    [SerializeField] private Material[] materials;
    
    [Tooltip("Which material slot indices to apply the variant to. Leave empty to default to slot 0.")]
    [SerializeField] private int[] materialSlots;
    
    /// <summary>
    /// Gets the number of available material variants
    /// </summary>
    public int VariantCount => materials?.Length ?? 0;
    
    /// <summary>
    /// Gets all material variants
    /// </summary>
    public Material[] Materials => materials;
    
    /// <summary>
    /// Applies a random material variant to the renderer on this GameObject
    /// </summary>
    public void ApplyRandomVariant()
    {
        if (materials == null || materials.Length == 0)
            return;
            
        Material selectedMaterial = materials[Random.Range(0, materials.Length)];
        ApplyMaterial(selectedMaterial);
    }
    
    /// <summary>
    /// Applies a specific material variant by index
    /// </summary>
    /// <param name="variantIndex">Index of the variant to apply</param>
    public void ApplyVariant(int variantIndex)
    {
        if (materials == null || variantIndex < 0 || variantIndex >= materials.Length)
        {
            Debug.LogWarning($"Invalid variant index {variantIndex} for {gameObject.name}");
            return;
        }
        
        ApplyMaterial(materials[variantIndex]);
    }
    
    /// <summary>
    /// Applies a material to the renderer on this GameObject
    /// </summary>
    private void ApplyMaterial(Material material)
    {
        if (material == null)
            return;
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
            return;
        
        // Get current materials array
        Material[] currentMaterials = renderer.sharedMaterials;
        
        // If no slots specified, default to slot 0
        if (materialSlots == null || materialSlots.Length == 0)
        {
            if (currentMaterials.Length > 0)
            {
                currentMaterials[0] = material;
                renderer.sharedMaterials = currentMaterials;
            }
        }
        else
        {
            // Apply to each specified slot
            foreach (int slot in materialSlots)
            {
                if (slot >= 0 && slot < currentMaterials.Length)
                {
                    currentMaterials[slot] = material;
                }
            }
            renderer.sharedMaterials = currentMaterials;
        }
    }
}
