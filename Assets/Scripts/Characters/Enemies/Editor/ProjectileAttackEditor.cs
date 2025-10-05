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
        private SerializedProperty projectileEffect;
        private SerializedProperty impactEffect;

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
                projectileEffect = serializedObject.FindProperty("projectileEffect");
                impactEffect = serializedObject.FindProperty("impactEffect");
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
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Projectile Effects", EditorStyles.boldLabel);
            if (projectileEffect != null) EditorGUILayout.PropertyField(projectileEffect);
            if (impactEffect != null) EditorGUILayout.PropertyField(impactEffect);
        }
    }
}
