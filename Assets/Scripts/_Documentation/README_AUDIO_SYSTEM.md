# Audio System Documentation

## Overview
The audio system is organized into several modular components that handle different aspects of game audio.

## Core Components

### 1. AudioDistanceManager
**Location:** `Audio/AudioDistanceManager.cs`
- Controls volume based on camera zoom (orthographic size)
- Affects all sounds except music
- Singleton pattern with DontDestroyOnLoad

**Key Settings:**
- `minZoomSize`: Camera size for 100% volume (default: 2)
- `maxZoomSize`: Camera size for minimum volume (default: 15)
- `minVolumeMultiplier`: Volume at max zoom (default: 0.2 = 20%)

### 2. SoundEffectsManager
**Location:** `Audio/SoundEffectsManager.cs`
- Manages all sound effects with object pooling
- Supports 3D and 2D sounds
- Handles pitch variation and volume control

**Usage:**
```csharp
// Play a sound
SoundEffectsManager.Instance.PlaySound("UI_Click");

// Play at position
SoundEffectsManager.Instance.PlaySound("Explosion", transform.position);
```

### 3. MusicManager
**Location:** `Audio/MusicManager.cs`
- Handles background music with crossfading
- Zone-based music system
- Not affected by camera zoom

**Usage:**
```csharp
// Change music zone
MusicManager.Instance.SetZone(MusicZoneType.Combat);

// Play specific track
MusicManager.Instance.PlayTrackByName("Boss Theme");
```

### 4. AmbientSoundZone
**Location:** `Audio/AmbientSoundZone.cs`
- Trigger-based ambient sounds
- Supports fade in/out
- Can be 2D or 3D positioned

**Setup:**
1. Add to GameObject with Collider
2. Set Collider as Trigger
3. Assign ambient sound clip
4. Configure fade duration and volume

### 5. FootstepSystem
**Location:** `Player/FootstepSystem.cs`
- Surface-based footstep sounds
- Particle effects matching surface
- Integrates with TerrainLayerDetector

**Features:**
- Material detection
- Terrain layer detection
- Customizable surface mappings
- Volume affected by camera zoom

### 6. AudioConstants
**Location:** `Audio/AudioConstants.cs`
- Central location for sound names
- Helper methods for common operations

## Setup Instructions

### 1. Create Audio Manager GameObject
1. Create empty GameObject named "AudioManager"
2. Add these components:
   - AudioDistanceManager
   - SoundEffectsManager
   - MusicManager

### 2. Configure AudioDistanceManager
- Set `Min Zoom Size` = 2
- Set `Max Zoom Size` = 15
- Set `Min Volume Multiplier` = 0.2
- Enable all categories except music

### 3. Add Sound Effects
In SoundEffectsManager:
1. Expand "Sound Effects" list
2. Add entries with:
   - Sound Name (e.g., "UI_Click")
   - Audio Clip
   - Volume and pitch settings
   - 3D/2D toggle

### 4. Add Music Tracks
In MusicManager:
1. Expand "Music Tracks" list
2. Add entries with:
   - Track Name
   - Audio Clip
   - Volume
   - Zones where it should play

### 5. Setup Footsteps
On Player GameObject:
1. Add FootstepSystem
2. Add TerrainLayerDetector
3. Assign default footstep sounds
4. Configure surface-specific sounds

## Audio Zones

### Music Zones
Use `MusicZoneTrigger` on trigger colliders:
- Laboratory
- Hangar
- Market
- Combat
- Victory
- etc.

### Ambient Zones
Use `AmbientSoundZone` on trigger colliders:
- Configure fade duration
- Set 2D/3D mode
- Adjust volume

## Best Practices

1. **Sound Naming Convention:**
   - UI: `UI_Click`, `UI_Error`
   - Player: `Player_Jump`, `Player_Damage`
   - Quest: `Quest_Start`, `Quest_Complete`

2. **Performance:**
   - SoundEffectsManager uses object pooling
   - Limit simultaneous sounds (default: 30)
   - Use 3D sounds for positioned audio

3. **Volume Control:**
   - Master volumes saved in PlayerPrefs
   - Camera zoom affects all except music
   - Individual sound volumes stack multiplicatively

## Troubleshooting

**No sound playing:**
- Check if AudioManager exists in scene
- Verify sound name matches exactly
- Check master volume settings

**Footsteps not working:**
- Ensure ground layer is set correctly
- Check movement threshold
- Verify audio clips are assigned

**Music not changing:**
- Check zone trigger setup
- Verify track has correct zones assigned
- Ensure only one MusicManager exists
