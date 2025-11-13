using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Dynamic;

/// CameraDisturbance creates a visual flash overlay effect on the screen that syncs with lamp blinking.
/// This simulates overstimulation by adding visual disturbance to the player's view.
/// The effect uses a UI Canvas with a white overlay image that pulses in sync with the lamps.

public class CameraDisturbance : MonoBehaviour
{
    [Tooltip("Controls how bright the white flash overlay gets. Lower values = more subtle effect. Range 0-1.")]
    public float flashIntensity = 0.1f;
    
    [Tooltip("How fast the flash pulses (in seconds). Should match the lamp blink speed for synchronization.")]
    public float flashSpeed = 0.5f;
    private Camera mainCamera;
    private bool isDisturbing = false;
    private float timer = 0f;
    private Image flashOverlay; // The white image that covers the screen
    private Canvas canvas; // The UI canvas that holds the overlay
    
    void Start()
    {
        // Get reference to the camera component on this GameObject
        // This allows the script to work when attached directly to a camera
        mainCamera = GetComponent<Camera>();
        
        // Fallback: if no camera on this object, use the main camera
        // This ensures it works even if attached to a different object
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Create the UI overlay system that will display the flash effect
        // We do this in Start() so it's ready before Update() runs
        CreateFlashOverlay();
    }
    

    /// Creates a UI Canvas and Image overlay to display the flash effect.
    /// The canvas is set to ScreenSpaceOverlay so it renders on top of everything.
    void CreateFlashOverlay()
    {
        // Create a new GameObject to hold the Canvas component
        GameObject canvasObj = new GameObject("FlashCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        
        // ScreenSpaceOverlay means the canvas renders on top of the 3D scene
        // This ensures our flash effect appears above everything else
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // High sorting order ensures this canvas renders on top of any other UI
        // This prevents other UI elements from covering our flash effect
        canvas.sortingOrder = 1000;
        
        // CanvasScaler makes the UI scale properly on different screen sizes
        // Without this, the overlay might not cover the full screen on different resolutions
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Reference resolution for scaling
        
        // GraphicRaycaster is needed for the Canvas to work properly
        // It handles input/raycasting for UI elements (even though we don't use it)
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create the actual flash overlay - a white image that covers the screen
        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(canvas.transform, false);
        
        // Image component displays a colored rectangle
        flashOverlay = flashObj.AddComponent<Image>();
        flashOverlay.color = new Color(1, 1, 1, 0); // White color, fully transparent (alpha = 0)
        
        // Configure the RectTransform to cover the entire screen
        // RectTransform is like Transform but for UI elements
        RectTransform rect = flashOverlay.rectTransform;
        
        // anchorMin and anchorMax set where the edges are anchored
        // Vector2.zero = bottom-left corner, Vector2.one = top-right corner
        // This makes the image stretch to fill the entire screen
        rect.anchorMin = Vector2.zero; // Anchor to bottom-left
        rect.anchorMax = Vector2.one;  // Anchor to top-right
        
        // sizeDelta = 0 means "stretch to fill the space between anchors"
        // anchoredPosition = 0 means "centered on the anchors"
        // Together, this makes the image cover the full screen
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        // Only update the flash effect if it's active and the overlay exists
        if (isDisturbing && flashOverlay != null)
        {
            // Increment timer by the time since last frame
            timer += Time.deltaTime;
            
            // Calculate flash intensity using a sine wave
            // This creates a smooth, repeating pulse pattern

            // Step 1: Normalize timer to a 0-1 range based on flashSpeed
            float t = timer / flashSpeed;
            
            // Mathf.Sin(t * Mathf.PI * 2) gives values from -1 to 1
            // Multiplying by PI*2 makes one complete cycle (0 to 2π radians)
            // * 0.5f + 0.5f converts the range from [-1, 1] to [0, 1]
            // This gives us a smooth curve that goes from 0 to 1 and back
            float intensity = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f;
            
            // Mathf.Pow(intensity, 1.5f) makes the curve less linear
            // Higher power values = softer curve (more gradual transitions)
            intensity = Mathf.Pow(intensity, 1.5f);
            
            // Step 4: Scale by flashIntensity to control overall brightness
            // flashIntensity is a multiplier (0.1 = 10% of full brightness)
            // This allows us to make the effect subtle or intense
            float flashAmount = intensity * flashIntensity;
            
            // Update the overlay's transparency (alpha channel)
            // Alpha = 0 is fully transparent (invisible)
            // Alpha = flashAmount makes it visible based on our calculated intensity
            Color flashColor = flashOverlay.color;
            flashColor.a = flashAmount; // Set alpha to our calculated flash amount
            flashOverlay.color = flashColor; // Apply the new color
        }
        else if (flashOverlay != null)
        {
            // When blinking stops, smoothly fade out the overlay
            // This prevents an abrupt cut-off when the effect stops
            Color flashColor = flashOverlay.color;
            
            // Lerp (linear interpolation) smoothly transitions from current alpha to 0
            // Time.deltaTime * 2f controls fade speed (2 = fades out in ~0.5 seconds)
            // This creates a smooth fade-out effect
            flashColor.a = Mathf.Lerp(flashColor.a, 0, Time.deltaTime * 2f);
            flashOverlay.color = flashColor;
        }
    }
    

    /// Called by LampController to start/stop the disturbance effect.
    /// This keeps the camera effect synchronized with the lamp blinking.
    public void SetDisturbance(bool disturbing)
    {
        isDisturbing = disturbing;
        
        // Reset timer when stopping so it starts fresh next time
        // This ensures the flash always starts from the beginning of the cycle
        if (!disturbing)
        {
            timer = 0f;
        }
    }
}

