using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Editor tool to automatically setup material randomization for rooms.
/// Finds material variants based on naming patterns (suffixes like _1, _2, _a, _b, etc.)
/// </summary>
public class RoomMaterialSetupTool : EditorWindow
{
    private GameObject roomRoot;
    private GameObject propsParent;
    
    private bool autoFindParents = true;
    private bool createGroups = true;
    private bool logProgress = true;
    
    private Vector2 scrollPosition;
    private string statusMessage = "";
    
    [MenuItem("Tools/Room Material Setup Tool")]
    public static RoomMaterialSetupTool ShowWindow()
    {
        var window = GetWindow<RoomMaterialSetupTool>("Material Setup");
        window.minSize = new Vector2(400, 500);
        return window;
    }
    
    /// <summary>
    /// Set the room root GameObject (used when opening from RoomMaterialManager inspector)
    /// </summary>
    public void SetRoomRoot(GameObject root)
    {
        roomRoot = root;
        
        // Auto-find parents if enabled
        if (autoFindParents)
        {
            FindParentsAutomatically();
        }
        
        Repaint();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Room Material Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Automatically setup material randomization by detecting material variants and creating groups.", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Room root selection
        EditorGUILayout.LabelField("Step 1: Select Room", EditorStyles.boldLabel);
        roomRoot = (GameObject)EditorGUILayout.ObjectField("Room Root GameObject", roomRoot, typeof(GameObject), true);
        
        if (roomRoot != null)
        {
            autoFindParents = EditorGUILayout.Toggle("Auto-Find Parents", autoFindParents);
            
            EditorGUILayout.Space(5);
            
            if (autoFindParents)
            {
                EditorGUILayout.HelpBox("Will search for child objects named 'Props', 'Furniture', 'Objects', etc.", MessageType.Info);
                
                if (GUILayout.Button("Find Props Automatically"))
                {
                    FindParentsAutomatically();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Step 2: Select Props Parent (Optional)", EditorStyles.boldLabel);
                propsParent = (GameObject)EditorGUILayout.ObjectField("Props Parent", propsParent, typeof(GameObject), true);
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Step 3: Setup Options", EditorStyles.boldLabel);
            
            createGroups = EditorGUILayout.Toggle("Create Separate Groups", createGroups);
            EditorGUILayout.HelpBox(createGroups ? 
                "Will create separate groups per material type (beds, rugs, etc.)" : 
                "Will create a single group for all props", MessageType.Info);
            
            logProgress = EditorGUILayout.Toggle("Log Progress", logProgress);
            
            EditorGUILayout.Space(10);
            
            // Setup button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Setup Material Randomization", GUILayout.Height(40)))
            {
                SetupMaterialRandomization();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
            
            // Status message
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please select a Room Root GameObject to begin.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Material Detection Info", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool detects material variants by:\n" +
            "• Finding materials in the same folder\n" +
            "• With similar base names\n" +
            "• Ending in: _1, _2, _3 or _a, _b, _c\n" +
            "• Or last character differences\n\n" +
            "Example: Bed_Material_1, Bed_Material_2, Bed_Material_3\n\n" +
            "Note: Only props are randomized. Walls and floors are shared between rooms.", 
            MessageType.None);
        
        EditorGUILayout.EndScrollView();
    }
    
    private void FindParentsAutomatically()
    {
        if (roomRoot == null) return;
        
        // Common naming patterns for props
        string[] propNames = { "Props", "Prop", "props", "prop", "Furniture", "furniture", "Objects", "objects" };
        
        Transform[] allChildren = roomRoot.GetComponentsInChildren<Transform>(true);
        
        propsParent = FindObjectByNames(allChildren, propNames);
        
        statusMessage = $"Found: Props={propsParent != null}";
        
        if (logProgress)
        {
            Debug.Log($"[RoomMaterialSetup] Auto-found parents:\n" +
                     $"  Props: {(propsParent != null ? propsParent.name : "Not Found")}");
        }
    }
    
    private GameObject FindObjectByNames(Transform[] transforms, string[] names)
    {
        foreach (var name in names)
        {
            var found = transforms.FirstOrDefault(t => t.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
            if (found != null) return found.gameObject;
        }
        return null;
    }
    
    private void SetupMaterialRandomization()
    {
        if (roomRoot == null)
        {
            statusMessage = "Error: No room root selected!";
            return;
        }
        
        // Get or create RoomMaterialManager
        var manager = roomRoot.GetComponent<RoomMaterialManager>();
        if (manager == null)
        {
            manager = roomRoot.AddComponent<RoomMaterialManager>();
            if (logProgress) Debug.Log($"[RoomMaterialSetup] Added RoomMaterialManager to {roomRoot.name}");
        }
        
        // Clear existing groups
        manager.ClearAllGroups();
        
        int totalGroups = 0;
        int totalRenderers = 0;
        int totalVariants = 0;
        
        // Setup props only
        if (propsParent != null)
        {
            var result = SetupPropsGroup(manager, propsParent);
            totalGroups += result.groups;
            totalRenderers += result.renderers;
            totalVariants += result.variants;
        }
        else
        {
            statusMessage = "Error: No props parent found!";
            return;
        }
        
        // Mark dirty for saving
        EditorUtility.SetDirty(manager);
        
        statusMessage = $"Setup Complete!\nGroups: {totalGroups} | Renderers: {totalRenderers} | Variants: {totalVariants}";
        
        if (logProgress)
        {
            Debug.Log($"[RoomMaterialSetup] Setup complete for {roomRoot.name}:\n" +
                     $"  Groups Created: {totalGroups}\n" +
                     $"  Renderers Added: {totalRenderers}\n" +
                     $"  Material Variants Found: {totalVariants}");
        }
    }
    
    private (int groups, int renderers, int variants) SetupPropsGroup(RoomMaterialManager manager, GameObject parent)
    {
        var renderers = parent.GetComponentsInChildren<Renderer>(true);
        
        if (renderers.Length == 0)
        {
            if (logProgress) Debug.LogWarning($"[RoomMaterialSetup] No renderers found in {parent.name}");
            return (0, 0, 0);
        }
        
        // PROPS: Group by material base name, create separate groups per material type
        Dictionary<string, List<Renderer>> materialGroups = new Dictionary<string, List<Renderer>>();
        Dictionary<string, List<Material>> variantsByBaseName = new Dictionary<string, List<Material>>();
        
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterials.Length == 0) continue;
            
            for (int matIndex = 0; matIndex < renderer.sharedMaterials.Length; matIndex++)
            {
                var material = renderer.sharedMaterials[matIndex];
                if (material == null) continue;
                
                // Get the base name and find variants
                string baseName = GetMaterialBaseName(material);
                var variants = FindMaterialVariants(material);
                
                if (variants.Count > 1) // Only setup if there are multiple variants
                {
                    string key = $"{baseName}_{matIndex}";
                    
                    if (!materialGroups.ContainsKey(key))
                    {
                        materialGroups[key] = new List<Renderer>();
                        variantsByBaseName[key] = variants;
                    }
                    
                    if (!materialGroups[key].Contains(renderer))
                    {
                        materialGroups[key].Add(renderer);
                    }
                }
            }
        }
        
        // Create groups in the manager
        int groupCount = 0;
        int rendererCount = 0;
        int variantCount = 0;
        
        foreach (var kvp in materialGroups)
        {
            string groupName = createGroups ? $"Props - {kvp.Key}" : "Props";
            
            var group = manager.AddGroup(groupName, false);
            group.renderers.AddRange(kvp.Value);
            
            // Add material variants
            var variants = variantsByBaseName[kvp.Key];
            int materialIndex = ExtractMaterialIndex(kvp.Key);
            
            foreach (var variant in variants)
            {
                group.materialVariants.Add(new MaterialRandomizer.MaterialOption(variant, materialIndex));
            }
            
            groupCount++;
            rendererCount += kvp.Value.Count;
            variantCount += variants.Count;
            
            if (logProgress)
            {
                Debug.Log($"[RoomMaterialSetup] Created group '{groupName}': {kvp.Value.Count} renderers, {variants.Count} variants");
            }
        }
        
        return (groupCount, rendererCount, variantCount);
    }
    
    private string GetMaterialBaseName(Material material)
    {
        string name = material.name;
        
        // Remove common suffixes
        if (name.Length >= 2)
        {
            // Check for _1, _2, _3, etc.
            if (name[name.Length - 2] == '_' && char.IsDigit(name[name.Length - 1]))
            {
                return name.Substring(0, name.Length - 2);
            }
            
            // Check for _a, _b, _c, etc.
            if (name[name.Length - 2] == '_' && char.IsLetter(name[name.Length - 1]))
            {
                return name.Substring(0, name.Length - 2);
            }
        }
        
        // Check for just digit at end
        if (name.Length >= 1 && char.IsDigit(name[name.Length - 1]))
        {
            return name.Substring(0, name.Length - 1);
        }
        
        return name;
    }
    
    private List<Material> FindMaterialVariants(Material baseMaterial)
    {
        List<Material> variants = new List<Material>();
        
        // Get the asset path
        string path = AssetDatabase.GetAssetPath(baseMaterial);
        if (string.IsNullOrEmpty(path)) return variants;
        
        string directory = Path.GetDirectoryName(path);
        string baseName = GetMaterialBaseName(baseMaterial);
        
        // Find all materials in the same directory
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { directory });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            
            if (mat != null)
            {
                string matBaseName = GetMaterialBaseName(mat);
                
                // Check if this material is a variant
                if (matBaseName.Equals(baseName, System.StringComparison.OrdinalIgnoreCase))
                {
                    variants.Add(mat);
                }
            }
        }
        
        return variants;
    }
    
    private int ExtractMaterialIndex(string key)
    {
        // Key format: "BaseName_Index"
        int lastUnderscore = key.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < key.Length - 1)
        {
            if (int.TryParse(key.Substring(lastUnderscore + 1), out int index))
            {
                return index;
            }
        }
        return 0;
    }
}


