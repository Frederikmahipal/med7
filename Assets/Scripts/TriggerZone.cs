using UnityEngine;

// TriggerZone creates an area that affects overstimulation when the player enters/exits.
// 
// - Attach this script to a GameObject with a Collider (set as "Is Trigger")
// - Choose what happens when player enters: Increase or Decrease
// - When player enters: calls OverstimulationController to add/remove triggers
// - When player exits: reverses the effect
public class TriggerZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("What happens when player enters this zone:\n" +
             "Increase = Overstimulation level goes up (e.g., queue, crowded area)\n" +
             "Decrease = Overstimulation level goes down (e.g., calm corner, quiet area)")]
    public ZoneType zoneType = ZoneType.Increase;

    [Tooltip("Unique name for this zone (for debugging). Example: 'QueueZone', 'CalmCorner', etc.")]
    public string zoneName = "TriggerZone";

    [Header("Player Detection")]
    [Tooltip("Tag that identifies the player GameObject. Default is 'Player'.")]
    public string playerTag = "Player";

    // Reference to the OverstimulationController in the scene
    private OverstimulationController overstimulationController;

    // Enum to define what type of zone this is
    public enum ZoneType
    {
        Increase,   // Increases overstimulation (e.g., queue, crowded area)
        Decrease    // Decreases overstimulation (e.g., calm corner, quiet area)
    }

    void Start()
    {
        // Find the OverstimulationController in the scene
        // We do this once at Start() so we don't search every frame
        overstimulationController = FindObjectOfType<OverstimulationController>();

        if (overstimulationController == null)
        {
            Debug.LogError($"'{zoneName}': No OverstimulationController found in scene");
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"'{zoneName}': Collider is not set as 'Is Trigger'");
        }
        else if (col == null)
        {
            Debug.LogWarning($"'{zoneName}': No Collider  found");
        }

        // Check if this zone has a Rigidbody (if not, add one automatically)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // This is required for OnTriggerEnter/Exit to work
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Kinematic = doesn't move, but still enables physics triggers
            rb.useGravity = false; // Don't need gravity for a trigger zone
        }
    }

    // Unity callback: Called when another GameObject with a Collider enters this trigger zone.
    // This happens automatically when a collider enters the trigger area.
    void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is the player
        // We check by tag (default is "Player")
        if (other.CompareTag(playerTag))
        {
            // Apply the zone effect based on the zone type
            if (zoneType == ZoneType.Increase)
            {
                // This is an overstimulation zone - tell controller to add a trigger
                // This will cause the overstimulation level to start increasing
                if (overstimulationController != null)
                {
                    overstimulationController.AddTrigger(zoneName);
                }
                else
                {
                    Debug.LogError($"'{zoneName}': OverstimulationController is null");
                }
            }
            else 
            {
                // This is a calm zone - tell controller to remove a trigger
                // This will cause the overstimulation level to start decreasing
                if (overstimulationController != null)
                {
                    overstimulationController.RemoveTrigger(zoneName);
                }
            }
        }
    }

    // Unity callback: Called when another GameObject with a Collider exits this trigger zone.
    // This happens automatically when a collider leaves the trigger area.
    void OnTriggerExit(Collider other)
    {
        // Check if the object that exited is the player
        if (other.CompareTag(playerTag))
        {
            // Reverse the zone effect
            if (zoneType == ZoneType.Increase)
            {
                // Player left an overstimulation zone - remove the trigger
                // This will cause the overstimulation level to start decreasing
                if (overstimulationController != null)
                {
                    overstimulationController.RemoveTrigger(zoneName);
                }
            }
            else // ZoneType.Decrease
            {
                // Player left a calm zone add a trigger back
                // This will cause the overstimulation level to start increasing again
                if (overstimulationController != null)
                {
                    overstimulationController.AddTrigger(zoneName);
                }
            }
            
        }
    }
}
