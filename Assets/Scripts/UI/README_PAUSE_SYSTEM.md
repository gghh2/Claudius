# Modern Pause Menu System

## Overview
The game now uses a modern UI-based pause menu system with `SetActive()` instead of the old OnGUI approach.

## Main Script
- **ModernPauseMenu.cs** - Main pause menu controller

## Features
- ✅ Uses Unity's Canvas UI system
- ✅ SetActive() for better performance
- ✅ Automatic cursor management
- ✅ Compatible with other UI managers
- ✅ Optional debug controls for testing

## Controls
- **ESC** - Toggle pause menu
- **R** (while paused) - Respawn at spawn point

## Setup Requirements
1. A Canvas with a PauseMenuPanel GameObject
2. UI Buttons for Resume, Respawn, and Quit
3. Assign references in the ModernPauseMenu component

## Best Practices
- The pause menu panel should be inactive at start
- Use SetActive(true/false) instead of alpha values
- Ensure an EventSystem exists in the scene

## Archived Files
Old files have been moved to `_Archive` folder:
- SimplePauseMenu.cs (old OnGUI system)
- Setup guides and debug scripts