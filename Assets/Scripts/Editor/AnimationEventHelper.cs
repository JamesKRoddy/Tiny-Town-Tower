using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Comprehensive editor tool to manage animation events for all animation types
/// Supports: Footsteps, Combat (Attack VFX, Weapon hitboxes), Work tasks, and custom events
/// </summary>
public class AnimationEventHelper : EditorWindow
{
    private enum EventCategory
    {
        Footsteps,
        Combat,
        Work,
        Custom
    }
    
    private AnimationClip selectedClip;
    private EventCategory selectedCategory = EventCategory.Footsteps;
    private Vector2 scrollPosition;
    private bool useNormalizedTime = false;
    
    // Footstep settings
    private float leftFootFrame = 0f;
    private float rightFootFrame = 0f;
    private float leftFootIntensity = 1.0f;
    private float rightFootIntensity = 1.0f;
    
    // Combat settings
    private float attackVfxFrame = 0f;
    private int attackDirection = 0;
    private float weaponStartFrame = 0f;
    private float weaponEndFrame = 0f;
    
    // Work settings
    private float workEffectFrame = 0f;
    private string workTaskEnum = "";
    
    // Custom event settings
    private string customEventName = "";
    private float customEventFrame = 0f;
    private string customEventStringParam = "";
    private float customEventFloatParam = 0f;
    private int customEventIntParam = 0;
    private bool useStringParam = false;
    private bool useFloatParam = false;
    private bool useIntParam = false;
    
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    
    [MenuItem("Tools/Animation Event Helper")]
    public static void ShowWindow()
    {
        AnimationEventHelper window = GetWindow<AnimationEventHelper>("Animation Events");
        window.minSize = new Vector2(450, 600);
    }
    
    private void OnEnable()
    {
        InitializeStyles();
    }
    
    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        
        subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
        subHeaderStyle.fontSize = 11;
    }
    
    private void OnGUI()
    {
        if (headerStyle == null) InitializeStyles();
        
        EditorGUILayout.Space(10);
        
        // Title
        EditorGUILayout.LabelField("Animation Event Helper", headerStyle);
        EditorGUILayout.HelpBox(
            "Comprehensive tool to add animation events to your animations.\n" +
            "Supports footsteps, combat, work tasks, and custom events.",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Animation clip selection
        DrawAnimationClipSection();
        
        if (selectedClip == null)
        {
            EditorGUILayout.HelpBox("Select an animation clip to begin.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.Space(10);
        
        // Category tabs
        DrawCategoryTabs();
        
        EditorGUILayout.Space(10);
        
        // Category-specific content
        switch (selectedCategory)
        {
            case EventCategory.Footsteps:
                DrawFootstepSection();
                break;
            case EventCategory.Combat:
                DrawCombatSection();
                break;
            case EventCategory.Work:
                DrawWorkSection();
                break;
            case EventCategory.Custom:
                DrawCustomSection();
                break;
        }
        
        EditorGUILayout.Space(10);
        
        // Existing events list
        DrawExistingEventsSection();
        
        EditorGUILayout.Space(10);
        
        // Action buttons
        DrawActionButtons();
    }
    
    private void DrawAnimationClipSection()
    {
        EditorGUILayout.LabelField("Animation Setup", subHeaderStyle);
        
        AnimationClip newClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip", 
            selectedClip, 
            typeof(AnimationClip), 
            false
        );
        
        if (newClip != selectedClip)
        {
            selectedClip = newClip;
            AnalyzeExistingEvents();
        }
        
        if (selectedClip != null)
        {
            // Check if animation is editable
            string assetPath = AssetDatabase.GetAssetPath(selectedClip);
            bool isModelAnimation = assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) || 
                                   assetPath.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase) ||
                                   assetPath.EndsWith(".blend", System.StringComparison.OrdinalIgnoreCase) ||
                                   assetPath.EndsWith(".dae", System.StringComparison.OrdinalIgnoreCase);
            
            if (isModelAnimation)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ This animation is inside a model file (FBX, etc.).\n\n" +
                    "Animation events ARE supported in model files, but:\n" +
                    "• Changes may not appear immediately\n" +
                    "• You must add events through the model's Import Settings\n" +
                    "• Click the button below to open the model's import settings",
                    MessageType.Warning
                );
                
                if (GUILayout.Button("Open Model Import Settings", GUILayout.Height(30)))
                {
                    // Get the main asset (the model file itself)
                    Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (mainAsset != null)
                    {
                        Selection.activeObject = mainAsset;
                        EditorGUIUtility.PingObject(mainAsset);
                        
                        // Open import settings
                        AssetDatabase.OpenAsset(mainAsset);
                    }
                }
                
                EditorGUILayout.Space(5);
            }
            
            float clipLength = selectedClip.length;
            float clipFrameRate = selectedClip.frameRate;
            int totalFrames = Mathf.RoundToInt(clipLength * clipFrameRate);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Length: {clipLength:F2}s  |  FPS: {clipFrameRate:F0}  |  Frames: {totalFrames}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Path: {assetPath}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            useNormalizedTime = EditorGUILayout.Toggle("Use Normalized Time (0-1)", useNormalizedTime);
        }
    }
    
    private void DrawCategoryTabs()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = selectedCategory == EventCategory.Footsteps ? Color.cyan : Color.white;
        if (GUILayout.Button("Footsteps", GUILayout.Height(30)))
            selectedCategory = EventCategory.Footsteps;
        
        GUI.backgroundColor = selectedCategory == EventCategory.Combat ? Color.red : Color.white;
        if (GUILayout.Button("Combat", GUILayout.Height(30)))
            selectedCategory = EventCategory.Combat;
        
        GUI.backgroundColor = selectedCategory == EventCategory.Work ? Color.yellow : Color.white;
        if (GUILayout.Button("Work", GUILayout.Height(30)))
            selectedCategory = EventCategory.Work;
        
        GUI.backgroundColor = selectedCategory == EventCategory.Custom ? Color.green : Color.white;
        if (GUILayout.Button("Custom", GUILayout.Height(30)))
            selectedCategory = EventCategory.Custom;
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawFootstepSection()
    {
        EditorGUILayout.LabelField("Footstep Events", subHeaderStyle);
        EditorGUILayout.HelpBox(
            "Add FootstepLeft and FootstepRight events at frames where feet touch ground.\n" +
            "Intensity (0-1) controls particle scale and audio volume (0.5=walking, 1.0=running).",
            MessageType.Info
        );
        
        int totalFrames = GetTotalFrames();
        
        // Left foot
        EditorGUILayout.LabelField("Left Foot", EditorStyles.boldLabel);
        if (useNormalizedTime)
            leftFootFrame = EditorGUILayout.Slider("Time", leftFootFrame, 0f, 1f);
        else
            leftFootFrame = EditorGUILayout.Slider("Frame", leftFootFrame, 0f, totalFrames);
        leftFootIntensity = EditorGUILayout.Slider("Intensity", leftFootIntensity, 0f, 1f);
        
        EditorGUILayout.Space(5);
        
        // Right foot
        EditorGUILayout.LabelField("Right Foot", EditorStyles.boldLabel);
        if (useNormalizedTime)
            rightFootFrame = EditorGUILayout.Slider("Time", rightFootFrame, 0f, 1f);
        else
            rightFootFrame = EditorGUILayout.Slider("Frame", rightFootFrame, 0f, totalFrames);
        rightFootIntensity = EditorGUILayout.Slider("Intensity", rightFootIntensity, 0f, 1f);
        
        // Show both representations
        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        if (useNormalizedTime)
        {
            EditorGUILayout.LabelField($"Left: Frame {Mathf.RoundToInt(leftFootFrame * totalFrames)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Right: Frame {Mathf.RoundToInt(rightFootFrame * totalFrames)}", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"Left: {leftFootFrame / totalFrames:F3} normalized", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Right: {rightFootFrame / totalFrames:F3} normalized", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Quick presets
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Walk\n(0.25/0.75, 0.5)"))
        {
            leftFootFrame = useNormalizedTime ? 0.25f : totalFrames * 0.25f;
            rightFootFrame = useNormalizedTime ? 0.75f : totalFrames * 0.75f;
            leftFootIntensity = 0.5f;
            rightFootIntensity = 0.5f;
        }
        
        if (GUILayout.Button("Run\n(0.20/0.70, 0.75)"))
        {
            leftFootFrame = useNormalizedTime ? 0.20f : totalFrames * 0.20f;
            rightFootFrame = useNormalizedTime ? 0.70f : totalFrames * 0.70f;
            leftFootIntensity = 0.75f;
            rightFootIntensity = 0.75f;
        }
        
        if (GUILayout.Button("Sprint\n(0.15/0.65, 1.0)"))
        {
            leftFootFrame = useNormalizedTime ? 0.15f : totalFrames * 0.15f;
            rightFootFrame = useNormalizedTime ? 0.65f : totalFrames * 0.65f;
            leftFootIntensity = 1.0f;
            rightFootIntensity = 1.0f;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Add button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Footstep Events", GUILayout.Height(35)))
        {
            AddFootstepEvents();
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawCombatSection()
    {
        EditorGUILayout.LabelField("Combat Events", subHeaderStyle);
        EditorGUILayout.HelpBox(
            "Add attack VFX and weapon hitbox events for combat animations.",
            MessageType.Info
        );
        
        int totalFrames = GetTotalFrames();
        
        // Attack VFX
        EditorGUILayout.LabelField("Attack VFX Event", EditorStyles.boldLabel);
        
        if (useNormalizedTime)
            attackVfxFrame = EditorGUILayout.Slider("VFX Time", attackVfxFrame, 0f, 1f);
        else
            attackVfxFrame = EditorGUILayout.Slider("VFX Frame", attackVfxFrame, 0f, totalFrames);
        
        attackDirection = EditorGUILayout.IntPopup("Attack Direction", attackDirection, 
            new[] { "Horizontal Left", "Horizontal Right", "Vertical Down", "Vertical Up" },
            new[] { 0, 1, 2, 3 });
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Attack VFX Event", GUILayout.Height(30)))
        {
            AddAttackVfxEvent();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        
        // Weapon hitbox
        EditorGUILayout.LabelField("Weapon Hitbox Events", EditorStyles.boldLabel);
        
        if (useNormalizedTime)
        {
            weaponStartFrame = EditorGUILayout.Slider("Start Time", weaponStartFrame, 0f, 1f);
            weaponEndFrame = EditorGUILayout.Slider("End Time", weaponEndFrame, 0f, 1f);
        }
        else
        {
            weaponStartFrame = EditorGUILayout.Slider("Start Frame", weaponStartFrame, 0f, totalFrames);
            weaponEndFrame = EditorGUILayout.Slider("End Frame", weaponEndFrame, 0f, totalFrames);
        }
        
        float duration = useNormalizedTime 
            ? (weaponEndFrame - weaponStartFrame) * selectedClip.length
            : (weaponEndFrame - weaponStartFrame) / selectedClip.frameRate;
        EditorGUILayout.LabelField($"Hitbox Duration: {duration:F3} seconds", EditorStyles.miniLabel);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Weapon Hitbox Events (Start + End)", GUILayout.Height(30)))
        {
            AddWeaponHitboxEvents();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        
        // Quick setup
        EditorGUILayout.LabelField("Quick Setup (All Combat Events)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Adds AttackVFX, Attack, and AttackEnd in correct order (standardized for all characters)", MessageType.Info);
        
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Quick Setup: Fast Attack\n(VFX: 0.2, Active: 0.25-0.45)", GUILayout.Height(35)))
        {
            attackVfxFrame = useNormalizedTime ? 0.2f : totalFrames * 0.2f;
            weaponStartFrame = useNormalizedTime ? 0.25f : totalFrames * 0.25f;
            weaponEndFrame = useNormalizedTime ? 0.45f : totalFrames * 0.45f;
            attackDirection = 0;
            AddAttackVfxEvent();
            AddWeaponHitboxEvents();
        }
        
        if (GUILayout.Button("Quick Setup: Heavy Attack\n(VFX: 0.3, Active: 0.4-0.7)", GUILayout.Height(35)))
        {
            attackVfxFrame = useNormalizedTime ? 0.3f : totalFrames * 0.3f;
            weaponStartFrame = useNormalizedTime ? 0.4f : totalFrames * 0.4f;
            weaponEndFrame = useNormalizedTime ? 0.7f : totalFrames * 0.7f;
            attackDirection = 2;
            AddAttackVfxEvent();
            AddWeaponHitboxEvents();
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawWorkSection()
    {
        EditorGUILayout.LabelField("Work Task Events", subHeaderStyle);
        EditorGUILayout.HelpBox(
            "Add work/task effect events (hammering, cooking, etc.).",
            MessageType.Info
        );
        
        int totalFrames = GetTotalFrames();
        
        if (useNormalizedTime)
            workEffectFrame = EditorGUILayout.Slider("Effect Time", workEffectFrame, 0f, 1f);
        else
            workEffectFrame = EditorGUILayout.Slider("Effect Frame", workEffectFrame, 0f, totalFrames);
        
        workTaskEnum = EditorGUILayout.TextField("Task Animation Enum", workTaskEnum);
        EditorGUILayout.HelpBox(
            "Leave empty to use character's current WorkTask, or enter TaskAnimation enum name\n" +
            "(e.g., COOKING_POT_STIR, HAMMER_STANDING, PLANTING_SEEDS)",
            MessageType.Info
        );
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Work Effect Event", GUILayout.Height(30)))
        {
            AddWorkEffectEvent();
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawCustomSection()
    {
        EditorGUILayout.LabelField("Custom Events", subHeaderStyle);
        EditorGUILayout.HelpBox(
            "Add custom animation events with optional parameters.",
            MessageType.Info
        );
        
        int totalFrames = GetTotalFrames();
        
        customEventName = EditorGUILayout.TextField("Function Name", customEventName);
        
        if (useNormalizedTime)
            customEventFrame = EditorGUILayout.Slider("Event Time", customEventFrame, 0f, 1f);
        else
            customEventFrame = EditorGUILayout.Slider("Event Frame", customEventFrame, 0f, totalFrames);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Parameters (Optional)", EditorStyles.boldLabel);
        
        useIntParam = EditorGUILayout.Toggle("Use Int Parameter", useIntParam);
        if (useIntParam)
        {
            customEventIntParam = EditorGUILayout.IntField("Int Value", customEventIntParam);
            useStringParam = false;
            useFloatParam = false;
        }
        
        useFloatParam = EditorGUILayout.Toggle("Use Float Parameter", useFloatParam);
        if (useFloatParam)
        {
            customEventFloatParam = EditorGUILayout.FloatField("Float Value", customEventFloatParam);
            useStringParam = false;
            useIntParam = false;
        }
        
        useStringParam = EditorGUILayout.Toggle("Use String Parameter", useStringParam);
        if (useStringParam)
        {
            customEventStringParam = EditorGUILayout.TextField("String Value", customEventStringParam);
            useIntParam = false;
            useFloatParam = false;
        }
        
        EditorGUILayout.Space(5);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Custom Event", GUILayout.Height(30)))
        {
            AddCustomEvent();
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawExistingEventsSection()
    {
        if (selectedClip == null) return;
        
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(selectedClip);
        
        EditorGUILayout.LabelField($"Existing Events ({events.Length})", subHeaderStyle);
        
        if (events.Length == 0)
        {
            EditorGUILayout.HelpBox("No animation events in this clip.", MessageType.Info);
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(150));
        
        foreach (var evt in events)
        {
            DrawEventEntry(evt);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawEventEntry(AnimationEvent evt)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        // Color code by event type
        Color eventColor = Color.white;
        if (GameConstants.AnimationEvents.IsFootstepEvent(evt.functionName))
            eventColor = Color.cyan;
        else if (GameConstants.AnimationEvents.IsCombatEvent(evt.functionName))
            eventColor = Color.red;
        else if (GameConstants.AnimationEvents.IsWorkEvent(evt.functionName))
            eventColor = Color.yellow;
        
        GUI.color = eventColor;
        
        float normalizedTime = evt.time / selectedClip.length;
        int frameNumber = Mathf.RoundToInt(normalizedTime * selectedClip.length * selectedClip.frameRate);
        
        EditorGUILayout.LabelField(evt.functionName, GUILayout.Width(150));
        EditorGUILayout.LabelField($"T: {evt.time:F3}s", GUILayout.Width(70));
        EditorGUILayout.LabelField($"F: {frameNumber}", GUILayout.Width(50));
        
        // Show parameters if any
        if (evt.intParameter != 0)
            EditorGUILayout.LabelField($"Int: {evt.intParameter}", GUILayout.Width(60));
        if (evt.floatParameter != 0)
            EditorGUILayout.LabelField($"Float: {evt.floatParameter:F2}", GUILayout.Width(70));
        if (!string.IsNullOrEmpty(evt.stringParameter))
            EditorGUILayout.LabelField($"Str: {evt.stringParameter}", GUILayout.Width(100));
        
        GUI.color = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawActionButtons()
    {
        EditorGUILayout.LabelField("Manage Events", subHeaderStyle);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Remove Footstep Events"))
        {
            RemoveEventsByNames(GameConstants.AnimationEvents.GetFootstepEventNames());
        }
        
        if (GUILayout.Button("Remove Combat Events"))
        {
            RemoveEventsByNames(GameConstants.AnimationEvents.GetCombatEventNames());
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Remove ALL Events"))
        {
            if (EditorUtility.DisplayDialog("Confirm Remove All", 
                "Remove ALL animation events? This cannot be undone!", 
                "Remove All", "Cancel"))
            {
                RemoveAllEvents();
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
    // ============================================================================
    // EVENT ADDING METHODS
    // ============================================================================
    
    private void AddFootstepEvents()
    {
        if (selectedClip == null) return;
        
        float leftTime = CalculateEventTime(leftFootFrame);
        float rightTime = CalculateEventTime(rightFootFrame);
        
        AddEvent(GameConstants.AnimationEvents.FootstepLeft, leftTime, leftFootIntensity);
        AddEvent(GameConstants.AnimationEvents.FootstepRight, rightTime, rightFootIntensity);
        
        SaveAndLog($"Added footstep events:\n  Left: {leftTime:F3}s (intensity: {leftFootIntensity:F2})\n  Right: {rightTime:F3}s (intensity: {rightFootIntensity:F2})");
    }
    
    private void AddAttackVfxEvent()
    {
        if (selectedClip == null) return;
        
        float time = CalculateEventTime(attackVfxFrame);
        AddEvent(GameConstants.AnimationEvents.AttackVFX, time, attackDirection);
        
        SaveAndLog($"Added AttackVFX event at {time:F3}s with direction {attackDirection}");
    }
    
    private void AddWeaponHitboxEvents()
    {
        if (selectedClip == null) return;
        
        float startTime = CalculateEventTime(weaponStartFrame);
        float endTime = CalculateEventTime(weaponEndFrame);
        
        AddEvent(GameConstants.AnimationEvents.Attack, startTime);
        AddEvent(GameConstants.AnimationEvents.AttackEnd, endTime);
        
        SaveAndLog($"Added weapon hitbox events:\n  Start: {startTime:F3}s\n  End: {endTime:F3}s");
    }
    
    private void AddWorkEffectEvent()
    {
        if (selectedClip == null) return;
        
        float time = CalculateEventTime(workEffectFrame);
        AddEvent(GameConstants.AnimationEvents.PlayTaskAnimationEffect, time, workTaskEnum);
        
        SaveAndLog($"Added work effect event at {time:F3}s with task: {(string.IsNullOrEmpty(workTaskEnum) ? "(auto)" : workTaskEnum)}");
    }
    
    private void AddCustomEvent()
    {
        if (selectedClip == null || string.IsNullOrEmpty(customEventName)) return;
        
        float time = CalculateEventTime(customEventFrame);
        
        if (useIntParam)
            AddEvent(customEventName, time, customEventIntParam);
        else if (useFloatParam)
            AddEvent(customEventName, time, customEventFloatParam);
        else if (useStringParam)
            AddEvent(customEventName, time, customEventStringParam);
        else
            AddEvent(customEventName, time);
        
        SaveAndLog($"Added custom event '{customEventName}' at {time:F3}s");
    }
    
    // ============================================================================
    // UTILITY METHODS
    // ============================================================================
    
    private void AddEvent(string functionName, float time, int intParam = 0)
    {
        AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(selectedClip);
        List<AnimationEvent> newEvents = new List<AnimationEvent>(existingEvents);
        
        AnimationEvent newEvent = new AnimationEvent();
        newEvent.time = time;
        newEvent.functionName = functionName;
        newEvent.intParameter = intParam;
        
        newEvents.Add(newEvent);
        
        // Check if this is a model animation and use ModelImporter if needed
        string assetPath = AssetDatabase.GetAssetPath(selectedClip);
        if (IsModelAnimation(assetPath))
        {
            SetModelAnimationEvents(assetPath, selectedClip.name, newEvents.ToArray());
        }
        else
        {
            AnimationUtility.SetAnimationEvents(selectedClip, newEvents.ToArray());
        }
    }
    
    private void AddEvent(string functionName, float time, float floatParam)
    {
        AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(selectedClip);
        List<AnimationEvent> newEvents = new List<AnimationEvent>(existingEvents);
        
        AnimationEvent newEvent = new AnimationEvent();
        newEvent.time = time;
        newEvent.functionName = functionName;
        newEvent.floatParameter = floatParam;
        
        newEvents.Add(newEvent);
        
        // Check if this is a model animation and use ModelImporter if needed
        string assetPath = AssetDatabase.GetAssetPath(selectedClip);
        if (IsModelAnimation(assetPath))
        {
            SetModelAnimationEvents(assetPath, selectedClip.name, newEvents.ToArray());
        }
        else
        {
            AnimationUtility.SetAnimationEvents(selectedClip, newEvents.ToArray());
        }
    }
    
    private void AddEvent(string functionName, float time, string stringParam)
    {
        AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(selectedClip);
        List<AnimationEvent> newEvents = new List<AnimationEvent>(existingEvents);
        
        AnimationEvent newEvent = new AnimationEvent();
        newEvent.time = time;
        newEvent.functionName = functionName;
        newEvent.stringParameter = stringParam;
        
        newEvents.Add(newEvent);
        
        // Check if this is a model animation and use ModelImporter if needed
        string assetPath = AssetDatabase.GetAssetPath(selectedClip);
        if (IsModelAnimation(assetPath))
        {
            SetModelAnimationEvents(assetPath, selectedClip.name, newEvents.ToArray());
        }
        else
        {
            AnimationUtility.SetAnimationEvents(selectedClip, newEvents.ToArray());
        }
    }
    
    private void RemoveEventsByNames(string[] eventNames)
    {
        if (selectedClip == null) return;
        
        AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(selectedClip);
        List<AnimationEvent> filteredEvents = existingEvents
            .Where(evt => !eventNames.Contains(evt.functionName))
            .ToList();
        
        // Check if this is a model animation and use ModelImporter if needed
        string assetPath = AssetDatabase.GetAssetPath(selectedClip);
        if (IsModelAnimation(assetPath))
        {
            SetModelAnimationEvents(assetPath, selectedClip.name, filteredEvents.ToArray());
        }
        else
        {
            AnimationUtility.SetAnimationEvents(selectedClip, filteredEvents.ToArray());
        }
        
        SaveAndLog($"Removed {existingEvents.Length - filteredEvents.Count} events");
    }
    
    private void RemoveAllEvents()
    {
        if (selectedClip == null) return;
        
        // Check if this is a model animation and use ModelImporter if needed
        string assetPath = AssetDatabase.GetAssetPath(selectedClip);
        if (IsModelAnimation(assetPath))
        {
            SetModelAnimationEvents(assetPath, selectedClip.name, new AnimationEvent[0]);
        }
        else
        {
            AnimationUtility.SetAnimationEvents(selectedClip, new AnimationEvent[0]);
        }
        
        SaveAndLog("Removed all events");
    }
    
    private bool IsModelAnimation(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return false;
        
        string extension = Path.GetExtension(assetPath).ToLower();
        return extension == ".fbx" || extension == ".obj" || 
               extension == ".blend" || extension == ".dae" ||
               extension == ".3ds" || extension == ".max";
    }
    
    private void SetModelAnimationEvents(string modelPath, string clipName, AnimationEvent[] events)
    {
        ModelImporter modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (modelImporter == null)
        {
            Debug.LogError($"[AnimationEventHelper] Could not get ModelImporter for {modelPath}");
            return;
        }
        
        // Get existing clip animations
        ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;
        if (clipAnimations == null || clipAnimations.Length == 0)
        {
            // Use default clips
            clipAnimations = modelImporter.defaultClipAnimations;
        }
        
        // Find the clip we're editing
        for (int i = 0; i < clipAnimations.Length; i++)
        {
            if (clipAnimations[i].name == clipName || clipAnimations[i].takeName == clipName)
            {
                clipAnimations[i].events = events;
                break;
            }
        }
        
        // Apply changes
        modelImporter.clipAnimations = clipAnimations;
        
        // Save and reimport
        EditorUtility.SetDirty(modelImporter);
        modelImporter.SaveAndReimport();
        
        Debug.Log($"[AnimationEventHelper] Updated events for model animation: {clipName} in {modelPath}");
    }
    
    private float CalculateEventTime(float frameOrNormalized)
    {
        if (useNormalizedTime)
        {
            return frameOrNormalized * selectedClip.length;
        }
        else
        {
            float normalized = frameOrNormalized / GetTotalFrames();
            return normalized * selectedClip.length;
        }
    }
    
    private int GetTotalFrames()
    {
        if (selectedClip == null) return 1;
        return Mathf.RoundToInt(selectedClip.length * selectedClip.frameRate);
    }
    
    private void SaveAndLog(string message)
    {
        if (selectedClip == null) return;
        
        // Mark the clip as dirty
        EditorUtility.SetDirty(selectedClip);
        
        // Get the asset path and reimport to ensure changes are written
        string assetPath = AssetDatabase.GetAssetPath(selectedClip);
        if (!string.IsNullOrEmpty(assetPath))
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
        
        // Save all assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[AnimationEventHelper] {selectedClip.name}: {message}");
        AnalyzeExistingEvents();
        
        // Force repaint to show updated events
        Repaint();
    }
    
    private void AnalyzeExistingEvents()
    {
        if (selectedClip == null) return;
        
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(selectedClip);
        
        foreach (var evt in events)
        {
            float normalizedTime = evt.time / selectedClip.length;
            int frameNumber = Mathf.RoundToInt(normalizedTime * selectedClip.length * selectedClip.frameRate);
            
            switch (evt.functionName)
            {
                case "FootstepLeft":
                    leftFootFrame = useNormalizedTime ? normalizedTime : frameNumber;
                    leftFootIntensity = evt.floatParameter > 0 ? evt.floatParameter : 1.0f;
                    break;
                case "FootstepRight":
                    rightFootFrame = useNormalizedTime ? normalizedTime : frameNumber;
                    rightFootIntensity = evt.floatParameter > 0 ? evt.floatParameter : 1.0f;
                    break;
                case "AttackVFX":
                    attackVfxFrame = useNormalizedTime ? normalizedTime : frameNumber;
                    attackDirection = evt.intParameter;
                    break;
                case "Attack":  // Standardized name
                    weaponStartFrame = useNormalizedTime ? normalizedTime : frameNumber;
                    break;
                case "AttackEnd":  // Standardized name
                    weaponEndFrame = useNormalizedTime ? normalizedTime : frameNumber;
                    break;
                case "PlayTaskAnimationEffect":
                    workEffectFrame = useNormalizedTime ? normalizedTime : frameNumber;
                    workTaskEnum = evt.stringParameter;
                    break;
            }
        }
    }
}

/// <summary>
/// Context menu for quick access to animation event helper
/// </summary>
public class AnimationEventContextMenu
{
    [MenuItem("Assets/Animation Events/Open Event Helper", false, 2000)]
    private static void OpenEventHelper()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        AnimationEventHelper window = EditorWindow.GetWindow<AnimationEventHelper>("Animation Events");
        
        if (clip != null)
        {
            // Use reflection to set the selected clip
            var field = typeof(AnimationEventHelper).GetField("selectedClip", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(window, clip);
        }
    }
    
    [MenuItem("Assets/Animation Events/Open Event Helper", true)]
    private static bool ValidateOpenEventHelper()
    {
        return Selection.activeObject is AnimationClip;
    }
    
    [MenuItem("Assets/Animation Events/Quick Add Footsteps (Walk)", false, 2001)]
    private static void QuickAddFootstepsWalk()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        if (clip == null) return;
        
        AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(clip);
        List<AnimationEvent> newEvents = new List<AnimationEvent>(existingEvents);
        
        AnimationEvent leftEvent = new AnimationEvent();
        leftEvent.time = 0.25f * clip.length;
        leftEvent.functionName = GameConstants.AnimationEvents.FootstepLeft;
        newEvents.Add(leftEvent);
        
        AnimationEvent rightEvent = new AnimationEvent();
        rightEvent.time = 0.75f * clip.length;
        rightEvent.functionName = GameConstants.AnimationEvents.FootstepRight;
        newEvents.Add(rightEvent);
        
        AnimationUtility.SetAnimationEvents(clip, newEvents.ToArray());
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[AnimationEventHelper] Added walk footsteps to {clip.name}");
    }
    
    [MenuItem("Assets/Animation Events/Quick Add Footsteps (Run)", false, 2002)]
    private static void QuickAddFootstepsRun()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        if (clip == null) return;
        
        AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(clip);
        List<AnimationEvent> newEvents = new List<AnimationEvent>(existingEvents);
        
        AnimationEvent leftEvent = new AnimationEvent();
        leftEvent.time = 0.20f * clip.length;
        leftEvent.functionName = GameConstants.AnimationEvents.FootstepLeft;
        newEvents.Add(leftEvent);
        
        AnimationEvent rightEvent = new AnimationEvent();
        rightEvent.time = 0.70f * clip.length;
        rightEvent.functionName = GameConstants.AnimationEvents.FootstepRight;
        newEvents.Add(rightEvent);
        
        AnimationUtility.SetAnimationEvents(clip, newEvents.ToArray());
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[AnimationEventHelper] Added run footsteps to {clip.name}");
    }
    
    [MenuItem("Assets/Animation Events/Quick Add Footsteps (Walk)", true)]
    [MenuItem("Assets/Animation Events/Quick Add Footsteps (Run)", true)]
    private static bool ValidateQuickAdd()
    {
        return Selection.activeObject is AnimationClip;
    }
}

