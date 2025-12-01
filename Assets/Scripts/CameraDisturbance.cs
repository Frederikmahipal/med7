using UnityEngine;

// CameraDisturbance creates a visual vignette overlay effect in VR that syncs with lamp blinking.
// This simulates overstimulation by adding visual disturbance to the player's view in VR.

public class CameraDisturbance : MonoBehaviour
{
    [Header("Vignette Settings")]
    [Tooltip("Maximum flash intensity when overstimulation is at 100% (1.0). Lower = less intense.")]
    public float maxFlashIntensity = 2.0f;

    [Tooltip("How fast the flash pulses (in seconds). Should match the lamp pulse speed for synchronization.")]
    public float flashSpeed = 0.5f;

    [Tooltip("How much of the screen the vignette covers. Higher = more edge coverage. Range 0-1.")]
    public float vignetteSize = 0.95f; // Set to 0.95f for very tight vignette, only small center visible 

    private Camera mainCamera;
    private float currentLevel = 0f; // Current overstimulation level (0.0 to 1.0)
    private float adaptationMultiplier = 1.0f;
    private float timer = 0f;
    private OverstimulationController overstimulationController;
    
    // VR overlay, quad mesh for each eye
    private GameObject vrOverlayQuadLeft;
    private GameObject vrOverlayQuadRight;
    private Material vrOverlayMaterial;
    private Texture2D vignetteTexture;

    void Awake()
    {
        // Force GameObject to be active if it's inactive
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // Force script to be enabled
        if (!enabled)
        {
            enabled = true;
        }
    }

    void Start()
    {
        // Find OverstimulationController to get adaptation multiplier
        overstimulationController = FindObjectOfType<OverstimulationController>();

        Camera[] allCameras = FindObjectsOfType<Camera>();
        
        // Log all cameras to verify we find the right one
        Debug.LogError($"CameraDisturbance: Found {allCameras.Length} camera(s) in scene:");
        for (int i = 0; i < allCameras.Length; i++)
        {
            Camera cam = allCameras[i];
            string parentName = cam.transform.parent != null ? cam.transform.parent.name : "None";
            string grandParentName = cam.transform.parent != null && cam.transform.parent.parent != null 
                ? cam.transform.parent.parent.name : "None";
            Debug.LogError($"CameraDisturbance: Camera[{i}] - Name={cam.name}, Tag={cam.tag}, Depth={cam.depth}, " +
                $"TargetEye={cam.stereoTargetEye}, Parent={parentName}, GrandParent={grandParentName}");
        }
        
        // Find the VR camera (the one under XR Origin)
        Camera vrCamera = null;
        
        foreach (Camera cam in allCameras)
        {
            // Look for camera with MainCamera tag that has "Camera Offset" as parent
            if (cam.tag == "MainCamera" && cam.transform.parent != null && 
                cam.transform.parent.name == "Camera Offset")
            {
                vrCamera = cam;
                Debug.LogError($"CameraDisturbance: Found VR camera! Name={cam.name}, Tag={cam.tag}, Parent={cam.transform.parent.name}, TargetEye={cam.stereoTargetEye}, Depth={cam.depth}, Near={cam.nearClipPlane}");
                break;
            }
        }
        
        if (vrCamera != null)
        {
            mainCamera = vrCamera;
            Debug.LogError($"CameraDisturbance: Using VR camera: {mainCamera.name}, TargetEye={mainCamera.stereoTargetEye}, Near={mainCamera.nearClipPlane}");
            
            // Set Near Clip Plane to 0.01 so objects close to camera render
            // Default is often 0.3 (30cm), which would cull our overlay at 1cm
            if (mainCamera.nearClipPlane > 0.01f)
            {
                Debug.LogError($"CameraDisturbance: Near Clip Plane was {mainCamera.nearClipPlane}, setting to 0.01 for VR overlay!");
                mainCamera.nearClipPlane = 0.01f;
            }
        }
        else
        {
            // Fallback to camera on this GameObject
            mainCamera = GetComponent<Camera>();
            string fallbackName = mainCamera != null ? mainCamera.name : "NULL";
            Debug.LogError($"CameraDisturbance: VR camera not found! Using fallback camera: {fallbackName}");
        }
        
        if (mainCamera == null)
        {
            Debug.LogError($"CameraDisturbance: No Camera component found! GameObject={gameObject.name}");
            enabled = false;
            return;
        }
        
        // Create VR overlay
        Debug.LogError($"CameraDisturbance: Creating VR overlay, Camera={mainCamera.name}");
        CreateVROverlayForBothEyes();
    }

    // Creates VR overlay - separate quad mesh for each eye
    void CreateVROverlayForBothEyes()
    {
        // Create vignette texture
        vignetteTexture = CreateVignetteTexture(512, 512);
        
        // Find Shader
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("UI/Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        
        if (shader == null)
        {
            return;
        }
        
        // Setup Material
        vrOverlayMaterial = new Material(shader);
        vrOverlayMaterial.mainTexture = vignetteTexture;
        vrOverlayMaterial.color = new Color(1, 1, 1, 0f); 
        
        //  Force material to render on top of everything
        vrOverlayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        vrOverlayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        vrOverlayMaterial.SetInt("_ZWrite", 0); 
        vrOverlayMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always); // Always draw on top
        vrOverlayMaterial.renderQueue = 5000; // Overlay queue (renders very late)
        
        // Calculate distance and scale
        float distance = 0.35f; 
        
        float fov = mainCamera.fieldOfView * Mathf.Deg2Rad;
        // Multiply by 1.5f to ensure edges are fully covered even if eyes rotate slightly
        float width = 2f * distance * Mathf.Tan(fov / 2f) * 1.5f; 
        float height = width * (mainCamera.pixelHeight / (float)mainCamera.pixelWidth);
        
        Vector3 quadScale = new Vector3(width, height, 1f);
        
        // Create quad for left eye
        vrOverlayQuadLeft = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vrOverlayQuadLeft.name = "VROverlayQuad_LeftEye";
        vrOverlayQuadLeft.transform.SetParent(mainCamera.transform, false);
        vrOverlayQuadLeft.transform.localPosition = new Vector3(0, 0, distance); 
        vrOverlayQuadLeft.transform.localRotation = Quaternion.identity;
        vrOverlayQuadLeft.transform.localScale = quadScale;
        
        // Setup Renderer (Left)
        ConfigureQuadRenderer(vrOverlayQuadLeft);

        // Create quad for right eye
        vrOverlayQuadRight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vrOverlayQuadRight.name = "VROverlayQuad_RightEye";
        vrOverlayQuadRight.transform.SetParent(mainCamera.transform, false);
        vrOverlayQuadRight.transform.localPosition = new Vector3(0, 0, distance);
        vrOverlayQuadRight.transform.localRotation = Quaternion.identity;
        vrOverlayQuadRight.transform.localScale = quadScale;
        
        // Setup Renderer (Right)
        ConfigureQuadRenderer(vrOverlayQuadRight);
        
        Debug.Log($"CameraDisturbance: VR overlay created! Dist={distance}, Scale={quadScale}");
    }

    // Helper function to avoid code duplication
    void ConfigureQuadRenderer(GameObject quad)
    {
        MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
        meshRenderer.material = vrOverlayMaterial;
        quad.layer = mainCamera.gameObject.layer;
        quad.SetActive(true);
        meshRenderer.enabled = true;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        if(quad.GetComponent<Collider>()) Destroy(quad.GetComponent<Collider>());
    }

    void Update()
    {
        // VR: Update material color for vignette effect
        if (vrOverlayMaterial != null)
        {
            // Calculate vignette intensity based on overstimulation level
            if (currentLevel > 0.001f)
            {
                // Get adaptation multiplier to adjust pulse speed
                if (overstimulationController != null)
                {
                    adaptationMultiplier = overstimulationController.GetAdaptationMultiplier();
                }

                // Calculate pulse intensity
                timer += Time.deltaTime;

                // Adjust pulse speed based on adaptation multiplier
                // Higher multiplier (1.2) = faster pulse, lower multiplier (0.8) = slower pulse
                float adjustedFlashSpeed = flashSpeed / adaptationMultiplier;
                float t = timer / adjustedFlashSpeed;
                float pulseIntensity = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f;
                pulseIntensity = Mathf.Pow(pulseIntensity, 1.5f);
                
                // Calculate final alpha based on level and pulse
                float flashAmount = currentLevel * maxFlashIntensity * pulseIntensity;
                flashAmount = Mathf.Clamp01(flashAmount); // Clamp to 0-1 range
                
                // Set material color with vignette texture and calculated alpha
                vrOverlayMaterial.color = new Color(1, 1, 1, flashAmount);
                
                // Ensure quads are active
                if (vrOverlayQuadLeft != null && !vrOverlayQuadLeft.activeSelf)
                {
                    vrOverlayQuadLeft.SetActive(true);
                }
                if (vrOverlayQuadRight != null && !vrOverlayQuadRight.activeSelf)
                {
                    vrOverlayQuadRight.SetActive(true);
                }
            }
            else
            {
                // Fade out when level is 0
                Color currentColor = vrOverlayMaterial.color;
                currentColor.a = Mathf.Lerp(currentColor.a, 0, Time.deltaTime * 2f);
                vrOverlayMaterial.color = currentColor;
            }
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
                float alpha = 0f;
                if (normalizedDistance > (1f - vignetteSize))
                {
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

    // Called by OverstimulationController to set the current overstimulation level.
    // level: 0.0 = no effects, 1.0 = maximum vignette
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

