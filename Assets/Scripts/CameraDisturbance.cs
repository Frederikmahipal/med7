using UnityEngine;
using UnityEngine.UI;

/// CameraDisturbance creates a visual flash overlay effect on the screen that syncs with lamp blinking.
/// This simulates overstimulation by adding visual disturbance to the player's view.
/// The effect uses a UI Canvas with a white overlay image that pulses in sync with the lamps.

public class CameraDisturbance : MonoBehaviour
{
    [Header("Flash/Vignette Settings")]
    [Tooltip("Maximum flash intensity when overstimulation is at 100% (1.0). Lower values = more subtle effect. Range 0-1.")]
    public float maxFlashIntensity = 0.1f;

    [Tooltip("How fast the flash pulses (in seconds). Should match the lamp pulse speed for synchronization.")]
    public float flashSpeed = 0.5f;

    [Tooltip("How much of the screen the vignette covers. Higher = more edge coverage. Range 0-1.")]
    public float vignetteSize = 0.3f;

    [Header("Blur Settings")]
    [Tooltip("Maximum blur intensity when overstimulation is at 100% (1.0). Higher = more blur.")]
    public float maxBlurIntensity = 2f;

    [Tooltip("How much of the screen to blur (0-1).")]
    public float blurCoverage = 0.5f; // 0.5 = blur half the screen

    private Camera mainCamera;
    private float currentLevel = 0f; // Current overstimulation level (0.0 to 1.0)
    private float timer = 0f;
    private Image flashOverlay; // The white image that covers the screen
    private Canvas canvas; // The UI canvas that holds the overlay

    void Start()
    {
        // Get reference to the camera component on this GameObject
        // This allows the script to work when attached directly to a camera
        mainCamera = GetComponent<Camera>();

        // Fallback: if no camera on this object, use the main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Create the UI overlay system that will display the flash effect
        // We do this in Start() so it's ready before Update() runs
        CreateFlashOverlay();

        // Setup blur effect
        SetupBlur();
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

        // Create a radial gradient texture for vignette effect
        // This makes the flash only appear at the edges, not the center
        Texture2D vignetteTexture = CreateVignetteTexture(256, 256);
        Sprite vignetteSprite = Sprite.Create(vignetteTexture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        flashOverlay.sprite = vignetteSprite;
        flashOverlay.type = Image.Type.Simple; // Use simple image type

        // Configure the RectTransform to cover the entire screen
        RectTransform rect = flashOverlay.rectTransform;
        rect.anchorMin = Vector2.zero; // Anchor to bottom-left
        rect.anchorMax = Vector2.one;  // Anchor to top-right
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        // Only update the flash effect if level is above 0 and the overlay exists
        if (currentLevel > 0.001f && flashOverlay != null)
        {
            // Increment timer by the time since last frame
            timer += Time.deltaTime;

            // Calculate flash intensity using a sine wave
            // This creates a smooth, repeating pulse pattern
            float t = timer / flashSpeed;

            // Mathf.Sin(t * Mathf.PI * 2) gives values from -1 to 1
            // Multiplying by PI*2 makes one complete cycle (0 to 2π radians)
            // * 0.5f + 0.5f converts the range from [-1, 1] to [0, 1]
            // This gives us a smooth curve that goes from 0 to 1 and back
            float pulseIntensity = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f;

            // Mathf.Pow makes the curve less linear
            pulseIntensity = Mathf.Pow(pulseIntensity, 1.5f);

            // Scale the flash intensity based on the current overstimulation level
            // 
            // At level 1.0: flashAmount = 0.1 * pulseIntensity (full intensity)
            // At level 0.0: flashAmount = 0 (no flash)
            float flashAmount = currentLevel * maxFlashIntensity * pulseIntensity;

            // Update the overlay's transparency
            Color flashColor = flashOverlay.color;
            flashColor.a = flashAmount; // Set alpha to our calculated flash amount
            flashOverlay.color = flashColor; // Apply the new color
        }
        else if (flashOverlay != null)
        {
            // When level is 0, smoothly fade out the overlay
            Color flashColor = flashOverlay.color;
            flashColor.a = Mathf.Lerp(flashColor.a, 0, Time.deltaTime * 2f);
            flashOverlay.color = flashColor;
        }
    }


    void SetupBlur()
    {
        // Blur will be applied automatically via OnRenderImage callback
        // No setup needed
    }

    /// Unity callback that processes the camera's rendered image.
    /// This allows us to apply blur effects to the final rendered frame.
    /// OnRenderImage is called automatically by Unity after the camera renders.

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (currentLevel < 0.001f || mainCamera == null)
        {
            // If level is 0, just pass through unchanged
            Graphics.Blit(source, destination);
            return;
        }

        // Calculate blur intensity based on the pulsing pattern
        // This syncs the blur with the lamp pulsing
        float t = timer / flashSpeed;
        float pulseIntensity = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f;
        pulseIntensity = Mathf.Pow(pulseIntensity, 1.5f);

        // Scale the blur intensity based on the current overstimulation level
        // Example: if currentLevel = 0.5 and maxBlurIntensity = 2.0:
        //   currentBlur = 0.5 * 2.0 * pulseIntensity = 1.0 * pulseIntensity
        // At level 1.0: currentBlur = 2.0 * pulseIntensity (full blur)
        // At level 0.0: currentBlur = 0 (no blur)
        float currentBlur = currentLevel * maxBlurIntensity * pulseIntensity;

        if (currentBlur > 0.1f) // Only apply blur if intensity is significant
        {
            // Create temporary render texture at lower resolution for blur effect
            // Lower resolution = more blur (downsampling creates a blur-like effect)
            int downSample = Mathf.RoundToInt(1f + currentBlur * 2f); // 1-3x downsampling based on blur
            int width = Mathf.Max(1, source.width / downSample);
            int height = Mathf.Max(1, source.height / downSample);

            RenderTexture blurred = RenderTexture.GetTemporary(width, height, 0);

            // Downsample the source image (creates blur effect)
            Graphics.Blit(source, blurred);

            // Upscale blurred version back to original resolution
            Graphics.Blit(blurred, destination);

            RenderTexture.ReleaseTemporary(blurred);
        }
        else
        {
            // No blur, just pass through
            Graphics.Blit(source, destination);
        }
    }

    // Creates a radial gradient texture for the vignette effect.
    // The texture is transparent in the center and opaque at the edges.

    Texture2D CreateVignetteTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(width / 2f, height / 2f);
        float maxDistance = Mathf.Sqrt(center.x * center.x + center.y * center.y);

        // Create radial gradient: transparent in center, opaque at edges
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate distance from center
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                // Normalize distance (0 at center, 1 at corners)
                float normalizedDistance = distance / maxDistance;

                // Create vignette: 0 at center, 1 at edges
                // vignetteSize controls how much of the screen is affected
                // Lower vignetteSize = smaller edge effect
                float alpha = 0f;
                if (normalizedDistance > (1f - vignetteSize))
                {
                    // Only show effect near edges
                    // Map from (1-vignetteSize) to 1.0, creating smooth fade
                    float edgeFactor = (normalizedDistance - (1f - vignetteSize)) / vignetteSize;
                    alpha = Mathf.SmoothStep(0f, 1f, edgeFactor);
                }

                // White color with calculated alpha
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    /// Called by OverstimulationController to set the current overstimulation level.
    /// level: 0.0 = no effects, 1.0 = maximum blur and flash
    /// The Update() and OnRenderImage() methods will automatically adjust effects based on this level.
    public void SetOverstimulationLevel(float level)
    {
        // Store the level (clamp to 0-1 range to be safe)
        currentLevel = Mathf.Clamp01(level);

        // Reset timer when level reaches 0 so it starts fresh next time
        if (currentLevel < 0.001f)
        {
            timer = 0f;
        }
    }
}

