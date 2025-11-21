# Unity Overstimulation System

## Table of Contents
1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [File-by-File Breakdown](#file-by-file-breakdown)
   - [OverstimulationController.cs](#overstimulationcontrollercs)
   - [TriggerZone.cs](#triggerzonecs)
   - [LampController.cs](#lampcontrollercs)
   - [LampIntensity.cs](#lampintensitycs)
   - [CameraDisturbance.cs](#cameradisturbancecs)

---

## Project Overview

### Features

- **Tracking stress levels** from 0.0 (calm) to 1.0 (maximum overstimulation)
- **Making lamps brighter** as stress increases
- **Adding camera blur and flash effects** to the player's view
- **Responding to trigger zones** in the environment

---

## System Architecture

### How Everything Connects

1. **Player enters a trigger zone**
   - `TriggerZone.cs` detects the player entering via Unity's physics system
   - Calls `OverstimulationController.AddTrigger()` to signal stress should increase

2. **Overstimulation level increases**
   - `OverstimulationController.cs` tracks the stress level (0.0 to 1.0)
   - Uses a two-speed system: fast increase below threshold, slow increase above threshold
   - Level changes are frame-rate independent using `Time.deltaTime`

3. **Visual effects activate**
   - `OverstimulationController` calls `LampController.SetOverstimulationLevel()`
   - `LampController` distributes the level to all `LampIntensity` components
   - `OverstimulationController` calls `CameraDisturbance.SetOverstimulationLevel()`
   - `CameraDisturbance` applies blur and flash effects to the camera

4. **Player leaves the trigger zone**
   - `TriggerZone` calls `OverstimulationController.RemoveTrigger()`
   - Level gradually decreases back to 0.0
   - All effects fade back to normal

### Execution Order

**Every Frame:**
1. Unity Physics detects trigger events → calls `TriggerZone.OnTriggerEnter/Exit()`
2. `TriggerZone` calls `OverstimulationController.AddTrigger/RemoveTrigger()`
3. `OverstimulationController.Update()` runs:
   - `UpdateLevel()` - modifies stress level based on active triggers
   - `UpdateAllEffects()` - sends level to all visual effects
4. `LampController` receives level → distributes to individual lamps
5. `LampIntensity.Update()` - each lamp updates its brightness
6. `CameraDisturbance.Update()` - updates flash overlay
7. `CameraDisturbance.OnRenderImage()` - applies blur effect

**Important:** Trigger events only fire when player enters/exits. The level updates every frame based on whether triggers are active.

### Lamp Selection and Effect Scaling

#### How Lamps Are Selected

**Two Modes:**

1. **All Lamps Mode** (`maxLampsInFocus = -1`):
   - ALL lamps are affected
   - Each lamp's brightness scales with stress level
   - At level 0.5 → all lamps at 50% brightness
   - At level 1.0 → all lamps at 100% brightness

2. **Limited Lamps Mode** (`maxLampsInFocus > 0`, e.g., `maxLampsInFocus = 3`):
   - Only the closest lamps to the player are affected
   - Number of affected lamps increases with stress level
   - Formula: `lampsToAffect = RoundToInt(level * maxLampsInFocus)`
   
   **How it works:**
   - Every frame, lamps are sorted by distance to player (closest first)
   - The closest N lamps get the stress level
   - As stress increases, more distant lamps get affected
   - Example with `maxLampsInFocus = 3`:
     - Level 0.33 → 1 closest lamp affected
     - Level 0.5 → 2 closest lamps affected
     - Level 1.0 → 3 closest lamps affected

**Technical Details:**
- Lamps are stored in `allLamps` list (in discovery order)
- When updating, a temporary sorted list is created (sorted by distance to player)
- The sorted list determines which lamps get affected
- Sorting happens every frame, so it updates as player moves

#### Effect Scaling

All effects use the same stress level (0.0 to 1.0) but scale differently:

**Lamp Brightness:**
- Formula: `intensity = 1.0 + (level * (maxMultiplier - 1.0))`
- At level 0.5 with maxMultiplier 1.6: `1.0 + (0.5 * 0.6) = 1.3` (30% brighter)
- Then multiplied by pulse factor for pulsing effect

**Flash Overlay:**
- Formula: `flashAmount = level * maxFlashIntensity * pulseIntensity`
- At level 0.5 with maxFlashIntensity 0.1: `0.5 * 0.1 = 0.05` (5% opacity)

**Camera Blur:**
- Formula: `blur = level * maxBlurIntensity * pulseIntensity`
- At level 0.5 with maxBlurIntensity 2.0: `0.5 * 2.0 = 1.0` (medium blur)
- Achieved by downsampling (rendering at lower resolution, then upscaling)

**Key Points:**
- All effects scale linearly with stress level
- All effects use the same pulse timing (synchronized)
- Each effect has its own maximum multiplier

### Design Philosophy

- **Centralized control**: `OverstimulationController` is the single source of truth for stress level
- **Modular effects**: Each visual effect (lamps, camera) is independent and receives the level
- **Physics-based triggers**: Uses Unity's trigger system for reliable player detection
- **Frame-rate independent**: All time-based calculations use `Time.deltaTime`

---

## File-by-File Breakdown

### OverstimulationController.cs

**Responsibility:** Central controller that manages the stress level and coordinates all visual effects.

**Key Components:**
- **`overstimulationLevel`**: The core stress value (0.0 to 1.0) that all effects read from
- **Threshold system**: Two-speed increase (fast below threshold, slow above) to simulate realistic stress buildup
- **Speed variables**: `fastIncreaseSpeed`, `slowIncreaseSpeed`, `decreaseSpeed` - all in units per second
- **Active trigger counter**: Tracks how many trigger zones are currently active

**Key Functions:**
- **`Start()`**: Finds `LampController` and `CameraDisturbance` in the scene. If `CameraDisturbance` doesn't exist, creates one on the player's camera.
- **`Update()`**: Every frame, updates the stress level and notifies all effects
- **`UpdateLevel()`**: Increases/decreases the level based on active triggers
  - Uses `Time.deltaTime` for frame-rate independent calculations
  - Chooses speed based on whether level is above/below threshold
  - Clamps value to 0.0-1.0 range
- **`UpdateAllEffects()`**: Distributes the current level to all visual effect systems
- **`AddTrigger()` / `RemoveTrigger()`**: Public methods for trigger zones to call when player enters/exits

**Why this design?**
- Single source of truth prevents conflicts between multiple systems
- Centralized level calculation ensures consistent behavior
- Public methods allow trigger zones to easily signal state changes
- Automatic component finding makes setup easier (no manual references needed)

**Connections:**
- Receives input from: `TriggerZone` (via `AddTrigger`/`RemoveTrigger`)
- Sends output to: `LampController`, `CameraDisturbance` (via `SetOverstimulationLevel`)

---

### TriggerZone.cs

**Responsibility:** Detects when the player enters/exits an area and notifies the `OverstimulationController`.

**Key Components:**
- **`ZoneType` enum**: Defines whether the zone increases or decreases stress
- **`zoneName`**: Unique identifier for debugging
- **`playerTag`**: Tag to identify the player GameObject (default: "Player")

**Key Functions:**
- **`Start()`**: 
  - Finds `OverstimulationController` in the scene
  - Validates that the Collider is set as a trigger
  - Automatically adds a kinematic Rigidbody if missing (required for trigger detection)
- **`OnTriggerEnter()`**: Unity callback when a collider enters the trigger
  - Checks if the entering object has the player tag
  - Calls `AddTrigger()` or `RemoveTrigger()` based on zone type
- **`OnTriggerExit()`**: Unity callback when a collider leaves the trigger
  - Reverses the effect (removes trigger for Increase zones, adds trigger for Decrease zones)

**Why this design?**
- Uses Unity's built-in physics trigger system for reliable detection
- Automatic Rigidbody setup reduces setup errors
- Zone type system allows for both stress-increasing and stress-decreasing areas
- Tag-based detection is flexible and doesn't require specific GameObject names

**Connections:**
- Receives input from: Unity physics system (trigger events)
- Sends output to: `OverstimulationController` (via `AddTrigger`/`RemoveTrigger`)

**Setup Requirements:**
- GameObject must have a Collider component with "Is Trigger" checked
- Player GameObject must have the "Player" tag (or match `playerTag`)

---

### LampController.cs

**Responsibility:** Manages all lamps in the scene and distributes the stress level to them.

**Key Components:**
- **`allLamps`**: List of all `LampIntensity` components found in the scene
- **`maxLampsInFocus`**: Controls how many lamps are affected at maximum stress
  - If -1: All lamps are affected, but intensity scales with level
  - If > 0: Only that many lamps are affected (sorted by distance to player)

**Key Functions:**
- **`Start()`**: Calls `FindAllLamps()` to discover and set up all lamps
- **`FindAllLamps()`**: 
  - Searches entire scene for GameObjects with names containing "SM_CeilingLamp_"
  - Excludes child objects (exact name "SM_CeilingLamp")
  - Adds `LampIntensity` component to each lamp if not already present
  - Stores references in `allLamps` list (in discovery order)
- **`SetOverstimulationLevel()`**: Called by `OverstimulationController` every frame
  - Calculates how many lamps should be affected based on level
  - If in limited mode: sorts lamps by distance to player (closest first)
  - Distributes the level to affected lamps
  - Sets unaffected lamps to level 0.0

**Why this design?**
- Automatic lamp discovery means no manual setup needed
- Two modes (all lamps vs. specific number) provide flexibility
- Distance-based sorting creates realistic effect (nearby lamps first)
- Centralized management makes it easy to control all lamps together

**Connections:**
- Receives input from: `OverstimulationController` (via `SetOverstimulationLevel`)
- Sends output to: All `LampIntensity` components (via `SetOverstimulationLevel`)

**Lamp Naming Convention:**
- Parent lamps: `SM_CeilingLamp_1`, `SM_CeilingLamp_2`, etc.
- Child objects: `SM_CeilingLamp` (excluded from search)

---

### LampIntensity.cs

**Responsibility:** Controls the brightness of a single lamp based on the stress level.

**Key Components:**
- **`maxIntensityMultiplier`**: Maximum brightness multiplier at 100% stress (e.g., 1.6 = 60% brighter)
- **`pulseSpeed`**: How fast the lamp pulses (in seconds per cycle)
- **`pulseVariation`**: How much the intensity varies during pulsing (0 = constant, 0.2 = 20% variation)
- **Material references**: Stores original emission color and intensity to restore later

**Key Functions:**
- **`Start()`**: 
  - Finds the `MeshRenderer` component (searches children first, then self)
  - Locates the lamp material by name patterns ("LampOn", "Lamp", "Glass")
  - Creates a material instance (copy) to avoid affecting other lamps
  - Stores original emission color and intensity
  - Handles different shader types (HDRP `_EmissiveColor`, Standard `_EmissionColor`)
- **`Update()`**: 
  - If level > 0: Calculates new brightness with pulsing effect
  - Uses sine wave for smooth pulsing: `Mathf.Sin(t * Mathf.PI * 2)`
  - Preserves original color while increasing intensity
  - Updates material emission property
  - If level = 0: Restores original brightness
- **`SetOverstimulationLevel()`**: Called by `LampController` to set the current stress level

**Why this design?**
- Material instances prevent affecting other lamps using the same material
- Color normalization preserves the lamp's original hue while increasing brightness
- Sine wave pulsing creates smooth, natural-looking variation
- Supports multiple shader types for compatibility

**Math Details:**
- **Base intensity**: `1.0 + (level * (maxMultiplier - 1.0))`
- **Pulsing**: Sine wave converted to 0-1 range, then applies `pulseVariation`
- **Color preservation**: Normalizes original color by dividing by intensity, then multiplies by new intensity

**Connections:**
- Receives input from: `LampController` (via `SetOverstimulationLevel`)
- Sends output to: Material emission properties (visual effect)

---

### CameraDisturbance.cs

**Responsibility:** Adds visual effects (blur and flash overlay) to the camera that sync with the stress level.

**Key Components:**
- **`maxFlashIntensity`**: Maximum flash opacity at 100% stress (0.0 to 1.0)
- **`flashSpeed`**: Pulse speed for flash effect (should match lamp pulse speed)
- **`vignetteSize`**: How much of the screen the vignette covers (0.0 to 1.0)
- **`maxBlurIntensity`**: Maximum blur strength at 100% stress
- **UI Canvas**: Created at runtime to display the flash overlay

**Key Functions:**
- **`Start()`**: 
  - Gets reference to the camera component
  - Creates UI Canvas with `ScreenSpaceOverlay` mode (renders on top of everything)
  - Creates flash overlay image with vignette texture
  - Sets up CanvasScaler for proper screen scaling
- **`CreateFlashOverlay()`**: 
  - Creates Canvas, Image, and necessary UI components
  - Generates vignette texture (radial gradient: transparent center, opaque edges)
  - Configures RectTransform to cover entire screen
- **`CreateVignetteTexture()`**: 
  - Generates a 256x256 texture with radial gradient
  - Uses distance from center to calculate alpha (transparency)
  - `vignetteSize` controls how much of the screen is affected
- **`Update()`**: 
  - Calculates flash intensity using sine wave (same as lamps for synchronization)
  - Applies `Mathf.Pow` to create softer transitions
  - Updates overlay alpha based on stress level and pulse
  - Smoothly fades out when level reaches 0
- **`OnRenderImage()`**: Unity callback for post-processing effects
  - Calculates blur intensity (synced with flash pulse)
  - Uses downsampling technique: render at lower resolution, then upscale
  - Creates blur effect by reducing resolution based on stress level
- **`SetOverstimulationLevel()`**: Called by `OverstimulationController` to set current stress level

**Why this design?**
- UI overlay ensures flash effect appears above all 3D content
- Vignette texture creates edge-focused effect (more realistic than full-screen flash)
- Downsampling blur is simple and performant (no complex shaders needed)
- Sine wave synchronization with lamps creates cohesive visual experience
- Automatic canvas creation means no manual UI setup required

**Math Details:**
- **Flash**: `level * maxFlashIntensity * pulseIntensity`
- **Blur downsampling**: `1 + (blurIntensity * 2)` - reduces resolution by 1x to 3x
- **Vignette alpha**: Only affects pixels where `normalizedDistance > (1 - vignetteSize)`
- **Sine wave**: Same formula as `LampIntensity` for synchronization

**Connections:**
- Receives input from: `OverstimulationController` (via `SetOverstimulationLevel`)
- Sends output to: Camera render pipeline (via `OnRenderImage`), UI Canvas (flash overlay)

**Setup:**
- Can be attached directly to camera, or will be auto-created by `OverstimulationController` if missing

---

## Summary

### System Flow

```
TriggerZone (player enters)
    ↓
OverstimulationController.AddTrigger()
    ↓
OverstimulationController.UpdateLevel() (increases level)
    ↓
OverstimulationController.UpdateAllEffects()
    ↓
    ├─→ LampController.SetOverstimulationLevel()
    │       ↓
    │   LampIntensity.SetOverstimulationLevel() (for each lamp)
    │       ↓
    │   Material emission brightness increases
    │
    └─→ CameraDisturbance.SetOverstimulationLevel()
            ↓
        Blur + Flash effects increase
```

### Key Design Decisions

1. **Centralized stress tracking**: One controller manages the level, preventing conflicts
2. **Frame-rate independence**: All time calculations use `Time.deltaTime`
3. **Automatic component discovery**: Scripts find each other automatically (less setup)
4. **Material instances**: Each lamp gets its own material copy (prevents shared material issues)
5. **Synchronized effects**: All effects use the same sine wave timing for cohesion
6. **Physics-based triggers**: Reliable player detection without manual checks
7. **Distance-based lamp selection**: Closest lamps to player are affected first (in limited mode)

