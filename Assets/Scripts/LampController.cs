using UnityEngine;
using System.Collections.Generic;

/// LampController is the main coordinator for the overstimulation effect.
/// It finds all lamps in the scene, adds blinking components to them, and controls them all together.
/// It also coordinates with CameraDisturbance to sync the screen flash effect.
/// This script is attached to an empty GameObject in the scene.

public class LampController : MonoBehaviour
{
    // List to store references to all blinking lamps in the scene
    // This allows us to control all lamps at once
    private List<BlinkingLamp> allLamps = new List<BlinkingLamp>();
    
    // Reference to the camera disturbance effect
    // This syncs the screen flash with the lamp blinking
    private CameraDisturbance cameraDisturbance;
    
    // Tracks whether lamps are currently blinking
    // Used to toggle on/off with the B key
    private bool lampsBlinking = false;
    
    void Start()
    {
        // Find all lamp objects in the scene and add blinking components to them
        // This happens once at the start so we have all lamps ready to control
        FindAllLamps();
        
        // Find the CameraDisturbance component that handles the screen flash effect
        // FindObjectOfType searches all GameObjects in the scene for this component
        cameraDisturbance = FindObjectOfType<CameraDisturbance>();
        
        // If no CameraDisturbance exists yet, create one on the main camera
        // This ensures the screen flash effect is always available
        // We check Camera.main != null to avoid errors if there's no camera in the scene
        if (cameraDisturbance == null && Camera.main != null)
        {
            // AddComponent creates a new component and attaches it to the GameObject
            // This automatically sets up the camera disturbance effect
            cameraDisturbance = Camera.main.gameObject.AddComponent<CameraDisturbance>();
        }
    }
    
    void Update()
    {
        // GetKeyDown only returns true on the frame when the key is first pressed
        if (Input.GetKeyDown(KeyCode.B))
        {
            // Toggle the blinking state (if false, becomes true; if true, becomes false)
            lampsBlinking = !lampsBlinking;
            
            // Tell all lamps to start or stop blinking
            ToggleAllLamps(lampsBlinking);
            
            // This syncs the screen flash with the lamp blinking for a cohesive effect
            if (cameraDisturbance != null)
            {
                cameraDisturbance.SetDisturbance(lampsBlinking);
            }
            
            // Log to console for debugging
            // This helps verify that the toggle is working correctly
            Debug.Log($"Lamps blinking: {lampsBlinking}");
        }
    }
    
    /// Searches the entire scene for lamp objects and adds BlinkingLamp components to them.
    void FindAllLamps()
    {
        // FindObjectsOfType gets ALL GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        // Loop through every object in the scene
        // TODO: maybe search for objects by component type instead of name?
        foreach (GameObject obj in allObjects)
        {
            // Look for lamp parent objects with names
            // We check for "SM_CeilingLamp_" to find numbered parent objects
            // We exclude objects named exactly "SM_CeilingLamp" because those are child objects, not parents
            if (obj.name.Contains("SM_CeilingLamp_") && obj.name != "SM_CeilingLamp")
            {
                // Check if this lamp already has a BlinkingLamp component
                // This prevents adding duplicate components if the script executes more than once
                BlinkingLamp lamp = obj.GetComponent<BlinkingLamp>();

  
                if (lamp == null)
                {
                    lamp = obj.AddComponent<BlinkingLamp>();
                }

                // Add the lamp to our list so we can control it later
                // This list is used in ToggleAllLamps() to control all lamps at once
                allLamps.Add(lamp);
            }
        }
            }
    

    /// Tells all lamps in the scene to start or stop blinking.
    void ToggleAllLamps(bool blinking)
    {
        // Loop through all lamps we found in FindAllLamps()
        foreach (BlinkingLamp lamp in allLamps)
        {
            // Check if the lamp reference is still valid
            if (lamp != null)
            {
                // Call SetBlinking on each lamp
                lamp.SetBlinking(blinking);
            }
        }
    }
}

