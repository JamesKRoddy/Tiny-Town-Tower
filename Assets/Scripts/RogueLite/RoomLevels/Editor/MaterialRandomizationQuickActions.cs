using UnityEngine;
using UnityEditor;

/// <summary>
/// Quick action menu items for material randomization
/// </summary>
public static class MaterialRandomizationQuickActions
{
    [MenuItem("GameObject/Material Randomization/Add Room Material Manager", false, 0)]
    private static void AddRoomMaterialManager()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject first.", "OK");
            return;
        }
        
        var manager = Selection.activeGameObject.GetComponent<RoomMaterialManager>();
        if (manager == null)
        {
            manager = Selection.activeGameObject.AddComponent<RoomMaterialManager>();
            Debug.Log($"[MaterialRandomization] Added RoomMaterialManager to {Selection.activeGameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[MaterialRandomization] RoomMaterialManager already exists on {Selection.activeGameObject.name}");
        }
        
        Selection.activeObject = manager;
    }
    
    [MenuItem("GameObject/Material Randomization/Add Material Randomizer (Single Object)", false, 1)]
    private static void AddMaterialRandomizer()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject first.", "OK");
            return;
        }
        
        var randomizer = Selection.activeGameObject.GetComponent<MaterialRandomizer>();
        if (randomizer == null)
        {
            randomizer = Selection.activeGameObject.AddComponent<MaterialRandomizer>();
            Debug.Log($"[MaterialRandomization] Added MaterialRandomizer to {Selection.activeGameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[MaterialRandomization] MaterialRandomizer already exists on {Selection.activeGameObject.name}");
        }
        
        Selection.activeObject = randomizer;
    }
    
    [MenuItem("GameObject/Material Randomization/Open Setup Tool", false, 20)]
    private static void OpenSetupTool()
    {
        RoomMaterialSetupTool.ShowWindow();
    }
    
    [MenuItem("GameObject/Material Randomization/Randomize Selected Room", false, 30)]
    private static void RandomizeSelectedRoom()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a room GameObject first.", "OK");
            return;
        }
        
        var manager = Selection.activeGameObject.GetComponent<RoomMaterialManager>();
        if (manager == null)
        {
            manager = Selection.activeGameObject.GetComponentInParent<RoomMaterialManager>();
        }
        
        if (manager != null)
        {
            manager.RandomizeAllMaterials();
            Debug.Log($"[MaterialRandomization] Randomized materials for {manager.gameObject.name}");
        }
        else
        {
            EditorUtility.DisplayDialog("No Manager Found", 
                "No RoomMaterialManager found on selected GameObject or its parents.", "OK");
        }
    }
    
    [MenuItem("GameObject/Material Randomization/Find Material Variants for Selected", false, 40)]
    private static void FindMaterialVariantsForSelected()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject with a Renderer.", "OK");
            return;
        }
        
        var renderer = Selection.activeGameObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            EditorUtility.DisplayDialog("No Renderer", "Selected GameObject has no Renderer component.", "OK");
            return;
        }
        
        if (renderer.sharedMaterials.Length == 0)
        {
            EditorUtility.DisplayDialog("No Materials", "Renderer has no materials assigned.", "OK");
            return;
        }
        
        Debug.Log($"=== Material Variants for {Selection.activeGameObject.name} ===");
        
        foreach (var material in renderer.sharedMaterials)
        {
            if (material == null) continue;
            
            string baseName = GetMaterialBaseName(material);
            var variants = FindMaterialVariants(material);
            
            Debug.Log($"\nMaterial: {material.name}");
            Debug.Log($"Base Name: {baseName}");
            Debug.Log($"Variants Found: {variants.Count}");
            
            foreach (var variant in variants)
            {
                Debug.Log($"  - {variant.name} ({AssetDatabase.GetAssetPath(variant)})");
            }
        }
    }
    
    // Helper methods from RoomMaterialSetupTool
    private static string GetMaterialBaseName(Material material)
    {
        string name = material.name;
        
        if (name.Length >= 2)
        {
            if (name[name.Length - 2] == '_' && char.IsDigit(name[name.Length - 1]))
                return name.Substring(0, name.Length - 2);
            
            if (name[name.Length - 2] == '_' && char.IsLetter(name[name.Length - 1]))
                return name.Substring(0, name.Length - 2);
        }
        
        if (name.Length >= 1 && char.IsDigit(name[name.Length - 1]))
            return name.Substring(0, name.Length - 1);
        
        return name;
    }
    
    private static System.Collections.Generic.List<Material> FindMaterialVariants(Material baseMaterial)
    {
        var variants = new System.Collections.Generic.List<Material>();
        
        string path = AssetDatabase.GetAssetPath(baseMaterial);
        if (string.IsNullOrEmpty(path)) return variants;
        
        string directory = System.IO.Path.GetDirectoryName(path);
        string baseName = GetMaterialBaseName(baseMaterial);
        
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { directory });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            
            if (mat != null)
            {
                string matBaseName = GetMaterialBaseName(mat);
                if (matBaseName.Equals(baseName, System.StringComparison.OrdinalIgnoreCase))
                {
                    variants.Add(mat);
                }
            }
        }
        
        return variants;
    }
    
    // Validation methods
    [MenuItem("GameObject/Material Randomization/Randomize Selected Room", true)]
    private static bool ValidateRandomizeSelectedRoom()
    {
        return Selection.activeGameObject != null;
    }
    
    [MenuItem("GameObject/Material Randomization/Find Material Variants for Selected", true)]
    private static bool ValidateFindMaterialVariants()
    {
        return Selection.activeGameObject != null && 
               Selection.activeGameObject.GetComponent<Renderer>() != null;
    }
}


