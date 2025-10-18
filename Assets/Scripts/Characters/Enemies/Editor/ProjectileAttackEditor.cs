using UnityEngine;
using UnityEditor;

namespace Enemies.Editor
{
    /// <summary>
    /// Custom editor for ProjectileAttack to provide organized inspector
    /// </summary>
    [CustomEditor(typeof(Attacks.ProjectileAttack))]
    public class ProjectileAttackEditor : AttackBaseEditor
    {
        private SerializedProperty projectileSpawnHeight;
        private SerializedProperty projectileSpeed;
        private SerializedProperty projectileMaxHeight;
        private SerializedProperty createDamageAreaOnImpact;
        private SerializedProperty impactDamageDuration;
        private SerializedProperty useTriggerBasedDamage;
        private SerializedProperty fallbackDamageRadius;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                projectileSpawnHeight = serializedObject.FindProperty("projectileSpawnHeight");
                projectileSpeed = serializedObject.FindProperty("projectileSpeed");
                projectileMaxHeight = serializedObject.FindProperty("projectileMaxHeight");
                createDamageAreaOnImpact = serializedObject.FindProperty("createDamageAreaOnImpact");
                impactDamageDuration = serializedObject.FindProperty("impactDamageDuration");
                useTriggerBasedDamage = serializedObject.FindProperty("useTriggerBasedDamage");
                fallbackDamageRadius = serializedObject.FindProperty("fallbackDamageRadius");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // Add projectile specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Projectile Settings", EditorStyles.boldLabel);
            
            if (projectileSpawnHeight != null)
            {
                float spawnHeight = projectileSpawnHeight.floatValue;
                spawnHeight = Mathf.Max(0f, EditorGUILayout.FloatField("Projectile Spawn Height", spawnHeight));
                projectileSpawnHeight.floatValue = spawnHeight;
            }
            
            if (projectileSpeed != null)
            {
                float speed = projectileSpeed.floatValue;
                speed = Mathf.Max(0.1f, EditorGUILayout.FloatField("Projectile Speed", speed));
                projectileSpeed.floatValue = speed;
            }
            
            if (projectileMaxHeight != null)
            {
                float maxHeight = projectileMaxHeight.floatValue;
                maxHeight = Mathf.Max(0f, EditorGUILayout.FloatField("Projectile Max Height", maxHeight));
                projectileMaxHeight.floatValue = maxHeight;
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Impact Settings", EditorStyles.boldLabel);
            
            if (createDamageAreaOnImpact != null) EditorGUILayout.PropertyField(createDamageAreaOnImpact);
            
            if (createDamageAreaOnImpact != null && createDamageAreaOnImpact.boolValue)
            {
                EditorGUI.indentLevel++;
                
                if (useTriggerBasedDamage != null) EditorGUILayout.PropertyField(useTriggerBasedDamage);
                
                if (useTriggerBasedDamage != null && useTriggerBasedDamage.boolValue)
                {
                    EditorGUILayout.HelpBox("Damage detection will use OnTriggerEnter on the impact effect's DamageArea component (or inherited classes like ZombieVomitPool) for precise shape-based detection.", MessageType.Info);
                }
                else
                {
                    if (fallbackDamageRadius != null)
                    {
                        float radius = fallbackDamageRadius.floatValue;
                        radius = Mathf.Max(0f, EditorGUILayout.FloatField("Damage Radius", radius));
                        fallbackDamageRadius.floatValue = radius;
                    }
                    EditorGUILayout.HelpBox("Using radius-based damage detection. For irregular shapes, enable trigger-based damage and add a DamageArea component (or inherited class like ZombieVomitPool) to the impact effect.", MessageType.Warning);
                }
                
                if (impactDamageDuration != null)
                {
                    float duration = impactDamageDuration.floatValue;
                    duration = Mathf.Max(0f, EditorGUILayout.FloatField("Impact Damage Duration", duration));
                    impactDamageDuration.floatValue = duration;
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        
        protected override void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            // Get projectile-specific values
            float spawnHeight = projectileSpawnHeight != null ? projectileSpawnHeight.floatValue : 1.5f;
            
            // Use EditorGUILayout for proper layout integration
            Rect rect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions - adjust center to account for labels
            float centerX = rect.x + rect.width / 2 + 60;
            float centerY = rect.y + rect.height / 2;
            float maxVisualRange = Mathf.Min((rect.width - 120), rect.height) / 2 - 15;
            
            // Scale factors for visualization
            float scale = maxVisualRange / Mathf.Max(maxRange, 1f);
            
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
            
            // Draw attack angle cone - cyan
            float angleRad = attackAngleThreshold * Mathf.Deg2Rad;
            Vector3 rightDir = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
            Vector3 leftDir = new Vector3(-Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
            
            Handles.color = new Color(0f, 1f, 1f, 0.6f);
            Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + rightDir.x, centerY + rightDir.z, 0));
            Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + leftDir.x, centerY + leftDir.z, 0));
            
            // Draw projectile spawn position - yellow
            float visualSpawnHeight = spawnHeight * scale * 0.5f; // Scale down for visualization
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
            GUI.Label(new Rect(labelX, labelY + 36, 120, 12), $"Angle: ±{attackAngleThreshold:F0}°", labelStyle);
            
            // Legend - positioned at bottom right
            float legendX = rect.x + rect.width - 120;
            float legendY = rect.y + rect.height - 45;
            GUI.Label(new Rect(legendX, legendY, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 10, 115, 10), "Gray: Too Close", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 20, 115, 10), "Red: Range", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 30, 115, 10), "Yellow: Spawn", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 40, 115, 10), "Cyan: Angle", labelStyle);
        }
    }
}
