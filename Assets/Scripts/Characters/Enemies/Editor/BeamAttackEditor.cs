using UnityEngine;
using UnityEditor;
using Enemies.Attacks;

namespace Enemies.Editor
{
    [CustomEditor(typeof(BeamAttack))]
    [CanEditMultipleObjects]
    public class BeamAttackEditor : AttackBaseEditor
    {
        private SerializedProperty beamHitLayers;
        private SerializedProperty damageInterval;
        private SerializedProperty beamFirePoint;
        private SerializedProperty beamVisual;
        private SerializedProperty headIKWeight;
        private SerializedProperty headIKRotationWeight;
        private SerializedProperty headIKLerpSpeed;
        private SerializedProperty maxHeadRotationAngle;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                beamHitLayers = serializedObject.FindProperty("beamHitLayers");
                damageInterval = serializedObject.FindProperty("damageInterval");
                beamFirePoint = serializedObject.FindProperty("beamFirePoint");
                beamVisual = serializedObject.FindProperty("beamVisual");
                headIKWeight = serializedObject.FindProperty("headIKWeight");
                headIKRotationWeight = serializedObject.FindProperty("headIKRotationWeight");
                headIKLerpSpeed = serializedObject.FindProperty("headIKLerpSpeed");
                maxHeadRotationAngle = serializedObject.FindProperty("maxHeadRotationAngle");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // Add beam specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Beam Settings", EditorStyles.boldLabel);
            
            if (beamHitLayers != null)
            {
                EditorGUILayout.PropertyField(beamHitLayers);
            }
            
            if (damageInterval != null)
            {
                float interval = damageInterval.floatValue;
                interval = Mathf.Max(0.01f, EditorGUILayout.FloatField("Damage Interval", interval));
                damageInterval.floatValue = interval;
            }
            
            if (beamFirePoint != null)
            {
                EditorGUILayout.PropertyField(beamFirePoint);
            }
            
            if (beamVisual != null)
            {
                EditorGUILayout.PropertyField(beamVisual);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Head IK Settings", EditorStyles.boldLabel);
            
            if (headIKWeight != null)
            {
                float weight = headIKWeight.floatValue;
                weight = Mathf.Clamp01(EditorGUILayout.Slider("Head IK Weight", weight, 0f, 1f));
                headIKWeight.floatValue = weight;
            }
            
            if (headIKRotationWeight != null)
            {
                float rotationWeight = headIKRotationWeight.floatValue;
                rotationWeight = Mathf.Clamp01(EditorGUILayout.Slider("Head IK Rotation Weight", rotationWeight, 0f, 1f));
                headIKRotationWeight.floatValue = rotationWeight;
            }
            
            if (headIKLerpSpeed != null)
            {
                float lerpSpeed = headIKLerpSpeed.floatValue;
                lerpSpeed = Mathf.Max(0.1f, EditorGUILayout.FloatField("Head IK Lerp Speed", lerpSpeed));
                headIKLerpSpeed.floatValue = lerpSpeed;
            }
            
            if (maxHeadRotationAngle != null)
            {
                float maxAngle = maxHeadRotationAngle.floatValue;
                maxAngle = Mathf.Clamp(EditorGUILayout.FloatField("Max Head Rotation Angle", maxAngle), 0f, 180f);
                maxHeadRotationAngle.floatValue = maxAngle;
            }
        }

        protected override void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            // Get beam-specific values
            Transform firePoint = beamFirePoint != null && beamFirePoint.objectReferenceValue != null ? beamFirePoint.objectReferenceValue as Transform : null;
            float headAngle = maxHeadRotationAngle != null ? maxHeadRotationAngle.floatValue : 90f;
            
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
            
            // Draw beam fire point - yellow
            if (firePoint != null)
            {
                // Calculate fire point position relative to center (simplified for editor visualization)
                Vector3 visualFirePoint = new Vector3(centerX, centerY, 0);
                
                Handles.color = new Color(1f, 1f, 0f, 0.8f);
                Handles.DrawSolidDisc(visualFirePoint, Vector3.forward, 4);
                
                // Draw beam direction from fire point
                Vector3 beamDirection = firePoint.forward;
                Vector3 visualBeamEnd = visualFirePoint + new Vector3(beamDirection.x, beamDirection.z, 0) * visualMaxRange;
                Handles.color = new Color(1f, 0.5f, 0f, 0.9f);
                Handles.DrawLine(visualFirePoint, visualBeamEnd);
                
                // Draw beam angle cone from fire point
                float angleRad = attackAngleThreshold * Mathf.Deg2Rad;
                Vector3 rightDir = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
                Vector3 leftDir = new Vector3(-Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
                
                Vector3 rightEnd = visualFirePoint + new Vector3(rightDir.x, rightDir.z, 0) * visualMaxRange;
                Vector3 leftEnd = visualFirePoint + new Vector3(leftDir.x, leftDir.z, 0) * visualMaxRange;
                
                Handles.color = new Color(0f, 1f, 1f, 0.6f);
                Handles.DrawLine(visualFirePoint, rightEnd);
                Handles.DrawLine(visualFirePoint, leftEnd);
            }
            else
            {
                // Draw default beam direction from center
                Handles.color = new Color(1f, 1f, 0f, 0.8f);
                Handles.DrawSolidDisc(new Vector3(centerX, centerY, 0), Vector3.forward, 4);
                
                // Draw beam direction
                Handles.color = new Color(1f, 0.5f, 0f, 0.9f);
                Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX, centerY - visualMaxRange, 0));
                
                // Draw beam angle cone
                float angleRad = attackAngleThreshold * Mathf.Deg2Rad;
                Vector3 rightDir = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
                Vector3 leftDir = new Vector3(-Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * visualMaxRange;
                
                Handles.color = new Color(0f, 1f, 1f, 0.6f);
                Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + rightDir.x, centerY + rightDir.z, 0));
                Handles.DrawLine(new Vector3(centerX, centerY, 0), new Vector3(centerX + leftDir.x, centerY + leftDir.z, 0));
            }
            
            // Draw head IK range - green arc (centered, showing full rotation range)
            float visualHeadRange = visualMaxRange * 0.8f; // Slightly smaller than max range
            Handles.color = new Color(0f, 1f, 0f, 0.3f);
            // Start from -headAngle/2 and sweep headAngle degrees
            Handles.DrawSolidArc(new Vector3(centerX, centerY, 0), Vector3.forward, Vector3.left, headAngle, visualHeadRange);
            Handles.color = new Color(0f, 1f, 0f, 0.8f);
            Handles.DrawWireArc(new Vector3(centerX, centerY, 0), Vector3.forward, Vector3.left, headAngle, visualHeadRange);
            
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
            GUI.Label(new Rect(labelX, labelY + 24, 120, 12), $"Beam Angle: ±{attackAngleThreshold:F0}°", labelStyle);
            GUI.Label(new Rect(labelX, labelY + 36, 120, 12), $"Head IK: ±{headAngle:F0}°", labelStyle);
            
            if (firePoint != null)
            {
                GUI.Label(new Rect(labelX, labelY + 48, 120, 12), "Fire Point: Set", labelStyle);
            }
            else
            {
                GUI.Label(new Rect(labelX, labelY + 48, 120, 12), "Fire Point: None", labelStyle);
            }
            
            // Legend - positioned at bottom right
            float legendX = rect.x + rect.width - 120;
            float legendY = rect.y + rect.height - 50;
            GUI.Label(new Rect(legendX, legendY, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 10, 115, 10), "Gray: Too Close", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 20, 115, 10), "Red: Range", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 30, 115, 10), "Yellow: Fire Point", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 40, 115, 10), "Orange: Beam", labelStyle);
            GUI.Label(new Rect(legendX, legendY + 50, 115, 10), "Green: Head IK", labelStyle);
        }
    }
}
