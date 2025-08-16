using UnityEngine;
using System;
using System.Reflection;

public class GenericStateMachineBehaviour : StateMachineBehaviour
{
    [Serializable]
    public class FunctionCall
    {
        public string componentTypeName;
        public string functionName;
        public bool callOnEnter = true;
        public bool callOnExit = false;
        public bool callOnUpdate = false;
    }

    [Serializable]
    public class ParameterModification
    {
        public string parameterName;
        public enum ParameterType { Bool, Int, Float, Trigger, ResetTrigger }
        public ParameterType parameterType;
        
        // Values for different parameter types
        public bool boolValue;
        public int intValue;
        public float floatValue;
        
        public bool modifyOnEnter = true;
        public bool modifyOnExit = false;
        public bool modifyOnUpdate = false;
    }

    [SerializeField] protected FunctionCall[] functionCalls;
    [SerializeField] protected ParameterModification[] parameterModifications;

    protected Component[] cachedComponents;
    protected MethodInfo[] cachedMethods;

    protected virtual void Awake()
    {
        if (functionCalls != null && functionCalls.Length > 0)
        {
            cachedComponents = new Component[functionCalls.Length];
            cachedMethods = new MethodInfo[functionCalls.Length];
        }
    }

    protected void CacheComponentsAndMethods(Animator animator)
    {
        if (functionCalls == null) return;

        for (int i = 0; i < functionCalls.Length; i++)
        {
            var call = functionCalls[i];
            if (string.IsNullOrEmpty(call.componentTypeName) || string.IsNullOrEmpty(call.functionName))
                continue;

            // Get the component type
            Type componentType = Type.GetType(call.componentTypeName);
            if (componentType == null)
            {
                Debug.LogWarning($"Component type {call.componentTypeName} not found");
                continue;
            }

            // Get the component
            cachedComponents[i] = animator.GetComponent(componentType);
            if (cachedComponents[i] == null)
            {
                Debug.LogWarning($"Component of type {call.componentTypeName} not found on {animator.gameObject.name}");
                continue;
            }

            // Get the method
            cachedMethods[i] = componentType.GetMethod(call.functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (cachedMethods[i] == null)
            {
                Debug.LogWarning($"Method {call.functionName} not found on component {call.componentTypeName}");
                continue;
            }
        }
    }

    protected void CallFunctions(bool isEnter, bool isExit, bool isUpdate)
    {
        if (functionCalls == null) return;

        for (int i = 0; i < functionCalls.Length; i++)
        {
            var call = functionCalls[i];
            if (cachedComponents[i] == null || cachedMethods[i] == null)
                continue;

            if ((isEnter && call.callOnEnter) || 
                (isExit && call.callOnExit) || 
                (isUpdate && call.callOnUpdate))
            {
                try
                {
                    cachedMethods[i].Invoke(cachedComponents[i], null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error calling {call.functionName} on {call.componentTypeName}: {e.Message}");
                }
            }
        }
    }

    protected void ModifyParameters(Animator animator, bool isEnter, bool isExit, bool isUpdate)
    {
        if (parameterModifications == null) return;

        foreach (var mod in parameterModifications)
        {
            if (string.IsNullOrEmpty(mod.parameterName))
                continue;

            if ((isEnter && mod.modifyOnEnter) || 
                (isExit && mod.modifyOnExit) || 
                (isUpdate && mod.modifyOnUpdate))
            {
                try
                {
                    switch (mod.parameterType)
                    {
                        case ParameterModification.ParameterType.Bool:
                            animator.SetBool(mod.parameterName, mod.boolValue);
                            break;
                        case ParameterModification.ParameterType.Int:
                            animator.SetInteger(mod.parameterName, mod.intValue);
                            break;
                        case ParameterModification.ParameterType.Float:
                            animator.SetFloat(mod.parameterName, mod.floatValue);
                            break;
                        case ParameterModification.ParameterType.Trigger:
                            animator.SetTrigger(mod.parameterName);
                            break;
                        case ParameterModification.ParameterType.ResetTrigger:
                            animator.ResetTrigger(mod.parameterName);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error modifying parameter {mod.parameterName}: {e.Message}");
                }
            }
        }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CacheComponentsAndMethods(animator);
        CallFunctions(true, false, false);
        ModifyParameters(animator, true, false, false);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CallFunctions(false, true, false);
        ModifyParameters(animator, false, true, false);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CallFunctions(false, false, true);
        ModifyParameters(animator, false, false, true);
    }
} 