using UnityEngine;

// AudioController manages audio for the overstimulation system.
// - Background noise: Loops continuously, but gets quieter as stress increases (ducking effect)
// - Rising sound: Volume/intensity increases with overstimulation level (takes over)

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
        
        // Auto-create background audio source if not assigned
        if (backgroundAudioSource == null)
        {
            backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
      
        // Auto-create rising audio source if not assigned
        if (risingAudioSource == null)
        {
            risingAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup rising audio
        risingAudioSource.clip = risingSound;
        risingAudioSource.loop = risingSoundLoops;
        risingAudioSource.volume = 0f; // Start silent
        risingAudioSource.playOnAwake = false;
        
        // Start playing rising sound (silent at start)
        if (risingSound != null)
        {
            risingAudioSource.Play();
        }
   
    }
    void Update()
    {
        if (overstimulationController != null)
        {
            // Get current overstimulation level (0.0 to 1.0)
            float overstimulationLevel = overstimulationController.GetLevel();
            
            // Gets louder as stress increases
            if (risingSound != null && risingAudioSource != null)
            {
                // Mathf.Lerp(start, end, percentage) = calculates a value between start and end
                // If overstimulationLevel = 0.0 -> returns risingMinVolume (0.0 = silent)
                // If overstimulationLevel = 1.0 -> returns risingMaxVolume (0.8 = 80% volume)
                // If overstimulationLevel = 0.5 -> returns halfway between min and max (0.4 = 40% volume)
                float targetRisingVolume = Mathf.Lerp(risingMinVolume, risingMaxVolume, overstimulationLevel);
                
                // Smoothly move current volume towards target volume
                // Time.deltaTime = time since last frame 
                // volumeSmoothing * Time.deltaTime = how fast to move 
                // This prevents sudden jumps - volume changes gradually over time
                currentRisingVolume = Mathf.Lerp(currentRisingVolume, targetRisingVolume, volumeSmoothing * Time.deltaTime);
                risingAudioSource.volume = currentRisingVolume;
                
                // Calculate pitch (how high/low the sound is)
                // If overstimulationLevel = 0.0 -> pitch = 1.0 (normal speed/pitch)
                // If overstimulationLevel = 1.0 -> pitch = 1.2 (20% faster/higher pitch)
                // Higher pitch = more intense/urgent feeling
                float targetPitch = Mathf.Lerp(1f, risingMaxPitch, overstimulationLevel);
                risingAudioSource.pitch = targetPitch;
            }
            
            // Background sound gets quieter as stress increases 
            if (backgroundSound != null && backgroundAudioSource != null)
            {
                // Calculate target volume - INVERSE of stress (opposite of rising sound)
                // If overstimulationLevel = 0.0 -> returns backgroundMaxVolume (0.5 = 50% volume, loud)
                // If overstimulationLevel = 1.0 -> returns backgroundMinVolume (0.1 = 10% volume, quiet)
                // As stress increases, background gets quieter (ducking effect)
                float targetBackgroundVolume = Mathf.Lerp(backgroundMaxVolume, backgroundMinVolume, overstimulationLevel);
                
                // Smoothly move current volume towards target volume (same as rising sound)
                // Prevents sudden volume jumps - changes gradually
                currentBackgroundVolume = Mathf.Lerp(currentBackgroundVolume, targetBackgroundVolume, volumeSmoothing * Time.deltaTime);
                backgroundAudioSource.volume = currentBackgroundVolume;
            }
        }
    }
}

