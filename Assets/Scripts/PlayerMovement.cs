using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    
    private float verticalRotation = 0;
    private float horizontalRotation = 0;
    
    void Start()
    {
        // disable if VR is active.
        if (GameObject.Find("XR Origin") != null)
        {
            this.enabled = false;
            Debug.Log("PlayerMovement: VR detected (XR Origin found), disabling keyboard/mouse movement. XR Interaction Toolkit will handle movement.");
            return;
        }
        
        try
        {
            var xrSettingsType = System.Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule");
            if (xrSettingsType != null)
            {
                var enabledProperty = xrSettingsType.GetProperty("enabled");
                if (enabledProperty != null)
                {
                    bool xrEnabled = (bool)enabledProperty.GetValue(null);
                    if (xrEnabled)
                    {
                        this.enabled = false;
                        return;
                    }
                }
            }
        }
        catch
        {
            // XR not available, continue with normal movement
        }
    }
    
    void Update()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Horizontal rotation (Y axis)
        horizontalRotation += mouseX;
        
        // Vertical rotation (X axis)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        
        // Apply both rotations
        transform.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
        
        // Movement - using transform.position (no collision)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        transform.position += move * moveSpeed * Time.deltaTime;
        
        // Unlock cursor with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
