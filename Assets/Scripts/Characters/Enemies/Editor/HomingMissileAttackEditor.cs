using UnityEngine;
using UnityEditor;

namespace Enemies.Editor
{
    /// <summary>
    /// Custom editor for HomingMissileAttack to provide organized inspector
    /// Inherits from ProjectileAttackEditor to reuse projectile settings
    /// </summary>
    [CustomEditor(typeof(Combat.Attacks.HomingMissileAttack))]
    public class HomingMissileAttackEditor : ProjectileAttackEditor
    {
        private SerializedProperty homingDuration;
        private SerializedProperty turnSpeed;
        private SerializedProperty explodeOnTimeout;
        private SerializedProperty missileSpeedMultiplier;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                homingDuration = serializedObject.FindProperty("homingDuration");
                turnSpeed = serializedObject.FindProperty("turnSpeed");
                explodeOnTimeout = serializedObject.FindProperty("explodeOnTimeout");
                missileSpeedMultiplier = serializedObject.FindProperty("missileSpeedMultiplier");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // First draw the parent's projectile settings
            base.DrawChildSpecificSettings();
            
            // Add homing missile specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Homing Missile Settings", EditorStyles.boldLabel);
            
            if (homingDuration != null)
            {
                float duration = homingDuration.floatValue;
                duration = Mathf.Max(0.1f, EditorGUILayout.FloatField("Homing Duration", duration));
                homingDuration.floatValue = duration;
            }
            
            if (turnSpeed != null)
            {
                float speed = turnSpeed.floatValue;
                speed = Mathf.Max(0f, EditorGUILayout.FloatField("Turn Speed (°/s)", speed));
                turnSpeed.floatValue = speed;
            }
            
            if (missileSpeedMultiplier != null)
            {
                float multiplier = missileSpeedMultiplier.floatValue;
                multiplier = Mathf.Max(0.1f, EditorGUILayout.FloatField("Missile Speed Multiplier", multiplier));
                missileSpeedMultiplier.floatValue = multiplier;
            }
            
            if (explodeOnTimeout != null)
            {
                EditorGUILayout.PropertyField(explodeOnTimeout);
            }
            
            // Add helpful info box
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Homing missiles track their target for the specified duration. " +
                "Make sure 'Use Trigger Based Damage' is UNCHECKED for instant area damage on impact.",
                MessageType.Info
            );
        }

        protected override void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            // Get projectile-specific values
            var projectileSpawnHeightProp = serializedObject.FindProperty("projectileSpawnHeight");
            var fallbackDamageRadiusProp = serializedObject.FindProperty("fallbackDamageRadius");
            
            float spawnHeight = projectileSpawnHeightProp != null ? projectileSpawnHeightProp.floatValue : 1.5f;
            float damageRadius = fallbackDamageRadiusProp != null ? fallbackDamageRadiusProp.floatValue : 2f;
            float duration = homingDuration != null ? homingDuration.floatValue : 3f;
            
            // Use EditorGUILayout for proper layout integration
            Rect rect = GUILayoutUtility.GetRect(0, 160, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions
            float centerX = rect.x + rect.width / 2 + 60;
            float centerY = rect.y + rect.height / 2;
            float maxVisualRange = Mathf.Min((rect.width - 120), rect.height) / 2 - 15;
            
            // Scale factors for visualization
            float scale = maxVisualRange / Mathf.Max(maxRange, 1f);
            
            // Draw ranges as concentric circles
            Handles.BeginGUI();
            
            // Min range (too close zone) - gray (homing missiles typically have 0 min range)
            if (minRange > 0)
            {
                float visualMinRange = minRange * scale;
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMinRange);
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMinRange);
            }
            
            // Max range (tracking range) - magenta
            float visualMaxRange = maxRange * scale;
            Handles.color = new Color(1f, 0f, 1f, 0.2f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            Handles.color = new Color(1f, 0f, 1f, 0.8f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            
            // Draw homing trajectory curves (multiple paths to show tracking behavior)
            Handles.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange for missiles
            
            // Draw 3 example missile paths with curves
            for (int i = 0; i < 3; i++)
            {
                float angle = (i - 1) * 25f; // -25, 0, +25 degrees
                Vector3 startDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 startPos = new Vector3(centerX, centerY, 0);
                Vector3 spawnOffset = new Vector3(0, -spawnHeight * scale * 0.5f, 0);
                
                // Draw curved path (simulating homing behavior)
                Vector3 currentPos = startPos + spawnOffset;
                int segments = 12;
                
                for (int j = 0; j < segments; j++)
                {
                    float t = j / (float)segments;
                    float nextT = (j + 1) / (float)segments;
                    
                    // Calculate curved trajectory (starts straight, curves toward center)
                    Vector3 straightComponent = startDir * visualMaxRange * 0.7f * t;
                    Vector3 homingComponent = -startDir * visualMaxRange * 0.5f * (t * t); // Quadratic curve back
                    
                    Vector3 nextStraightComponent = startDir * visualMaxRange * 0.7f * nextT;
                    Vector3 nextHomingComponent = -startDir * visualMaxRange * 0.5f * (nextT * nextT);
                    
                    Vector3 pos = startPos + spawnOffset + straightComponent + homingComponent;
                    Vector3 nextPos = startPos + spawnOffset + nextStraightComponent + nextHomingComponent;
                    
                    // Fade alpha along the path
                    Handles.color = new Color(1f, 0.5f, 0f, 0.8f - t * 0.5f);
                    Handles.DrawLine(pos, nextPos);
                }
                
                // Draw missile at end of path
                Vector3 endPos = startPos + spawnOffset + startDir * visualMaxRange * 0.7f * 0.7f;
                Handles.color = new Color(1f, 0.3f, 0f, 1f);
                Handles.DrawSolidDisc(endPos, Vector3.forward, 3);
            }
            
            // Draw explosion radius at a sample impact point
            Vector3 sampleImpactPos = new Vector3(centerX, centerY, 0) + new Vector3(0, visualMaxRange * 0.4f, 0);
            float visualDamageRadius = damageRadius * scale;
            Handles.color = new Color(1f, 0.3f, 0f, 0.3f);
            Handles.DrawSolidDisc(sampleImpactPos, Vector3.forward, visualDamageRadius);
            Handles.color = new Color(1f, 0.3f, 0f, 0.8f);
            Handles.DrawWireDisc(sampleImpactPos, Vector3.forward, visualDamageRadius);
            
            // Draw projectile spawn position - yellow
            float visualSpawnHeight = spawnHeight * scale * 0.5f;
            Handles.color = new Color(1f, 1f, 0f, 0.8f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY - visualSpawnHeight, 0), Vector3.forward, 3);
            
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
            GUI.Label(new Rect(labelX, labelY + 24, 120, 12), $"Spawn Height: {spawnHeight:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 36, 120, 12), $"Tracking: {duration:F1}s", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 48, 120, 12), $"Explosion: {damageRadius:F1}m", labelStyle);
            
            // Legend - positioned at bottom right
            float legendX = rect.x + rect.width - 120;
            float legendY = rect.y + rect.height - 55;
            GUI.Label(new Rect(legendX, legendY, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 10, 115, 10), "Magenta: Track Range", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 20, 115, 10), "Orange: Missiles", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 30, 115, 10), "Orange Circle: Blast", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 40, 115, 10), "Yellow: Spawn", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 50, 115, 10), "White: Enemy", labelStyle);
        }
    }
}

