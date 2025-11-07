/*
 * ============================================================================
 * FOOTSTEP SYSTEM SETUP GUIDE
 * ============================================================================
 * 
 * This system provides surface-aware footstep VFX and sounds for humanoid characters
 * (NPCs and Zombies). It uses Unity's Humanoid IK system for accurate foot placement
 * and automatically detects ground surface types.
 * 
 * ============================================================================
 * OVERVIEW
 * ============================================================================
 * 
 * Components:
 * - SurfaceType.cs: Defines surface types (grass, stone, wood, etc.) and detection
 * - CharacterEffects.cs: Stores footstep effects per character type and surface
 * - EffectManager.cs: Plays effects based on character type and surface
 * - CharacterAnimationEvents.cs: Animation event handlers called from animations
 * 
 * ============================================================================
 * STEP 1: SETUP ANIMATION EVENTS
 * ============================================================================
 * 
 * For each walk/run animation in your blend tree:
 * 
 * 1. Select the animation clip in the Project window
 * 2. Open the Animation window (Window > Animation > Animation)
 * 3. Find the frames where each foot touches the ground
 * 4. Add Animation Events at those frames:
 * 
 *    LEFT FOOT:
 *    - Click the keyframe timeline at the contact frame
 *    - Click "Add Animation Event" button
 *    - Set Function: "FootstepLeft"
 *    
 *    RIGHT FOOT:
 *    - Click the keyframe timeline at the contact frame
 *    - Click "Add Animation Event" button
 *    - Set Function: "FootstepRight"
 * 
 * TIPS FOR FINDING FOOT CONTACT FRAMES:
 * - Play animation slowly in preview
 * - Look for the frame where foot is completely on ground
 * - For walk: Usually around 25-30% and 75-80% of cycle
 * - For run: Usually around 20-25% and 70-75% of cycle
 * - Each foot should trigger once per animation cycle
 * 
 * ALTERNATIVE: Generic Footstep Event
 * - If you don't need precise foot placement, use "Footstep()" method
 * - This plays effect at character's root position
 * 
 * ============================================================================
 * STEP 2: CONFIGURE CHARACTER EFFECTS
 * ============================================================================
 * 
 * For each character type, create/configure CharacterEffects asset:
 * 
 * 1. In Project window: Right-click > Create > Scriptable Objects > Effects > Character Effects
 * 2. Name it (e.g., "Effects_Zombie_Melee" or "Effects_Human_Male")
 * 3. Set the Character Type field to match the character enum
 * 4. Configure Surface Footstep Effects:
 * 
 *    Size: Add entries for each surface type you want
 *    
 *    For each entry:
 *    - Surface Type: Choose surface (GRASS, STONE, WOOD, etc.)
 *    - Footstep Effects: Add one or more EffectDefinition assets
 *      (System will randomly pick one each footstep)
 * 
 * EXAMPLE SETUP:
 * 
 *    Surface Footstep Effects (Size: 3)
 *    
 *    Element 0:
 *      Surface Type: DEFAULT
 *      Footstep Effects (Size: 2)
 *        - Footstep_Generic_01
 *        - Footstep_Generic_02
 *    
 *    Element 1:
 *      Surface Type: GRASS
 *      Footstep Effects (Size: 3)
 *        - Footstep_Grass_01
 *        - Footstep_Grass_02
 *        - Footstep_Grass_03
 *    
 *    Element 2:
 *      Surface Type: STONE
 *      Footstep Effects (Size: 2)
 *        - Footstep_Stone_01
 *        - Footstep_Stone_02
 * 
 * NOTE: DEFAULT surface is used as fallback when specific surface not found
 * 
 * ============================================================================
 * STEP 3: CREATE EFFECT DEFINITIONS
 * ============================================================================
 * 
 * Create EffectDefinition assets for footsteps:
 * 
 * 1. Right-click > Create > Scriptable Objects > Effects > Effect Definition
 * 2. Name it descriptively (e.g., "Footstep_Grass_Zombie")
 * 3. Configure:
 * 
 *    Prefabs: Add particle system prefabs (dust, debris, etc.)
 *    - Can add multiple for variation
 *    - Play Mode: Random (picks one) or All (plays all)
 *    
 *    Sounds: Add footstep audio clips
 *    - Can add multiple for variation
 *    - Min/Max Pitch: 0.9-1.1 (for variation)
 *    - Volume: 0.5-1.0 (footsteps should be subtle)
 *    - Spatial Blend: 1.0 (full 3D positioning)
 *    
 *    Duration: Usually 0 (auto-detect from particles)
 *    - For footsteps, 0.5-1.0 seconds is typical
 * 
 * RECOMMENDED VFX:
 * - Small dust puffs for dirt/grass
 * - Small debris particles for stone/concrete
 * - Wood chips for wood surfaces
 * - Water splashes for water
 * - Different scales for different character sizes
 * 
 * ============================================================================
 * STEP 4: ASSIGN TO EFFECT MANAGER
 * ============================================================================
 * 
 * 1. Find EffectManager in your scene (usually on GameManager or similar)
 * 2. In Inspector, expand "Character Effects"
 * 3. Add your CharacterEffects assets to the array
 * 4. Make sure each CharacterType has one entry
 * 
 * The system will automatically:
 * - Initialize effect pools on game start
 * - Match character types to their effects
 * - Detect surfaces and play appropriate effects
 * 
 * ============================================================================
 * STEP 5: CONFIGURE SURFACE DETECTION (OPTIONAL)
 * ============================================================================
 * 
 * For better surface detection, you can:
 * 
 * OPTION A: Use SurfaceIdentifier Component
 * - Add SurfaceIdentifier to ground objects
 * - Set Surface Type field
 * - Most explicit and reliable method
 * 
 * OPTION B: Use Physics Materials
 * - Name physics materials with keywords: "grass", "stone", "wood", etc.
 * - Assign to ground colliders
 * - System automatically detects from material name
 * 
 * OPTION C: Use GameObject Names
 * - Name ground objects with keywords: "grass", "stone", "floor", etc.
 * - Fallback method when other options not available
 * 
 * DETECTION PRIORITY:
 * 1. SurfaceIdentifier component (highest priority)
 * 2. Physics material name
 * 3. GameObject name
 * 4. DEFAULT surface (fallback)
 * 
 * ============================================================================
 * STEP 6: DEBUGGING AND TESTING
 * ============================================================================
 * 
 * Enable Debug Mode:
 * 1. Select character in scene
 * 2. Find CharacterAnimationEvents component
 * 3. Check "Debug Footsteps"
 * 4. Adjust "Surface Detection Distance" if needed (default: 1.0m)
 * 
 * Debug output shows:
 * - Which foot triggered (LEFT/RIGHT)
 * - Detected surface type
 * - Position where effect played
 * - Green debug lines showing ground normals (in scene view)
 * 
 * Common Issues:
 * - No effects playing: Check Character Effects asset is assigned to EffectManager
 * - Wrong surface detected: Add SurfaceIdentifier components or check physics materials
 * - Effects at wrong position: Check character has humanoid avatar and foot bones
 * - Effects too loud/quiet: Adjust volume in EffectDefinition
 * 
 * ============================================================================
 * ADVANCED: CHARACTER-SPECIFIC VARIATIONS
 * ============================================================================
 * 
 * Different character types can have unique footsteps:
 * 
 * ZOMBIES:
 * - Heavier, dragging sounds
 * - More debris particles
 * - Lower pitch variation (0.8-0.9)
 * - Louder volume
 * 
 * HUMANS:
 * - Lighter, cleaner footsteps
 * - Subtle dust only
 * - Normal pitch (0.9-1.1)
 * - Quieter volume
 * 
 * LARGE ENEMIES (Tanks, etc.):
 * - Very heavy impacts
 * - Large debris particles
 * - Ground shakes (use Camera shake)
 * - Much lower pitch (0.6-0.8)
 * - Louder volume
 * 
 * ============================================================================
 * PERFORMANCE CONSIDERATIONS
 * ============================================================================
 * 
 * The system is optimized with object pooling, but for best performance:
 * 
 * - Use EffectDefinition.duration to auto-cleanup effects
 * - Keep particle systems lightweight (< 50 particles)
 * - Use simple particle textures
 * - Limit max audio sources (Unity default: 32)
 * - Consider reducing footstep frequency at distance (LOD)
 * 
 * Effect Pooling:
 * - Pool size configured in EffectManager (default: 20 per effect)
 * - Effects automatically return to pool when done
 * - Pooling prevents garbage collection spikes
 * 
 * ============================================================================
 * API REFERENCE
 * ============================================================================
 * 
 * Animation Event Methods (Add to animation clips):
 * - FootstepLeft()        : Left foot contact (precise IK positioning)
 * - FootstepRight()       : Right foot contact (precise IK positioning)
 * - Footstep()            : Generic footstep (character root position)
 * 
 * Script API (Call from code):
 * 
 * // Detect surface at position
 * SurfaceType surface = SurfaceDetector.DetectSurface(position, rayDistance);
 * 
 * // Detect surface at character
 * SurfaceType surface = SurfaceDetector.DetectSurfaceAtCharacter(transform);
 * 
 * // Play footstep with auto-detection
 * EffectManager.Instance.PlayFootstepEffect(position, normal, characterType, transform);
 * 
 * // Play footstep on specific surface
 * EffectManager.Instance.PlayFootstepEffect(position, normal, characterType, surfaceType);
 * 
 * // Get effects for character and surface
 * CharacterEffects effects = GetCharacterEffects(characterType);
 * EffectDefinition[] footsteps = effects.GetFootstepEffectsForSurface(surfaceType);
 * 
 * ============================================================================
 * EXAMPLE WORKFLOW
 * ============================================================================
 * 
 * Here's a complete example for setting up zombie footsteps:
 * 
 * 1. CREATE EFFECT DEFINITIONS:
 *    - Footstep_Zombie_Grass (3 variants with grass sounds + dust VFX)
 *    - Footstep_Zombie_Stone (3 variants with stone sounds + debris VFX)
 *    - Footstep_Zombie_Wood (3 variants with wood sounds + splinter VFX)
 * 
 * 2. CREATE CHARACTER EFFECTS:
 *    - Create: Effects_Zombie_Melee
 *    - Character Type: ZOMBIE_MELEE
 *    - Surface Footstep Effects:
 *      * GRASS → Footstep_Zombie_Grass variants
 *      * STONE → Footstep_Zombie_Stone variants
 *      * WOOD → Footstep_Zombie_Wood variants
 * 
 * 3. ADD TO EFFECT MANAGER:
 *    - Add Effects_Zombie_Melee to Character Effects array
 * 
 * 4. ADD ANIMATION EVENTS:
 *    - Open zombie walk animation
 *    - Add FootstepLeft() at frame 8 (left contact)
 *    - Add FootstepRight() at frame 24 (right contact)
 *    - Repeat for run animation
 * 
 * 5. TEST:
 *    - Enable Debug Footsteps on zombie prefab
 *    - Play in editor
 *    - Walk zombie around
 *    - Verify effects play on correct surfaces
 * 
 * ============================================================================
 */

// This file contains documentation only - no actual code
// Keep this file in your project as reference for the footstep system

