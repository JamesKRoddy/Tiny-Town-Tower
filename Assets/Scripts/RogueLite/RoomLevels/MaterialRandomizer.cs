using UnityEngine;
using System.Collections.Generic;

public class MaterialRandomizer : MonoBehaviour
{
    [System.Serializable]
    public class MaterialOption
    {
        [Tooltip("The material to use")]
        public Material material;
        
        [Tooltip("Material index on the renderer (0 = first material, 1 = second, etc.)")]
        public int materialIndex = 0;
        
        public MaterialOption(Material mat, int index = 0)
        {
            material = mat;
            materialIndex = index;
        }
    }
    
    [Header("Randomization Settings")]
    [Tooltip("When enabled, all renderers will use the same random material option")]
    [SerializeField] private bool synchronizeAll = false;
    
    [Tooltip("List of material options to randomly choose from")]
    [SerializeField] private List<MaterialOption> materialOptions = new List<MaterialOption>();
    
    [Header("Target Settings")]
    [Tooltip("If empty, will randomize materials on all child renderers")]
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();
    
    [Header("Debug")]
    [SerializeField] private bool logRandomization = false;
    
    private void Start()
    {
        // Auto-populate target renderers if none specified
        if (targetRenderers.Count == 0)
        {
            targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
        }
    }
    
    /// <summary>
    /// Randomize materials on all target renderers
    /// </summary>
    public void RandomizeMaterials()
    {
        if (materialOptions.Count == 0)
        {
            if (logRandomization)
                Debug.LogWarning($"[MaterialRandomizer] No material options set on {gameObject.name}");
            return;
        }
        
        if (targetRenderers.Count == 0)
        {
            if (logRandomization)
                Debug.LogWarning($"[MaterialRandomizer] No target renderers found on {gameObject.name}");
            return;
        }
        
        if (synchronizeAll)
        {
            RandomizeSynchronized();
        }
        else
        {
            RandomizeIndividual();
        }
    }
    
    /// <summary>
    /// Randomize each renderer to potentially different materials
    /// </summary>
    private void RandomizeIndividual()
    {
        foreach (var renderer in targetRenderers)
        {
            if (renderer == null) continue;
            
            ApplyRandomMaterial(renderer);
        }
        
        if (logRandomization)
            Debug.Log($"[MaterialRandomizer] Randomized {targetRenderers.Count} renderers individually on {gameObject.name}");
    }
    
    /// <summary>
    /// Randomize all renderers to the same material option
    /// </summary>
    private void RandomizeSynchronized()
    {
        // Pick one random material option for all renderers
        MaterialOption chosenOption = materialOptions[Random.Range(0, materialOptions.Count)];
        
        foreach (var renderer in targetRenderers)
        {
            if (renderer == null) continue;
            
            ApplyMaterialOption(renderer, chosenOption);
        }
        
        if (logRandomization)
            Debug.Log($"[MaterialRandomizer] Applied synchronized material '{chosenOption.material.name}' " +
                     $"at index {chosenOption.materialIndex} to {targetRenderers.Count} renderers on {gameObject.name}");
    }
    
    /// <summary>
    /// Apply a random material option to a renderer
    /// </summary>
    private void ApplyRandomMaterial(Renderer renderer)
    {
        MaterialOption option = materialOptions[Random.Range(0, materialOptions.Count)];
        ApplyMaterialOption(renderer, option);
    }
    
    /// <summary>
    /// Apply a specific material option to a renderer
    /// </summary>
    private void ApplyMaterialOption(Renderer renderer, MaterialOption option)
    {
        if (option.material == null)
        {
            if (logRandomization)
                Debug.LogWarning($"[MaterialRandomizer] Material option has null material on {gameObject.name}");
            return;
        }
        
        // Get the current materials array
        Material[] materials = renderer.materials;
        
        // Ensure the material index is valid
        if (option.materialIndex >= 0 && option.materialIndex < materials.Length)
        {
            materials[option.materialIndex] = option.material;
            renderer.materials = materials;
            
            if (logRandomization)
                Debug.Log($"[MaterialRandomizer] Applied '{option.material.name}' at index {option.materialIndex} " +
                         $"to {renderer.gameObject.name}");
        }
        else if (logRandomization)
        {
            Debug.LogWarning($"[MaterialRandomizer] Material index {option.materialIndex} out of range " +
                           $"for renderer {renderer.gameObject.name} (has {materials.Length} materials)");
        }
    }
    
    /// <summary>
    /// Add a material option programmatically
    /// </summary>
    public void AddMaterialOption(Material material, int index = 0)
    {
        materialOptions.Add(new MaterialOption(material, index));
    }
    
    /// <summary>
    /// Clear all material options
    /// </summary>
    public void ClearMaterialOptions()
    {
        materialOptions.Clear();
    }
    
    /// <summary>
    /// Set whether to synchronize all renderers
    /// </summary>
    public void SetSynchronizeAll(bool synchronize)
    {
        synchronizeAll = synchronize;
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Randomize Now (Editor)")]
    private void RandomizeInEditor()
    {
        // Auto-populate target renderers if none specified
        if (targetRenderers.Count == 0)
        {
            targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
        }
        
        RandomizeMaterials();
    }
    
    [ContextMenu("Find All Renderers")]
    private void FindAllRenderers()
    {
        targetRenderers.Clear();
        targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
        Debug.Log($"[MaterialRandomizer] Found {targetRenderers.Count} renderers");
    }
    
    [ContextMenu("Log Material Info")]
    private void LogMaterialInfo()
    {
        Debug.Log($"[MaterialRandomizer] === Material Info for {gameObject.name} ===");
        Debug.Log($"Synchronize All: {synchronizeAll}");
        Debug.Log($"Material Options: {materialOptions.Count}");
        Debug.Log($"Target Renderers: {targetRenderers.Count}");
        
        for (int i = 0; i < materialOptions.Count; i++)
        {
            var option = materialOptions[i];
            string matName = option.material != null ? option.material.name : "NULL";
            Debug.Log($"  Option {i}: {matName} at index {option.materialIndex}");
        }
        
        foreach (var renderer in targetRenderers)
        {
            if (renderer != null)
            {
                Debug.Log($"  Renderer: {renderer.gameObject.name} - {renderer.materials.Length} materials");
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    Debug.Log($"    [{i}] {renderer.materials[i].name}");
                }
            }
        }
    }
    #endif
}
