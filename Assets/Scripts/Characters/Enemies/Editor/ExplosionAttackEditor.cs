using UnityEngine;
using UnityEditor;
using Enemies.Attacks;

namespace Enemies.Editor
{
    [CustomEditor(typeof(ExplosionAttack))]
    [CanEditMultipleObjects]
    public class ExplosionAttackEditor : AttackBaseEditor
    {
        private SerializedProperty explosionRadius;
        private SerializedProperty poiseDamageMultiplier;
        private SerializedProperty dieAfterExplosion;
        private SerializedProperty explodeOnDeath;
        private SerializedProperty explodeOnDamage;
        private SerializedProperty minDamageToExplode;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                explosionRadius = serializedObject.FindProperty("explosionRadius");
                poiseDamageMultiplier = serializedObject.FindProperty("poiseDamageMultiplier");
                dieAfterExplosion = serializedObject.FindProperty("dieAfterExplosion");
                explodeOnDeath = serializedObject.FindProperty("explodeOnDeath");
                explodeOnDamage = serializedObject.FindProperty("explodeOnDamage");
                minDamageToExplode = serializedObject.FindProperty("minDamageToExplode");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // Add explosion specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Explosion Settings", EditorStyles.boldLabel);
            
            if (explosionRadius != null)
            {
                float radius = explosionRadius.floatValue;
                radius = Mathf.Max(0f, EditorGUILayout.FloatField("Explosion Radius", radius));
                explosionRadius.floatValue = radius;
            }
            
            
            if (poiseDamageMultiplier != null)
            {
                float multiplier = poiseDamageMultiplier.floatValue;
                multiplier = Mathf.Max(0f, EditorGUILayout.FloatField("Poise Damage Multiplier", multiplier));
                poiseDamageMultiplier.floatValue = multiplier;
            }
            
            if (dieAfterExplosion != null)
            {
                EditorGUILayout.PropertyField(dieAfterExplosion);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Explosion Triggers", EditorStyles.boldLabel);
            
            if (explodeOnDeath != null)
            {
                EditorGUILayout.PropertyField(explodeOnDeath);
            }
            
            if (explodeOnDamage != null)
            {
                EditorGUILayout.PropertyField(explodeOnDamage);
                
                if (explodeOnDamage.boolValue && minDamageToExplode != null)
                {
                    EditorGUI.indentLevel++;
                    float minDamage = minDamageToExplode.floatValue;
                    minDamage = Mathf.Max(0f, EditorGUILayout.FloatField("Min Damage To Explode", minDamage));
                    minDamageToExplode.floatValue = minDamage;
                    EditorGUI.indentLevel--;
                }
            }
        }


        protected override void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            // Get explosion-specific values
            float explosionRadius = this.explosionRadius != null ? this.explosionRadius.floatValue : 5f;
            
            // Use EditorGUILayout for proper layout integration
            Rect rect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions - center the visualization
            float centerX = rect.x + rect.width / 2;
            float centerY = rect.y + rect.height / 2;
            float maxVisualRange = Mathf.Min(rect.width, rect.height) / 2 - 20;
            
            // Scale factors for visualization
            float scale = maxVisualRange / Mathf.Max(explosionRadius, maxRange, 1f);
            
            // Draw ranges as concentric circles
            Handles.BeginGUI();
            
            // Detonation range (when to trigger) - yellow
            float visualMaxRange = maxRange * scale;
            Handles.color = new Color(1f, 1f, 0f, 0.2f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            Handles.color = new Color(1f, 1f, 0f, 0.8f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualMaxRange);
            
            // Explosion radius - red/orange gradient effect
            float visualExplosionRadius = explosionRadius * scale;
            
            // Draw multiple rings for explosion effect
            for (int i = 5; i >= 0; i--)
            {
                float t = i / 5f;
                float currentRadius = visualExplosionRadius * (0.3f + t * 0.7f);
                float alpha = 0.1f + (1f - t) * 0.4f;
                Handles.color = new Color(1f, 0.2f + t * 0.3f, 0f, alpha);
                Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, currentRadius);
            }
            
            // Final explosion ring
            Handles.color = new Color(1f, 0.2f, 0f, 0.95f);
            Handles.DrawWireDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualExplosionRadius);
            
            // Draw inner core - bright white/yellow
            Handles.color = new Color(1f, 1f, 0.5f, 0.9f);
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, visualExplosionRadius * 0.3f);
            
            // Draw center point (enemy/bomb position) - white
            Handles.color = Color.white;
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, 5);
            
            // Draw radiating lines for explosion effect
            Handles.color = new Color(1f, 0.5f, 0f, 0.7f);
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Handles.DrawLine(
                    new Vector3(centerX, centerY, 0) + direction * visualExplosionRadius * 0.3f,
                    new Vector3(centerX, centerY, 0) + direction * visualExplosionRadius
                );
            }
            
            Handles.EndGUI();
            
            // Labels
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 9;
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            
            // Top labels
            GUI.Label(new Rect(rect.x + 5, rect.y + 5, 150, 15), $"Detonation Range: {maxRange:F1}m", labelStyle);
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(rect.x + 5, rect.y + 22, 150, 15), $"Explosion Radius: {explosionRadius:F1}m", labelStyle);
            
            // Legend
            labelStyle.fontSize = 8;
            labelStyle.fontStyle = FontStyle.Normal;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            float legendY = rect.y + rect.height - 30;
            GUI.Label(new Rect(rect.x + 5, legendY, 150, 12), "Legend:", labelStyle);
            GUI.Label(new Rect(rect.x + 5, legendY + 12, 150, 12), "Yellow: Detonation Range", labelStyle);
            GUI.Label(new Rect(rect.x + 155, legendY + 12, 150, 12), "Red: Explosion Damage", labelStyle);
        }
    }
}

