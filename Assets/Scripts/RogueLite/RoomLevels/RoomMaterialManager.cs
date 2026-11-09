using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized material randomization manager for rooms.
/// More efficient than adding MaterialRandomizer to every object.
/// </summary>
public class RoomMaterialManager : MonoBehaviour
{
    [System.Serializable]
    public class RendererMaterialInfo
    {
        public Renderer renderer;
        public int materialIndex;
        public List<Material> variants = new List<Material>();
    }
    
    [System.Serializable]
    public class MaterialGroup
    {
        [Tooltip("Name for this material group (e.g., 'Walls', 'Floors', 'Beds')")]
        public string groupName = "Material Group";
        
        [Tooltip("When enabled, all renderers will pick the same variant INDEX from their own materials")]
        public bool synchronizeGroup = false;
        
        [Tooltip("Renderers with their individual material variants")]
        public List<RendererMaterialInfo> rendererMaterials = new List<RendererMaterialInfo>();
        
        [Tooltip("Renderers that will have their materials randomized (legacy)")]
        public List<Renderer> renderers = new List<Renderer>();
        
        [Tooltip("Material variants to randomly choose from (legacy)")]
        public List<MaterialRandomizer.MaterialOption> materialVariants = new List<MaterialRandomizer.MaterialOption>();
        
        [HideInInspector] public bool foldout = true; // For editor display
    }
    
    [Header("Material Groups")]
    [SerializeField] private List<MaterialGroup> materialGroups = new List<MaterialGroup>();
    
    [Header("Settings")]
    [Tooltip("Randomize materials on Start")]
    [SerializeField] private bool randomizeOnStart = true;
    
    [Header("Debug")]
    [SerializeField] private bool logRandomization = false;
    
    private void Start()
    {
        if (randomizeOnStart)
        {
            RandomizeAllMaterials();
        }
    }
    
    /// <summary>
    /// Randomize materials for all groups
    /// </summary>
    public void RandomizeAllMaterials()
    {
        foreach (var group in materialGroups)
        {
            RandomizeGroup(group);
        }
        
        if (logRandomization)
            Debug.Log($"[RoomMaterialManager] Randomized {materialGroups.Count} material groups on {gameObject.name}");
    }
    
    /// <summary>
    /// Randomize materials for a specific group
    /// </summary>
    private void RandomizeGroup(MaterialGroup group)
    {
        // Use new system if available
        if (group.rendererMaterials != null && group.rendererMaterials.Count > 0)
        {
            if (group.synchronizeGroup)
            {
                RandomizeGroupSynchronizedByIndex(group);
            }
            else
            {
                RandomizeGroupIndividualNew(group);
            }
            return;
        }
        
        // Fallback to legacy system
        if (group.materialVariants.Count == 0)
        {
            if (logRandomization)
                Debug.LogWarning($"[RoomMaterialManager] Group '{group.groupName}' has no material variants");
            return;
        }
        
        if (group.renderers.Count == 0)
        {
            if (logRandomization)
                Debug.LogWarning($"[RoomMaterialManager] Group '{group.groupName}' has no renderers");
            return;
        }
        
        if (group.synchronizeGroup)
        {
            RandomizeGroupSynchronized(group);
        }
        else
        {
            RandomizeGroupIndividual(group);
        }
    }
    
    /// <summary>
    /// NEW: Randomize by picking the same variant INDEX for all renderers
    /// </summary>
    private void RandomizeGroupSynchronizedByIndex(MaterialGroup group)
    {
        if (group.rendererMaterials.Count == 0) return;
        
        // Find the maximum number of variants any renderer has
        int maxVariants = 0;
        foreach (var info in group.rendererMaterials)
        {
            if (info.variants.Count > maxVariants)
                maxVariants = info.variants.Count;
        }
        
        if (maxVariants == 0)
        {
            if (logRandomization)
                Debug.LogWarning($"[RoomMaterialManager] Group '{group.groupName}' has no variants");
            return;
        }
        
        // Pick a random variant index (0, 1, 2, etc.)
        int chosenIndex = Random.Range(0, maxVariants);
        
        // Apply that variant index to each renderer (wrapping if they have fewer variants)
        foreach (var info in group.rendererMaterials)
        {
            if (info.renderer == null || info.variants.Count == 0) continue;
            
            // Wrap index if this renderer has fewer variants
            int actualIndex = chosenIndex % info.variants.Count;
            Material chosenMaterial = info.variants[actualIndex];
            
            ApplyMaterialToRenderer(info.renderer, chosenMaterial, info.materialIndex);
        }
        
        if (logRandomization)
            Debug.Log($"[RoomMaterialManager] Randomized group '{group.groupName}' synchronized to variant index {chosenIndex} ({group.rendererMaterials.Count} renderers)");
    }
    
    /// <summary>
    /// NEW: Randomize each renderer individually from its own variants
    /// </summary>
    private void RandomizeGroupIndividualNew(MaterialGroup group)
    {
        foreach (var info in group.rendererMaterials)
        {
            if (info.renderer == null || info.variants.Count == 0) continue;
            
            Material chosenMaterial = info.variants[Random.Range(0, info.variants.Count)];
            ApplyMaterialToRenderer(info.renderer, chosenMaterial, info.materialIndex);
        }
        
        if (logRandomization)
            Debug.Log($"[RoomMaterialManager] Randomized group '{group.groupName}' individually ({group.rendererMaterials.Count} renderers)");
    }
    
    /// <summary>
    /// LEGACY: Randomize each renderer in the group to potentially different materials
    /// </summary>
    private void RandomizeGroupIndividual(MaterialGroup group)
    {
        foreach (var renderer in group.renderers)
        {
            if (renderer == null) continue;
            
            var option = group.materialVariants[Random.Range(0, group.materialVariants.Count)];
            ApplyMaterialOption(renderer, option);
        }
        
        if (logRandomization)
            Debug.Log($"[RoomMaterialManager] Randomized group '{group.groupName}' individually ({group.renderers.Count} renderers)");
    }
    
    /// <summary>
    /// LEGACY: Randomize all renderers in the group to the same material
    /// </summary>
    private void RandomizeGroupSynchronized(MaterialGroup group)
    {
        var chosenOption = group.materialVariants[Random.Range(0, group.materialVariants.Count)];
        
        foreach (var renderer in group.renderers)
        {
            if (renderer == null) continue;
            
            ApplyMaterialOption(renderer, chosenOption);
        }
        
        if (logRandomization)
            Debug.Log($"[RoomMaterialManager] Randomized group '{group.groupName}' synchronized to '{chosenOption.material.name}' ({group.renderers.Count} renderers)");
    }
    
    /// <summary>
    /// Apply a material to a renderer at a specific index
    /// </summary>
    private void ApplyMaterialToRenderer(Renderer renderer, Material material, int materialIndex)
    {
        if (material == null)
        {
            if (logRandomization)
                Debug.LogWarning($"[RoomMaterialManager] Null material");
            return;
        }
        
        Material[] materials = renderer.materials;
        
        if (materialIndex >= 0 && materialIndex < materials.Length)
        {
            materials[materialIndex] = material;
            renderer.materials = materials;
        }
        else if (logRandomization)
        {
            Debug.LogWarning($"[RoomMaterialManager] Material index {materialIndex} out of range for {renderer.gameObject.name}");
        }
    }
    
    /// <summary>
    /// LEGACY: Apply a specific material option to a renderer
    /// </summary>
    private void ApplyMaterialOption(Renderer renderer, MaterialRandomizer.MaterialOption option)
    {
        if (option.material == null)
        {
            if (logRandomization)
                Debug.LogWarning($"[RoomMaterialManager] Material option has null material");
            return;
        }
        
        Material[] materials = renderer.materials;
        
        if (option.materialIndex >= 0 && option.materialIndex < materials.Length)
        {
            materials[option.materialIndex] = option.material;
            renderer.materials = materials;
        }
        else if (logRandomization)
        {
            Debug.LogWarning($"[RoomMaterialManager] Material index {option.materialIndex} out of range for {renderer.gameObject.name}");
        }
    }
    
    /// <summary>
    /// Add a new material group
    /// </summary>
    public MaterialGroup AddGroup(string groupName, bool synchronized = false)
    {
        var group = new MaterialGroup
        {
            groupName = groupName,
            synchronizeGroup = synchronized
        };
        materialGroups.Add(group);
        return group;
    }
    
    /// <summary>
    /// Get a material group by name
    /// </summary>
    public MaterialGroup GetGroup(string groupName)
    {
        return materialGroups.Find(g => g.groupName == groupName);
    }
    
    /// <summary>
    /// Clear all material groups
    /// </summary>
    public void ClearAllGroups()
    {
        materialGroups.Clear();
    }
    
    public List<MaterialGroup> GetAllGroups() => materialGroups;
}


