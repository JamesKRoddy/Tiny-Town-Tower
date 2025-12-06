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
        
        // Method parameter support
        public enum ParameterType { None, Int, Float, Bool, String }
        public ParameterType parameterType = ParameterType.None;
        
        // Parameter values
        public int intParameter;
        public float floatParameter;
        public bool boolParameter;
        public string stringParameter;
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
        
        // Random animation support
        public bool useRandom = false;
        public int randomMinValue = 0;
        public int randomMaxValue = 1;
        public bool randomMaxInclusive = true; // For INT types, max is inclusive; for FLOAT, it's exclusive
        
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

            // Get the method - try to find method with or without parameters
            MethodInfo method = null;
            
            // First, try to find method based on parameter type
            if (call.parameterType != FunctionCall.ParameterType.None)
            {
                Type[] parameterTypes = null;
                switch (call.parameterType)
                {
                    case FunctionCall.ParameterType.Int:
                        parameterTypes = new Type[] { typeof(int) };
                        break;
                    case FunctionCall.ParameterType.Float:
                        parameterTypes = new Type[] { typeof(float) };
                        break;
                    case FunctionCall.ParameterType.Bool:
                        parameterTypes = new Type[] { typeof(bool) };
                        break;
                    case FunctionCall.ParameterType.String:
                        parameterTypes = new Type[] { typeof(string) };
                        break;
                }
                
                if (parameterTypes != null)
                {
                    method = componentType.GetMethod(call.functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
                }
            }
            
            // If not found with parameters, try without parameters (for backwards compatibility)
            if (method == null)
            {
                method = componentType.GetMethod(call.functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            if (method == null)
            {
                Debug.LogWarning($"Method {call.functionName} not found on component {call.componentTypeName} with the specified parameter type");
                continue;
            }
            
            cachedMethods[i] = method;
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
                    // Prepare parameters based on parameter type
                    object[] parameters = null;
                    if (call.parameterType != FunctionCall.ParameterType.None)
                    {
                        switch (call.parameterType)
                        {
                            case FunctionCall.ParameterType.Int:
                                parameters = new object[] { call.intParameter };
                                break;
                            case FunctionCall.ParameterType.Float:
                                parameters = new object[] { call.floatParameter };
                                break;
                            case FunctionCall.ParameterType.Bool:
                                parameters = new object[] { call.boolParameter };
                                break;
                            case FunctionCall.ParameterType.String:
                                parameters = new object[] { call.stringParameter };
                                break;
                        }
                    }
                    
                    cachedMethods[i].Invoke(cachedComponents[i], parameters);
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
                            if (mod.useRandom)
                            {
                                int randomValue = UnityEngine.Random.Range(mod.randomMinValue, mod.randomMaxValue + (mod.randomMaxInclusive ? 1 : 0));
                                animator.SetInteger(mod.parameterName, randomValue);
                            }
                            else
                            {
                                animator.SetInteger(mod.parameterName, mod.intValue);
                            }
                            break;
                        case ParameterModification.ParameterType.Float:
                            if (mod.useRandom)
                            {
                                float randomValue = UnityEngine.Random.Range(mod.randomMinValue, mod.randomMaxValue + (mod.randomMaxInclusive ? 0f : 1f));
                                animator.SetFloat(mod.parameterName, randomValue);
                            }
                            else
                            {
                                animator.SetFloat(mod.parameterName, mod.floatValue);
                            }
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