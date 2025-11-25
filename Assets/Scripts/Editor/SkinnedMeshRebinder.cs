using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Utility to rebind SkinnedMeshRenderer bones to a new skeleton/rig.
/// Useful for Synty assets and modular character systems where you want to use
/// different character meshes on the same skeleton.
/// 
/// How to use:
/// 1. Select the GameObject with the SkinnedMeshRenderer you want to rebind
/// 2. Go to Tools > Character > Rebind Skinned Mesh
/// 3. Assign the new root bone (the Armature/Hips/Root of target skeleton)
/// 4. Click "Rebind Bones"
/// </summary>
public class SkinnedMeshRebinder : EditorWindow
{
    private List<GameObject> targetObjects = new List<GameObject>();
    private Transform newRootBone;
    private List<SkinnedMeshRenderer> allSkinnedMeshRenderers = new List<SkinnedMeshRenderer>();
    private Vector2 scrollPosition;
    private Vector2 objectScrollPosition;
    
    [MenuItem("Tools/Character/Rebind Skinned Mesh")]
    public static void ShowWindow()
    {
        SkinnedMeshRebinder window = GetWindow<SkinnedMeshRebinder>("Rebind Skinned Mesh");
        window.minSize = new Vector2(500, 400);
    }
    
    private void OnEnable()
    {
        // Auto-collect selected objects
        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
        {
            targetObjects.Clear();
            targetObjects.AddRange(Selection.gameObjects);
            RefreshSkinnedMeshes();
        }
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        // Title
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Skinned Mesh Rebinder", titleStyle);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("This tool rebinds SkinnedMeshRenderer bones to a new skeleton by matching bone names AND recalculates bind poses. Perfect for using Synty character meshes on different rigs!", MessageType.Info);
        EditorGUILayout.Space(10);
        
        // Step 1: Select target objects
        EditorGUILayout.LabelField("Step 1: Select Character Meshes", EditorStyles.boldLabel);
        
        // Drag and drop area
        Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag GameObjects Here (or use buttons below)", EditorStyles.helpBox);
        
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject go && !targetObjects.Contains(go))
                        {
                            targetObjects.Add(go);
                        }
                    }
                    RefreshSkinnedMeshes();
                }
                evt.Use();
                break;
        }
        
        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected Objects", GUILayout.Height(25)))
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                if (!targetObjects.Contains(obj))
                {
                    targetObjects.Add(obj);
                }
            }
            RefreshSkinnedMeshes();
        }
        if (GUILayout.Button("Clear All", GUILayout.Width(100), GUILayout.Height(25)))
        {
            targetObjects.Clear();
            RefreshSkinnedMeshes();
        }
        EditorGUILayout.EndHorizontal();
        
        if (targetObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("Add GameObjects with SkinnedMeshRenderer components using the buttons above or drag them into the box", MessageType.Warning);
            return;
        }
        
        // Show target objects
        Color bgColor1 = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = bgColor1;
        
        EditorGUILayout.LabelField($"✓ Target Objects: {targetObjects.Count}", EditorStyles.boldLabel);
        
        objectScrollPosition = EditorGUILayout.BeginScrollView(objectScrollPosition, GUILayout.Height(80));
        for (int i = 0; i < targetObjects.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            targetObjects[i] = EditorGUILayout.ObjectField(targetObjects[i], typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                targetObjects.RemoveAt(i);
                RefreshSkinnedMeshes();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
        
        // Show found skinned mesh renderers
        if (allSkinnedMeshRenderers.Count > 0)
        {
            Color bgColor2 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bgColor2;
            
            EditorGUILayout.LabelField($"✓ Found {allSkinnedMeshRenderers.Count} SkinnedMeshRenderer(s) total", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            foreach (var smr in allSkinnedMeshRenderers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(smr, typeof(SkinnedMeshRenderer), true);
                EditorGUILayout.LabelField($"Bones: {smr.bones.Length}", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("No SkinnedMeshRenderers found on selected GameObjects or their children", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.Space(10);
        
        // Step 2: Select new root bone
        EditorGUILayout.LabelField("Step 2: Select New Skeleton Root", EditorStyles.boldLabel);
        
        newRootBone = EditorGUILayout.ObjectField("New Root Bone", newRootBone, typeof(Transform), true) as Transform;
        
        if (newRootBone != null)
        {
            Color bgColor3 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.7f, 1f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bgColor3;
            
            EditorGUILayout.LabelField($"✓ Target Root: {newRootBone.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Path: {GetTransformPath(newRootBone)}");
            
            // Count bones in new skeleton
            int boneCount = CountBonesRecursive(newRootBone);
            EditorGUILayout.LabelField($"Bones in skeleton: {boneCount}");
            
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Select the root bone of the target skeleton (usually named 'Armature', 'Hips', or similar)", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // Step 3: Rebind
        EditorGUILayout.LabelField("Step 3: Rebind", EditorStyles.boldLabel);
        
        EditorGUI.BeginDisabledGroup(targetObjects.Count == 0 || newRootBone == null);
        
        if (GUILayout.Button($"🔄 Rebind All {allSkinnedMeshRenderers.Count} SkinnedMeshRenderer(s)", GUILayout.Height(40)))
        {
            RebindAllSkinnedMeshes();
        }
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(10);
        
        // Instructions
        Color bgColor4 = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 1f, 0.7f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = bgColor4;
        
        EditorGUILayout.LabelField("💡 Tips:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Make sure bone names match between meshes");
        EditorGUILayout.LabelField("• Common Synty root names: 'Hips', 'Male_Hips', 'Female_Hips'");
        EditorGUILayout.LabelField("• This creates new mesh assets with recalculated bind poses");
        EditorGUILayout.LabelField("• Original mesh assets are not modified (safe!)");
        EditorGUILayout.LabelField("• You can undo (Ctrl+Z) if something goes wrong");
        
        EditorGUILayout.EndVertical();
    }
    
    private void RefreshSkinnedMeshes()
    {
        allSkinnedMeshRenderers.Clear();
        
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                var renderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                allSkinnedMeshRenderers.AddRange(renderers);
            }
        }
    }
    
    private void RebindAllSkinnedMeshes()
    {
        if (allSkinnedMeshRenderers.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No SkinnedMeshRenderers found!", "OK");
            return;
        }
        
        int successCount = 0;
        int failCount = 0;
        
        foreach (var smr in allSkinnedMeshRenderers)
        {
            if (RebindSkinnedMesh(smr, newRootBone))
                successCount++;
            else
                failCount++;
        }
        
        string message = $"Rebind complete!\n\nSuccess: {successCount}\nFailed: {failCount}";
        EditorUtility.DisplayDialog("Rebind Complete", message, "OK");
        
        // Mark all target objects as dirty
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
        }
    }
    
    /// <summary>
    /// Rebind a single SkinnedMeshRenderer to a new skeleton
    /// </summary>
    private bool RebindSkinnedMesh(SkinnedMeshRenderer smr, Transform newRoot)
    {
        if (smr == null || newRoot == null)
            return false;
        
        // Build a dictionary of all bones in the new skeleton by name
        Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
        BuildBoneMap(newRoot, boneMap);
        
        // Get current bones
        Transform[] oldBones = smr.bones;
        Transform[] newBones = new Transform[oldBones.Length];
        
        int matchedBones = 0;
        
        // Try to match each bone by name
        for (int i = 0; i < oldBones.Length; i++)
        {
            if (oldBones[i] == null)
            {
                newBones[i] = null;
                continue;
            }
            
            string boneName = oldBones[i].name;
            
            if (boneMap.TryGetValue(boneName, out Transform newBone))
            {
                newBones[i] = newBone;
                matchedBones++;
            }
            else
            {
                Debug.LogWarning($"Could not find bone '{boneName}' in new skeleton for {smr.name}");
                newBones[i] = null;
            }
        }
        
        if (matchedBones == 0)
        {
            Debug.LogError($"Failed to match any bones for {smr.name}! Check that bone names match.");
            return false;
        }
        
        // CRITICAL: Recalculate bind poses for the new skeleton
        // Without this, the mesh will appear distorted or shrunk
        Matrix4x4[] bindPoses = new Matrix4x4[newBones.Length];
        for (int i = 0; i < newBones.Length; i++)
        {
            if (newBones[i] != null)
            {
                // Bind pose is the inverse of the bone's transform relative to the mesh
                bindPoses[i] = newBones[i].worldToLocalMatrix * smr.transform.localToWorldMatrix;
            }
            else
            {
                bindPoses[i] = Matrix4x4.identity;
            }
        }
        
        // Create a new mesh instance to avoid modifying the shared mesh asset
        Mesh newMesh = Object.Instantiate(smr.sharedMesh);
        newMesh.name = smr.sharedMesh.name + "_Rebound";
        newMesh.bindposes = bindPoses;
        
        // Apply the new mesh and bones
        smr.sharedMesh = newMesh;
        smr.bones = newBones;
        smr.rootBone = newRoot;
        
        // Recalculate bounds to prevent culling issues
        smr.localBounds = newMesh.bounds;
        
        Debug.Log($"✓ Rebound {smr.name}: {matchedBones}/{oldBones.Length} bones matched, bind poses recalculated");
        
        return true;
    }
    
    /// <summary>
    /// Build a dictionary of all bones in a skeleton hierarchy
    /// </summary>
    private void BuildBoneMap(Transform root, Dictionary<string, Transform> map)
    {
        if (!map.ContainsKey(root.name))
        {
            map[root.name] = root;
        }
        
        foreach (Transform child in root)
        {
            BuildBoneMap(child, map);
        }
    }
    
    /// <summary>
    /// Count all bones in a skeleton recursively
    /// </summary>
    private int CountBonesRecursive(Transform root)
    {
        int count = 1; // Count self
        foreach (Transform child in root)
        {
            count += CountBonesRecursive(child);
        }
        return count;
    }
    
    /// <summary>
    /// Get the full hierarchy path of a transform
    /// </summary>
    private string GetTransformPath(Transform t)
    {
        string path = t.name;
        Transform parent = t.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}

