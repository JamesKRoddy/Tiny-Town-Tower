using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(BuildingRooms))]
public class BuildingRoomsPropertyDrawer : PropertyDrawer
{
    private Dictionary<GameObject, Texture2D> previewCache = new Dictionary<GameObject, Texture2D>();
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 88; // Much bigger height for 80x80 thumbnails
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var buildingRoomProp = property.FindPropertyRelative("buildingRoom");
        var difficultyProp = property.FindPropertyRelative("difficulty");
        var excludeFromTestingProp = property.FindPropertyRelative("excludeFromTesting");
        
        // Calculate rects
        var previewRect = new Rect(position.x, position.y, 80, 80);
        var objectFieldRect = new Rect(position.x + 88, position.y, position.width - 88, EditorGUIUtility.singleLineHeight);
        var difficultyRect = new Rect(position.x + 88, position.y + EditorGUIUtility.singleLineHeight + 2, 80, EditorGUIUtility.singleLineHeight);
        var excludeRect = new Rect(position.x + 175, position.y + EditorGUIUtility.singleLineHeight + 2, position.width - 175, EditorGUIUtility.singleLineHeight);
        
        // Draw preview
        if (buildingRoomProp.objectReferenceValue != null)
        {
            var preview = GetPrefabPreview(buildingRoomProp.objectReferenceValue as GameObject);
            if (preview != null)
            {
                GUI.DrawTexture(previewRect, preview);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, Color.gray);
                GUI.Label(previewRect, "?", EditorStyles.centeredGreyMiniLabel);
            }
        }
        else
        {
            EditorGUI.DrawRect(previewRect, Color.gray);
        }
        
        // Draw object field
        EditorGUI.ObjectField(objectFieldRect, buildingRoomProp, typeof(GameObject), GUIContent.none);
        
        // Draw difficulty
        EditorGUI.LabelField(new Rect(difficultyRect.x, difficultyRect.y, 70, difficultyRect.height), "Difficulty:");
        EditorGUI.PropertyField(new Rect(difficultyRect.x + 75, difficultyRect.y, 30, difficultyRect.height), difficultyProp, GUIContent.none);
        
        // Draw exclude from testing
        EditorGUI.LabelField(new Rect(excludeRect.x, excludeRect.y, 80, excludeRect.height), "Exclude:");
        EditorGUI.PropertyField(new Rect(excludeRect.x + 50, excludeRect.y, 20, excludeRect.height), excludeFromTestingProp, GUIContent.none);
        
        EditorGUI.EndProperty();
    }
    
    private Texture2D GetPrefabPreview(GameObject prefab)
    {
        if (prefab == null) return null;
        
        if (!previewCache.ContainsKey(prefab))
        {
            var preview = AssetPreview.GetAssetPreview(prefab);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(prefab);
            }
            if (preview == null)
            {
                var content = EditorGUIUtility.ObjectContent(prefab, typeof(GameObject));
                preview = content.image as Texture2D;
            }
            previewCache[prefab] = preview;
        }
        
        return previewCache[prefab];
    }
}
