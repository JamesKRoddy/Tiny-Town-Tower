using UnityEngine;
using UnityEditor;
using Enemies.Attacks;

namespace Enemies.Editor
{
    [CustomEditor(typeof(JumpAttack))]
    [CanEditMultipleObjects]
    public class JumpAttackEditor : AttackBaseEditor
    {
        private SerializedProperty jumpRadius;
        private SerializedProperty jumpHeight;
        private SerializedProperty jumpDuration;
        private SerializedProperty predictionFactor;
        private SerializedProperty finalJumpLockPercentage;
        private SerializedProperty rotationSpeed;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Check if serializedObject is valid before accessing properties
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                jumpRadius = serializedObject.FindProperty("jumpRadius");
                jumpHeight = serializedObject.FindProperty("jumpHeight");
                jumpDuration = serializedObject.FindProperty("jumpDuration");
                predictionFactor = serializedObject.FindProperty("predictionFactor");
                finalJumpLockPercentage = serializedObject.FindProperty("finalJumpLockPercentage");
                rotationSpeed = serializedObject.FindProperty("rotationSpeed");
            }
        }

        protected override void DrawChildSpecificSettings()
        {
            // Add jump attack specific settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Jump Settings", EditorStyles.boldLabel);
            
            if (jumpRadius != null)
            {
                float radius = jumpRadius.floatValue;
                radius = Mathf.Clamp(EditorGUILayout.FloatField("Jump Radius", radius), 1f, 10f);
                jumpRadius.floatValue = radius;
            }
            
            if (jumpHeight != null)
            {
                float height = jumpHeight.floatValue;
                height = Mathf.Clamp(EditorGUILayout.FloatField("Jump Height", height), 1f, 30f);
                jumpHeight.floatValue = height;
            }
            
            if (jumpDuration != null)
            {
                float duration = jumpDuration.floatValue;
                duration = Mathf.Clamp(EditorGUILayout.FloatField("Jump Duration", duration), 0.1f, 3f);
                jumpDuration.floatValue = duration;
            }
            
            if (predictionFactor != null)
            {
                float prediction = predictionFactor.floatValue;
                prediction = Mathf.Clamp01(EditorGUILayout.Slider("Prediction Factor", prediction, 0f, 1f));
                predictionFactor.floatValue = prediction;
            }
            
            if (finalJumpLockPercentage != null)
            {
                float lockPercentage = finalJumpLockPercentage.floatValue;
                lockPercentage = Mathf.Clamp01(EditorGUILayout.Slider("Final Jump Lock %", lockPercentage, 0f, 1f));
                finalJumpLockPercentage.floatValue = lockPercentage;
            }
            
            if (rotationSpeed != null)
            {
                float rotation = rotationSpeed.floatValue;
                rotation = Mathf.Clamp(EditorGUILayout.FloatField("Rotation Speed", rotation), 90f, 720f);
                rotationSpeed.floatValue = rotation;
            }
        }


        protected override void DrawAttackVisualization(float minRange, float maxRange, float attackAngleThreshold)
        {
            // Get jump-specific values
            float jumpRadius = this.jumpRadius != null ? this.jumpRadius.floatValue : 3f;
            float jumpHeight = this.jumpHeight != null ? this.jumpHeight.floatValue : 5f;
            
            // Use EditorGUILayout for proper layout integration
            Rect rect = GUILayoutUtility.GetRect(0, 160, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            // Calculate positions - side view
            float startX = rect.x + 40;
            float endX = rect.x + rect.width - 40;
            float groundY = rect.y + rect.height - 30;
            float maxJumpHeight = rect.height - 60;
            
            // Scale for visualization
            float horizontalScale = (endX - startX) / Mathf.Max(maxRange, 1f);
            float verticalScale = maxJumpHeight / Mathf.Max(jumpHeight * 2f, 1f); // *2 for visual clarity
            
            // Draw ground line
            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Handles.DrawLine(new Vector3(rect.x, groundY, 0), new Vector3(rect.x + rect.width, groundY, 0));
            
            // Draw min range marker
            if (minRange > 0)
            {
                float minX = startX + minRange * horizontalScale;
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                Handles.DrawLine(new Vector3(minX, groundY - 5, 0), new Vector3(minX, groundY + 5, 0));
            }
            
            // Draw max range marker
            float maxX = startX + maxRange * horizontalScale;
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Handles.DrawLine(new Vector3(maxX, groundY - 5, 0), new Vector3(maxX, groundY + 5, 0));
            
            // Draw jump arc
            int segments = 30;
            Vector3 previousPoint = new Vector3(startX, groundY, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float horizontalPos = startX + (maxX - startX) * t;
                
                // Calculate arc height using sine wave
                float arcHeight = Mathf.Sin(t * Mathf.PI) * jumpHeight * verticalScale;
                float verticalPos = groundY - arcHeight;
                
                Vector3 currentPoint = new Vector3(horizontalPos, verticalPos, 0);
                
                // Color gradient from yellow to orange
                Handles.color = Color.Lerp(new Color(1f, 1f, 0f, 0.8f), new Color(1f, 0.5f, 0f, 0.8f), t);
                Handles.DrawLine(previousPoint, currentPoint);
                
                // Draw dots along the arc
                if (i % 3 == 0)
                {
                    Handles.DrawSolidDisc(currentPoint, Vector3.forward, 2);
                }
                
                previousPoint = currentPoint;
            }
            
            // Draw landing radius at destination
            float landingRadiusVisual = jumpRadius * horizontalScale * 0.5f; // Scale down for side view
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Handles.DrawSolidArc(
                new Vector3(maxX, groundY, 0),
                Vector3.forward,
                Vector3.left,
                180f,
                landingRadiusVisual
            );
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Handles.DrawWireArc(
                new Vector3(maxX, groundY, 0),
                Vector3.forward,
                Vector3.left,
                180f,
                landingRadiusVisual
            );
            
            // Draw start position (enemy)
            Handles.color = Color.white;
            Handles.DrawSolidDisc(new Vector3(startX, groundY, 0), Vector3.forward, 5);
            
            // Draw landing position marker
            Handles.color = new Color(1f, 0f, 0f, 0.8f);
            Handles.DrawSolidDisc(new Vector3(maxX, groundY, 0), Vector3.forward, 4);
            
            // Draw peak of jump
            float peakX = startX + (maxX - startX) * 0.5f;
            float peakY = groundY - jumpHeight * verticalScale;
            Handles.color = new Color(0f, 1f, 1f, 0.6f);
            Handles.DrawWireCube(new Vector3(peakX, peakY, 0), new Vector3(8, 8, 0));
            
            Handles.EndGUI();
            
            // Labels
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 8;
            labelStyle.normal.textColor = Color.white;
            
            // Top labels
            float labelY = rect.y + 5;
            GUI.Label(new Rect(rect.x + 5, labelY, 150, 12), $"Max Range: {maxRange:F1}m", labelStyle);
            GUI.Label(new Rect(rect.x + 5, labelY + 12, 150, 12), $"Jump Height: {jumpHeight:F1}m", labelStyle);
            GUI.Label(new Rect(rect.x + 5, labelY + 24, 150, 12), $"Landing Radius: {jumpRadius:F1}m", labelStyle);
            
            if (minRange > 0)
            {
                GUI.Label(new Rect(rect.x + 5, labelY + 36, 150, 12), $"Min Range: {minRange:F1}m", labelStyle);
            }
            
            // Legend
            float legendX = rect.x + rect.width - 120;
            float legendYPos = rect.y + 5;
            GUI.Label(new Rect(legendX, legendYPos, 115, 10), "Legend:", labelStyle);
            GUI.Label(new Rect(legendX, legendYPos + 10, 115, 10), "White: Start", labelStyle);
            GUI.Label(new Rect(legendX, legendYPos + 20, 115, 10), "Yellow: Jump Arc", labelStyle);
            GUI.Label(new Rect(legendX, legendYPos + 30, 115, 10), "Red: Landing", labelStyle);
            GUI.Label(new Rect(legendX, legendYPos + 40, 115, 10), "Cyan: Peak", labelStyle);
            
            // Bottom note
            labelStyle.fontSize = 7;
            labelStyle.fontStyle = FontStyle.Italic;
            GUI.Label(new Rect(rect.x + 5, groundY + 10, rect.width - 10, 12), "Side View - Jump arc shows enemy leaping from start position to max range", labelStyle);
        }
    }
}

