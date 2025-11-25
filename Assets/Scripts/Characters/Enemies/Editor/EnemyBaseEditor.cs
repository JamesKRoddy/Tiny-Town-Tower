using UnityEngine;
using UnityEditor;
using Enemies;

namespace Enemies.Editor
{
    /// <summary>
    /// Custom inspector for EnemyBase with organized sections and visual hierarchy
    /// Similar structure to SettlerNPCEditor for consistency
    /// </summary>
    [CustomEditor(typeof(EnemyBase), true)]
    [CanEditMultipleObjects]
    public class EnemyBaseEditor : UnityEditor.Editor
    {
        // Serialized Properties
        private SerializedProperty characterType;
        private SerializedProperty useRootMotion;
        private SerializedProperty stoppingDistance;
        private SerializedProperty rotationSpeed;
        private SerializedProperty movementSpeed;
        private SerializedProperty acceleration;
        private SerializedProperty angularSpeed;
        private SerializedProperty obstacleBoundsOffset;
        private SerializedProperty maxHealth;
        private SerializedProperty maxPoise;
        private SerializedProperty poiseRecoveryRate;
        private SerializedProperty poiseRecoveryDelay;
        private SerializedProperty showCollisionDebug;
        
        // Cooldown Movement
        private SerializedProperty enableCooldownMovement;
        private SerializedProperty cooldownMovementMinDistance;
        private SerializedProperty cooldownMovementMaxDistance;
        private SerializedProperty cooldownMovementMinDuration;
        private SerializedProperty cooldownMovementMaxDuration;
        
        // Head Tracking
        private SerializedProperty enableHeadTracking;
        private SerializedProperty headTrackingWeight;
        private SerializedProperty headTrackingRotationWeight;
        private SerializedProperty headTrackingLerpSpeed;
        private SerializedProperty maxHeadTrackingAngle;
        private SerializedProperty headTrackingDistance;
        
        // Appearance System (for HumanoidEnemy)
        private SerializedProperty appearanceSystem;
        private SerializedProperty randomizeAppearanceOnSpawn;

        protected virtual void OnEnable()
        {
            characterType = serializedObject.FindProperty("characterType");
            useRootMotion = serializedObject.FindProperty("useRootMotion");
            stoppingDistance = serializedObject.FindProperty("stoppingDistance");
            rotationSpeed = serializedObject.FindProperty("rotationSpeed");
            movementSpeed = serializedObject.FindProperty("movementSpeed");
            acceleration = serializedObject.FindProperty("acceleration");
            angularSpeed = serializedObject.FindProperty("angularSpeed");
            obstacleBoundsOffset = serializedObject.FindProperty("obstacleBoundsOffset");
            maxHealth = serializedObject.FindProperty("maxHealth");
            maxPoise = serializedObject.FindProperty("maxPoise");
            poiseRecoveryRate = serializedObject.FindProperty("poiseRecoveryRate");
            poiseRecoveryDelay = serializedObject.FindProperty("poiseRecoveryDelay");
            showCollisionDebug = serializedObject.FindProperty("showCollisionDebug");
            
            // Cooldown Movement
            enableCooldownMovement = serializedObject.FindProperty("enableCooldownMovement");
            cooldownMovementMinDistance = serializedObject.FindProperty("cooldownMovementMinDistance");
            cooldownMovementMaxDistance = serializedObject.FindProperty("cooldownMovementMaxDistance");
            cooldownMovementMinDuration = serializedObject.FindProperty("cooldownMovementMinDuration");
            cooldownMovementMaxDuration = serializedObject.FindProperty("cooldownMovementMaxDuration");
            
            // Head Tracking
            enableHeadTracking = serializedObject.FindProperty("enableHeadTracking");
            headTrackingWeight = serializedObject.FindProperty("headTrackingWeight");
            headTrackingRotationWeight = serializedObject.FindProperty("headTrackingRotationWeight");
            headTrackingLerpSpeed = serializedObject.FindProperty("headTrackingLerpSpeed");
            maxHeadTrackingAngle = serializedObject.FindProperty("maxHeadTrackingAngle");
            headTrackingDistance = serializedObject.FindProperty("headTrackingDistance");
            
            // Appearance System (may be null for non-humanoid enemies)
            appearanceSystem = serializedObject.FindProperty("appearanceSystem");
            randomizeAppearanceOnSpawn = serializedObject.FindProperty("randomizeAppearanceOnSpawn");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EnemyBase enemy = (EnemyBase)target;

            // Header Section
            DrawColoredSection("🎯 Custom Inspector Active", () => {
                EditorGUILayout.HelpBox($"Custom inspector for {target.GetType().Name}", MessageType.Info);
            }, new Color(0.2f, 0.6f, 1f, 0.3f)); // Blue tint
            
            // Enemy Info Section (Runtime)
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                DrawEnemyInfoSection(enemy);
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Runtime enemy information will be displayed here during Play Mode", MessageType.Info);
            }
            
            // Character Type
            DrawColoredSection("🎭 Character Type", () => {
                EditorGUILayout.PropertyField(characterType);
            }, new Color(0.9f, 0.8f, 1f, 0.3f)); // Light purple tint
            
            // Appearance System (for HumanoidEnemy)
            if (appearanceSystem != null)
            {
                DrawColoredSection("👤 Appearance System", () => {
                    // Preview size slider
                    AppearanceOptionDrawer.DrawPreviewSizeSlider();
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.PropertyField(appearanceSystem);
                    EditorGUILayout.PropertyField(randomizeAppearanceOnSpawn);
                    
                    if (Application.isPlaying && appearanceSystem.objectReferenceValue != null)
                    {
                        EditorGUILayout.HelpBox("Appearance is managed at runtime", MessageType.Info);
                    }
                }, new Color(0.8f, 0.9f, 1f, 0.3f)); // Light blue tint
            }
            
            // Movement Settings
            DrawColoredSection("🏃 Movement & Navigation", () => {
                EditorGUILayout.PropertyField(useRootMotion);
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Navigation", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(stoppingDistance);
                EditorGUILayout.PropertyField(obstacleBoundsOffset);
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Speed & Rotation", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(movementSpeed);
                EditorGUILayout.PropertyField(rotationSpeed);
                EditorGUILayout.PropertyField(acceleration);
                EditorGUILayout.PropertyField(angularSpeed);
            }, new Color(0.9f, 0.8f, 1f, 0.3f)); // Light purple tint

            // Health & Combat
            DrawColoredSection("❤️ Health & Combat", () => {
                EditorGUILayout.PropertyField(maxHealth);
                
                // Health bar visualization
                if (Application.isPlaying)
                {
                    DrawHealthBar(enemy.Health, enemy.MaxHealth);
                }
            }, new Color(1f, 0.8f, 0.8f, 0.3f)); // Light red tint

            // Poise System
            DrawColoredSection("💪 Poise System", () => {
                EditorGUILayout.PropertyField(maxPoise);
                EditorGUILayout.PropertyField(poiseRecoveryRate);
                EditorGUILayout.PropertyField(poiseRecoveryDelay);
                
                // Poise bar visualization
                if (Application.isPlaying)
                {
                    DrawPoiseBar(enemy.Poise, enemy.MaxPoise);
                }
            }, new Color(1f, 0.9f, 0.8f, 0.3f)); // Light orange tint

            // Cooldown Movement Settings
            DrawColoredSection("🎯 Cooldown Movement", () => {
                EditorGUILayout.PropertyField(enableCooldownMovement);
                
                if (enableCooldownMovement != null && enableCooldownMovement.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(cooldownMovementMinDistance);
                    EditorGUILayout.PropertyField(cooldownMovementMaxDistance);
                    EditorGUILayout.PropertyField(cooldownMovementMinDuration);
                    EditorGUILayout.PropertyField(cooldownMovementMaxDuration);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox("Enable to make enemies reposition during attack cooldowns", MessageType.Info);
                }
            }, new Color(0.9f, 1f, 0.9f, 0.3f)); // Light green tint

            // Head Tracking Settings (only for humanoid enemies)
            bool isHumanoid = target is HumanoidEnemy;
            if (isHumanoid)
            {
                DrawColoredSection("👀 Head Tracking (Humanoid)", () => {
                    EditorGUILayout.PropertyField(enableHeadTracking);
                    
                    if (enableHeadTracking != null && enableHeadTracking.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(headTrackingWeight);
                        EditorGUILayout.PropertyField(headTrackingRotationWeight);
                        EditorGUILayout.PropertyField(headTrackingLerpSpeed);
                        EditorGUILayout.PropertyField(maxHeadTrackingAngle);
                        EditorGUILayout.PropertyField(headTrackingDistance);
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Enable to make the enemy's head track the player", MessageType.Info);
                    }
                }, new Color(0.8f, 1f, 1f, 0.3f)); // Light cyan tint
            }
            
            // Attack Components Section (Runtime)
            if (Application.isPlaying)
            {
                DrawAttackComponentsSection(enemy);
            }

            // Debug Settings
            DrawColoredSection("🔧 Debug Settings", () => {
                EditorGUILayout.PropertyField(showCollisionDebug);
                
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
                    }
                }
            }, new Color(0.9f, 0.9f, 0.9f, 0.3f)); // Light gray tint
            
            // Draw separator
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);

            // Draw remaining properties not covered by custom sections
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                "characterType",
                "appearanceSystem",
                "randomizeAppearanceOnSpawn",
                "useRootMotion",
                "stoppingDistance",
                "rotationSpeed",
                "movementSpeed",
                "acceleration",
                "angularSpeed",
                "obstacleBoundsOffset",
                "maxHealth",
                "maxPoise",
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
        /// Draw a section with a colored background (matches SettlerNPC style)
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
        /// Draw enemy runtime information section
        /// </summary>
        private void DrawEnemyInfoSection(EnemyBase enemy)
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
                
                // Combat state
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Combat State", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Attacking", enemy.isAttacking ? "Yes" : "No");
                
                // Target information
                if (enemy.NavMeshTarget != null)
                {
                    float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.NavMeshTarget.position);
                    EditorGUILayout.LabelField("Target Distance", $"{distanceToTarget:F2}m");
                }
                
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
                }
            }, new Color(0.8f, 1f, 0.8f, 0.3f)); // Light green tint
        }
        
        /// <summary>
        /// Draw attack components section
        /// </summary>
        private void DrawAttackComponentsSection(EnemyBase enemy)
        {
            var attacks = enemy.GetComponents<AttackBase>();
            if (attacks.Length > 0)
            {
                DrawColoredSection($"⚔️ Attack Components ({attacks.Length})", () => {
                    foreach (var attack in attacks)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        // Attack type name with color
                        string attackTypeName = attack.GetType().Name;
                        GUI.color = Color.cyan;
                        EditorGUILayout.LabelField($"• {attackTypeName}", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                        
                        // Attack details
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField($"Min Range: {attack.minRange:F1}m");
                        EditorGUILayout.LabelField($"Max Range: {attack.maxRange:F1}m");
                        EditorGUILayout.LabelField($"Damage: {attack.damage:F1}");
                        EditorGUILayout.LabelField($"Cooldown: {attack.cooldown:F1}s");
                        EditorGUILayout.LabelField($"Can Attack: {attack.CanAttack()}");
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }
                }, new Color(1f, 0.8f, 0.9f, 0.3f)); // Light pink tint
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
}
