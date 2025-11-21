using UnityEngine;


/// LampIntensity controls the brightness of individual lamps based on overstimulation level.
/// Lamps get brighter (not blinking) when overstimulation is active, creating an intense, overwhelming light effect.
/// The script is attached to lamp parent objects and finds the child object with the actual lamp mesh.

public class LampIntensity : MonoBehaviour
{
    [Header("Intensity Settings")]
    [Tooltip("Maximum brightness multiplier when overstimulation is at 100% (1.0). Example: 1.6 = 60% brighter than normal.")]
    public float maxIntensityMultiplier = 1.6f;

    [Tooltip("How fast the intensity pulses (in seconds). Lower = faster pulsing.")]
    public float pulseSpeed = 0.5f;

    [Tooltip("How much the intensity pulses (0 = constant, 0.2 = slight pulsing).")]
    public float pulseVariation = 0.2f;

    // Current overstimulation level (0.0 to 1.0)
    // 0.0 = normal brightness, 1.0 = maximum brightness
    private float currentLevel = 0f;

    private MeshRenderer meshRenderer; // The component that renders the lamp mesh
    private Material lampMaterial; // The material instance we'll modify
    private Color originalEmissionColor; // Store original color to restore later
    private float originalEmissionIntensity; // Store original emission intensity
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

        // Find and prepare the lamp material for intensity control
        if (meshRenderer != null)
        {
            // Get all materials on this renderer
            Material[] materials = meshRenderer.materials;

            // Search through all materials to find the one that makes the lamp glow
            // Try multiple material name patterns in case the naming is different
            bool materialFound = false;
            foreach (Material mat in materials)
            {
                if (mat == null) continue;

                // Try different material name patterns
                if (mat.name.Contains("LampOn") || mat.name.Contains("Lamp") || mat.name.Contains("Glass"))
                {
                    // Create a new Material instance instead of modifying the original
                    lampMaterial = new Material(mat);

                    // HDRP uses "_EmissiveColor"
                    if (lampMaterial.HasProperty("_EmissiveColor"))
                    {
                        originalEmissionColor = lampMaterial.GetColor("_EmissiveColor");
                        // Enable emission if it's not already enabled
                        if (lampMaterial.HasProperty("_EnableEmission"))
                        {
                            lampMaterial.SetFloat("_EnableEmission", 1f);
                        }
                    }
                    else if (lampMaterial.HasProperty("_EmissionColor"))
                    {
                        originalEmissionColor = lampMaterial.GetColor("_EmissionColor");
                        // Enable emission for standard shader
                        lampMaterial.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        // If no emission property found, try to get base color and use that
                        if (lampMaterial.HasProperty("_BaseColor"))
                        {
                            originalEmissionColor = lampMaterial.GetColor("_BaseColor");
                        }
                        else if (lampMaterial.HasProperty("_Color"))
                        {
                            originalEmissionColor = lampMaterial.GetColor("_Color");
                        }
                        else
                        {
                            // Fallback: use white
                            originalEmissionColor = Color.white;
                        }
                    }

                    // Calculate original emission intensity (brightness)
                    // We use the maximum RGB value to determine how bright it was originally
                    // This helps us preserve the color while increasing brightness
                    originalEmissionIntensity = Mathf.Max(originalEmissionColor.r, originalEmissionColor.g, originalEmissionColor.b);
                    
                    // If intensity is 0, set a default so we can still make it brighter
                    if (originalEmissionIntensity < 0.001f)
                    {
                        originalEmissionIntensity = 1f;
                        originalEmissionColor = Color.white;
                    }

                    // Replace the original material with our new instance in the materials array
                    // This ensures the renderer uses our modifiable copy instead of the shared original
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materials[i].name == mat.name)
                        {
                            materials[i] = lampMaterial; // Replace with our instance
                        }
                    }

                    // Apply the updated materials array back to the renderer
                    meshRenderer.materials = materials;
                    materialFound = true;
                    break; // Found the material, no need to keep searching
                }
            }
            
            // If no material was found, try using the first material anyway
            if (!materialFound && materials.Length > 0 && materials[0] != null)
            {
                lampMaterial = new Material(materials[0]);
                if (lampMaterial.HasProperty("_EmissiveColor"))
                {
                    originalEmissionColor = lampMaterial.GetColor("_EmissiveColor");
                    if (lampMaterial.HasProperty("_EnableEmission"))
                    {
                        lampMaterial.SetFloat("_EnableEmission", 1f);
                    }
                }
                else if (lampMaterial.HasProperty("_EmissionColor"))
                {
                    originalEmissionColor = lampMaterial.GetColor("_EmissionColor");
                    lampMaterial.EnableKeyword("_EMISSION");
                }
                else if (lampMaterial.HasProperty("_BaseColor"))
                {
                    originalEmissionColor = lampMaterial.GetColor("_BaseColor");
                }
                else
                {
                    originalEmissionColor = Color.white;
                }
                
                originalEmissionIntensity = Mathf.Max(originalEmissionColor.r, originalEmissionColor.g, originalEmissionColor.b);
                if (originalEmissionIntensity < 0.001f)
                {
                    originalEmissionIntensity = 1f;
                    originalEmissionColor = Color.white;
                }
                
                materials[0] = lampMaterial;
                meshRenderer.materials = materials;
            }
        }
    }

    void Update()
    {
        // Only modify intensity if we have a material to modify
        if (lampMaterial != null)
        {
            if (currentLevel > 0.001f) // If level is very small, treat as 0
            {
                // Overstimulation is active - make the lamp brighter based on the level
                // Increment timer for pulsing effect
                timer += Time.deltaTime;

                // Calculate pulsing intensity using a sine wave
                // This makes the brightness pulse slightly to add to the overstimulation effect
                float t = timer / pulseSpeed;

                // Mathf.Sin(t * Mathf.PI * 2) creates a wave that goes from -1 to 1
                // Multiplying by PI*2 creates one complete cycle (360 degrees)
                // * 0.5f + 0.5f converts the range from [-1, 1] to [0, 1]
                // This gives us a smooth curve: 0 → 1 → 0 → 1 (repeating)
                float sineValue = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f; // 0 to 1

                // Calculate pulsing factor (slight variation in brightness)
                // pulseVariation controls how much it pulses (0 = constant, 1 = full pulse)
                float pulseFactor = 1f + (sineValue * pulseVariation); // 1.0 to (1.0 + pulseVariation)

                // Calculate the intensity multiplier based on the current level
                // Example: if currentLevel = 0.5 and maxIntensityMultiplier = 1.6:
                //   intensity = 1.0 + (0.5 * (1.6 - 1.0)) = 1.0 + 0.3 = 1.3 (30% brighter)
                // At level 1.0: intensity = 1.6 (60% brighter)
                // At level 0.0: intensity = 1.0 (normal brightness)
                float baseIntensity = 1f + (currentLevel * (maxIntensityMultiplier - 1f));
                
                // Apply pulsing to the intensity
                float intensity = baseIntensity * pulseFactor;

                // Preserve the original color while increasing brightness
                // Normalize the color first, then scale it by the new intensity
                // This keeps the warm yellow/orange color instead of making it white
                Color normalizedColor = originalEmissionColor;
                if (originalEmissionIntensity > 0.001f)
                {
                    // Normalize the color to preserve its hue and saturation
                    normalizedColor = originalEmissionColor / originalEmissionIntensity;
                }

                // Apply the new intensity while preserving color
                float newIntensity = originalEmissionIntensity * intensity;
                Color emissionColor = normalizedColor * newIntensity;

                // Update the material's emission color
                // Try HDRP property first, then Standard
                if (lampMaterial.HasProperty("_EmissiveColor"))
                {
                    lampMaterial.SetColor("_EmissiveColor", emissionColor);
                }
                else if (lampMaterial.HasProperty("_EmissionColor"))
                {
                    lampMaterial.SetColor("_EmissionColor", emissionColor);
                }
                else if (lampMaterial.HasProperty("_BaseColor"))
                {
                    // Fallback: modify base color if no emission property
                    lampMaterial.SetColor("_BaseColor", emissionColor);
                }
            }
            else
            {
                // Level is 0 - restore normal brightness
                if (lampMaterial.HasProperty("_EmissiveColor"))
                {
                    lampMaterial.SetColor("_EmissiveColor", originalEmissionColor);
                }
                else if (lampMaterial.HasProperty("_EmissionColor"))
                {
                    lampMaterial.SetColor("_EmissionColor", originalEmissionColor);
                }
                else if (lampMaterial.HasProperty("_BaseColor"))
                {
                    lampMaterial.SetColor("_BaseColor", originalEmissionColor);
                }
                timer = 0f; // Reset timer so pulsing starts fresh next time
            }
        }
    }


    /// Called by LampController to set the overstimulation level for this lamp.
    /// level: 0.0 = normal brightness, 1.0 = maximum brightness
    /// The Update() method will automatically adjust the lamp's brightness based on this level.
    public void SetOverstimulationLevel(float level)
    {
        // Store the level (clamp to 0-1 range to be safe)
        currentLevel = Mathf.Clamp01(level);
        
        // The Update() method will use this level to adjust brightness automatically
    }
}

