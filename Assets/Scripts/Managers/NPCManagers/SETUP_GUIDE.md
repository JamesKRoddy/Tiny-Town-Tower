# Quick Setup Guide - Enemy Group Coordination

## What Was Added

✅ **EnemyManager.cs** - Central coordination system with all settings in Inspector
✅ **Integrated with existing enemy systems** (EnemyBase, Zombie, AttackBase)

## 2-Step Setup

### Step 1: Add EnemyManager to Your Scene

1. Open your game scene (e.g., RogueLite scene)
2. Create empty GameObject: `GameObject > Create Empty`
3. Name it "EnemyManager"
4. Add Component: `EnemyManager`

**That's it!** The system will work with default settings.

### Step 2: Configure Settings (Optional)

All settings are directly in the EnemyManager Inspector - no need for separate config assets!

1. Enter Play mode
2. Enable "Show Debug" on EnemyManager to see coordination in action
3. Tweak settings directly in the Inspector:
   - **Easier combat**: Max attackers = 1, Stagger delay = 1.0s
   - **Harder combat**: Max attackers = 3-4, Stagger delay = 0.2s
   - **Strategic combat**: Enable Aggro System
4. Changes in Inspector update in real-time (while in play mode)

## What You'll Notice

### Attack Staggering
- Enemies won't all attack at once anymore
- There's a rhythm to combat (attack → pause → attack)
- Queue system prevents overwhelming the player

### Flanking & Circling Behavior
- Enemies dynamically spread out around the player
- They circle around while waiting to attack
- Maintain personal space to avoid clustering
- Attack from different angles for tactical combat
- Creates fluid, organic-feeling encounters

### Aggro/Threat System
- Tracks who the player is damaging
- Can prioritize targets based on threat
- Threat decays over time

## Recommended Settings by Difficulty

### Easy Mode
- Max Simultaneous Attackers: **1**
- Attack Stagger Delay: **1.0s**
- Post Attack Cooldown: **1.5s**
- Enable Flanking: **false**

### Normal Mode (Default)
- Max Simultaneous Attackers: **2**
- Attack Stagger Delay: **0.5s**
- Post Attack Cooldown: **1.0s**
- Enable Flanking: **true**
- Enable Circling: **true**
- Min Angle Between Enemies: **45°**
- Circling Speed: **30°/s**
- Personal Space Radius: **1.5m**

### Hard Mode
- Max Simultaneous Attackers: **3**
- Attack Stagger Delay: **0.3s**
- Post Attack Cooldown: **0.5s**
- Enable Flanking: **true**
- Min Angle Between Enemies: **30°**

### Tactical Mode
- Max Simultaneous Attackers: **2**
- Attack Stagger Delay: **0.5s**
- Enable Aggro System: **true**
- Enable Flanking: **true**

## Testing Checklist

- [ ] EnemyManager GameObject exists in scene
- [ ] Multiple enemies spawn correctly
- [ ] Enemies don't all attack simultaneously
- [ ] Console shows coordination logs (if debug enabled)
- [ ] Enemies spread out around player (if flanking enabled)
- [ ] Performance is good (60 FPS maintained)

## Troubleshooting

**Nothing seems different:**
- Check that EnemyManager is in the scene and enabled
- Enable "Show Debug" to see if enemies are registering
- Make sure you have 3+ enemies to see coordination

**Compile errors:**
- Close and reopen Unity to force recompilation
- Check that all new files are in correct locations
- Verify namespaces are correct

**Enemies acting weird:**
- Start with default settings
- Disable flanking first, test attack coordination
- Enable features one at a time
- Check NavMesh is properly baked

## Next Steps

Once working:
1. Tune settings based on playtesting feedback
2. Consider difficulty scaling (adjust settings based on wave/room number)
3. Experiment with different enemy types and counts
4. Use the public API to dynamically adjust coordination during gameplay

## Need Help?

Check the full documentation in `EnemyManager_README.md` for:
- Detailed API reference
- Performance considerations
- Advanced usage examples
- Integration with other systems

