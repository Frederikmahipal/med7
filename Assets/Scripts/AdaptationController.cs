using UnityEngine;
using System.Collections.Generic;

// AdaptationController monitors camera/head movement to detect user discomfort/stability.
// Adapts effect intensity based on user behavior: stable users get increased intensity (1.2x),
// uncomfortable users get reduced intensity (0.8x).
public class AdaptationController : MonoBehaviour
{
    [Header("Adaptation Settings")]
    [Tooltip("Minimum intensity when user shows discomfort (rapid movements).")]
    [Range(0.5f, 1.0f)]
    public float minIntensityMultiplier = 0.8f;
    
    [Tooltip("Maximum intensity when user is stable (no rapid movements).")]
    [Range(1.0f, 1.5f)]
    public float maxIntensityMultiplier = 1.2f;
    
    [Header("Movement Detection")]
    [Tooltip("Rotation speed threshold for detecting rapid movement (degrees per second).")]
    public float rapidRotationThreshold = 90f;
    
    [Tooltip("Comfort zone, movement below this is considered normal (degrees per second).")]
    public float comfortZoneRotation = 30f;
    
    [Header("Runtime (Read-Only)")]
    [Tooltip("Current adaptation multiplier, shows if system is adapting.")]
    public float currentAdaptationMultiplier = 1.0f;
    
    // Fixed values
    private const float AdaptationSpeed = 1.0f;
    private const float AnalysisWindow = 2.0f;
    
    // Current adaptation multiplier (public for Inspector display)
    private float targetAdaptationMultiplier = 1.0f;
    
    // Tracking
    private Transform headTransform;
    private Quaternion lastRotation;
    private float lastUpdateTime;
    
    // Movement history
    private Queue<float> rotationSpeeds = new Queue<float>();
    private Queue<float> timestamps = new Queue<float>();
    
    // Current analysis values (for logging/external access)
    private float currentAvgRotationSpeed = 0f;
    private float currentDiscomfortLevel = 0f;
    
    void Start()
    {
        // Find camera/head transform
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Camera playerCamera = playerObj.GetComponentInChildren<Camera>();
            headTransform = playerCamera != null ? playerCamera.transform : playerObj.transform;
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                headTransform = mainCam.transform;
            }
            else
            {
                GameObject xrOrigin = GameObject.Find("XR Origin");
                if (xrOrigin != null)
                {
                    Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>();
                    if (xrCamera != null)
                    {
                        headTransform = xrCamera.transform;
                    }
                }
            }
        }
        
        if (headTransform == null)
        {
            Debug.LogWarning("Could not find transform");
            return;
        }
        
        lastRotation = headTransform.rotation;
        lastUpdateTime = Time.time;
    }
    
    void Update()
    {
        if (headTransform == null) return;
        
        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;
        if (deltaTime <= 0) return;
        
        // Calculate rotation speed
        float rotationDelta = Quaternion.Angle(lastRotation, headTransform.rotation);
        float rotationSpeed = rotationDelta / deltaTime;
        
        // Debug: Log if rotation is detected (only first few times to avoid spam)
        if (rotationSpeed > 1f && rotationSpeeds.Count < 3)
        {
            Debug.Log($"AdaptationController: Rotation detected! Speed: {rotationSpeed:F2} deg/s");
        }
        
        // Store in history
        rotationSpeeds.Enqueue(rotationSpeed);
        timestamps.Enqueue(currentTime);
        
        // Remove old data
        while (timestamps.Count > 0 && currentTime - timestamps.Peek() > AnalysisWindow)
        {
            rotationSpeeds.Dequeue();
            timestamps.Dequeue();
        }
        
        // Analyze behavior and update target multiplier
        if (rotationSpeeds.Count > 0)
        {
            float avgRotationSpeed = 0f;
            foreach (float speed in rotationSpeeds)
            {
                avgRotationSpeed += speed;
            }
            avgRotationSpeed /= rotationSpeeds.Count;
            currentAvgRotationSpeed = avgRotationSpeed; // Store for external access
            
            // Calculate discomfort level (0.0 = no discomfort, 1.0 = maximum)
            float discomfortLevel = 0f;
            if (avgRotationSpeed > comfortZoneRotation)
            {
                discomfortLevel = Mathf.Clamp01((avgRotationSpeed - comfortZoneRotation) / (rapidRotationThreshold - comfortZoneRotation));
            }
            currentDiscomfortLevel = discomfortLevel; // Store for external access
            
            // Interpolate multiplier: 0.0 discomfort = max (1.2), 1.0 discomfort = min (0.8)
            targetAdaptationMultiplier = Mathf.Lerp(maxIntensityMultiplier, minIntensityMultiplier, discomfortLevel);
        }
        
        // Smoothly adjust towards target
        currentAdaptationMultiplier = Mathf.Lerp(
            currentAdaptationMultiplier,
            targetAdaptationMultiplier,
            AdaptationSpeed * Time.deltaTime
        );
        
        // Update for next frame
        lastRotation = headTransform.rotation;
        lastUpdateTime = currentTime;
    }
    
    // Get the current adaptation multiplier (0.8 to 1.2).
    public float GetAdaptationMultiplier()
    {
        return currentAdaptationMultiplier;
    }
    
    // Get the current average head rotation speed (degrees per second).
    public float GetAverageRotationSpeed()
    {
        return currentAvgRotationSpeed;
    }
    
    // Get the current discomfort level (0.0 = no discomfort, 1.0 = maximum discomfort).
    public float GetDiscomfortLevel()
    {
        return currentDiscomfortLevel;
    }
}
