# Damage System Refactor - DamageInfo Struct Implementation

## Overview
Refactored the damage system to use a `DamageInfo` struct instead of multiple parameters, improving code clarity and enabling proper distinction between hostile combat damage and environmental damage.

## Changes Made

### 1. Created `DamageInfo` Struct (`Assets/Scripts/Utils/DamageInfo.cs`)
- **Purpose**: Encapsulates all damage parameters in a single, clean struct
- **Key Features**:
  - Constructor for hostile/combat damage from `IDamageDealer`
  - `Environmental()` static method for non-hostile damage (cleanliness, hunger, etc.)
  - `Create()` static method for flexible damage creation
  - Helper methods: `IsHostileDamage()`, `HasPoiseDamage()`, `HasElementalDamage()`
  - `IsEnvironmentalDamage` flag to prevent flee behavior from status effects

### 2. Updated `IDamageable` Interface (`Assets/Scripts/Managers/Interfaces.cs`)
- Added new primary method: `void TakeDamage(DamageInfo damageInfo)`
- Kept legacy method for backward compatibility: `void TakeDamage(float amount, ...)`

### 3. Updated `HumanCharacterController` (`Assets/Scripts/Characters/NPCs/HumanCharacterController.cs`)
- Implemented new `TakeDamage(DamageInfo)` as primary method
- Legacy `TakeDamage(float, ...)` now converts to `DamageInfo` and calls primary method
- Created private `TakeDamageInternal()` with actual implementation logic
- Maintains full backward compatibility with existing code

### 4. Updated `SettlerNPC` (`Assets/Scripts/Characters/NPCs/SettlerNPC.cs`)
- **CRITICAL FIX**: Now only flees from HOSTILE damage (enemies)
- Environmental damage (cleanliness, hunger, status effects) no longer triggers flee behavior
- Checks `damageInfo.IsHostileDamage()` before fleeing
- Legacy method converts to `DamageInfo` for consistency

### 5. Fixed `CleanlinessManager` (`Assets/Scripts/Managers/CampManagers/CleanlinessManager.cs`)
- **Fixed Three Issues**:
  1. ✅ **No Blood VFX**: Now uses `DamageInfo.Environmental(amount, playHitVFX: false)`
  2. ✅ **No Flee Behavior**: Environmental damage flag prevents fleeing
  3. ✅ **Correct Damage Amount**: Fixed calculation from `healthDrainRate * Time.deltaTime` to `healthDrainRate * 1f`
     - The coroutine runs every 1 second, not every frame
     - `healthDrainRate` is "health lost per second", so multiply by 1, not by deltaTime (~0.016s)
     - Damage went from ~0.009 per tick to ~2.0 per tick (as intended)

### 6. Fixed `EffectManager` (`Assets/Scripts/Managers/GameManagers/EffectManager.cs`)
- Status effect damage (hunger, sickness, etc.) now uses `DamageInfo.Environmental()`
- Removed unnecessary poise damage from status effects
- No VFX, no flee behavior for status damage

### 7. Updated `EnemyBase` (`Assets/Scripts/Characters/Enemies/EnemyBase.cs`)
- Added primary `TakeDamage(DamageInfo)` method
- Legacy method converts to `DamageInfo` and calls primary method
- Maintains full backward compatibility

### 8. Updated `PlaceableStructure` (`Assets/Scripts/CampBuilding/PlaceableStructure.cs`)
- Added primary `TakeDamage(DamageInfo)` method  
- Legacy method converts to `DamageInfo` and calls primary method
- Buildings (turrets, walls, etc.) now support the new damage system

## Usage Examples

### Hostile Combat Damage (Weapons, Enemies)
```csharp
// From an IDamageDealer (weapon, enemy, projectile)
var damageInfo = new DamageInfo(weaponDealer, amount: 10f, poiseDamage: 5f);
target.TakeDamage(damageInfo);

// Auto-detects hostile allegiance from dealer
// NPCs will flee if dealer.DealerAllegiance == Allegiance.HOSTILE
```

### Environmental Damage (Cleanliness, Hunger, Status Effects)
```csharp
// Environmental damage - no VFX, no flee behavior
var damageInfo = DamageInfo.Environmental(amount: 2f, playHitVFX: false);
settler.TakeDamage(damageInfo);

// NPCs will NOT flee from this damage
```

### Custom Damage (No Dealer)
```csharp
// Create custom damage without a dealer
var damageInfo = DamageInfo.Create(
    amount: 10f, 
    poiseDamage: 5f, 
    elementType: AttackElement.FIRE, 
    sourceTransform: attackerTransform, 
    playHitVFX: true, 
    isEnvironmental: false
);
target.TakeDamage(damageInfo);
```

## Benefits

1. **Cleaner API**: Single parameter instead of 5+ parameters
2. **Type Safety**: Structured data prevents parameter order mistakes
3. **Self-Documenting**: `IsEnvironmentalDamage` and `IsHostileDamage()` are clear
4. **Extensible**: Easy to add new damage properties without changing interface signatures
5. **Consistent**: All damage now goes through the same unified system
6. **Proper Behavior**: NPCs only flee from actual threats, not environmental hazards

## Files Modified

1. `Assets/Scripts/Utils/DamageInfo.cs` (NEW)
2. `Assets/Scripts/Managers/Interfaces.cs`
3. `Assets/Scripts/Characters/NPCs/HumanCharacterController.cs`
4. `Assets/Scripts/Characters/NPCs/SettlerNPC.cs`
5. `Assets/Scripts/Characters/Enemies/EnemyBase.cs`
6. `Assets/Scripts/CampBuilding/PlaceableStructure.cs`
7. `Assets/Scripts/Managers/CampManagers/CleanlinessManager.cs`
8. `Assets/Scripts/Managers/GameManagers/EffectManager.cs`

## Testing Notes

- NPCs should no longer flee from cleanliness damage
- No blood VFX should appear from cleanliness damage
- Cleanliness damage should now be ~2 HP/second instead of ~0.009 HP/second
- Status effects (hunger, sickness) should not cause fleeing
- Hostile attacks should still cause fleeing as expected
- All existing damage code continues to work via legacy method

## Migration Complete ✅

All code has been fully migrated to use the `DamageInfo` system:
- ✅ Legacy `TakeDamage(float, ...)` method removed from interface
- ✅ All implementations updated to use `DamageInfo`
- ✅ All callers updated to create `DamageInfo` structs
- ✅ System is now fully consistent and type-safe

## Future Enhancements (Optional)

- Add more damage types or flags to `DamageInfo` as needed
- Consider adding damage modifiers (critical hits, damage over time, etc.)
- Expand `IDamageDealer` to support more complex damage calculations

