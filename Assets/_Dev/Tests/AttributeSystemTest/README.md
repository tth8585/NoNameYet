# AttributeSystem Test Scene

Test suite for AttributeSystem functionality.

## Setup

### Option 1: Auto-Create (Recommended)
1. Open Unity Editor
2. Go to menu: **TTH → Tests → Create AttributeSystem Test Scene**
3. Scene will be created at `Assets/_Dev/Tests/AttributeSystemTest/AttributeSystemTest.unity`

### Option 2: Manual Setup
1. Create a new scene
2. Add an empty GameObject named "AttributeSystemTest"
3. Attach `AttributeSystemTestRunner.cs` component
4. Save scene as `AttributeSystemTest.unity` in this folder

## How to Test

1. Open `AttributeSystemTest.unity`
2. Press **Play** in Unity Editor
3. Open **Console** window to see output
4. Call test methods via code or directly invoke on the component:

## Test Methods

In the Console or via Inspector component, you can call:

- **Initialize()** - Setup base stats and class caps
- **TestAddModifier()** - Add +10 ATK buff (3 sec duration)
- **TestMultiplyModifier()** - Apply x1.5 DEF buff (4 sec)
- **TestOverride()** - Set SPD to 0 (paralyzed effect)
- **TestCapHitting()** - Add progression mods that exceed class cap
- **TestStackingPolicy()** - Test stacking unique modifiers
- **PrintStats()** - Print current resolved stats

## Output

Logs appear in Console showing:
- Base stats
- Current resolved stats (with modifiers applied)
- Cap enforcement
- Override states
- Modifier expiration over time

## What's Being Tested

✅ Base stat values  
✅ Modifier stacking (Add, Multiply)  
✅ Duration timers  
✅ Stacking policies (UniqueByStackKey)  
✅ Override layer (highest priority)  
✅ Class stat caps enforcement  
✅ Dirty tracking / caching  
✅ Source type filtering (Progression vs Bonus)  
