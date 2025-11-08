using UnityEngine;
using UnityEditor;
using Enemies;

namespace Enemies.Editor
{
    [CustomEditor(typeof(EnemyBase), true)]
    [CanEditMultipleObjects]
    public class EnemyBaseEditor : UnityEditor.Editor
    {
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

        private bool showMovementSettings = true;
        private bool showHealthSettings = true;
        private bool showPoiseSettings = true;
        private bool showCooldownMovementSettings = false;
        private bool showHeadTrackingSettings = false;
        private bool showDebugSettings = false;

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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header with enemy type
            EditorGUILayout.Space(5);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            EditorGUILayout.LabelField(target.GetType().Name, headerStyle);
            EditorGUILayout.Space(5);

            // Character Type
            EditorGUILayout.PropertyField(characterType);
            EditorGUILayout.Space(5);

            // Movement Settings
            showMovementSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showMovementSettings, "Movement Settings");
            if (showMovementSettings)
            {
                EditorGUI.indentLevel++;
                
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
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Health Settings
            showHealthSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showHealthSettings, "Health Settings");
            if (showHealthSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(maxHealth);
                
                // Health bar visualization
                if (Application.isPlaying)
                {
                    EnemyBase enemy = (EnemyBase)target;
                    DrawHealthBar(enemy.Health, enemy.MaxHealth);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Poise Settings
            showPoiseSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showPoiseSettings, "Poise Settings");
            if (showPoiseSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(maxPoise);
                EditorGUILayout.PropertyField(poiseRecoveryRate);
                EditorGUILayout.PropertyField(poiseRecoveryDelay);
                
                // Poise bar visualization
                if (Application.isPlaying)
                {
                    EnemyBase enemy = (EnemyBase)target;
                    DrawPoiseBar(enemy.Poise, enemy.MaxPoise);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Cooldown Movement Settings
            showCooldownMovementSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showCooldownMovementSettings, "Cooldown Movement Settings");
            if (showCooldownMovementSettings)
            {
                EditorGUI.indentLevel++;
                
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
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Head Tracking Settings
            showHeadTrackingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showHeadTrackingSettings, "Head Tracking Settings");
            if (showHeadTrackingSettings)
            {
                EditorGUI.indentLevel++;
                
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
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Debug Settings
            showDebugSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugSettings, "Debug Settings");
            if (showDebugSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(showCollisionDebug);
                
                // Runtime debug info
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(3);
                    EnemyBase enemy = (EnemyBase)target;
                    
                    EditorGUILayout.LabelField("Runtime Info", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField($"Is Attacking: {enemy.isAttacking}");
                    EditorGUILayout.LabelField($"Health: {enemy.Health:F1} / {enemy.MaxHealth:F1}");
                    EditorGUILayout.LabelField($"Poise: {enemy.Poise:F1} / {enemy.MaxPoise:F1}");
                    
                    // Show attack components
                    var attacks = enemy.GetComponents<AttackBase>();
                    if (attacks.Length > 0)
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.LabelField($"Attack Components ({attacks.Length})", EditorStyles.miniBoldLabel);
                        foreach (var attack in attacks)
                        {
                            EditorGUILayout.LabelField($"  • {attack.GetType().Name}");
                        }
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);

            // Draw remaining properties
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                "characterType",
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
