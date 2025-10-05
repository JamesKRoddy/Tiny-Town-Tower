using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

[CustomEditor(typeof(GenericStateMachineBehaviour))]
public class GenericStateMachineBehaviourEditor : Editor
{
    private SerializedProperty functionCallsProperty;
    private SerializedProperty parameterModificationsProperty;

    private void OnEnable()
    {
        functionCallsProperty = serializedObject.FindProperty("functionCalls");
        parameterModificationsProperty = serializedObject.FindProperty("parameterModifications");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generic State Machine Behaviour", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Function Calls Section
        DrawFunctionCallsSection();

        EditorGUILayout.Space();

        // Parameter Modifications Section
        DrawParameterModificationsSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawFunctionCallsSection()
    {
        EditorGUILayout.LabelField("Function Calls", EditorStyles.boldLabel);
        
        if (functionCallsProperty != null)
        {
            // Show array size control
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size", GUILayout.Width(40));
            functionCallsProperty.arraySize = EditorGUILayout.IntField(functionCallsProperty.arraySize);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Function Calls will invoke methods on components when entering/exiting/updating states.", MessageType.Info);
            
            // Draw custom elements for each array item
            for (int i = 0; i < functionCallsProperty.arraySize; i++)
            {
                DrawFunctionCallElement(i);
            }
        }
    }

    private void DrawFunctionCallElement(int index)
    {
        SerializedProperty element = functionCallsProperty.GetArrayElementAtIndex(index);
        
        EditorGUILayout.BeginVertical("box");
        
        SerializedProperty componentTypeName = element.FindPropertyRelative("componentTypeName");
        SerializedProperty functionName = element.FindPropertyRelative("functionName");
        SerializedProperty callOnEnter = element.FindPropertyRelative("callOnEnter");
        SerializedProperty callOnExit = element.FindPropertyRelative("callOnExit");
        SerializedProperty callOnUpdate = element.FindPropertyRelative("callOnUpdate");

        EditorGUILayout.LabelField($"Function Call {index + 1}", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(componentTypeName, new GUIContent("Component Type"));
        EditorGUILayout.PropertyField(functionName, new GUIContent("Function Name"));
        
        EditorGUILayout.PropertyField(callOnEnter, new GUIContent("Call On Enter"));
        EditorGUILayout.PropertyField(callOnExit, new GUIContent("Call On Exit"));
        EditorGUILayout.PropertyField(callOnUpdate, new GUIContent("Call On Update"));

        // Simple validation - just check if the class exists
        if (!string.IsNullOrEmpty(componentTypeName.stringValue) && !string.IsNullOrEmpty(functionName.stringValue))
        {
            Type componentType = Type.GetType(componentTypeName.stringValue);
            
            // Try with assembly name if the first attempt failed
            if (componentType == null)
            {
                componentType = Type.GetType($"{componentTypeName.stringValue}, Assembly-CSharp");
            }
            
            if (componentType != null)
            {
                EditorGUILayout.HelpBox("✓ Class type found", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ Class '{componentTypeName.stringValue}' not found in project", MessageType.Warning);
                EditorGUILayout.HelpBox("Try: 'Enemies.Zombie' (with namespace) or 'Zombie' (without namespace)", MessageType.Info);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawParameterModificationsSection()
    {
        EditorGUILayout.LabelField("Parameter Modifications", EditorStyles.boldLabel);
        
        if (parameterModificationsProperty != null)
        {
            // Show array size control
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size", GUILayout.Width(40));
            parameterModificationsProperty.arraySize = EditorGUILayout.IntField(parameterModificationsProperty.arraySize);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Parameter Modifications will set animator parameters when entering/exiting/updating states.", MessageType.Info);
            
            // Draw custom elements for each array item
            for (int i = 0; i < parameterModificationsProperty.arraySize; i++)
            {
                DrawParameterModificationElement(i);
            }
        }
    }

    private void DrawParameterModificationElement(int index)
    {
        SerializedProperty element = parameterModificationsProperty.GetArrayElementAtIndex(index);
        
        EditorGUILayout.BeginVertical("box");
        
        SerializedProperty parameterName = element.FindPropertyRelative("parameterName");
        SerializedProperty parameterType = element.FindPropertyRelative("parameterType");
        SerializedProperty boolValue = element.FindPropertyRelative("boolValue");
        SerializedProperty intValue = element.FindPropertyRelative("intValue");
        SerializedProperty floatValue = element.FindPropertyRelative("floatValue");
        SerializedProperty useRandom = element.FindPropertyRelative("useRandom");
        SerializedProperty randomMinValue = element.FindPropertyRelative("randomMinValue");
        SerializedProperty randomMaxValue = element.FindPropertyRelative("randomMaxValue");
        SerializedProperty randomMaxInclusive = element.FindPropertyRelative("randomMaxInclusive");
        SerializedProperty modifyOnEnter = element.FindPropertyRelative("modifyOnEnter");
        SerializedProperty modifyOnExit = element.FindPropertyRelative("modifyOnExit");
        SerializedProperty modifyOnUpdate = element.FindPropertyRelative("modifyOnUpdate");

        EditorGUILayout.LabelField($"Parameter {index + 1}", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(parameterName, new GUIContent("Parameter Name"));
        EditorGUILayout.PropertyField(parameterType, new GUIContent("Parameter Type"));
        
        // Show value fields based on parameter type
        GenericStateMachineBehaviour.ParameterModification.ParameterType paramType = 
            (GenericStateMachineBehaviour.ParameterModification.ParameterType)parameterType.enumValueIndex;
        
        if (paramType == GenericStateMachineBehaviour.ParameterModification.ParameterType.Bool)
        {
            EditorGUILayout.PropertyField(boolValue, new GUIContent("Bool Value"));
        }
        else if (paramType == GenericStateMachineBehaviour.ParameterModification.ParameterType.Int || 
                 paramType == GenericStateMachineBehaviour.ParameterModification.ParameterType.Float)
        {
            EditorGUILayout.PropertyField(useRandom, new GUIContent("Use Random Value"));
            
            if (useRandom.boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(randomMinValue, new GUIContent("Min Value"));
                EditorGUILayout.PropertyField(randomMaxValue, new GUIContent("Max Value"));
                EditorGUILayout.EndHorizontal();
                
                if (paramType == GenericStateMachineBehaviour.ParameterModification.ParameterType.Int)
                {
                    EditorGUILayout.PropertyField(randomMaxInclusive, new GUIContent("Max Inclusive (for INT)"));
                    EditorGUILayout.HelpBox("For INT parameters: Max Inclusive = true means max value is included in range", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("For FLOAT parameters: Max value is exclusive (not included in range)", MessageType.Info);
                }
            }
            else
            {
                if (paramType == GenericStateMachineBehaviour.ParameterModification.ParameterType.Int)
                {
                    EditorGUILayout.PropertyField(intValue, new GUIContent("Int Value"));
                }
                else
                {
                    EditorGUILayout.PropertyField(floatValue, new GUIContent("Float Value"));
                }
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("When to Modify:", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(modifyOnEnter, new GUIContent("Modify On Enter"));
        EditorGUILayout.PropertyField(modifyOnExit, new GUIContent("Modify On Exit"));
        EditorGUILayout.PropertyField(modifyOnUpdate, new GUIContent("Modify On Update"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
}
