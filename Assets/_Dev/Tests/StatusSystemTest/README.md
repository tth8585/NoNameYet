# StatusSystem Test Scene

Test suite for StatusSystem functionality.

## Setup

### Option 1: Auto-Create (Recommended)
1. Open Unity Editor
2. Go to menu: **TTH → Tests → Create StatusSystem Test Scene**
3. Scene will be created at `Assets/_Dev/Tests/StatusSystemTest/StatusSystemTest.unity`

### Option 2: Manual Setup
1. Create a new scene
2. Add an empty GameObject named "StatusSystemTest"
3. Attach `StatusSystemTestRunner.cs` component
4. Save scene as `StatusSystemTest.unity` in this folder

## How to Test

1. Open `StatusSystemTest.unity`
2. Press **Play** in Unity Editor
3. Open **Console** window to see output
4. Call test methods via code or directly invoke on the component:

## Test Methods

In the Console or via Inspector component, you can call:

- **Initialize()** - Setup StatusSystem
- **TestAddStatus()** - Add single status (Berserk, 3 sec)
- **TestAddMultiple()** - Add multiple statuses at once
- **TestRefreshDuration()** - Test refresh behavior (same ID refreshes duration)
- **TestRemove()** - Remove a status
- **TestClearAll()** - Clear all active statuses
- **TestInfiniteStatus()** - Add status with infinite duration
- **TestVersionTracking()** - Test version increment on changes
- **PrintStatuses()** - Print all active statuses

## Output

Logs appear in Console showing:
- Active statuses with remaining duration
- Version changes
- Status change events
- Expiration behavior

## What's Being Tested

✅ Add/Remove statuses  
✅ Duration timers & expiration  
✅ Refresh behavior (same ID refreshes)  
✅ Event callbacks (OnChanged)  
✅ Version tracking  
✅ Infinite duration statuses  
✅ Clear all functionality  
✅ Query methods (Has, GetRemainingSeconds)  

## Status Types Available

- **Berserk** - Increased damage
- **Slow** - Decreased speed
- **Paralyze** - Can't move
- **Speedy** - Increased speed
- **Dazed** - Confused
- **Healing** - Recovering HP
- **Energized** - Full of energy
