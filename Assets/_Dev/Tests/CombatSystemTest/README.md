# CombatSystem Test Scene

Comprehensive test suite for CombatSystem hit processing and damage calculation.

## Setup

### Option 1: Auto-Create (Recommended)
1. Open Unity Editor
2. Go to menu: **TTH → Tests → Create CombatSystem Test Scene**
3. Scene will be created at `Assets/_Dev/Tests/CombatSystemTest/CombatSystemTest.unity`

### Option 2: Manual Setup
1. Create a new scene
2. Add an empty GameObject named "CombatSystemTest"
3. Attach `CombatSystemTestRunner.cs` component
4. Save scene as `CombatSystemTest.unity` in this folder

## How to Test

1. Open `CombatSystemTest.unity`
2. Press **Play** in Unity Editor
3. Open **Console** window to see output
4. Call test methods from Inspector or code

## Test Methods

### Core Tests
- **TestBasicHit()** - Single hit: Attacker (ATK=30) → Defender (DEF=20) = 10 damage
- **TestMultipleHits()** - Fire 5 consecutive hits until defender dies
- **TestDamageVariation()** - Verify damage calculation (ATK - DEF)
- **TestDefenderModification()** - Add DEF buff and see damage reduction

### Edge Case Tests
- **TestHitDeadTarget()** - Try to hit already-dead target (should be rejected)
- **TestHitNullAttacker()** - Try to hit with null attacker (should be rejected)
- **TestReset()** - Reset defender to full HP and clear modifiers

## Event System (NEW)

All events are logged in Console:

```csharp
_events.OnHit          // Valid hit processed (even if dmg=0)
_events.OnDamage       // finalDamage > 0
_events.OnKilled       // Defender dies
_events.OnHitRejected  // Hit is rejected (dead, null, invulnerable, etc.)
```

## Output Example

```
[CombatSystemTest] Initializing...
[INITIAL STATE]
  Attacker: HP=100, ATK=30
  Defender: HP=120, DEF=20, Dead=False

[TestBasicHit] 
[OnHit] Valid=True, DMG=10, HP=110
[OnDamage] Base=10, Mitigated=10, Final=10
[AFTER HIT]
  Attacker: HP=100, ATK=30
  Defender: HP=110, DEF=20, Dead=False
```

## What's Being Tested

✅ Basic hit processing pipeline  
✅ Damage calculation (ATK - DEF, min 0)  
✅ Resource application (HP delta)  
✅ Invalid hit rejection (dead target, null entity)  
✅ Event emission (OnHit, OnDamage, OnKilled, OnHitRejected)  
✅ Attribute modification (DEF buff testing)  
✅ Multiple consecutive hits  
✅ Damage variation with attribute changes  

## Combat System Pipeline

```
HitEvent Created
  ↓
PreHit Checks:
  - Null entity check
  - Dead target check
  - Invulnerable check
  ↓
Snapshot Stats:
  - ATK, DEF, Max HP, Current HP
  ↓
Proc.OnHit Hook
  ↓
Damage Calculation:
  - Base damage (default = ATK)
  - Mitigation: baseDamage - DEF
  - Clamp: min 0
  ↓
Apply to Resources:
  - Apply HP delta
  - Clamp to max
  ↓
Proc.AfterHit Hook
  ↓
Emit Events:
  - OnHit (if valid)
  - OnDamage (if dmg > 0)
  - OnKilled (if dead)
  - OnHitRejected (if invalid)
```

## Damage Formula

```
Base Damage = ATK (or provided by proc)
Mitigated Damage = Base Damage - DEF
Final Damage = max(0, Mitigated Damage)
```

Example: ATK=30, DEF=20 → Final = max(0, 30-20) = 10

## Known Behaviors

- Minimum damage is 0 (no negative healing)
- Dead targets reject all hits
- Null entities reject hits
- Same HitEvent can be processed multiple times (no dedup)
- HP is clamped to max when MaxHP changes
