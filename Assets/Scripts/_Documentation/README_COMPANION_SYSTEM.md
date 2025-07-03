# Companion System Documentation

## Active Companion Scripts

### Currently Used (as shown in Unity Inspector):
1. **CompanionController** ✓
   - Main controller for companion behavior
   - Handles movement, following, animations
   
2. **CompanionSetupHelper** ✓
   - Helper script with presets for different companion types
   - Quick setup for chicken, rabbit, dog, cat, bird, frog behaviors
   
3. **CompanionSpeedSync** ✓
   - Synchronizes companion speed with player movement
   - Ensures companion keeps up with player

## Script Purposes

### CompanionController.cs
- **Purpose**: Core companion AI and movement
- **Features**:
  - Three movement types: Continuous, Hopping, AnimationDriven
  - Follow player with configurable distance
  - Wander behavior when idle
  - Animation system integration
  - Sound system for idle/move/happy states

### CompanionSetupHelper.cs
- **Purpose**: Quick configuration tool
- **Features**:
  - Presets for common companion types
  - Audio source setup
  - Easy parameter adjustment
  - Context menu actions for quick setup

### CompanionSpeedSync.cs
- **Purpose**: Dynamic speed adjustment
- **Features**:
  - Matches companion speed to player
  - Prevents companion from lagging behind
  - Smooth speed transitions

## Archived Scripts

The following scripts have been moved to `_Archive/Companion`:

1. **CompanionAnimatorDebug.cs**
   - Debug tool for animation system
   - Shows animation states with F8 key
   - Not needed in production

2. **GUIDE_ANIMATOR_CHICKEN.txt**
   - Setup guide for animator configuration
   - Moved to archive as documentation

## Configuration Tips

### For Chicken Companion:
```
Movement Type: AnimationDriven
Follow Distance: 3f
Move Speed: 7.2f (90% of player speed)
Animation: Use "Jump" for movement
```

### For Other Animals:
Use CompanionSetupHelper presets:
- Select preset type in Inspector
- Right-click → "Apply Preset"
- Fine-tune parameters as needed

## Audio Setup
1. Add AudioSource component
2. Right-click CompanionSetupHelper → "Setup Audio Source"
3. Assign sound clips to CompanionController

## Summary

The companion system is now streamlined with only essential scripts:
- Core functionality: CompanionController
- Setup assistance: CompanionSetupHelper  
- Speed matching: CompanionSpeedSync

Debug tools have been archived but remain available if needed.