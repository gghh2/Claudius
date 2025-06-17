# Camera System Documentation

## Active Camera Scripts

### Currently Used (as shown in Unity Inspector):
1. **CameraFollow** (Located in Player folder)
   - Main camera follow script for player tracking
   
2. **SimpleCameraObstacleHandler** 
   - Handles transparency for objects between camera and player
   - The active obstacle handler being used
   
3. **SkyboxFixer** (Located in Utils folder)
   - Fixes skybox rendering issues
   
4. **OrthographicFogAdapter**
   - Adapts fog settings for orthographic camera

## Remaining Camera Scripts

### Camera Folder:
- **SimpleCameraObstacleHandler.cs** ✓ (ACTIVE)
- **OrthographicFogAdapter.cs** ✓ (ACTIVE)

## Archived Scripts (Obsolete/Debug)

The following scripts have been moved to Archive/Camera:

1. **CameraClippingDiagnostic.cs**
   - Debug script for camera clipping issues
   - Not needed in production

2. **TransparencyTest.cs**
   - Test script for transparency functionality
   - Debug/test purpose only

3. **CameraObstacleTransparency.cs**
   - Alternative implementation of obstacle transparency
   - Replaced by SimpleCameraObstacleHandler

4. **AlphaOnlyCameraObstacleHandler.cs**
   - Another alternative implementation
   - Replaced by SimpleCameraObstacleHandler

## Summary

The camera system is now cleaned up with only the essential scripts remaining:
- Main camera control: CameraFollow
- Obstacle handling: SimpleCameraObstacleHandler
- Visual fixes: SkyboxFixer, OrthographicFogAdapter

All debug and alternative implementations have been archived.