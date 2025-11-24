using UnityEngine;
using System.Collections.Generic;

/// LampController manages all lamps in the scene and makes them brighter based on overstimulation level.
/// 
/// HOW IT WORKS:
/// - Finds all lamps in the scene and adds LampIntensity components to them
/// - Receives the overstimulation level (0.0 to 1.0) from OverstimulationController
/// - Tells each lamp how bright to be based on the level
/// - As level increases, more lamps get affected and they get brighter
/// 
/// This script is attached to an empty GameObject in the scene.

public class LampController : MonoBehaviour
{
    [Header("Lamp Selection")]
    [Tooltip("How many lamps should be affected when overstimulation is at maximum (1.0). At level 0.5, half this many lamps will be affected. Set to -1 to affect ALL lamps at maximum level.")]
    public int maxLampsInFocus = -1; // -1 = affect all lamps at max level
    
    // List to store references to all lamps in the scene
    private List<LampIntensity> allLamps = new List<LampIntensity>();
    
    // Current overstimulation level (0.0 to 1.0)
    private float currentLevel = 0f;
    
    void Start()
    {

        FindAllLamps();
    }
    
    /// Searches the entire scene for lamp objects and adds LampIntensity components to them.
    /// The LampIntensity component controls how bright each lamp is.
    void FindAllLamps()
    {
        // FindObjectsOfType gets ALL GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        // Loop through every object in the scene
        foreach (GameObject obj in allObjects)
        {
            // Look for lamp parent objects with names like "SM_CeilingLamp_14"
            // We check for "SM_CeilingLamp_" to find numbered parent objects
            // We exclude objects named exactly "SM_CeilingLamp" because those are child objects, not parents
            if (obj.name.Contains("SM_CeilingLamp_") && obj.name != "SM_CeilingLamp")
            {
                // Check if this lamp already has a LampIntensity component
                LampIntensity lamp = obj.GetComponent<LampIntensity>();

                if (lamp == null)
                {
                    // Add the LampIntensity component so we can control this lamp's brightness
                    lamp = obj.AddComponent<LampIntensity>();
                }

                // Add the lamp to our list so we can control it later
                allLamps.Add(lamp);
            }
        }
    }

    /// Called by OverstimulationController to update all lamps based on the current level.
    /// level: 0.0 = no overstimulation (lamps normal), 1.0 = maximum (lamps very bright)
    public void SetOverstimulationLevel(float level)
    {
        // Store the current level
        currentLevel = Mathf.Clamp01(level); // Clamp to 0-1 range

        // Calculate how many lamps should be affected based on the level
        // If maxLampsInFocus is -1, affect all lamps at max level
        int lampsToAffect;
        if (maxLampsInFocus < 0)
        {
            // Affect all lamps, scaled by level
            // At level 1.0, all lamps are affected. At level 0.5, all lamps are affected but at 50% intensity
            lampsToAffect = allLamps.Count; // All lamps get the level, but intensity scales with level
        }
        else
        {
            // Affect a specific number of lamps based on level
            // Example: if level = 0.5 and maxLampsInFocus = 3, then 1.5 lamps (rounded to 2)
            lampsToAffect = Mathf.RoundToInt(currentLevel * maxLampsInFocus);
            lampsToAffect = Mathf.Clamp(lampsToAffect, 0, allLamps.Count); // Can't exceed total lamps
        }

        // If we're in limited mode, sort lamps by distance to player (closest first)
        List<LampIntensity> sortedLamps = allLamps;
        if (maxLampsInFocus >= 0 && lampsToAffect > 0)
        {
            // Get player position
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 playerPos = player.transform.position;
                
                // Sort lamps by distance to player (closest first)
                sortedLamps = new List<LampIntensity>(allLamps);
                sortedLamps.Sort((lamp1, lamp2) =>
                {
                    if (lamp1 == null || lamp2 == null) return 0;
                    float dist1 = Vector3.Distance(playerPos, lamp1.transform.position);
                    float dist2 = Vector3.Distance(playerPos, lamp2.transform.position);
                    return dist1.CompareTo(dist2);
                });
            }
        }

        // Update each lamp
        for (int i = 0; i < allLamps.Count; i++)
        {
            if (allLamps[i] != null)
            {
                if (maxLampsInFocus < 0)
                {
                    // Affect ALL lamps, but scale intensity by level
  
                    allLamps[i].SetOverstimulationLevel(currentLevel);
                }
                else
                {
                    // Find this lamp's index in the sorted list
                    int sortedIndex = sortedLamps.IndexOf(allLamps[i]);
                    
                    // First 'lampsToAffect' lamps (closest to player) get the full level
                    // Other lamps stay at normal brightness (level = 0)
                    if (sortedIndex >= 0 && sortedIndex < lampsToAffect)
                    {
                        // This lamp should be affected - tell it the current level
                        allLamps[i].SetOverstimulationLevel(currentLevel);
                    }
                    else
                    {
                        // This lamp should not be affected - set level to 0 (normal brightness)
                        allLamps[i].SetOverstimulationLevel(0f);
                    }
                }
            }
        }
    }
}

