using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// CameraDisturbance drives a real post-processing vignette effect on the VR camera.
// The built-in Post Processing Stack v2 handles the actual fullscreen effect, while
// this script maps the adapted overstimulation level to vignette intensity.
public class CameraDisturbance : MonoBehaviour
{
    [Header("Post-Processing Vignette")]
    [Tooltip("Minimum vignette intensity when there is no overstimulation.")]
    [Range(0f, 1f)]
    public float minIntensity = 0.34f;

    [Tooltip("Maximum vignette intensity at peak overstimulation.")]
    [Range(0f, 1f)]
    public float maxIntensity = 0.62f;

    [Tooltip("Border smoothness of the vignette.")]
    [Range(0.01f, 1f)]
    public float smoothness = 0.55f;

    [Tooltip("How circular the vignette should be.")]
    [Range(0f, 1f)]
    public float roundness = 1f;

    [Tooltip("How quickly the vignette pulses.")]
    public float flashSpeed = 0.72f;

    [Tooltip("How much the pulse modulates the intensity around the base level.")]
    [Range(0f, 0.3f)]
    public float pulseAmount = 0.05f;

    private Camera mainCamera;
    private OverstimulationController overstimulationController;
    private PostProcessLayer postProcessLayer;
    private PostProcessVolume postProcessVolume;
    private Vignette vignette;
    private float currentLevel;
    private float timer;

    void Awake()
    {
        overstimulationController = FindObjectOfType<OverstimulationController>();
        mainCamera = ResolveCamera();

        if (mainCamera == null)
        {
            Debug.LogError("CameraDisturbance: Could not find a VR camera.");
            enabled = false;
            return;
        }

        if (!SetUpPostProcessing())
        {
            Debug.LogError("CameraDisturbance: Failed to create post-processing vignette.");
            enabled = false;
            return;
        }

        Debug.Log("CameraDisturbance: Post-processing vignette initialized.");
    }

    void Update()
    {
        if (vignette == null)
            return;

        float adaptationMultiplier = 1.0f;
        if (overstimulationController != null)
            adaptationMultiplier = Mathf.Max(0.01f, overstimulationController.GetAdaptationMultiplier());

        float baseVisualLevel = Mathf.SmoothStep(0f, 1f, currentLevel);
        float pulseContribution = 0f;

        if (currentLevel > 0.001f)
        {
            timer += Time.deltaTime;
            float adjustedFlashSpeed = Mathf.Lerp(flashSpeed, flashSpeed * 0.65f, baseVisualLevel) / adaptationMultiplier;
            float pulse = Mathf.Sin((timer / adjustedFlashSpeed) * Mathf.PI * 2f) * 0.5f + 0.5f;
            pulse = pulse * pulse * (3f - 2f * pulse);
            pulseContribution = pulseAmount * baseVisualLevel * pulse;
        }

        float baseIntensity = Mathf.Lerp(minIntensity, maxIntensity, baseVisualLevel);
        vignette.intensity.value = Mathf.Clamp01(baseIntensity + pulseContribution);
        vignette.smoothness.value = Mathf.Lerp(smoothness, 0.68f, baseVisualLevel);
        vignette.roundness.value = roundness;
        vignette.color.value = Color.black;
        vignette.rounded.value = true;
        vignette.center.value = new Vector2(0.5f, 0.5f);
    }

    void OnDestroy()
    {
        if (postProcessVolume != null)
        {
            RuntimeUtilities.DestroyVolume(postProcessVolume, true, true);
            postProcessVolume = null;
        }
    }

    Camera ResolveCamera()
    {
        Camera attachedCamera = GetComponent<Camera>();
        if (attachedCamera != null)
            return attachedCamera;

        GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>();
            if (xrCamera != null)
                return xrCamera;
        }

        return Camera.main;
    }

    bool SetUpPostProcessing()
    {
        PostProcessResources resources = Resources.Load<PostProcessResources>("PostProcessResources");
        if (resources == null)
        {
            Debug.LogError("CameraDisturbance: Resources/PostProcessResources.asset is missing.");
            return false;
        }

        // Force the VR camera down the render-texture path so fullscreen post effects
        // actually get a target to run on in the built-in pipeline on Quest.
        mainCamera.forceIntoRenderTexture = true;
        mainCamera.allowHDR = true;

        postProcessLayer = mainCamera.GetComponent<PostProcessLayer>();
        if (postProcessLayer == null)
            postProcessLayer = mainCamera.gameObject.AddComponent<PostProcessLayer>();

        int postProcessingLayer = LayerMask.NameToLayer("PostProcessing");
        if (postProcessingLayer < 0)
            postProcessingLayer = mainCamera.gameObject.layer;

        postProcessLayer.volumeTrigger = null;
        postProcessLayer.volumeLayer = 1 << postProcessingLayer;
        postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
        postProcessLayer.stopNaNPropagation = true;
        postProcessLayer.finalBlitToCameraTarget = false;
        postProcessLayer.Init(resources);

        vignette = ScriptableObject.CreateInstance<Vignette>();
        vignette.enabled.Override(true);
        vignette.mode.Override(VignetteMode.Classic);
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(minIntensity);
        vignette.smoothness.Override(smoothness);
        vignette.roundness.Override(roundness);
        vignette.rounded.Override(true);

        postProcessVolume = PostProcessManager.instance.QuickVolume(postProcessingLayer, 100f, vignette);
        postProcessVolume.isGlobal = true;
        postProcessVolume.priority = 100f;

        return true;
    }

    public void SetOverstimulationLevel(float level)
    {
        currentLevel = Mathf.Clamp01(level);
        if (currentLevel < 0.001f)
            timer = 0f;
    }
}
