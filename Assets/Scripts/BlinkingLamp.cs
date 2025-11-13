using UnityEngine;


/// BlinkingLamp makes a lamp's emission (glow) pulse between dim and bright.
/// This creates a blinking effect by modifying the material's emission color intensity.
/// The script is attached to lamp parent objects and finds the child object with the actual lamp mesh.

public class BlinkingLamp : MonoBehaviour
{
    [Tooltip("How fast the lamp blinks (in seconds). Lower = faster blinking.")]
    public float blinkSpeed = 0.5f;
    
    [Tooltip("How dim the lamp gets when blinking (0 = completely off, 1 = full brightness).")]
    public float minIntensity = 0.1f;
    
    [Tooltip("How bright the lamp gets when blinking. Usually matches the original lamp brightness.")]
    public float maxIntensity = 1f;
    
    public bool isBlinking = false; // Controlled by LampController
    
    private MeshRenderer meshRenderer; // The component that renders the lamp mesh
    private Material lampMaterial; // The material instance we'll modify
    private Color originalEmissionColor; // Store original color to restore later
    private float timer = 0f; // Tracks time for animation
    
    void Start()
    {
        // Find the MeshRenderer component that actually displays the lamp
        // GetComponentInChildren searches child objects first (the SM_CeilingLamp child)
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        
        // Fallback: if not found in children, check this object itself
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        
        // Find and prepare the lamp material for blinking
        if (meshRenderer != null)
        {
            // Get all materials on this renderer
            Material[] materials = meshRenderer.materials;
            
            // Search through all materials to find the one that makes the lamp glow
            foreach (Material mat in materials)
            {
                // The lamp's glowing material is named "M_Glass_LampOn"
                // We check if the material name contains "LampOn" to identify it
                if (mat != null && mat.name.Contains("LampOn"))
                {
                    // Create a new Material instance instead of modifying the original - if we need to only stimulate specific parts of the shop? 
                    lampMaterial = new Material(mat);
                    
                    // Store the original emission color before we start modifying it
                    // "_EmissionColor" is the shader property that controls how much the material glows
                    // We need to save this so we can restore it when blinking stops
                    originalEmissionColor = lampMaterial.GetColor("_EmissionColor");
                    
                    // Replace the original material with our new instance in the materials array
                    // This ensures the renderer uses our modifiable copy instead of the shared original
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materials[i].name.Contains("LampOn"))
                        {
                            materials[i] = lampMaterial; // Replace with our instance
                        }
                    }
                    
                    // Apply the updated materials array back to the renderer
                    meshRenderer.materials = materials;
                    break; // Found the material, no need to keep searching
                }
            }
        }
    }
    
    void Update()
    {
        // Only animate if blinking is enabled and we have a material to modify
        if (isBlinking && lampMaterial != null)
        {
            // Increment timer by time since last frame
            timer += Time.deltaTime;
            
            // Calculate the current intensity using a sine wave
            
            // Dividing by blinkSpeed means one complete cycle takes 'blinkSpeed' seconds
            float t = timer / blinkSpeed;
            
            // Mathf.Sin(t * Mathf.PI * 2) creates a wave that goes from -1 to 1
            // Multiplying by PI*2 creates one complete cycle (360 degrees)
            // * 0.5f + 0.5f converts the range from [-1, 1] to [0, 1]
            // This gives us a smooth curve: 0 → 1 → 0 → 1 (repeating)
            float sineValue = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f;
            
            // Mathf.Lerp linearly interpolates between min and max based on the sine value
            // When sineValue = 0, intensity = minIntensity (dim)
            // When sineValue = 1, intensity = maxIntensity (bright)
            // This creates smooth transitions between dim and bright
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, sineValue);
            
            // Apply the intensity to the emission color
            // Multiplying the original color by intensity scales its brightness
            // intensity = 0.1 means 10% brightness (dim)
            // intensity = 1.0 means 100% brightness (full glow)
            // This preserves the color (yellow/orange) but changes how bright it is
            Color emissionColor = originalEmissionColor * intensity;
            
            // Update the material's emission color
            // This is what actually makes the lamp appear to blink
            // The shader uses this color to determine how much the material glows
            lampMaterial.SetColor("_EmissionColor", emissionColor);
        }
    }
    

    /// Called by LampController to start or stop blinking.
    /// When stopping, restores the original emission color so the lamp returns to normal.
  
    public void SetBlinking(bool blinking)
    {
        isBlinking = blinking;
        
        
        // When stopping, restore the original emission color
        // This ensures the lamp returns to its normal brightness when blinking stops
        if (!blinking && lampMaterial != null)
        {
            // Restore the original emission color we saved in Start()
            lampMaterial.SetColor("_EmissionColor", originalEmissionColor);

            // Reset timer so blinking starts from the beginning next time
            timer = 0f;
        }
    }
}

