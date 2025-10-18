using UnityEngine;
using UnityEditor;
using Enemies.Attacks;

namespace Enemies.Editor
{
    [CustomEditor(typeof(ShockwaveAttack))]
    [CanEditMultipleObjects]
    public class ShockwaveAttackEditor : AttackBaseEditor
    {
        private SerializedProperty maxShockwaveRadius;
        private SerializedProperty shockwaveSpeed;
        private SerializedProperty shockwaveWidth;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                maxShockwaveRadius = serializedObject.FindProperty("maxShockwaveRadius");
                shockwaveSpeed = serializedObject.FindProperty("shockwaveSpeed");
                shockwaveWidth = serializedObject.FindProperty("shockwaveWidth");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // Add shockwave specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Shockwave Settings", EditorStyles.boldLabel);
            
            if (maxShockwaveRadius != null)
            {
                float radius = maxShockwaveRadius.floatValue;
                radius = Mathf.Max(0f, EditorGUILayout.FloatField("Max Shockwave Radius", radius));
                maxShockwaveRadius.floatValue = radius;
            }
            
            if (shockwaveSpeed != null)
            {
                float speed = shockwaveSpeed.floatValue;
                speed = Mathf.Max(0.1f, EditorGUILayout.FloatField("Shockwave Speed", speed));
                shockwaveSpeed.floatValue = speed;
            }
            
            if (shockwaveWidth != null)
            {
                float width = shockwaveWidth.floatValue;
                width = Mathf.Max(0.1f, EditorGUILayout.FloatField("Shockwave Width", width));
                shockwaveWidth.floatValue = width;
            }
            
        }

        protected override void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            // Get shockwave-specific values
            float shockRadius = maxShockwaveRadius != null ? maxShockwaveRadius.floatValue : 10f;
            float shockWidth = shockwaveWidth != null ? shockwaveWidth.floatValue : 1f;
            
            // Use EditorGUILayout for proper layout integration
            Rect rect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions - adjust center to account for labels
            float centerX = rect.x + rect.width / 2 + 60;
            float centerY = rect.y + rect.height / 2;
            float maxVisualRange = Mathf.Min((rect.width - 120), rect.height) / 2 - 15;
            
            // Scale factors for visualization
            float scale = maxVisualRange / Mathf.Max(shockRadius, maxRange, 1f);
            
            // Draw ranges as concentric circles
            Handles.BeginGUI();
            
            // Min range (too close zone) - gray
            if (minRange > 0)
            {
                float visualMinRange = minRange * scale;
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMinRange);
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMinRange);
            }
            
            // Max range (attack activation range) - red
            float visualMaxRange = maxRange * scale;
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.2f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            
            // Shockwave radius - orange
            float visualShockwaveRadius = shockRadius * scale;
            Handles.color = new Color(1f, 0.5f, 0f, 0.3f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualShockwaveRadius);
            Handles.color = new Color(1f, 0.5f, 0f, 0.9f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualShockwaveRadius);
            
            // Draw shockwave width ring - brighter orange
            float visualShockwaveInner = (shockRadius - shockWidth) * scale;
            if (visualShockwaveInner > 0)
            {
                Handles.color = new Color(1f, 0.7f, 0.2f, 0.7f);
                Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualShockwaveInner);
            }
            
            // Draw attack angle cone - cyan
            float angleRad = attackAngleThreshold * Mathf.Deg2Rad;
            Vector3 rightDir = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
            Vector3 leftDir = new Vector3(-Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
            
            Handles.color = new Color(0f, 1f, 1f, 0.6f);
            Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + rightDir.x, centerY + rightDir.z, 0));
            Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + leftDir.x, centerY + leftDir.z, 0));
            
            // Draw center point (enemy position) - white
            Handles.color = Color.white;
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, 4);
            
            Handles.EndGUI();
            
            // Labels - positioned to avoid overlap
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 8;
            labelStyle.normal.textColor = Color.white;
            
            // Left side labels
            float labelX = rect.x + 5;
            float labelY = rect.y + 5;
            GUI.Label(new Rect(labelX, labelY, 120, 12), $"Min: {minRange:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 12, 120, 12), $"Max: {maxRange:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 24, 120, 12), $"Shockwave: {shockRadius:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 36, 120, 12), $"Width: {shockWidth:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 48, 120, 12), $"Angle: ±{attackAngleThreshold:F0}°", labelStyle);
            
            // Legend - positioned at bottom right
            float legendX = rect.x + rect.width - 120;
            float legendY = rect.y + rect.height - 45;
            GUI.Label(new Rect(legendX, legendY, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 10, 115, 10), "Gray: Too Close", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 20, 115, 10), "Red: Range", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 30, 115, 10), "Orange: Shockwave", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 40, 115, 10), "Cyan: Angle", labelStyle);
        }

    }
}

