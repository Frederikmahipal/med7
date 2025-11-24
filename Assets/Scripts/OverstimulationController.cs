using UnityEngine;

/// OverstimulationController is the main controller for the adaptive overstimulation system.
/// It tracks a "stress level" from 0.0 (calm) to 1.0 (maximum overstimulation).
/// All visual effects (lamps, camera blur, etc.) scale based on this level.
/// 
/// HOW IT WORKS:
/// - The level starts at 0.0 (no overstimulation)
/// - When triggers are active (like being in a queue), the level increases
/// - When triggers are removed, the level decreases back to 0.0
/// - All effects automatically scale: 0.0 = normal, 1.0 = maximum intensity
/// 
/// This script should be attached to an empty GameObject in the scene.

public class OverstimulationController : MonoBehaviour
{
    [Header("Overstimulation Level")]
    [Tooltip("Current stress level: 0.0 = calm, 1.0 = maximum overstimulation. This value changes automatically based on triggers.")]
    [Range(0f, 1f)]
    public float overstimulationLevel = 0f;

    [Header("Level Change Speed")]
    [Tooltip("Threshold level (0-1). Below this, level increases FAST. Above this, level increases SLOW. Example: 0.5 = fast until 50%, then slow.")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Tooltip("FAST increase speed (per second) - used when level is BELOW the threshold. Lower = slower. Example: 0.1 = takes 5 seconds to reach threshold.")]
    public float fastIncreaseSpeed = 0.1f;

    [Tooltip("SLOW increase speed (per second) - used when level is ABOVE the threshold. Lower = slower. Example: 0.05 = takes 10 seconds to go from threshold to max.")]
    public float slowIncreaseSpeed = 0.05f;

    [Tooltip("How fast the level decreases when triggers are removed (per second). Lower = slower. Example: 0.1 = takes 10 seconds to go from 1.0 to 0.0.")]
    public float decreaseSpeed = 0.1f;

    // References to other systems that need to know the level
    private LampController lampController;
    private CameraDisturbance cameraDisturbance;

    // Tracks how many triggers are currently active
    // If this is > 0, level increases. If 0, level decreases.
    private int activeTriggers = 0;

    void Start()
    {
        // Find the LampController so we can tell it about level changes
        lampController = FindObjectOfType<LampController>();
        if (lampController == null)
        {
            Debug.LogWarning("OverstimulationController: No LampController found in scene! " +
                           "Make sure you have a GameObject with LampController script in your scene.");
        }

        // Find the CameraDisturbance so we can tell it about level changes
        cameraDisturbance = FindObjectOfType<CameraDisturbance>();
        if (cameraDisturbance == null)
        {
            // Find the player GameObject by tag (same tag used by TriggerZone)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                // Get the Camera component from the player
                Camera playerCamera = playerObj.GetComponent<Camera>();
                if (playerCamera != null)
                {
                    // Create CameraDisturbance on the player's camera
                    cameraDisturbance = playerCamera.gameObject.AddComponent<CameraDisturbance>();
                }
            }
        }
    }

    void Update()
    {
        // Update the level based on active triggers
        UpdateLevel();

        // Tell all systems about the new level
        UpdateAllEffects();
    }

    /// Updates the overstimulation level based on active triggers.
    /// If triggers are active, level increases (fast before threshold, slow after).
    /// If not, level decreases.
    void UpdateLevel()
    {
        if (activeTriggers > 0)
        {
            // Triggers are active - increase the level
            
            // Choose speed based on whether we're above or below the threshold
            // Below threshold: use fast speed (quickly builds up initial stress)
            // Above threshold: use slow speed (slowly builds to maximum - more realistic)
            float currentSpeed;
            if (overstimulationLevel < threshold)
            {
                // We're below the threshold - increase FAST
                currentSpeed = fastIncreaseSpeed;
            }
            else
            {
                // We're above the threshold - increase SLOW
                currentSpeed = slowIncreaseSpeed;
            }

            // Increase the level by the chosen speed
            // Time.deltaTime is the time since last frame (usually ~0.016 seconds)
            // currentSpeed is how much to add per second
            // So we add: currentSpeed * Time.deltaTime per frame
            overstimulationLevel += currentSpeed * Time.deltaTime;

            // Clamp to maximum of 1.0 (can't go above 100%)
            overstimulationLevel = Mathf.Clamp01(overstimulationLevel);
        }
        else
        {
            // No triggers active - decrease the level back to 0
            // This always uses the same decrease speed (no threshold for decreasing)
            overstimulationLevel -= decreaseSpeed * Time.deltaTime;

            // Clamp to minimum of 0.0 (can't go below 0%)
            overstimulationLevel = Mathf.Clamp01(overstimulationLevel);
        }
    }

    /// Tells all visual effects about the current level.
    /// This makes lamps brighter, camera blur stronger, etc. based on the level.
    void UpdateAllEffects()
    {
        // Tell LampController about the current level
        // It will make lamps brighter based on this value (0.0 = normal, 1.0 = max brightness)
        lampController?.SetOverstimulationLevel(overstimulationLevel);

        // Tell CameraDisturbance about the current level
        // It will make blur/flash stronger based on this value
        if (cameraDisturbance != null)
        {
            cameraDisturbance.SetOverstimulationLevel(overstimulationLevel);
        }
        else
        {
            // Try to find it again (in case it was created after Start)
            cameraDisturbance = FindObjectOfType<CameraDisturbance>();
            
            // Try to create it if still missing
            if (cameraDisturbance == null && Time.frameCount % 60 == 0) // Only try once per second
            {
                // Find the player GameObject by tag and get its Camera component
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    Camera playerCamera = playerObj.GetComponent<Camera>();
                    if (playerCamera != null)
                    {
                        cameraDisturbance = playerCamera.gameObject.AddComponent<CameraDisturbance>();
                    }
                }
            }
        }
    }

    /// Call this when a trigger becomes active (e.g., player enters queue zone).
    /// Each trigger should have a unique name for debugging.
    /// Example: AddTrigger("QueueZone") when player enters queue
    public void AddTrigger(string triggerName)
    {
        activeTriggers++;
    }

    /// Call this when a trigger becomes inactive (e.g., player leaves queue zone).
    public void RemoveTrigger(string triggerName)
    {
        activeTriggers = Mathf.Max(0, activeTriggers - 1); // Can't go below 0
    }

    /// Get the current overstimulation level (0.0 to 1.0).
    public float GetLevel()
    {
        return overstimulationLevel;
    }
}

