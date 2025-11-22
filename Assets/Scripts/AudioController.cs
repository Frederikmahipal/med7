using UnityEngine;

/// AudioController manages audio for the overstimulation system.
/// - Background noise: Loops continuously, but gets QUIETER as stress increases (ducking effect)
/// - Rising sound: Volume/intensity INCREASES with overstimulation level (takes over)

public class AudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource for background noise (loops continuously). If not assigned, will be created automatically.")]
    public AudioSource backgroundAudioSource;
    
    [Tooltip("AudioSource for rising sound (intensity changes with overstimulation). If not assigned, will be created automatically.")]
    public AudioSource risingAudioSource;

    [Header("Background Sound (Loops)")]
    [Tooltip("Audio clip for background noise (people talking, ambient sounds). This will loop continuously.")]
    public AudioClip backgroundSound;
    
    [Tooltip("Maximum volume when overstimulation = 0.0 (calm). Background is loudest when stress is low.")]
    [Range(0f, 1f)]
    public float backgroundMaxVolume = 0.5f;
    
    [Tooltip("Minimum volume when overstimulation = 1.0 (maximum). Background gets quieter as stress increases.")]
    [Range(0f, 1f)]
    public float backgroundMinVolume = 0.1f;

    [Header("Rising Sound (Scales with Overstimulation)")]
    [Tooltip("Audio clip that gets more intense as overstimulation increases. Can be same as background or different.")]
    public AudioClip risingSound;
    
    [Tooltip("Minimum volume when overstimulation = 0.0 (calm).")]
    [Range(0f, 1f)]
    public float risingMinVolume = 0f;
    
    [Tooltip("Maximum volume when overstimulation = 1.0 (maximum).")]
    [Range(0f, 1f)]
    public float risingMaxVolume = 0.8f;
    
    public float risingMaxPitch = 1.2f;
    
    [Tooltip("Should the rising sound loop?")]
    public bool risingSoundLoops = true;

    [Header("Smoothing")]
    [Tooltip("How fast the rising sound volume changes (higher = faster response). Prevents jarring audio jumps.")]
    [Range(0.1f, 10f)]
    public float volumeSmoothing = 2f;

    // Reference to OverstimulationController to get the current level of overstimulation
    private OverstimulationController overstimulationController;
    
    // Current smoothed volumes 
    private float currentRisingVolume = 0f;
    private float currentBackgroundVolume = 0f;

    void Start()
    {
        // Find the OverstimulationController
        overstimulationController = FindObjectOfType<OverstimulationController>();
        
        // Setup background audio
        backgroundAudioSource.clip = backgroundSound;
        backgroundAudioSource.loop = true;
        backgroundAudioSource.volume = backgroundMaxVolume; // Start at max volume (no stress)
        currentBackgroundVolume = backgroundMaxVolume;
        backgroundAudioSource.playOnAwake = true;
        
        // Start playing background sound
        if (backgroundSound != null)
        {
            backgroundAudioSource.Play();
        }
      
        // Setup rising audio
        risingAudioSource.clip = risingSound;
        risingAudioSource.loop = risingSoundLoops;
        risingAudioSource.volume = 0f; // Start silent
        risingAudioSource.playOnAwake = false;
        
        // Start playing rising sound (it will be silent at first)
        if (risingSound != null)
        {
            risingAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioController: No rising sound assigned! Rising audio won't play.");
        }
    }

    void Update()
    {
        if (overstimulationController != null)
        {
            // Get current overstimulation level (0.0 to 1.0)
            float overstimulationLevel = overstimulationController.GetLevel();
            
            // Update RISING SOUND: Gets louder as stress increases
            if (risingSound != null)
            {
                // Level 0.0 → risingMinVolume, Level 1.0 → risingMaxVolume
                float targetRisingVolume = Mathf.Lerp(risingMinVolume, risingMaxVolume, overstimulationLevel);
                
                // Smoothly transition to target volume (prevents jarring audio jumps)
                currentRisingVolume = Mathf.Lerp(currentRisingVolume, targetRisingVolume, volumeSmoothing * Time.deltaTime);
                risingAudioSource.volume = currentRisingVolume;
                
                // Level 0.0 → pitch 1.0 (normal), Level 1.0 → risingMaxPitch
                float targetPitch = Mathf.Lerp(1f, risingMaxPitch, overstimulationLevel);
                risingAudioSource.pitch = targetPitch;
            }
            
            // Update BACKGROUND SOUND: Gets quieter as stress increases 
            if (backgroundSound != null && backgroundAudioSource != null)
            {
                
                // Level 0.0 → backgroundMaxVolume (loud), Level 1.0 → backgroundMinVolume (quiet)
                float targetBackgroundVolume = Mathf.Lerp(backgroundMaxVolume, backgroundMinVolume, overstimulationLevel);
                
                // Smoothly transition to target volume
                currentBackgroundVolume = Mathf.Lerp(currentBackgroundVolume, targetBackgroundVolume, volumeSmoothing * Time.deltaTime);
                backgroundAudioSource.volume = currentBackgroundVolume;
            }
        }
    }
}

