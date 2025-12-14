using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for SettlerNPC to display NPC-specific information
/// Inherits from HumanCharacterControllerEditor to get base stats display
/// </summary>
[CustomEditor(typeof(SettlerNPC))]
public class SettlerNPCEditor : HumanCharacterControllerEditor
{
    public override void OnInspectorGUI()
    {
        SettlerNPC settler = (SettlerNPC)target;
        
        // Draw custom sections first
        DrawColoredSection("🎯 Custom Inspector Active", () => {
            EditorGUILayout.HelpBox("This is the custom SettlerNPC inspector!", MessageType.Info);
        }, new Color(0.2f, 0.6f, 1f, 0.3f)); // Blue tint
        
        DrawSettlerInfoSection(settler);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            DrawNPCSystemsSection(settler);
            
            EditorGUILayout.Space(5);
            DrawNPCStateSection(settler);
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Additional NPC information will be displayed here during Play Mode", MessageType.Info);
        }
        
        // Draw modifiable stats section
        EditorGUILayout.Space(10);
        DrawColoredSection("📊 Modifiable Stats", () => {
            if (Application.isPlaying)
            {
                var baseStats = settler.GetBaseStats();
                var currentStats = settler.GetCurrentStats();
                
                // Draw a box around the stats
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Health
                DrawStatComparison("Max Health", baseStats.maxHealth, currentStats.maxHealth);
                
                // Poise
                DrawStatComparison("Max Poise", baseStats.maxPoise, currentStats.maxPoise);
                
                // Movement Speed
                DrawStatComparison("Move Max Speed", baseStats.moveMaxSpeed, currentStats.moveMaxSpeed);
                
                // Rotation Speed
                DrawStatComparison("Rotation Speed", baseStats.rotationSpeed, currentStats.rotationSpeed);
                
                // Dash Speed
                DrawStatComparison("Dash Speed", baseStats.dashSpeed, currentStats.dashSpeed);
                
                // Dash Cooldown
                DrawStatComparison("Dash Cooldown", baseStats.dashCooldown, currentStats.dashCooldown);
                
                // If this is an NPC, show NPC-specific stats
                if (currentStats is NPCModifiableStats npcCurrent && baseStats is NPCModifiableStats npcBase)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("NPC-Specific Stats", EditorStyles.miniBoldLabel);
                    
                    DrawStatComparison("Max Stamina", npcBase.maxStamina, npcCurrent.maxStamina);
                    DrawStatComparison("Additional Mutation Slots", npcBase.additionalMutationSlots, npcCurrent.additionalMutationSlots);
                }
                
                EditorGUILayout.EndVertical();
                
                // Show total modifiers summary
                bool hasModifiers = HasAnyModifiers(baseStats, currentStats);
                if (hasModifiers)
                {
                    EditorGUILayout.HelpBox("Stats are currently modified by characteristics/mutations", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Modifiable Stats will be displayed here during Play Mode", MessageType.Info);
            }
        }, new Color(0.9f, 0.9f, 1f, 0.3f)); // Light purple tint
        
        // Draw separator
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        // Draw only the fields we want to show (excluding the ones we're replacing)
        DrawCustomInspectorFields(settler);
    }
    
    /// <summary>
    /// Draw only the inspector fields we want to show, excluding the ones we're replacing with custom UI
    /// </summary>
    private void DrawCustomInspectorFields(SettlerNPC settler)
    {
        SerializedObject serializedObject = new SerializedObject(settler);
        
        // Movement & Navigation
        DrawColoredSection("🏃 Movement & Navigation", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dashDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vaultDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vaultCooldown"));
        }, new Color(0.9f, 0.8f, 1f, 0.3f)); // Light purple tint
        
        // Vault System
        DrawColoredSection("🦘 Vault System", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleLayers"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("capsuleCastRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vaultHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vaultOffset"));
        }, new Color(1f, 0.8f, 0.9f, 0.3f)); // Light pink tint
        
        // Character Controller
        DrawColoredSection("🎮 Character Controller", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stepOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeLimit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skinWidth"));
        }, new Color(0.8f, 1f, 0.9f, 0.3f)); // Light mint tint
        
        // Enhanced Navigation
        DrawColoredSection("🧭 Enhanced Navigation", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxVaultHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minVaultHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleAnalysisRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("heightCheckRayCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoNavigateObstacles"));
        }, new Color(0.9f, 1f, 0.8f, 0.3f)); // Light lime tint
        
        // Climbing System
        DrawColoredSection("🧗 Climbing System", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxClimbHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("climbDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("climbCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("climbCheckDistance"));
        }, new Color(1f, 0.9f, 0.8f, 0.3f)); // Light peach tint
        
        // Gravity System
        DrawColoredSection("🌍 Gravity System", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableGravity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terminalVelocity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxFallDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fallingMovementMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groundedBufferTime"));
        }, new Color(0.8f, 0.8f, 1f, 0.3f)); // Light blue tint
        
        // Health & Combat
        DrawColoredSection("❤️ Health & Combat", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("health"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damageCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("poise"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("poiseRecoveryRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("poiseRecoveryDelay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resistances"));
        }, new Color(1f, 0.8f, 0.8f, 0.3f)); // Light red tint
        
        // NPC Data
        DrawColoredSection("👤 NPC Data", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("settlerName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("settlerAge"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("settlerDescription"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initializationContext"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasBeenInitialized"));
        }, new Color(0.9f, 0.9f, 0.8f, 0.3f)); // Light yellow tint
        
        // Appearance System
        DrawColoredSection("🎨 Appearance System", () => {
            // Preview size slider
            AppearanceOptionDrawer.DrawPreviewSizeSlider();
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("appearanceSystem"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("characteristicSystem"));
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                
                // Randomize Appearance button
                Color originalBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("🎲 Randomize Appearance", GUILayout.Height(25)))
                {
                    var appearanceSystem = settler.GetAppearanceSystem();
                    if (appearanceSystem != null)
                    {
                        appearanceSystem.ClearCurrentAppearance();
                        appearanceSystem.RandomizeAppearance();
                        Debug.Log($"[SettlerNPCEditor] Randomized appearance for {settler.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SettlerNPCEditor] Cannot randomize appearance - appearanceSystem is null on {settler.name}");
                    }
                }
                GUI.backgroundColor = originalBg;
                
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Appearance and characteristics are managed at runtime", MessageType.Info);
            }
        }, new Color(0.8f, 0.9f, 1f, 0.3f)); // Light blue tint
        
        // Stamina System
        DrawColoredSection("⚡ Stamina System", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentStamina"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseStaminaDrainInGameHours"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseStaminaRegenInGameHours"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaDrainRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaRegenRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseFatigueRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fatigueRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sleepStaminaRegenMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nightFatigueMultiplier"));
        }, new Color(0.8f, 1f, 1f, 0.3f)); // Light cyan tint
        
        // Apply any changes
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// Draw a section with a colored background
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
    /// Draw settler identification information
    /// </summary>
    private void DrawSettlerInfoSection(SettlerNPC settler)
    {
        DrawColoredSection("👤 Settler Information", () => {
            EditorGUILayout.LabelField("Name", settler.GetSettlerName());
            EditorGUILayout.LabelField("Age", settler.GetSettlerAge().ToString());
            EditorGUILayout.LabelField("Description", settler.GetSettlerDescription(), EditorStyles.wordWrappedLabel);
        }, new Color(0.8f, 0.9f, 1f, 0.3f)); // Light blue tint
    }
    
    /// <summary>
    /// Draw NPC systems status (characteristics, health, stamina, hunger)
    /// </summary>
    private void DrawNPCSystemsSection(SettlerNPC settler)
    {
        DrawColoredSection("⚙️ NPC Systems Status", () => {
            // Health status
            float healthPercent = (settler.Health / settler.MaxHealth) * 100f;
            Color healthColor = healthPercent > 50f ? Color.green : (healthPercent > 25f ? Color.yellow : Color.red);
            GUI.color = healthColor;
            EditorGUILayout.LabelField("Health", $"{settler.Health:F1} / {settler.MaxHealth:F1} ({healthPercent:F0}%)");
            GUI.color = Color.white;
            
            // Stamina status
            float staminaPercent = settler.GetStaminaPercentage();
            Color staminaColor = staminaPercent > 50f ? Color.green : (staminaPercent > 25f ? Color.yellow : Color.red);
            GUI.color = staminaColor;
            EditorGUILayout.LabelField("Stamina", $"{settler.currentStamina:F1} / {settler.maxStamina:F1} ({staminaPercent:F0}%)");
            GUI.color = Color.white;
            
            // Hunger status
            float hungerPercent = settler.GetHungerPercentage() * 100f;
            Color hungerColor = hungerPercent > 50f ? Color.green : (hungerPercent > 25f ? Color.yellow : Color.red);
            GUI.color = hungerColor;
            EditorGUILayout.LabelField("Hunger", $"{hungerPercent:F0}%");
            GUI.color = Color.white;
            
            // Health status enum (legacy)
            EditorGUILayout.LabelField("Health Status", settler.GetHealthStatus().ToString());
            
            // Active status effects (from EffectManager)
            if (Managers.EffectManager.Instance != null)
            {
                var activeEffects = settler.GetActiveStatusEffects();
                if (activeEffects != null && activeEffects.Count > 0)
                {
                    // Use new slot-based organization system
                    var organized = StatusEffectUtils.OrganizeForDisplay(activeEffects);
                    
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField($"Active Status Effects ({activeEffects.Count} total, organized by slot):", EditorStyles.miniBoldLabel);
                    
                    // PRIMARY CONDITIONS (Combat, Elementals) - Most urgent
                    if (organized.PrimaryConditions.Count > 0)
                    {
                        EditorGUILayout.Space(2);
                        GUI.color = new Color(1f, 0.9f, 0.9f);
                        EditorGUILayout.LabelField("⚠ Critical:", EditorStyles.miniLabel);
                        GUI.color = Color.white;
                        
                        foreach (var effectType in organized.PrimaryConditions)
                        {
                            Color effectColor = StatusEffectUtils.GetEffectColor(effectType);
                            GUI.color = effectColor;
                            string description = StatusEffectUtils.GetEffectDescription(effectType);
                            EditorGUILayout.LabelField($"  • {description}", EditorStyles.miniBoldLabel);
                            GUI.color = Color.white;
                        }
                    }
                    
                    // HEALTH STATUS (Hunger, Fatigue, Sickness)
                    if (organized.HealthStatus.Count > 0)
                    {
                        EditorGUILayout.Space(2);
                        GUI.color = new Color(1f, 1f, 0.9f);
                        EditorGUILayout.LabelField("❤ Health:", EditorStyles.miniLabel);
                        GUI.color = Color.white;
                        
                        foreach (var effectType in organized.HealthStatus)
                        {
                            Color effectColor = StatusEffectUtils.GetEffectColor(effectType);
                            GUI.color = effectColor;
                            string description = StatusEffectUtils.GetEffectDescription(effectType);
                            EditorGUILayout.LabelField($"  • {description}", EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                    }
                    
                    // ACTIVITY STATUS (Working, Sleeping, etc.)
                    if (organized.ActivityStatus.Count > 0)
                    {
                        EditorGUILayout.Space(2);
                        GUI.color = new Color(0.9f, 0.9f, 1f);
                        EditorGUILayout.LabelField("⚙ Activity:", EditorStyles.miniLabel);
                        GUI.color = Color.white;
                        
                        foreach (var effectType in organized.ActivityStatus)
                        {
                            Color effectColor = StatusEffectUtils.GetEffectColor(effectType);
                            GUI.color = effectColor;
                            string description = StatusEffectUtils.GetEffectDescription(effectType);
                            EditorGUILayout.LabelField($"  • {description}", EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                    }
                    
                    // SPECIAL STATUS (Medical, Healthy)
                    if (organized.SpecialStatus.Count > 0)
                    {
                        EditorGUILayout.Space(2);
                        GUI.color = new Color(0.9f, 1f, 0.9f);
                        EditorGUILayout.LabelField("✓ Special:", EditorStyles.miniLabel);
                        GUI.color = Color.white;
                        
                        foreach (var effectType in organized.SpecialStatus)
                        {
                            Color effectColor = StatusEffectUtils.GetEffectColor(effectType);
                            GUI.color = effectColor;
                            string description = StatusEffectUtils.GetEffectDescription(effectType);
                            EditorGUILayout.LabelField($"  • {description}", EditorStyles.miniLabel);
                            GUI.color = Color.white;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.Space(3);
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("Active Status Effects: None (Healthy)", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            
            // Characteristics
            if (settler.CharacteristicSystem?.EquippedCharacteristics != null && settler.CharacteristicSystem.EquippedCharacteristics.Count > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"Characteristics ({settler.CharacteristicSystem.EquippedCharacteristics.Count}):", EditorStyles.miniBoldLabel);
                foreach (var characteristic in settler.CharacteristicSystem.EquippedCharacteristics)
                {
                    EditorGUILayout.LabelField($"  • {characteristic.name}", EditorStyles.miniLabel);
                }
            }
        }, new Color(0.8f, 1f, 0.8f, 0.3f)); // Light green tint
    }
    
    /// <summary>
    /// Draw current task state and work assignment
    /// </summary>
    private void DrawNPCStateSection(SettlerNPC settler)
    {
        DrawColoredSection("🎯 Current State", () => {
            // Current task
            EditorGUILayout.LabelField("Task", settler.GetCurrentTaskType().ToString());
            
            // Work assignment
            if (settler.HasAssignedWork())
            {
                GUI.color = Color.cyan;
                EditorGUILayout.LabelField("Work", settler.GetAssignedWork()?.name ?? "Unknown");
                GUI.color = Color.white;
                
                if (settler.IsOnBreak)
                {
                    EditorGUILayout.LabelField("Status", "On Break");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Work", "None");
            }
            
            // Bed assignment
            bool hasBed = settler.HasAssignedBed();
            GUI.color = hasBed ? Color.green : Color.yellow;
            EditorGUILayout.LabelField("Bed", hasBed ? "Assigned" : "None");
            GUI.color = Color.white;
        }, new Color(1f, 0.9f, 0.8f, 0.3f)); // Light orange tint
    }
    
    /// <summary>
    /// Draw a single stat comparison (base vs current)
    /// </summary>
    private void DrawStatComparison(string label, float baseValue, float currentValue)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField(label, GUILayout.Width(150));
        
        // Base value
        EditorGUILayout.LabelField($"Base: {baseValue:F1}", GUILayout.Width(80));
        
        // Current value with color coding
        bool isModified = Mathf.Abs(currentValue - baseValue) > 0.001f;
        if (isModified)
        {
            GUI.color = currentValue > baseValue ? Color.green : Color.red;
            float difference = currentValue - baseValue;
            string sign = difference > 0 ? "+" : "";
            EditorGUILayout.LabelField($"Current: {currentValue:F1} ({sign}{difference:F1})", GUILayout.Width(150));
            GUI.color = Color.white;
        }
        else
        {
            EditorGUILayout.LabelField($"Current: {currentValue:F1}", GUILayout.Width(150));
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// Draw a single stat comparison for integers
    /// </summary>
    private void DrawStatComparison(string label, int baseValue, int currentValue)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField(label, GUILayout.Width(150));
        
        // Base value
        EditorGUILayout.LabelField($"Base: {baseValue}", GUILayout.Width(80));
        
        // Current value with color coding
        bool isModified = currentValue != baseValue;
        if (isModified)
        {
            GUI.color = currentValue > baseValue ? Color.green : Color.red;
            int difference = currentValue - baseValue;
            string sign = difference > 0 ? "+" : "";
            EditorGUILayout.LabelField($"Current: {currentValue} ({sign}{difference})", GUILayout.Width(150));
            GUI.color = Color.white;
        }
        else
        {
            EditorGUILayout.LabelField($"Current: {currentValue}", GUILayout.Width(150));
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// Check if any stats have been modified
    /// </summary>
    private bool HasAnyModifiers(CharacterModifiableStats baseStats, CharacterModifiableStats currentStats)
    {
        if (Mathf.Abs(currentStats.maxHealth - baseStats.maxHealth) > 0.001f) return true;
        if (Mathf.Abs(currentStats.maxPoise - baseStats.maxPoise) > 0.001f) return true;
        if (Mathf.Abs(currentStats.moveMaxSpeed - baseStats.moveMaxSpeed) > 0.001f) return true;
        if (Mathf.Abs(currentStats.rotationSpeed - baseStats.rotationSpeed) > 0.001f) return true;
        if (Mathf.Abs(currentStats.dashSpeed - baseStats.dashSpeed) > 0.001f) return true;
        if (Mathf.Abs(currentStats.dashCooldown - baseStats.dashCooldown) > 0.001f) return true;
        
        if (currentStats is NPCModifiableStats npcCurrent && baseStats is NPCModifiableStats npcBase)
        {
            if (Mathf.Abs(npcCurrent.maxStamina - npcBase.maxStamina) > 0.001f) return true;
            if (npcCurrent.additionalMutationSlots != npcBase.additionalMutationSlots) return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get color based on status effect type for visual coding
    /// </summary>
    // Removed GetStatusEffectColor() - now using centralized StatusEffectUtils.GetEffectColor()
}

