using UnityEngine;
using UnityEditor;
using Enemies.Attacks;

namespace Enemies.Editor
{
    [CustomEditor(typeof(AnimationAttack))]
    [CanEditMultipleObjects]
    public class CloseRangeAttackEditor : AttackBaseEditor
    {
        private SerializedProperty attackRadius;
        private SerializedProperty targetLayer;
        private SerializedProperty buildingAttackRange;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                attackRadius = serializedObject.FindProperty("attackRadius");
                targetLayer = serializedObject.FindProperty("targetLayer");
                buildingAttackRange = serializedObject.FindProperty("buildingAttackRange");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // Add close range specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Close Range Settings", EditorStyles.boldLabel);
            
            if (attackRadius != null)
            {
                // Ensure non-negative values
                float radius = attackRadius.floatValue;
                radius = Mathf.Max(0f, EditorGUILayout.FloatField("Attack Radius", radius));
                attackRadius.floatValue = radius;
            }
            
            if (targetLayer != null) EditorGUILayout.PropertyField(targetLayer);
            
            if (buildingAttackRange != null)
            {
                // Ensure non-negative values
                float buildingRange = buildingAttackRange.floatValue;
                buildingRange = Mathf.Max(0f, EditorGUILayout.FloatField("Building Attack Range", buildingRange));
                buildingAttackRange.floatValue = buildingRange;
            }
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;
                
            serializedObject.Update();

            // Call base inspector (this draws all the common attack properties)
            base.OnInspectorGUI();

            // Add close range specific settings to the Attack Visualization Settings section
            // This will be handled by overriding the base class method

            // Add unified visual graphic for close range attacks
            EditorGUILayout.LabelField("Attack Visualization", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Get the values from the base class properties
            float minRange = serializedObject.FindProperty("minRange").floatValue;
            float maxRange = serializedObject.FindProperty("maxRange").floatValue;
            float attackAngleThreshold = serializedObject.FindProperty("attackAngleThreshold").floatValue;
            float radius = attackRadius != null ? attackRadius.floatValue : 1.5f;
            
            // Use EditorGUILayout with proper height allocation
            EditorGUILayout.BeginVertical(GUILayout.Height(160));
            DrawUnifiedAttackVisualization(minRange, maxRange, attackAngleThreshold, radius);
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUnifiedAttackVisualization(float minRange, float maxRange, float attackAngleThreshold, float attackRadius)
        {
            // Use EditorGUILayout for proper layout integration
            Rect rect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions - adjust center to account for labels
            float centerX = rect.x + rect.width / 2 + 60; // Move right to avoid left labels
            float centerY = rect.y + rect.height / 2;
            float maxVisualRange = Mathf.Min((rect.width - 120), rect.height) / 2 - 15; // Account for label space
            
            // Scale factors for visualization
            float scale = maxVisualRange / Mathf.Max(maxRange, attackRadius, 1f);
            
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
            
            // Max range (attack range) - red
            float visualMaxRange = maxRange * scale;
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.2f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            
            // Attack radius (damage area) - yellow
            float visualAttackRadius = attackRadius * scale;
            Handles.color = new Color(1f, 1f, 0f, 0.3f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualAttackRadius);
            Handles.color = new Color(1f, 1f, 0f, 0.8f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualAttackRadius);
            
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
            
            // Left side labels - positioned outside the circle
            float labelX = rect.x + 5;
            float labelY = rect.y + 5;
            GUI.Label(new Rect(labelX, labelY, 120, 12), $"Min: {minRange:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 12, 120, 12), $"Max: {maxRange:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 24, 120, 12), $"Radius: {attackRadius:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 36, 120, 12), $"Angle: ±{attackAngleThreshold:F0}°", labelStyle);
            
            // Legend - positioned at bottom right with more space
            float legendX = rect.x + rect.width - 120;
            float legendY = rect.y + rect.height - 45;
            GUI.Label(new Rect(legendX, legendY, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 10, 115, 10), "Gray: Too Close", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 20, 115, 10), "Red: Range", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 30, 115, 10), "Yellow: Damage", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 40, 115, 10), "Cyan: Angle", labelStyle);
        }
    }
}
