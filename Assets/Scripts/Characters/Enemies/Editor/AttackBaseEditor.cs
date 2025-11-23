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
        private SerializedProperty attackDescription;
        private SerializedProperty attackEquipment;
        private SerializedProperty attackEffectObjects;
        private SerializedProperty startEffect;
        private SerializedProperty attackEffect;
        private SerializedProperty hitEffect;
        private SerializedProperty endEffect;
        private SerializedProperty startEffectDelay;
        private SerializedProperty attackEffectDelay;
        private SerializedProperty hitEffectDelay;
        private SerializedProperty endEffectDelay;
        private SerializedProperty elementalDamageBonus;
        private SerializedProperty useAnimatorTiming;
        private SerializedProperty attackTrigger;
        private SerializedProperty warningDelay;
        private SerializedProperty attackDelay;
        private SerializedProperty attackEndDelay;
        private SerializedProperty allowRotationDuringAttack;

        private bool showRangeSettings = true;
        private bool showDamageSettings = true;
        private bool showEffectSettings = true;
        private bool showVisualizationSettings = true;
        private bool showTimingSettings = true;

        protected virtual void OnEnable()
        {
            attackDescription = serializedObject.FindProperty("attackDescription");
            attackType = serializedObject.FindProperty("attackType");
            damage = serializedObject.FindProperty("damage");
            poiseDamage = serializedObject.FindProperty("poiseDamage");
            attackElement = serializedObject.FindProperty("attackElement");
            cooldown = serializedObject.FindProperty("cooldown");
            minRange = serializedObject.FindProperty("minRange");
            maxRange = serializedObject.FindProperty("maxRange");
            attackAngleThreshold = serializedObject.FindProperty("attackAngleThreshold");
            attackEquipment = serializedObject.FindProperty("attackEquipment");
            attackEffectObjects = serializedObject.FindProperty("attackEffectObjects");
            startEffect = serializedObject.FindProperty("startEffect");
            attackEffect = serializedObject.FindProperty("attackEffect");
            hitEffect = serializedObject.FindProperty("hitEffect");
            endEffect = serializedObject.FindProperty("endEffect");
            startEffectDelay = serializedObject.FindProperty("startEffectDelay");
            attackEffectDelay = serializedObject.FindProperty("attackEffectDelay");
            hitEffectDelay = serializedObject.FindProperty("hitEffectDelay");
            endEffectDelay = serializedObject.FindProperty("endEffectDelay");
            elementalDamageBonus = serializedObject.FindProperty("elementalDamageBonus");
            useAnimatorTiming = serializedObject.FindProperty("useAnimatorTiming");
            attackTrigger = serializedObject.FindProperty("attackTrigger");
            warningDelay = serializedObject.FindProperty("warningDelay");
            attackDelay = serializedObject.FindProperty("attackDelay");
            attackEndDelay = serializedObject.FindProperty("attackEndDelay");
            allowRotationDuringAttack = serializedObject.FindProperty("allowRotationDuringAttack");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space(5);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            EditorGUILayout.LabelField(target.GetType().Name, headerStyle);
            EditorGUILayout.Space(5);
            
            // Attack Description Section
            if (attackDescription != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("Attack Description", EditorStyles.boldLabel);
                
                // Show the description text area
                EditorGUILayout.PropertyField(attackDescription, GUIContent.none);
                
                // If there's a description, show it nicely formatted
                if (!string.IsNullOrWhiteSpace(attackDescription.stringValue))
                {
                    EditorGUILayout.Space(3);
                    GUIStyle descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    descriptionStyle.fontSize = 11;
                    descriptionStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
                    descriptionStyle.padding = new RectOffset(10, 10, 5, 5);
                    
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField(attackDescription.stringValue, descriptionStyle);
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Add a custom description to document this attack.\n\n" +
                        "Example:\n" +
                        "\"Close-range melee attack with 2m range. Triggers when enemy is within striking distance. " +
                        "Use attackType=1 for animation override.\"",
                        MessageType.None);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

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

            // Attack Timing Settings - Show either Animation or Timeline based on toggle
            showTimingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showTimingSettings, "Attack Timing");
            if (showTimingSettings)
            {
                EditorGUI.indentLevel++;
                
                // Toggle between animator and timeline
                if (useAnimatorTiming != null)
                {
                    EditorGUILayout.PropertyField(useAnimatorTiming, new GUIContent("Use Animator Timing", "Use animator events to trigger attack phases. If false, uses timeline delays."));
                }
                
                EditorGUILayout.Space(3);
                
                // Show appropriate settings based on timing mode
                if (useAnimatorTiming != null && useAnimatorTiming.boolValue)
                {
                    // Animator-based timing
                    EditorGUILayout.LabelField("Animator Settings", EditorStyles.miniBoldLabel);
                    if (attackTrigger != null) 
                    {
                        EditorGUILayout.PropertyField(attackTrigger, new GUIContent("Attack Trigger", "Animator trigger parameter name"));
                    }
                    EditorGUILayout.HelpBox("Attack phases (Warning/Attack/End) are triggered by animation events.", MessageType.Info);
                }
                else
                {
                    // Timeline-based timing
                    EditorGUILayout.LabelField("Timeline Settings", EditorStyles.miniBoldLabel);
                    if (warningDelay != null)
                    {
                        EditorGUILayout.PropertyField(warningDelay, new GUIContent("Warning Delay", "Time before AttackWarning is called (visual indicator)"));
                    }
                    if (attackDelay != null)
                    {
                        EditorGUILayout.PropertyField(attackDelay, new GUIContent("Attack Delay", "Time before Attack is executed (damage dealing)"));
                    }
                    if (attackEndDelay != null)
                    {
                        EditorGUILayout.PropertyField(attackEndDelay, new GUIContent("End Delay", "Time before AttackEnd is called (cleanup)"));
                    }
                    
                    // Visual timeline
                    DrawTimelineVisualization();
                }
                
                EditorGUILayout.Space(3);
                
                // Common settings
                if (allowRotationDuringAttack != null) 
                {
                    EditorGUILayout.PropertyField(allowRotationDuringAttack, new GUIContent("Allow Rotation During Attack", "Enable rotation tracking during attack"));
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
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

            // Attack Visual System Section
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Attack Visual System", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            // Permanent Equipment
            EditorGUILayout.LabelField("Permanent Equipment (Always Visible)", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Equipment that's ALWAYS visible to show what attack this enemy has.\n\n" +
                "Examples:\n" +
                "• Dynamite sticks → ExplosionAttack\n" +
                "• Gun model → ProjectileAttack\n" +
                "• Sword/weapon → Melee attacks\n" +
                "• Rocket launcher → Missile attacks",
                MessageType.Info);
            
            if (attackEquipment != null)
            {
                EditorGUILayout.PropertyField(attackEquipment, new GUIContent("Equipment GameObjects"), true);
            }
            
            EditorGUILayout.Space(5);
            
            // Temporary Effects
            EditorGUILayout.LabelField("Temporary Effects (During Attacks Only)", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Effects that appear ONLY during attacks.\n\n" +
                "Examples:\n" +
                "• Weapon trails (sword trails, smoke)\n" +
                "• Muzzle flashes\n" +
                "• Charge effects (glowing particles)\n" +
                "• Attack telegraphs (warning indicators)\n\n" +
                "Start these objects disabled in the scene!",
                MessageType.Info);
            
            if (attackEffectObjects != null)
            {
                EditorGUILayout.PropertyField(attackEffectObjects, new GUIContent("Effect GameObjects"), true);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            // Draw child class specific settings (like projectile settings) - always visible
            DrawChildSpecificSettings();

            // Note: Specific attack types (like CloseRangeAttack) will add their own visualizations

            // Draw child class properties
            DrawPropertiesExcluding(serializedObject, 
                "m_Script",
                "attackDescription",
                "attackType", 
                "damage", 
                "poiseDamage", 
                "attackElement", 
                "elementalDamageBonus",
                "useAnimatorTiming",
                "attackTrigger",
                "warningDelay",
                "attackDelay",
                "attackEndDelay",
                "allowRotationDuringAttack",
                "cooldown", 
                "minRange", 
                "maxRange", 
                "attackAngleThreshold", 
                "attackEquipment",
                "attackEffectObjects",
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
                "impactEffect",
                "homingDuration",
                "turnSpeed",
                "explodeOnTimeout",
                "missileSpeedMultiplier",
                // BeamAttack specific properties
                "beamHitLayers",
                "damageInterval",
                "beamFirePoint",
                "beamVisual",
                "headIKWeight",
                "headIKRotationWeight",
                "headIKLerpSpeed",
                "maxHeadRotationAngle");

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
            
            // Draw the visual graphics
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Get current values for visualization
            float currentMinRange = minRange != null ? minRange.floatValue : 0f;
            float currentMaxRange = maxRange != null ? maxRange.floatValue : 5f;
            float currentAngle = attackAngleThreshold != null ? attackAngleThreshold.floatValue : 30f;
            
            // Use EditorGUILayout with proper height allocation
            EditorGUILayout.BeginVertical(GUILayout.Height(160));
            DrawAttackVisualization(currentMinRange, currentMaxRange, currentAngle);
            EditorGUILayout.EndVertical();
        }
        
        protected virtual void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
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
            
            // Labels - positioned to avoid overlap
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 8;
            labelStyle.normal.textColor = Color.white;
            
            // Left side labels - positioned outside the circle
            float labelX = rect.x + 5;
            float labelY = rect.y + 5;
            GUI.Label(new Rect(labelX, labelY, 120, 12), $"Min: {minRange:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 12, 120, 12), $"Max: {maxRange:F1}m", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 24, 120, 12), $"Angle: ±{attackAngleThreshold:F0}°", labelStyle);
            
            // Legend - positioned at bottom right with more space
            float legendX = rect.x + rect.width - 120;
            float legendY = rect.y + rect.height - 45;
            GUI.Label(new Rect(legendX, legendY, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 10, 115, 10), "Gray: Too Close", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 20, 115, 10), "Red: Range", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 30, 115, 10), "Cyan: Angle", labelStyle);
        }
        
        protected virtual void DrawChildSpecificSettings()
        {
            // Override in child classes to add specific settings
        }
        
        /// <summary>
        /// Draw a visual timeline showing attack phases
        /// </summary>
        protected virtual void DrawTimelineVisualization()
        {
            if (warningDelay == null || attackDelay == null || attackEndDelay == null) return;
            
            float warning = warningDelay.floatValue;
            float attack = attackDelay.floatValue;
            float end = attackEndDelay.floatValue;
            float totalTime = Mathf.Max(end, 0.1f);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Timeline Preview", EditorStyles.miniLabel);
            
            // Draw timeline bar
            Rect timelineRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            timelineRect.x += 5;
            timelineRect.width -= 10;
            
            // Background
            EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            
            // Calculate positions
            float warningPos = (warning / totalTime) * timelineRect.width;
            float attackPos = (attack / totalTime) * timelineRect.width;
            float endPos = timelineRect.width;
            
            // Draw phases
            Rect startRect = new Rect(timelineRect.x, timelineRect.y, warningPos, timelineRect.height);
            Rect warningRect = new Rect(timelineRect.x + warningPos, timelineRect.y, attackPos - warningPos, timelineRect.height);
            Rect attackRect = new Rect(timelineRect.x + attackPos, timelineRect.y, endPos - attackPos, timelineRect.height);
            
            EditorGUI.DrawRect(startRect, new Color(0.3f, 0.3f, 0.3f, 0.8f)); // Start phase (dark gray)
            EditorGUI.DrawRect(warningRect, new Color(1f, 0.8f, 0f, 0.6f));    // Warning phase (yellow)
            EditorGUI.DrawRect(attackRect, new Color(1f, 0.3f, 0.3f, 0.6f));   // Attack phase (red)
            
            // Labels
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 9;
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            
            if (warningPos > 30) GUI.Label(startRect, "Start", labelStyle);
            if (attackPos - warningPos > 40) GUI.Label(warningRect, $"Warning\n{warning:F2}s", labelStyle);
            if (endPos - attackPos > 40) GUI.Label(attackRect, $"Attack\n{attack:F2}s", labelStyle);
            
            // Time marker at bottom
            labelStyle.fontSize = 8;
            labelStyle.alignment = TextAnchor.UpperRight;
            GUI.Label(new Rect(timelineRect.x + timelineRect.width - 50, timelineRect.y + timelineRect.height + 2, 50, 12), 
                     $"End: {end:F2}s", labelStyle);
        }

    }
}
