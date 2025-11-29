using UnityEngine;
using UnityEditor;
using Enemies;
using Enemies.Editor;

/// <summary>
/// Custom inspector for HumanoidEnemy with organized sections similar to SettlerNPCEditor.
/// Extends EnemyBaseEditor to inherit common enemy functionality while adding humanoid-specific features.
/// </summary>
[CustomEditor(typeof(HumanoidEnemy), true)] // true allows derived classes to use this editor
public class HumanoidEnemyEditor : EnemyBaseEditor
{
    // Humanoid-specific serialized properties
    private SerializedProperty appearanceSystemProp;
    private SerializedProperty randomizeAppearanceOnSpawnProp;
    
    // ModularEnemy serialized properties
    private SerializedProperty selectionStrategy;
    private SerializedProperty attackSwitchCooldown;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // HumanoidEnemy properties
        appearanceSystemProp = serializedObject.FindProperty("appearanceSystem");
        randomizeAppearanceOnSpawnProp = serializedObject.FindProperty("randomizeAppearanceOnSpawn");
        
        // ModularEnemy properties
        selectionStrategy = serializedObject.FindProperty("selectionStrategy");
        attackSwitchCooldown = serializedObject.FindProperty("attackSwitchCooldown");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        HumanoidEnemy enemy = (HumanoidEnemy)target;

        // Header Section
        DrawColoredSection("🎯 Humanoid Enemy Inspector", () => {
            EditorGUILayout.HelpBox($"Custom inspector for {target.GetType().Name}", MessageType.Info);
        }, new Color(0.2f, 0.6f, 1f, 0.3f)); // Blue tint
        
        // Enemy Status (Runtime)
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            DrawHumanoidStatusSection(enemy);
            
            EditorGUILayout.Space(5);
            DrawCombatStatusSection(enemy);
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Runtime enemy information will be displayed here during Play Mode", MessageType.Info);
        }
        
        // Appearance System Section
        DrawColoredSection("🎨 Appearance System", () => {
            // Preview size slider
            AppearanceOptionDrawer.DrawPreviewSizeSlider();
            EditorGUILayout.Space(5);
            
            if (appearanceSystemProp != null)
            {
                EditorGUILayout.PropertyField(appearanceSystemProp);
            }
            if (randomizeAppearanceOnSpawnProp != null)
            {
                EditorGUILayout.PropertyField(randomizeAppearanceOnSpawnProp);
            }
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                
                // Randomize Appearance button
                Color originalBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("🎲 Randomize Appearance", GUILayout.Height(25)))
                {
                    var appearanceSystem = enemy.GetAppearanceSystem();
                    if (appearanceSystem != null)
                    {
                        appearanceSystem.ClearCurrentAppearance();
                        appearanceSystem.RandomizeAppearance();
                        Debug.Log($"[HumanoidEnemyEditor] Randomized appearance for {enemy.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[HumanoidEnemyEditor] Cannot randomize appearance - appearanceSystem is null on {enemy.name}");
                    }
                }
                GUI.backgroundColor = originalBg;
                
                // Show current appearance info
                var currentAppearance = enemy.GetCurrentAppearanceData();
                if (currentAppearance != null)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.HelpBox("Appearance is active and managed at runtime", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Appearance randomization available during Play Mode", MessageType.Info);
            }
        }, new Color(0.8f, 0.9f, 1f, 0.3f)); // Light blue tint
        
        // Attack System Section (ModularEnemy)
        DrawColoredSection("⚔️ Attack System", () => {
            if (selectionStrategy != null)
            {
                EditorGUILayout.PropertyField(selectionStrategy, new GUIContent("Selection Strategy", "How the enemy chooses which attack to use"));
            }
            if (attackSwitchCooldown != null)
            {
                EditorGUILayout.PropertyField(attackSwitchCooldown, new GUIContent("Switch Cooldown", "Minimum time between switching attacks"));
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Humanoid Combat", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Melee Attack Angle", "30° (constant)");
            EditorGUILayout.HelpBox("Humanoid enemies must face within 30° of their target to perform melee attacks.", MessageType.Info);
            
            // Show attack components in play mode
            if (Application.isPlaying)
            {
                DrawAttackComponentsInfo(enemy);
            }
        }, new Color(1f, 0.8f, 0.9f, 0.3f)); // Light pink tint
        
        // Movement & Navigation Section
        DrawColoredSection("🏃 Movement & Navigation", () => {
            var useRootMotion = serializedObject.FindProperty("useRootMotion");
            var stoppingDistance = serializedObject.FindProperty("stoppingDistance");
            var rotationSpeed = serializedObject.FindProperty("rotationSpeed");
            var movementSpeed = serializedObject.FindProperty("movementSpeed");
            var acceleration = serializedObject.FindProperty("acceleration");
            var angularSpeed = serializedObject.FindProperty("angularSpeed");
            var obstacleBoundsOffset = serializedObject.FindProperty("obstacleBoundsOffset");
            
            if (useRootMotion != null) EditorGUILayout.PropertyField(useRootMotion);
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Navigation", EditorStyles.miniBoldLabel);
            if (stoppingDistance != null) EditorGUILayout.PropertyField(stoppingDistance);
            if (obstacleBoundsOffset != null) EditorGUILayout.PropertyField(obstacleBoundsOffset);
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Speed & Rotation", EditorStyles.miniBoldLabel);
            if (movementSpeed != null) EditorGUILayout.PropertyField(movementSpeed);
            if (rotationSpeed != null) EditorGUILayout.PropertyField(rotationSpeed);
            if (acceleration != null) EditorGUILayout.PropertyField(acceleration);
            if (angularSpeed != null) EditorGUILayout.PropertyField(angularSpeed);
        }, new Color(0.9f, 0.8f, 1f, 0.3f)); // Light purple tint

        // Health & Combat Section
        DrawColoredSection("❤️ Health & Combat", () => {
            var maxHealthProp = serializedObject.FindProperty("maxHealth");
            var healthProp = serializedObject.FindProperty("health");
            
            if (maxHealthProp != null) EditorGUILayout.PropertyField(maxHealthProp);
            
            // Health bar visualization
            if (Application.isPlaying)
            {
                DrawHealthBar(enemy.Health, enemy.MaxHealth);
            }
        }, new Color(1f, 0.8f, 0.8f, 0.3f)); // Light red tint

        // Poise System Section
        DrawColoredSection("💪 Poise System", () => {
            var maxPoiseProp = serializedObject.FindProperty("maxPoise");
            var poiseProp = serializedObject.FindProperty("poise");
            var poiseRecoveryRateProp = serializedObject.FindProperty("poiseRecoveryRate");
            var poiseRecoveryDelayProp = serializedObject.FindProperty("poiseRecoveryDelay");
            
            if (maxPoiseProp != null) EditorGUILayout.PropertyField(maxPoiseProp);
            if (poiseRecoveryRateProp != null) EditorGUILayout.PropertyField(poiseRecoveryRateProp);
            if (poiseRecoveryDelayProp != null) EditorGUILayout.PropertyField(poiseRecoveryDelayProp);
            
            // Poise bar visualization
            if (Application.isPlaying)
            {
                DrawPoiseBar(enemy.Poise, enemy.MaxPoise);
            }
        }, new Color(1f, 0.9f, 0.8f, 0.3f)); // Light orange tint

        // Cooldown Movement Section
        DrawColoredSection("🎯 Cooldown Movement", () => {
            var enableCooldownMovement = serializedObject.FindProperty("enableCooldownMovement");
            var cooldownMovementMinDistance = serializedObject.FindProperty("cooldownMovementMinDistance");
            var cooldownMovementMaxDistance = serializedObject.FindProperty("cooldownMovementMaxDistance");
            var cooldownMovementMinDuration = serializedObject.FindProperty("cooldownMovementMinDuration");
            var cooldownMovementMaxDuration = serializedObject.FindProperty("cooldownMovementMaxDuration");
            
            if (enableCooldownMovement != null)
            {
                EditorGUILayout.PropertyField(enableCooldownMovement);
                
                if (enableCooldownMovement.boolValue)
                {
                    EditorGUI.indentLevel++;
                    if (cooldownMovementMinDistance != null) EditorGUILayout.PropertyField(cooldownMovementMinDistance);
                    if (cooldownMovementMaxDistance != null) EditorGUILayout.PropertyField(cooldownMovementMaxDistance);
                    if (cooldownMovementMinDuration != null) EditorGUILayout.PropertyField(cooldownMovementMinDuration);
                    if (cooldownMovementMaxDuration != null) EditorGUILayout.PropertyField(cooldownMovementMaxDuration);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox("Enable to make humanoid enemies reposition during attack cooldowns", MessageType.Info);
                }
            }
        }, new Color(0.9f, 1f, 0.9f, 0.3f)); // Light green tint

        // Head Tracking Section (Humanoid-specific)
        DrawColoredSection("👀 Head Tracking", () => {
            var enableHeadTracking = serializedObject.FindProperty("enableHeadTracking");
            var headTrackingWeight = serializedObject.FindProperty("headTrackingWeight");
            var headTrackingRotationWeight = serializedObject.FindProperty("headTrackingRotationWeight");
            var headTrackingLerpSpeed = serializedObject.FindProperty("headTrackingLerpSpeed");
            var maxHeadTrackingAngle = serializedObject.FindProperty("maxHeadTrackingAngle");
            var headTrackingDistance = serializedObject.FindProperty("headTrackingDistance");
            
            if (enableHeadTracking != null)
            {
                EditorGUILayout.PropertyField(enableHeadTracking);
                
                if (enableHeadTracking.boolValue)
                {
                    EditorGUI.indentLevel++;
                    if (headTrackingWeight != null) EditorGUILayout.PropertyField(headTrackingWeight);
                    if (headTrackingRotationWeight != null) EditorGUILayout.PropertyField(headTrackingRotationWeight);
                    if (headTrackingLerpSpeed != null) EditorGUILayout.PropertyField(headTrackingLerpSpeed);
                    if (maxHeadTrackingAngle != null) EditorGUILayout.PropertyField(maxHeadTrackingAngle);
                    if (headTrackingDistance != null) EditorGUILayout.PropertyField(headTrackingDistance);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox("Enable to make the humanoid enemy's head track the player", MessageType.Info);
                }
            }
        }, new Color(0.8f, 1f, 1f, 0.3f)); // Light cyan tint
        
        // Character Type Section
        DrawColoredSection("🎭 Character Type", () => {
            var characterType = serializedObject.FindProperty("characterType");
            if (characterType != null)
            {
                EditorGUILayout.PropertyField(characterType);
            }
        }, new Color(0.9f, 0.9f, 0.8f, 0.3f)); // Light yellow tint

        // Debug Settings Section
        DrawColoredSection("🔧 Debug Settings", () => {
            var showCollisionDebug = serializedObject.FindProperty("showCollisionDebug");
            if (showCollisionDebug != null)
            {
                EditorGUILayout.PropertyField(showCollisionDebug);
            }
            
            // Runtime debug info
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Runtime Info", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Is Attacking: {enemy.isAttacking}");
                EditorGUILayout.LabelField($"Position: {enemy.transform.position}");
                EditorGUILayout.LabelField($"Has Target: {enemy.NavMeshTarget != null}");
                if (enemy.NavMeshTarget != null)
                {
                    float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.NavMeshTarget.position);
                    EditorGUILayout.LabelField($"Distance to Target: {distanceToTarget:F2}m");
                    
                    // Calculate angle to target
                    Vector3 directionToTarget = (enemy.NavMeshTarget.position - enemy.transform.position).normalized;
                    float angleToTarget = Vector3.Angle(enemy.transform.forward, directionToTarget);
                    EditorGUILayout.LabelField($"Angle to Target: {angleToTarget:F1}°");
                    
                    // Show if within melee attack angle
                    bool withinMeleeAngle = angleToTarget <= 30f;
                    GUI.color = withinMeleeAngle ? Color.green : Color.yellow;
                    EditorGUILayout.LabelField($"Melee Ready: {(withinMeleeAngle ? "Yes" : "No")}");
                    GUI.color = Color.white;
                }
            }
        }, new Color(0.9f, 0.9f, 0.9f, 0.3f)); // Light gray tint
        
        // Draw separator
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Additional Properties", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);

        // Draw remaining properties not covered by custom sections
        DrawPropertiesExcluding(serializedObject,
            "m_Script",
            "characterType",
            "appearanceSystem",
            "randomizeAppearanceOnSpawn",
            "selectionStrategy",
            "attackSwitchCooldown",
            "useRootMotion",
            "stoppingDistance",
            "rotationSpeed",
            "movementSpeed",
            "acceleration",
            "angularSpeed",
            "obstacleBoundsOffset",
            "maxHealth",
            "health",
            "maxPoise",
            "poise",
            "poiseRecoveryRate",
            "poiseRecoveryDelay",
            "enableCooldownMovement",
            "cooldownMovementMinDistance",
            "cooldownMovementMaxDistance",
            "cooldownMovementMinDuration",
            "cooldownMovementMaxDuration",
            "enableHeadTracking",
            "headTrackingWeight",
            "headTrackingRotationWeight",
            "headTrackingLerpSpeed",
            "maxHeadTrackingAngle",
            "headTrackingDistance",
            "showCollisionDebug");

        serializedObject.ApplyModifiedProperties();

        // Repaint in play mode for live updates
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
    
    /// <summary>
    /// Draw a section with a colored background (matches SettlerNPCEditor style)
    /// </summary>
    private void DrawColoredSection(string title, System.Action content, Color backgroundColor)
    {
        EditorGUILayout.Space(5);
        
        // Store original background color
        Color originalColor = GUI.backgroundColor;
        
        // Set background color
        GUI.backgroundColor = backgroundColor;
        
        // Draw section with colored background
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        content?.Invoke();
        EditorGUILayout.EndVertical();
        
        // Restore original background color
        GUI.backgroundColor = originalColor;
    }
    
    /// <summary>
    /// Draw humanoid enemy status section (health, poise, status effects)
    /// </summary>
    private void DrawHumanoidStatusSection(HumanoidEnemy enemy)
    {
        DrawColoredSection("⚡ Enemy Status", () => {
            // Health status
            float healthPercent = (enemy.Health / enemy.MaxHealth) * 100f;
            Color healthColor = healthPercent > 50f ? Color.green : (healthPercent > 25f ? Color.yellow : Color.red);
            GUI.color = healthColor;
            EditorGUILayout.LabelField("Health", $"{enemy.Health:F1} / {enemy.MaxHealth:F1} ({healthPercent:F0}%)");
            GUI.color = Color.white;
            
            // Poise status
            float poisePercent = (enemy.Poise / enemy.MaxPoise) * 100f;
            Color poiseColor = poisePercent > 50f ? Color.cyan : (poisePercent > 25f ? Color.yellow : new Color(1f, 0.5f, 0f));
            GUI.color = poiseColor;
            EditorGUILayout.LabelField("Poise", $"{enemy.Poise:F1} / {enemy.MaxPoise:F1} ({poisePercent:F0}%)");
            GUI.color = Color.white;
            
            // Active status effects
            if (Managers.EffectManager.Instance != null)
            {
                var activeEffects = enemy.GetActiveStatusEffects();
                if (activeEffects != null && activeEffects.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField($"Active Status Effects ({activeEffects.Count}):", EditorStyles.miniBoldLabel);
                    
                    foreach (var effectType in activeEffects)
                    {
                        Color effectColor = StatusEffectUtils.GetEffectColor(effectType);
                        GUI.color = effectColor;
                        string description = StatusEffectUtils.GetEffectDescription(effectType);
                        EditorGUILayout.LabelField($"  • {description}", EditorStyles.miniLabel);
                        GUI.color = Color.white;
                    }
                }
                else
                {
                    EditorGUILayout.Space(3);
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("Active Status Effects: None", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
        }, new Color(0.8f, 1f, 0.8f, 0.3f)); // Light green tint
    }
    
    /// <summary>
    /// Draw combat status section
    /// </summary>
    private void DrawCombatStatusSection(HumanoidEnemy enemy)
    {
        DrawColoredSection("⚔️ Combat Status", () => {
            // Combat state
            EditorGUILayout.LabelField("Attacking", enemy.isAttacking ? "Yes" : "No");
            
            // Current attack
            var currentAttack = enemy.GetCurrentAttack();
            if (currentAttack != null)
            {
                GUI.color = Color.cyan;
                EditorGUILayout.LabelField("Current Attack", currentAttack.GetType().Name);
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("Current Attack", "None");
            }
            
            // Target information
            if (enemy.NavMeshTarget != null)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Target Info", EditorStyles.miniBoldLabel);
                float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.NavMeshTarget.position);
                EditorGUILayout.LabelField($"Distance: {distanceToTarget:F2}m");
                
                // Calculate angle
                Vector3 directionToTarget = (enemy.NavMeshTarget.position - enemy.transform.position).normalized;
                float angleToTarget = Vector3.Angle(enemy.transform.forward, directionToTarget);
                EditorGUILayout.LabelField($"Angle: {angleToTarget:F1}°");
            }
        }, new Color(1f, 0.9f, 0.8f, 0.3f)); // Light orange tint
    }
    
    /// <summary>
    /// Draw attack components information
    /// </summary>
    private void DrawAttackComponentsInfo(HumanoidEnemy enemy)
    {
        var attacks = enemy.GetComponents<AttackBase>();
        if (attacks.Length > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Attack Components ({attacks.Length}):", EditorStyles.miniBoldLabel);
            
            foreach (var attack in attacks)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Attack type name with color
                string attackTypeName = attack.GetType().Name;
                bool canAttack = attack.CanAttack();
                GUI.color = canAttack ? Color.green : Color.gray;
                EditorGUILayout.LabelField($"• {attackTypeName}", EditorStyles.boldLabel);
                GUI.color = Color.white;
                
                // Attack details
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Range: {attack.minRange:F1}m - {attack.maxRange:F1}m");
                EditorGUILayout.LabelField($"Damage: {attack.damage:F1}");
                EditorGUILayout.LabelField($"Cooldown: {attack.cooldown:F1}s");
                EditorGUILayout.LabelField($"Can Attack: {canAttack}");
                EditorGUI.indentLevel--;
                
                EditorGUILayout.EndVertical();
            }
        }
    }

    /// <summary>
    /// Draw a health bar visualization
    /// </summary>
    private void DrawHealthBar(float current, float max)
    {
        Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
        rect.height = 20;
        
        // Background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        
        // Health bar
        float healthPercent = max > 0 ? current / max : 0;
        Rect healthRect = new Rect(rect.x, rect.y, rect.width * healthPercent, rect.height);
        
        Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
        EditorGUI.DrawRect(healthRect, healthColor);
        
        // Label
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;
        GUI.Label(rect, $"Health: {current:F0} / {max:F0} ({healthPercent * 100:F0}%)", labelStyle);
    }

    /// <summary>
    /// Draw a poise bar visualization
    /// </summary>
    private void DrawPoiseBar(float current, float max)
    {
        Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
        rect.height = 20;
        
        // Background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        
        // Poise bar
        float poisePercent = max > 0 ? current / max : 0;
        Rect poiseRect = new Rect(rect.x, rect.y, rect.width * poisePercent, rect.height);
        
        Color poiseColor = Color.Lerp(new Color(1f, 0.5f, 0f), Color.cyan, poisePercent);
        EditorGUI.DrawRect(poiseRect, poiseColor);
        
        // Label
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;
        GUI.Label(rect, $"Poise: {current:F0} / {max:F0} ({poisePercent * 100:F0}%)", labelStyle);
    }
}
