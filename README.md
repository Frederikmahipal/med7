# Overstimulation Simulation (VR)

## Table of Contents
1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [Adaptive System](#adaptive-system)
4. [File-by-File Breakdown](#file-by-file-breakdown)
   - [OverstimulationController.cs](#overstimulationcontrollercs)
   - [AdaptationController.cs](#adaptationcontrollercs)
   - [TriggerZone.cs](#triggerzonecs)
   - [CameraDisturbance.cs](#cameradisturbancecs)
   - [AudioController.cs](#audiocontrollercs)
5. [Technical Details](#technical-details)
6. [Setup Guide](#setup-guide)

---

## Project Overview

### Purpose
This Unity project simulates sensory overstimulation for an empathy simulation, designed to help people without autism understand the experience of sensory overload. The system creates an immersive **VR experience** where environmental triggers gradually increase stress levels, manifesting as visual and audio effects.

### Features

- **VR-Only Implementation**: Designed specifically for VR headsets (Oculus Quest)
- **Adaptive Stress Tracking**: Centralized stress level (0.0 to 1.0) that adapts to individual user behavior
- **Real-time Adaptation**: VR behavior tracking adjusts effect intensity based on user movement patterns
- **Visual Effects**: 
  - VR vignette overlay (tunnel vision effect) that darkens peripheral vision
  - Synchronized pulsing with pulse speed adapting to user behavior
- **Audio Effects**: 
  - Background ambient sounds that duck (get quieter) as stress increases
  - Rising intensity sounds that increase in volume and pitch
- **Environmental Triggers**: Physics-based zones that increase or decrease stress when entered
- **Frame-rate Independent**: All calculations use `Time.deltaTime` for consistent behavior

### Adaptive Media System
The system implements an adaptive media approach that monitors user behavior in real-time and adjusts the intensity of overstimulation effects accordingly. This ensures:
- **Stable users** (minimal head movement) receive increased intensity (up to 1.2x) to ensure they experience the full simulation
- **Uncomfortable users** (rapid head movements) receive reduced intensity (down to 0.8x) to prevent overwhelming them and maintain engagement

**Important Note**: The adaptation multiplier affects both intensity and pulse speed:
- **Intensity**: A 1.2x multiplier means effects reach 100% when base level is 0.83, while 0.8x means effects only reach 80% of maximum intensity
- **Pulse Speed**: Higher multiplier (1.2) = faster pulsing (more intense), lower multiplier (0.8) = slower pulsing (more calm)

---

## System Architecture

### High-Level Flow

```
Player enters TriggerZone
    ↓
TriggerZone detects entry → OverstimulationController.AddTrigger()
    ↓
OverstimulationController.UpdateLevel() (increases stress level)
    ↓
AdaptationController monitors head movement → calculates adaptation multiplier
    ↓
OverstimulationController.UpdateAllEffects() (applies adaptation)
    ↓
    ├─→ CameraDisturbance.SetOverstimulationLevel(adaptedLevel)
    │       ↓
    │   VR vignette overlay intensity increases (tunnel vision)
    │
    └─→ AudioController.GetAdaptedLevel()
            ↓
        Background volume decreases, Rising volume increases
```

### Component Relationships

**Central Controller**: `OverstimulationController` is the single source of truth for the stress level. All other systems receive updates from it.

**Adaptive Layer**: `AdaptationController` monitors user behavior and provides an adaptation multiplier that modifies how intense the effects appear.

**Effect Systems**: `CameraDisturbance` and `AudioController` are independent modules that receive the adapted stress level and apply their respective effects.

**Trigger System**: `TriggerZone` components detect player presence and notify the controller to increase or decrease stress.

### Execution Order (Per Frame)

1. **Unity Physics**: Detects trigger events → calls `TriggerZone.OnTriggerEnter/Exit()`
2. **TriggerZone**: Calls `OverstimulationController.AddTrigger/RemoveTrigger()`
3. **OverstimulationController.Update()**:
   - `UpdateLevel()` - modifies stress level based on active triggers
   - `UpdateAllEffects()` - retrieves adaptation multiplier and applies it
4. **AdaptationController.Update()**: 
   - Calculates rotation speed
   - Updates movement history
   - Calculates discomfort level and adaptation multiplier
5. **CameraDisturbance.Update()**: Updates VR vignette overlay intensity, adjusts pulse speed based on adaptation multiplier
6. **AudioController.Update()**: Updates audio volumes based on adapted level

**Important**: Trigger events only fire when player enters/exits. The level updates every frame based on whether triggers are active.

---

## Adaptive System

### Overview
The adaptive system (`AdaptationController`) monitors user head/camera movement to detect signs of discomfort or stability. Based on this analysis, it adjusts the intensity of all overstimulation effects in real-time.

### How It Works

#### 1. Movement Tracking
The system tracks camera/head rotation speed every frame:
- Calculates the angular difference between current and previous rotation
- Converts to degrees per second: `rotationSpeed = rotationDelta / deltaTime`
- Stores rotation speeds in a queue with timestamps

#### 2. Analysis Window
Movement data is analyzed over a sliding 2-second window:
- Old data (older than 2 seconds) is automatically removed
- Average rotation speed is calculated from the window
- This prevents momentary spikes from causing abrupt changes

#### 3. Discomfort Calculation
The system calculates a discomfort level (0.0 to 1.0) based on average rotation speed:

```
if (avgRotationSpeed <= comfortZoneRotation):
    discomfortLevel = 0.0  // Normal movement, no discomfort
    
else if (avgRotationSpeed >= rapidRotationThreshold):
    discomfortLevel = 1.0  // Maximum discomfort detected
    
else:
    // Gradual interpolation between comfort zone and threshold
    discomfortLevel = (avgRotationSpeed - comfortZoneRotation) / 
                      (rapidRotationThreshold - comfortZoneRotation)
```

**Default Values**:
- `comfortZoneRotation = 30°/s` - Movement below this is considered normal
- `rapidRotationThreshold = 90°/s` - Movement above this indicates maximum discomfort

#### 4. Adaptation Multiplier
The discomfort level is used to interpolate an adaptation multiplier:

```
targetAdaptationMultiplier = Lerp(maxMultiplier, minMultiplier, discomfortLevel)
```

**Default Values**:
- `maxIntensityMultiplier = 1.2` - Applied when discomfort = 0.0 (stable user)
- `minIntensityMultiplier = 0.8` - Applied when discomfort = 1.0 (uncomfortable user)

#### 5. Smooth Transition
The multiplier smoothly transitions to prevent jarring changes:

```
currentAdaptationMultiplier = Lerp(
    currentAdaptationMultiplier,
    targetAdaptationMultiplier,
    AdaptationSpeed * Time.deltaTime
)
```

Where `AdaptationSpeed = 1.0` (configurable, but currently fixed).

#### 6. Application to Effects
The adaptation multiplier is applied to the base stress level:

```
adaptedLevel = overstimulationLevel × currentAdaptationMultiplier
adaptedLevel = Clamp01(adaptedLevel)  // Keep in 0.0-1.0 range
```

This adapted level is then sent to all effect systems (camera, audio).

**Important**: Because `adaptedLevel` is clamped to 0.0-1.0, a 1.2x multiplier doesn't make effects stronger than 100% - it makes them reach 100% faster (when base level is 0.83 instead of 1.0). A 0.8x multiplier means effects only reach 80% of maximum intensity.

### Design Rationale

**Why increase intensity for stable users?**
- Stable head movement suggests the user is comfortable and engaged
- Increasing intensity ensures they experience the full simulation
- Prevents the simulation from being too mild for users who can handle more

**Why decrease intensity for uncomfortable users?**
- Rapid head movement often indicates discomfort or disorientation
- Reducing intensity prevents overwhelming the user
- Maintains engagement by keeping the experience challenging but manageable

**Why use a sliding window?**
- Prevents momentary movements (like looking around) from causing abrupt changes
- Provides smoother, more natural adaptation
- Reduces false positives from normal exploration

**Why gradual interpolation?**
- Binary on/off adaptation would be jarring and noticeable
- Gradual changes feel more natural and less intrusive
- Allows fine-grained adjustment based on movement patterns

### Example Scenarios

**Scenario 1: Stable User**
- User stands still or moves head slowly (< 30°/s)
- `discomfortLevel = 0.0`
- `adaptationMultiplier = 1.2`
- Effects reach maximum (1.0) when base level is 0.83
- At base level 0.5, effects appear as if level is 0.6

**Scenario 2: Uncomfortable User**
- User rapidly looks around (> 90°/s average)
- `discomfortLevel = 1.0`
- `adaptationMultiplier = 0.8`
- Effects only reach 80% of maximum intensity
- At base level 1.0, effects appear as if level is 0.8

**Scenario 3: Moderate Movement**
- User moves head at 60°/s average (between comfort zone and threshold)
- `discomfortLevel = (60 - 30) / (90 - 30) = 0.5`
- `adaptationMultiplier = Lerp(1.2, 0.8, 0.5) = 1.0`
- Effects appear at base intensity (no adaptation)

---

## File-by-File Breakdown

### OverstimulationController.cs

**Responsibility**: Central controller that manages the stress level and coordinates all visual/audio effects. Integrates the adaptive system to personalize the experience.

**Key Components**:
- **`overstimulationLevel`**: The core stress value (0.0 to 1.0) that all effects read from
- **`adaptedLevel`**: The stress level after applying VR behavior adaptation (visible in Inspector)
- **`currentAdaptationMultiplier`**: Current adaptation multiplier from AdaptationController (visible in Inspector)
- **Threshold system**: Two-speed increase (fast below threshold, slow above) to simulate realistic stress buildup
- **Speed variables**: `fastIncreaseSpeed`, `slowIncreaseSpeed`, `decreaseSpeed` - all in units per second
- **Active trigger counter**: Tracks how many trigger zones are currently active
- **`enableAdaptiveIntensity`**: Toggle to enable/disable adaptive behavior

**Key Functions**:
- **`Start()`**: 
  - Finds `CameraDisturbance` and `AdaptationController` in the scene
  - Auto-creates `CameraDisturbance` on VR camera if missing (searches for camera under "XR Origin" → "Camera Offset")
  - Auto-creates `AdaptationController` if missing and adaptive system is enabled
- **`Update()`**: Every frame, updates the stress level and notifies all effects
- **`UpdateLevel()`**: Increases/decreases the level based on active triggers
  - Uses `Time.deltaTime` for frame-rate independent calculations
  - Chooses speed based on whether level is above/below threshold
  - Clamps value to 0.0-1.0 range
- **`UpdateAllEffects()`**: 
  - Retrieves adaptation multiplier from `AdaptationController` (if enabled)
  - Applies multiplier to base stress level: `adaptedLevel = overstimulationLevel × multiplier`
  - Clamps `adaptedLevel` to 0.0-1.0 range
  - Distributes the adapted level to all visual/audio effect systems
- **`AddTrigger()` / `RemoveTrigger()`**: Public methods for trigger zones to call when player enters/exits
- **`GetAdaptedLevel()`**: Public method to get the current adapted level (used by AudioController)
- **`GetAdaptationMultiplier()`**: Public method to get the current adaptation multiplier (used by CameraDisturbance to adjust pulse speed)

**Why this design?**
- Single source of truth prevents conflicts between multiple systems
- Centralized level calculation ensures consistent behavior
- Adaptive layer is transparent to effect systems (they just receive adapted level)
- Public methods allow trigger zones to easily signal state changes
- Automatic component finding makes setup easier (no manual references needed)

**Connections**:
- Receives input from: `TriggerZone` (via `AddTrigger`/`RemoveTrigger`), `AdaptationController` (via `GetAdaptationMultiplier`)
- Sends output to: `CameraDisturbance` (via `SetOverstimulationLevel`), `AudioController` (via `GetAdaptedLevel`)

---

### AdaptationController.cs

**Responsibility**: Monitors camera/head movement to detect user discomfort or stability. Provides an adaptation multiplier that adjusts effect intensity in real-time.

**Key Components**:
- **`minIntensityMultiplier`**: Minimum intensity when user shows discomfort (default: 0.8)
- **`maxIntensityMultiplier`**: Maximum intensity when user is stable (default: 1.2)
- **`rapidRotationThreshold`**: Rotation speed threshold for detecting rapid movement (default: 90°/s)
- **`comfortZoneRotation`**: Movement below this is considered normal (default: 30°/s)
- **`currentAdaptationMultiplier`**: Current adaptation multiplier (visible in Inspector for debugging)
- **Movement history**: Queues storing rotation speeds and timestamps for analysis window
- **Fixed values**: `AdaptationSpeed = 1.0`, `AnalysisWindow = 2.0` seconds

**Key Functions**:
- **`Start()`**: 
  - Finds camera/head transform (searches for Player tag, Main Camera, or XR Origin)
  - Initializes tracking variables
  - Disables script if camera not found
- **`Update()`**: 
  - Calculates rotation speed from current and previous rotation
  - Stores rotation speed and timestamp in queues
  - Removes old data outside analysis window
  - Calculates average rotation speed
  - Calculates discomfort level (0.0 to 1.0)
  - Interpolates target adaptation multiplier
  - Smoothly transitions current multiplier towards target
- **`GetAdaptationMultiplier()`**: Returns current adaptation multiplier (called by OverstimulationController)

**Algorithm Details**:
1. **Rotation Speed Calculation**:
   ```
   rotationDelta = Quaternion.Angle(lastRotation, currentRotation)
   rotationSpeed = rotationDelta / deltaTime  // degrees per second
   ```

2. **Discomfort Level**:
   ```
   if (avgSpeed <= comfortZone): discomfort = 0.0
   else if (avgSpeed >= threshold): discomfort = 1.0
   else: discomfort = (avgSpeed - comfortZone) / (threshold - comfortZone)
   ```

3. **Adaptation Multiplier**:
   ```
   targetMultiplier = Lerp(maxMultiplier, minMultiplier, discomfort)
   currentMultiplier = Lerp(currentMultiplier, targetMultiplier, speed * deltaTime)
   ```

**Why this design?**
- Rotation tracking is more reliable than position tracking (less affected by intentional movement)
- Sliding window prevents momentary movements from causing abrupt changes
- Gradual interpolation creates smooth, natural adaptation
- Comfort zone prevents normal exploration from triggering adaptation
- Public multiplier field allows Inspector debugging

**Connections**:
- Receives input from: Camera/head transform (rotation data)
- Sends output to: `OverstimulationController` (via `GetAdaptationMultiplier`)

---

### TriggerZone.cs

**Responsibility**: Detects when the player enters/exits an area and notifies the `OverstimulationController`.

**Key Components**:
- **`ZoneType` enum**: Defines whether the zone increases or decreases stress
- **`zoneName`**: Unique identifier for debugging
- **`playerTag`**: Tag to identify the player GameObject (default: "Player")

**Key Functions**:
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

**Connections**:
- Receives input from: Unity physics system (trigger events)
- Sends output to: `OverstimulationController` (via `AddTrigger`/`RemoveTrigger`)

**Setup Requirements**:
- GameObject must have a Collider component with "Is Trigger" checked
- Player GameObject must have the "Player" tag (or match `playerTag`)

---

### CameraDisturbance.cs

**Responsibility**: Creates a VR vignette overlay effect that simulates tunnel vision (Weak Central Coherence). The effect darkens peripheral vision, forcing focus on central details.

**Key Components**:
- **`maxFlashIntensity`**: Maximum vignette intensity at 100% stress (default: 2.0 for VR visibility)
- **`flashSpeed`**: Base pulse speed for vignette effect (adjusted by adaptation multiplier)
- **`vignetteSize`**: How much of the screen the vignette covers (default: 0.95 = very tight, only small center visible)
- **VR overlay quads**: Two quad meshes (one per eye) positioned very close to camera
- **Vignette texture**: Radial gradient texture (transparent center, opaque edges - no "mark" in center)

**Key Functions**:
- **`Start()`**: 
  - Finds VR camera (searches for camera with "MainCamera" tag under "Camera Offset" parent)
  - Sets camera `nearClipPlane` to 0.01f (critical for VR overlay visibility)
  - Creates VR overlay quads for both eyes
- **`CreateVROverlayForBothEyes()`**: 
  - Generates vignette texture (radial gradient: transparent center, opaque edges)
  - Creates material with "Unlit/Transparent" shader
  - Sets up material for transparency and rendering on top (renderQueue = 5000)
  - Creates two quad meshes (left and right eye)
  - Positions quads at 0.35m from camera with calculated scale (1.5x FOV coverage)
  - Configures renderers (no shadows, proper layer)
- **`CreateVignetteTexture()`**: 
  - Generates a 512x512 texture with radial gradient
  - Uses distance from center to calculate alpha (transparency)
  - `vignetteSize` controls how much of the screen is affected (0.95 = very tight)
  - Center is completely transparent (no "mark" or subtle vignette in center)
  - Uses smooth gradient curve for natural effect
- **`Update()`**: 
  - Gets adaptation multiplier from `OverstimulationController`
  - Adjusts pulse speed based on adaptation multiplier (higher = faster, lower = slower)
  - Calculates vignette intensity using sine wave
  - Uses `Mathf.SmoothStep` for smooth transitions (no steep curves)
  - Updates material alpha based on stress level and pulse
  - Smoothly fades out when level reaches 0
- **`SetOverstimulationLevel()`**: Called by `OverstimulationController` to set current stress level

**Why this design?**
- Direct quad overlay is more reliable in VR than UI Canvas (Built-in pipeline)
- Two quads (one per eye) ensure proper stereo rendering
- Near clip plane adjustment is critical for objects very close to camera
- Large scale multiplier (1.5x) ensures full FOV coverage
- Radial gradient creates realistic tunnel vision effect (transparent center, no "mark")
- Pulse speed adapts to user behavior (faster for stable users, slower for uncomfortable users)
- High render queue ensures overlay renders on top of everything

**Math Details**:
- **Vignette intensity**: `level * maxFlashIntensity * pulseIntensity`
- **Quad scale**: `2 * distance * Tan(FOV/2) * 1.5` (ensures full coverage)
- **Vignette alpha**: Only affects pixels where `normalizedDistance > (1 - vignetteSize)`
- **Sine wave**: Uses standard sine wave calculation for pulsing

**Connections**:
- Receives input from: `OverstimulationController` (via `SetOverstimulationLevel`)
- Sends output to: VR camera overlay (via quad meshes)

**Setup**:
- Can be attached directly to VR camera, or will be auto-created by `OverstimulationController` if missing
- Automatically finds VR camera under "XR Origin" → "Camera Offset"

**VR-Specific Notes**:
- **No blur effect**: Blur is not used in VR (OnRenderImage doesn't work reliably in VR)
- **No desktop support**: This script is VR-only
- **Near clip plane**: Must be 0.01f or lower for overlay to be visible

---

### AudioController.cs

**Responsibility**: Manages audio for the overstimulation system. Background noise ducks (gets quieter) as stress increases, while rising sounds increase in volume and pitch.

**Key Components**:
- **`backgroundAudioSource`**: AudioSource for background ambient sounds
- **`risingAudioSource`**: AudioSource for rising intensity sounds
- **`backgroundSound`**: Audio clip for background noise (loops continuously)
- **`risingSound`**: Audio clip that increases with stress
- **Volume ranges**: `backgroundMaxVolume`, `backgroundMinVolume`, `risingMinVolume`, `risingMaxVolume`
- **`risingMaxPitch`**: Maximum pitch at 100% stress (default: 1.2 = 20% higher)
- **`volumeSmoothing`**: How fast volume changes (prevents jarring audio jumps)

**Key Functions**:
- **`Start()`**: 
  - Finds `OverstimulationController` in the scene
  - Auto-creates audio sources if not assigned
  - Sets up background audio (loops, starts playing)
  - Sets up rising audio (loops, starts playing but silent)
- **`Update()`**: 
  - Gets adapted overstimulation level from `OverstimulationController.GetAdaptedLevel()`
  - Calculates target volumes for both audio sources
  - Smoothly interpolates current volumes towards targets
  - Updates rising audio pitch based on stress level

**Audio Behavior**:
- **Background Sound**: 
  - Volume decreases as stress increases (ducking effect)
  - Formula: `volume = Lerp(backgroundMaxVolume, backgroundMinVolume, level)`
  - At level 0.0: Volume = `backgroundMaxVolume` (loud)
  - At level 1.0: Volume = `backgroundMinVolume` (quiet)
  
- **Rising Sound**:
  - Volume increases as stress increases
  - Pitch increases as stress increases (higher = more intense)
  - Formula: `volume = Lerp(risingMinVolume, risingMaxVolume, level)`
  - Formula: `pitch = Lerp(1.0, risingMaxPitch, level)`
  - At level 0.0: Volume = `risingMinVolume` (silent), Pitch = 1.0 (normal)
  - At level 1.0: Volume = `risingMaxVolume` (loud), Pitch = `risingMaxPitch` (higher)

**Why this design?**
- Uses adapted level to ensure audio responds to user behavior
- Smooth volume transitions prevent jarring audio jumps
- Ducking effect creates sense of being overwhelmed (background fades)
- Rising pitch adds urgency and intensity
- Automatic audio source creation simplifies setup

**Connections**:
- Receives input from: `OverstimulationController` (via `GetAdaptedLevel`)
- Sends output to: Audio system (via AudioSource components)

---

## Technical Details

### Stress Level Calculation

The base stress level is calculated using a two-speed threshold system:

```
if (activeTriggers > 0):
    if (level < threshold):
        level += fastIncreaseSpeed * deltaTime
    else:
        level += slowIncreaseSpeed * deltaTime
    level = Clamp01(level)
else:
    level -= decreaseSpeed * deltaTime
    level = Clamp01(level)
```

**Rationale**: Fast initial buildup simulates quick stress response, while slow increase above threshold simulates gradual escalation to maximum.

### Adaptation Application

The adaptation multiplier is applied to the base level:

```
adaptedLevel = overstimulationLevel × adaptationMultiplier
adaptedLevel = Clamp01(adaptedLevel)
```

**Important**: Because `adaptedLevel` is clamped to 0.0-1.0:
- A 1.2x multiplier doesn't make effects stronger than 100% - it makes them reach 100% faster (when base level is 0.83)
- A 0.8x multiplier means effects only reach 80% of maximum intensity

### Effect Scaling

All effects use the adapted level but scale differently:

**Camera Vignette**:
```
adjustedFlashSpeed = flashSpeed / adaptationMultiplier
pulseIntensity = sin(timer / adjustedFlashSpeed * PI * 2)
vignetteIntensity = level * maxFlashIntensity * pulseIntensity
```

**Audio**:
```
backgroundVolume = Lerp(maxVolume, minVolume, level)
risingVolume = Lerp(minVolume, maxVolume, level)
risingPitch = Lerp(1.0, maxPitch, level)
```

### Frame-Rate Independence

All time-based calculations use `Time.deltaTime`:
- Level changes: `level += speed * Time.deltaTime`
- Adaptation smoothing: `Lerp(current, target, speed * Time.deltaTime)`
- Audio smoothing: `Lerp(current, target, smoothing * Time.deltaTime)`

This ensures consistent behavior at 30 FPS, 60 FPS, or 120 FPS.

### Synchronization

All pulsing effects use the same sine wave calculation, but pulse speed is adjusted by adaptation multiplier:

```
adjustedPulseSpeed = basePulseSpeed / adaptationMultiplier
t = timer / adjustedPulseSpeed
pulseIntensity = (sin(t * PI * 2) + 1) / 2  // Convert to 0-1 range
```

Pulse speed adapts to user behavior:
- **Stable users (1.2x)**: Pulse 20% faster (more intense)
- **Uncomfortable users (0.8x)**: Pulse 25% slower (more calm)



---
