# Universal Effects Setup Guide

This guide explains how to set up universal effects in the EffectManager.

## Overview

The effect system uses **universal effects** that apply to ALL character types:

### **Elemental Effects:**
1. **Hit Effects** - Played on the character's mesh when they take elemental damage
2. **Impact Effects** - Played on the ground/impact point when elemental damage occurs

### **Status Effects:**
1. **Visual Effects** - Particle systems, animations, material changes
2. **Gameplay Effects** - Movement speed, animation speed, action prevention
3. **Floating Text** - Status messages above characters

**Benefits of Universal Effects:**
- ✅ **One-time setup**: Configure effects once, works for all characters
- ✅ **Consistent**: Same fire effects for humans, zombies, bosses, etc.
- ✅ **Maintainable**: Easy to update effects globally
- ✅ **Performance**: No duplication of effect definitions

## Setup Instructions

### 1. Create Character Effects ScriptableObject

1. Right-click in Project window
2. Navigate to `Create > Scriptable Objects > Effects > Character Effects`
3. Name it appropriately (e.g., "HumanCharacterEffects", "ZombieCharacterEffects")

### 2. Configure Universal Elemental Effects

In the EffectManager inspector, you'll see a "Universal Elemental Effects" array:

#### For each element you want to support:
1. **Set Array Size**: Increase the "Universal Elemental Effects" array size
2. **Select Element Type**: Choose from dropdown (Fire, Ice, Electric, Poison, Bleed, Holy, Shadow)
3. **Assign Hit Effects**: Drag particle system prefabs for effects on character mesh
4. **Assign Impact Effects**: Drag particle system prefabs for effects on ground/impact point

**Note**: These effects are universal and apply to ALL character types, so you only need to set them up once!

### 3. Configure Universal Status Effects

In the EffectManager inspector, you'll see a "Universal Status Effects" array:

#### For each status effect you want to support:
1. **Set Array Size**: Increase the "Universal Status Effects" array size
2. **Select Status Type**: Choose from dropdown (Sick, On Fire, Frozen, etc.)
3. **Configure Visual Effects**: Set particle systems, material changes, floating text
4. **Configure Gameplay Effects**: Set movement speed, animation speed, duration, etc.

**Note**: These status effects are universal and apply to ALL character types!

#### Example Elemental Effects:
- **Fire**: Hit effects = sparks/flames on character, Impact effects = ground fire/scorch marks
- **Ice**: Hit effects = frost crystals on character, Impact effects = ice patches on ground
- **Electric**: Hit effects = lightning on character, Impact effects = electric arcs on ground
- **Poison**: Hit effects = gas clouds on character, Impact effects = poison puddles on ground
- **Bleed**: Hit effects = blood splatter on character, Impact effects = blood pools on ground
- **Holy**: Hit effects = golden light on character, Impact effects = divine energy on ground
- **Shadow**: Hit effects = dark energy on character, Impact effects = shadow patches on ground

#### Example Status Effects:
- **Sick**: Green particles, slowed movement, "Sick" floating text
- **On Fire**: Fire particles, red tint, "On Fire" floating text
- **Frozen**: Ice crystals, blue tint, slowed movement
- **Stunned**: Dizzy stars, stopped movement, "Stunned" floating text

### 4. Assign to EffectManager

1. Select the EffectManager in the scene
2. In the Character Effects array, add your Character Effects ScriptableObject
3. Set the Character Type to match (e.g., HUMAN, ZOMBIE, etc.)

### 5. Create Effect Definitions

For each elemental effect you want to use:

1. Right-click in Project window
2. Navigate to `Create > Scriptable Objects > Effects > Effect Definition`
3. Name it descriptively (e.g., "FireHitEffect", "IceImpactEffect")
4. Assign particle system prefabs and audio clips
5. Configure duration, looping, and other properties

For each status effect you want to use:

1. Right-click in Project window
2. Navigate to `Create > Scriptable Objects > Effects > Status Effect Definition`
3. Name it descriptively (e.g., "SickStatusEffect", "OnFireStatusEffect")
4. Configure visual effects, gameplay effects, and floating text

### 6. Example Setup

```
EffectManager (Universal):
├── Universal Elemental Effects Array:
│   ├── [0] Element Type: Fire
│   │   ├── Hit Effects: [FireSparks, BurnMarks]
│   │   └── Impact Effects: [GroundFire, ScorchMarks]
│   ├── [1] Element Type: Ice
│   │   ├── Hit Effects: [FrostCrystals, IceShards]
│   │   └── Impact Effects: [IcePatch, FrostGround]
│   └── ... (other elements as needed)
└── Universal Status Effects Array:
    ├── [0] Status Type: Sick
    │   ├── Visual Effect: GreenParticles
    │   ├── Movement Speed: 0.5x
    │   └── Floating Text: "Sick"
    ├── [1] Status Type: On Fire
    │   ├── Visual Effect: FireParticles
    │   ├── Character Tint: Red
    │   └── Floating Text: "On Fire"
    └── ... (other status effects as needed)

Character Effects (Per Character Type):
├── HumanCharacterEffects: [Blood, Death, Footstep, Spawn, Idle effects]
├── ZombieCharacterEffects: [Blood, Death, Footstep, Spawn, Idle effects]
└── ... (character-specific effects only)
```

## Usage

The system automatically plays the appropriate effects when:
- Characters take elemental damage from weapons
- Boss attacks deal elemental damage
- Enemy attacks deal elemental damage

## Tips

1. **Hit Effects**: Should be relatively small and focused on the character
2. **Impact Effects**: Should be larger and cover the ground area
3. **Duration**: Hit effects can be shorter, impact effects can last longer
4. **Variety**: Use multiple effect definitions per element for visual variety
5. **Performance**: Use object pooling (already implemented) for better performance

## Testing

To test your effects:
1. Set up a weapon with elemental damage type
2. Attack a character with the appropriate resistance setup
3. Observe both hit effects (on character) and impact effects (on ground)
4. Adjust effect definitions as needed for visual impact
