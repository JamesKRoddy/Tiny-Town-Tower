using UnityEngine;
using UnityEditor;
using Enemies.Attacks;

namespace Enemies.Editor
{
    [CustomEditor(typeof(AttackBase), true)]
    [CanEditMultipleObjects]
    public class AttackBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty attackType;
        private SerializedProperty damage;
        private SerializedProperty poiseDamage;
        private SerializedProperty attackElement;
        private SerializedProperty cooldown;
        private SerializedProperty minRange;
        private SerializedProperty maxRange;
        private SerializedProperty attackAngleThreshold;
        private SerializedProperty attackGameObjects;
        private SerializedProperty startEffect;
        private SerializedProperty attackEffect;
        private SerializedProperty hitEffect;
        private SerializedProperty endEffect;
        private SerializedProperty startEffectDelay;
        private SerializedProperty attackEffectDelay;
        private SerializedProperty hitEffectDelay;
        private SerializedProperty endEffectDelay;
        private SerializedProperty elementalDamageBonus;
        private SerializedProperty attackTrigger;
        private SerializedProperty allowRotationDuringAttack;

        private bool showRangeSettings = true;
        private bool showDamageSettings = true;
        private bool showEffectSettings = true;
        private bool showVisualizationSettings = true;

        protected virtual void OnEnable()
        {
            attackType = serializedObject.FindProperty("attackType");
            damage = serializedObject.FindProperty("damage");
            poiseDamage = serializedObject.FindProperty("poiseDamage");
            attackElement = serializedObject.FindProperty("attackElement");
            cooldown = serializedObject.FindProperty("cooldown");
            minRange = serializedObject.FindProperty("minRange");
            maxRange = serializedObject.FindProperty("maxRange");
            attackAngleThreshold = serializedObject.FindProperty("attackAngleThreshold");
            attackGameObjects = serializedObject.FindProperty("attackGameObjects");
            startEffect = serializedObject.FindProperty("startEffect");
            attackEffect = serializedObject.FindProperty("attackEffect");
            hitEffect = serializedObject.FindProperty("hitEffect");
            endEffect = serializedObject.FindProperty("endEffect");
            startEffectDelay = serializedObject.FindProperty("startEffectDelay");
            attackEffectDelay = serializedObject.FindProperty("attackEffectDelay");
            hitEffectDelay = serializedObject.FindProperty("hitEffectDelay");
            endEffectDelay = serializedObject.FindProperty("endEffectDelay");
            elementalDamageBonus = serializedObject.FindProperty("elementalDamageBonus");
            attackTrigger = serializedObject.FindProperty("attackTrigger");
            allowRotationDuringAttack = serializedObject.FindProperty("allowRotationDuringAttack");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(target.GetType().Name, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Attack Type
            if (attackType != null) EditorGUILayout.PropertyField(attackType);
            EditorGUILayout.Space(5);

            // Damage Settings
            showDamageSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDamageSettings, "Damage Settings");
            if (showDamageSettings)
            {
                EditorGUI.indentLevel++;
                if (damage != null) EditorGUILayout.PropertyField(damage);
                if (poiseDamage != null) EditorGUILayout.PropertyField(poiseDamage);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Elemental Damage Settings
            EditorGUILayout.LabelField("Elemental Damage", EditorStyles.boldLabel);
            if (attackElement != null) EditorGUILayout.PropertyField(attackElement);
            if (elementalDamageBonus != null) EditorGUILayout.PropertyField(elementalDamageBonus);
            EditorGUILayout.Space(5);

            // Animation Settings
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            if (attackTrigger != null) EditorGUILayout.PropertyField(attackTrigger);
            if (allowRotationDuringAttack != null) 
            {
                EditorGUILayout.PropertyField(allowRotationDuringAttack, new GUIContent("Allow Rotation During Attack", "Enable NavMeshAgent rotation during attack (useful for tracking moving targets)"));
            }
            EditorGUILayout.Space(5);

            // Attack Visualization Settings - All visualization controls in one place
            showVisualizationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showVisualizationSettings, "Attack Visualization Settings");
            if (showVisualizationSettings)
            {
                EditorGUI.indentLevel++;
                DrawVisualizationSettings();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Effect Settings (only show if we have effect properties)
            bool hasEffectProperties = startEffect != null || attackEffect != null || 
                                     hitEffect != null || endEffect != null;
            
            if (hasEffectProperties)
            {
                showEffectSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showEffectSettings, "Attack Effects");
                if (showEffectSettings)
                {
                    EditorGUI.indentLevel++;
                    if (startEffect != null) EditorGUILayout.PropertyField(startEffect);
                    if (startEffectDelay != null) EditorGUILayout.PropertyField(startEffectDelay);
                    if (attackEffect != null) EditorGUILayout.PropertyField(attackEffect);
                    if (attackEffectDelay != null) EditorGUILayout.PropertyField(attackEffectDelay);
                    if (hitEffect != null) EditorGUILayout.PropertyField(hitEffect);
                    if (hitEffectDelay != null) EditorGUILayout.PropertyField(hitEffectDelay);
                    if (endEffect != null) EditorGUILayout.PropertyField(endEffect);
                    if (endEffectDelay != null) EditorGUILayout.PropertyField(endEffectDelay);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space(5);
            }

            // Game Objects - simple and clean
            if (attackGameObjects != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(attackGameObjects, new GUIContent("Attack Game Objects"), true);
                EditorGUILayout.Space(5);
            }

            // Note: Specific attack types (like CloseRangeAttack) will add their own visualizations

            // Draw child class properties
            DrawPropertiesExcluding(serializedObject, 
                "m_Script", 
                "attackType", 
                "damage", 
                "poiseDamage", 
                "attackElement", 
                "elementalDamageBonus",
                "attackTrigger",
                "allowRotationDuringAttack",
                "cooldown", 
                "minRange", 
                "maxRange", 
                "attackAngleThreshold", 
                "attackGameObjects",
                "startEffect",
                "startEffectDelay",
                "attackEffect",
                "attackEffectDelay",
                "hitEffect",
                "hitEffectDelay",
                "endEffect",
                "endEffectDelay",
                "attackRadius",
                "targetLayer",
                "buildingAttackRange",
                "projectileSpawnHeight",
                "projectileSpeed",
                "projectileMaxHeight",
                "createDamageAreaOnImpact",
                "impactDamageDuration",
                "useTriggerBasedDamage",
                "fallbackDamageRadius",
                "projectileEffect",
                "impactEffect");

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawVisualizationSettings()
        {
            // Range settings - all visualization controls in one place
            EditorGUILayout.LabelField("Range Settings", EditorStyles.boldLabel);
            if (minRange != null && maxRange != null)
            {
                float minVal = minRange.floatValue;
                float maxVal = maxRange.floatValue;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Range");
                EditorGUILayout.MinMaxSlider(ref minVal, ref maxVal, 0f, 50f);
                EditorGUILayout.EndHorizontal();
                
                // Ensure non-negative values
                minVal = Mathf.Max(0f, minVal);
                maxVal = Mathf.Max(0f, maxVal);
                
                minRange.floatValue = minVal;
                maxRange.floatValue = maxVal;
                
                minRange.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField("Min Range", minRange.floatValue));
                maxRange.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField("Max Range", maxRange.floatValue));
            }
            EditorGUILayout.Space(5);
            
            // Angle settings
            EditorGUILayout.LabelField("Angle Settings", EditorStyles.boldLabel);
            if (attackAngleThreshold != null)
            {
                // Ensure non-negative values
                float angle = attackAngleThreshold.floatValue;
                angle = Mathf.Max(0f, EditorGUILayout.FloatField("Attack Angle Threshold", angle));
                attackAngleThreshold.floatValue = angle;
            }
            EditorGUILayout.Space(5);
            
            // Timing settings
            EditorGUILayout.LabelField("Timing Settings", EditorStyles.boldLabel);
            if (cooldown != null)
            {
                // Ensure non-negative values
                float cooldownVal = cooldown.floatValue;
                cooldownVal = Mathf.Max(0f, EditorGUILayout.FloatField("Cooldown", cooldownVal));
                cooldown.floatValue = cooldownVal;
            }
            
            // Call child class specific settings
            DrawChildSpecificSettings();
        }
        
        protected virtual void DrawChildSpecificSettings()
        {
            // Override in child classes to add specific settings
        }

        private void DrawBaseAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
            rect.height = 100;
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions
            float centerX = rect.x + rect.width / 2;
            float centerY = rect.y + rect.height / 2;
            float maxVisualRange = Mathf.Min(rect.width, rect.height) / 2 - 10;
            
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
            
            // Draw center point (enemy position) - white
            Handles.color = Color.white;
            Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, 4);
            
            Handles.EndGUI();
            
            // Labels
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 9;
            labelStyle.normal.textColor = Color.white;
            
            // Top labels
            GUI.Label(new Rect(rect.x + 5, rect.y + 5, 150, 15), $"Min Range: {minRange:F1}m", labelStyle);
            GUI.Label(new Rect(rect.x + 5, rect.y + 20, 150, 15), $"Max Range: {maxRange:F1}m", labelStyle);
            GUI.Label(new Rect(rect.x + 5, rect.y + 35, 150, 15), $"Angle: ±{attackAngleThreshold:F0}°", labelStyle);
            
            // Legend
            float legendY = rect.y + rect.height - 30;
            GUI.Label(new Rect(rect.x + 5, legendY, 80, 12), "Legend:", labelStyle);
            GUI.Label(new Rect(rect.x + 5, legendY + 12, 80, 12), "Gray: Too Close", labelStyle);
            GUI.Label(new Rect(rect.x + 80, legendY, 80, 12), "Red: Attack Range", labelStyle);
            GUI.Label(new Rect(rect.x + 80, legendY + 12, 80, 12), "Cyan: Angle", labelStyle);
        }
    }
}
