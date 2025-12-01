using UnityEngine;

// OverstimulationController is the central controller that manages the stress level and coordinates all visual/audio effects.
// Integrates the adaptive system to personalize the experience.
public class OverstimulationController : MonoBehaviour
{
    [Header("Overstimulation Level")]
    [Tooltip("The core stress value (0.0 to 1.0) that all effects read from")]
    [Range(0f, 1f)]
    public float overstimulationLevel = 0f;

    [Header("Adaptive System (Read-Only)")]
    [Tooltip("The stress level after applying VR behavior adaptation (visible in Inspector)")]
    [Range(0f, 1f)]
    public float adaptedLevel = 0f;

    [Tooltip("Current adaptation multiplier from AdaptationController (visible in Inspector)")]
    public float currentAdaptationMultiplier = 1.0f;

    [Header("Level Change Speed")]
    [Tooltip("Threshold level (0-1). Below this, level increases FAST. Above this, level increases SLOW.")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Tooltip("FAST increase speed (per second) - used when level is BELOW the threshold")]
    public float fastIncreaseSpeed = 0.1f;

    [Tooltip("SLOW increase speed (per second) - used when level is ABOVE the threshold")]
    public float slowIncreaseSpeed = 0.05f;

    [Tooltip("How fast the level decreases when triggers are removed (per second)")]
    public float decreaseSpeed = 0.1f;

    [Header("Adaptive Behavior")]
    [Tooltip("Toggle to enable/disable adaptive behavior")]
    public bool enableAdaptiveIntensity = true;

    private CameraDisturbance cameraDisturbance;
    private AdaptationController adaptationController;
    private int activeTriggers = 0;

    void Start()
    {
        // Auto-creates CameraDisturbance on VR camera if missing
        cameraDisturbance = FindObjectOfType<CameraDisturbance>();
        if (cameraDisturbance == null)
        {
            // Searches for camera under "XR Origin" → "Camera Offset"
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                Camera vrCamera = xrOrigin.GetComponentInChildren<Camera>();
                if (vrCamera != null)
                {
                    cameraDisturbance = vrCamera.gameObject.AddComponent<CameraDisturbance>();
                }
            }
        }

        // Auto-creates AdaptationController if missing and adaptive system is enabled
        if (enableAdaptiveIntensity)
        {
            adaptationController = FindObjectOfType<AdaptationController>();
            if (adaptationController == null)
            {
                GameObject trackerObj = new GameObject("AdaptationController");
                adaptationController = trackerObj.AddComponent<AdaptationController>();
            }
        }
    }

    void Update()
    {
        // Every frame, updates the stress level and notifies all effects
        UpdateLevel();
        UpdateAllEffects();
    }

    // Increases/decreases the level based on active triggers
    void UpdateLevel()
    {
        if (activeTriggers > 0)
        {
            // Chooses speed based on whether level is above/below threshold
            float currentSpeed = overstimulationLevel < threshold ? fastIncreaseSpeed : slowIncreaseSpeed;
            
            // Uses Time.deltaTime for frame-rate independent calculations
            overstimulationLevel += currentSpeed * Time.deltaTime;
            
            // Clamps value to 0.0-1.0 range
            overstimulationLevel = Mathf.Clamp01(overstimulationLevel);
        }
        else
        {
            overstimulationLevel -= decreaseSpeed * Time.deltaTime;
            overstimulationLevel = Mathf.Clamp01(overstimulationLevel);
        }
    }

    // Retrieves adaptation multiplier and applies it to base stress level
    void UpdateAllEffects()
    {
        float adaptationMultiplier = 1.0f;
        
        // Retrieves adaptation multiplier from AdaptationController (if enabled)
        if (enableAdaptiveIntensity && adaptationController != null)
        {
            adaptationMultiplier = adaptationController.GetAdaptationMultiplier();
        }

        currentAdaptationMultiplier = adaptationMultiplier;

        // Applies multiplier to base stress level: adaptedLevel = overstimulationLevel × multiplier
        adaptedLevel = overstimulationLevel * adaptationMultiplier;
        
        // Clamps adaptedLevel to 0.0-1.0 range
        adaptedLevel = Mathf.Clamp01(adaptedLevel);

        // Distributes the adapted level to all visual/audio effect systems
        if (cameraDisturbance != null)
        {
            cameraDisturbance.SetOverstimulationLevel(adaptedLevel);
        }
    }

    // Public methods for trigger zones to call when player enters/exits
    public void AddTrigger(string triggerName)
    {
        activeTriggers++;
    }

    public void RemoveTrigger(string triggerName)
    {
        activeTriggers = Mathf.Max(0, activeTriggers - 1);
    }

    // Public method to get the current adapted level (used by AudioController)
    public float GetAdaptedLevel()
    {
        return adaptedLevel;
    }

    public float GetLevel()
    {
        return overstimulationLevel;
    }

    // Public method to get the current adaptation multiplier (used by effects to adjust pulse speed)
    public float GetAdaptationMultiplier()
    {
        return currentAdaptationMultiplier;
    }
}
