using UnityEngine;
using System.IO;
using System.Text;

// AdaptationDataLogger records data to CSV file to prove the adaptive system works.

// To disable logging uncheck "Enable Logging" in the Inspector.
public class AdaptationDataLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    [Tooltip("Enable/disable data logging. Uncheck to disable without removing component.")]
    public bool enableLogging = false;
    
    [Tooltip("How often to log data (in seconds). Lower = more data points. 0.1 = 10 times per second.")]
    [Range(0.05f, 1.0f)]
    public float logInterval = 0.1f;
    
    [Tooltip("Custom filename (optional). If empty, uses: adaptation_data_[timestamp].csv")]
    public string customFileName = "";
    
    // References to other systems
    private OverstimulationController overstimulationController;
    private AdaptationController adaptationController;
    
    // Logging state
    private StreamWriter csvWriter;
    private string csvFilePath;
    private float lastLogTime;
    private float sessionStartTime;
    private bool isLogging = false;
    
    void Start()
    {
        // Find required components
        RefreshReferences();
        if (overstimulationController == null)
        {
            Debug.LogWarning("No OverstimulationController found");
            enableLogging = false;
            return;
        }

        // Initialize logging if enabled
        if (enableLogging)
        {
            StartLogging();
        }
    }
    
    void Update()
    {
        RefreshReferences();

        // Check if logging was enabled/disabled in Inspector
        if (enableLogging && !isLogging)
        {
            StartLogging();
        }
        else if (!enableLogging && isLogging)
        {
            StopLogging();
        }
        
        // Log data at specified interval
        if (isLogging && Time.time - lastLogTime >= logInterval)
        {
            LogData();
            lastLogTime = Time.time;
        }
    }
    
    void StartLogging()
    {
        if (isLogging) return; // Already logging

        // Use Unity's persistent path so logging works both in the editor and on Quest/Android builds.
        string dataDir = Path.Combine(Application.persistentDataPath, "Data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        
        // Generate filename
        string fileName;
        if (!string.IsNullOrEmpty(customFileName))
        {
            fileName = customFileName.EndsWith(".csv") ? customFileName : customFileName + ".csv";
        }
        else
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"adaptation_data_{timestamp}.csv";
        }
        
        csvFilePath = Path.Combine(dataDir, fileName);
        
        // Create CSV file and write header
        csvWriter = new StreamWriter(csvFilePath, false, Encoding.UTF8);
        csvWriter.WriteLine("Time,OverstimulationLevel,AdaptedLevel,AdaptationMultiplier,HeadRotationSpeed,DiscomfortLevel,ActiveTriggers");
        csvWriter.Flush();
        
        sessionStartTime = Time.time;
        lastLogTime = Time.time;
        isLogging = true;
        
        Debug.Log($"Started logging to {csvFilePath}");
    }
    
    void LogData()
    {
        if (csvWriter == null || !isLogging) return;

        RefreshReferences();

        // Get data
        float time = Time.time - sessionStartTime;
        float overstimLevel = overstimulationController != null ? overstimulationController.GetLevel() : 0f;
        float adaptedLevel = overstimulationController != null ? overstimulationController.GetAdaptedLevel() : 0f;
        float adaptationMultiplier = 1.0f;
        float headRotationSpeed = 0f;
        float discomfortLevel = 0f;
        int activeTriggers = 0;
        
        // Get adaptation multiplier from OverstimulationController (this is the actual multiplier being used)
        if (overstimulationController != null)
        {
            adaptationMultiplier = overstimulationController.currentAdaptationMultiplier;
            activeTriggers = overstimulationController.GetActiveTriggersCount();
        }
        
        // Get adaptive data from AdaptationController for rotation speed and discomfort
        if (adaptationController != null)
        {
            headRotationSpeed = adaptationController.GetAverageRotationSpeed();
            discomfortLevel = adaptationController.GetDiscomfortLevel();
        }
        
        // Write CSV line
        csvWriter.WriteLine($"{time:F3},{overstimLevel:F4},{adaptedLevel:F4},{adaptationMultiplier:F4},{headRotationSpeed:F2},{discomfortLevel:F4},{activeTriggers}");
        csvWriter.Flush(); // Ensure data is written immediately
    }

    void RefreshReferences()
    {
        if (overstimulationController == null)
        {
            overstimulationController = FindObjectOfType<OverstimulationController>();
        }

        if (adaptationController == null)
        {
            adaptationController = FindObjectOfType<AdaptationController>();
            if (adaptationController == null && overstimulationController != null && overstimulationController.enableAdaptiveIntensity)
            {
                // OverstimulationController may auto-create this after Start, so we keep retrying.
                return;
            }
        }
    }
    
    void StopLogging()
    {
        if (!isLogging) return;
        
        if (csvWriter != null)
        {
            csvWriter.Close();
            csvWriter = null;
        }
        
        isLogging = false;
        Debug.Log($"Stopped logging. File saved to: {csvFilePath}");
    }
    
    void OnDestroy()
    {
        StopLogging();
    }
    
    void OnApplicationQuit()
    {
        StopLogging();
    }
    
}
