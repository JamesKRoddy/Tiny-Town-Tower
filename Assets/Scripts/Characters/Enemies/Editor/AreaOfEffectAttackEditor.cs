using UnityEngine;
using UnityEditor;
using Enemies.Attacks;

namespace Enemies.Editor
{
    [CustomEditor(typeof(AreaOfEffectAttack))]
    [CanEditMultipleObjects]
    public class AreaOfEffectAttackEditor : AttackBaseEditor
    {
        private SerializedProperty aoeRadius;
        private SerializedProperty aoeDuration;
        private SerializedProperty damageInterval;
        private SerializedProperty damagePerTick;
        private SerializedProperty poiseDamagePerTick;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                aoeRadius = serializedObject.FindProperty("aoeRadius");
                aoeDuration = serializedObject.FindProperty("aoeDuration");
                damageInterval = serializedObject.FindProperty("damageInterval");
                damagePerTick = serializedObject.FindProperty("damagePerTick");
                poiseDamagePerTick = serializedObject.FindProperty("poiseDamagePerTick");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // Add area of effect specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Area of Effect Settings", EditorStyles.boldLabel);
            
            if (aoeRadius != null)
            {
                float radius = aoeRadius.floatValue;
                radius = Mathf.Max(0f, EditorGUILayout.FloatField("AOE Radius", radius));
                aoeRadius.floatValue = radius;
            }
            
            if (aoeDuration != null)
            {
                float duration = aoeDuration.floatValue;
                duration = Mathf.Max(0f, EditorGUILayout.FloatField("AOE Duration", duration));
                aoeDuration.floatValue = duration;
            }
            
            if (damageInterval != null)
            {
                float interval = damageInterval.floatValue;
                interval = Mathf.Max(0.01f, EditorGUILayout.FloatField("Damage Interval", interval));
                damageInterval.floatValue = interval;
            }
            
            if (damagePerTick != null)
            {
                float damage = damagePerTick.floatValue;
                damage = Mathf.Max(0f, EditorGUILayout.FloatField("Damage Per Tick", damage));
                damagePerTick.floatValue = damage;
            }
            
            if (poiseDamagePerTick != null)
            {
                float poiseDamage = poiseDamagePerTick.floatValue;
                poiseDamage = Mathf.Max(0f, EditorGUILayout.FloatField("Poise Damage Per Tick", poiseDamage));
                poiseDamagePerTick.floatValue = poiseDamage;
            }
        }


        protected override void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            // Get AOE-specific values
            float aoeRadius = this.aoeRadius != null ? this.aoeRadius.floatValue : 5f;
            
            // Use EditorGUILayout for proper layout integration
            Rect rect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions - adjust center to account for labels
            float centerX = rect.x + rect.width / 2 + 60;
            float centerY = rect.y + rect.height / 2;
            float maxVisualRange = Mathf.Min((rect.width - 120), rect.height) / 2 - 15;
            
            // Scale factors for visualization - need to account for both attack range and AOE radius
            float maxDistance = Mathf.Max(maxRange + aoeRadius, 1f);
            float scale = maxVisualRange / maxDistance;
            
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
            
            // Max range (attack targeting range) - red
            float visualMaxRange = maxRange * scale;
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.2f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            
            // Draw attack angle cone - cyan
            float angleRad = attackAngleThreshold * Mathf.Deg2Rad;
            Vector3 rightDir = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
            Vector3 leftDir = new Vector3(-Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
            
            Handles.color = new Color(0f, 1f, 1f, 0.6f);
            Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + rightDir.x, centerY + rightDir.z, 0));
            Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + leftDir.x, centerY + leftDir.z, 0));
            
            // Draw example AOE at target position (at max range)
            float visualAoeRadius = aoeRadius * scale;
            Vector3 aoeCenter = new Vector3(centerX, centerY - visualMaxRange, 0); // Position at max range
            Handles.color = new Color(1f, 0f, 1f, 0.3f); // Magenta
            Handles.DrawSolidDisc(aoeCenter, Vector3.forward, visualAoeRadius);
            Handles.color = new Color(1f, 0f, 1f, 0.9f);
            Handles.DrawWireDisc(aoeCenter, Vector3.forward, visualAoeRadius);
            
            // Draw center point (enemy position) - white
            Handles.color = Color.white;
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, 4);
            
            // Draw target indicator
            Handles.color = new Color(1f, 1f, 0f, 0.8f);
            Handles.DrawSolidDisc(aoeCenter, Vector3.forward, 3);
            
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
            GUI.Label(new Rect(labelX, labelY + 24, 120, 12), $"AOE Radius: {aoeRadius:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 36, 120, 12), $"Angle: ±{attackAngleThreshold:F0}°", labelStyle);
            
            // Legend - positioned at bottom right
            float legendX = rect.x + rect.width - 120;
            float legendY = rect.y + rect.height - 45;
            GUI.Label(new Rect(legendX, legendY, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 10, 115, 10), "Gray: Too Close", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 20, 115, 10), "Red: Range", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 30, 115, 10), "Magenta: AOE", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 40, 115, 10), "Cyan: Angle", labelStyle);
        }
    }
}

