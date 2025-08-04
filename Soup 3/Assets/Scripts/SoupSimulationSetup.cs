using System.Reflection;
using UnityEngine;
using Seb.Fluid.Simulation;
using Seb.Fluid.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This script helps you set up the soup simulation quickly.
/// Add this to an empty GameObject to automatically configure everything.
/// </summary>
public class SoupSimulationSetup : MonoBehaviour
{
    [Header("Setup Instructions")]
    [TextArea(5, 10)]
    public string instructions = 
        "1. Make sure you have the FluidSim.compute shader in Assets/Resources/\n" +
        "2. Create particle rendering shaders (Particle3DSurf.shader and ParticleBillboard.shader)\n" +
        "3. Assign your bowl GameObject to the Bowl Controller field\n" +
        "4. Click 'Auto Setup Soup Simulation' button in Play mode\n" +
        "5. Adjust settings on the SoupFluidController component\n\n" +
        "Controls:\n" +
        "WASD - Tilt the bowl\n" +
        "Space - Pause/Resume simulation\n" +
        "R - Reset simulation\n" +
        "Q - Slow motion mode";
    
    [Header("Setup Configuration")]
    public BowlTiltController bowlController;
    public ComputeShader fluidComputeShader;
    public Shader particleShader3D;
    public Shader particleBillboardShader;
    
    [Header("Soup Settings")]
    public int soupParticleCount = 1500;
    public Vector3 soupSpawnSize = new Vector3(0.8f, 0.3f, 0.8f);
    public float soupViscosity = 0.3f;
    public Color soupBaseColor = new Color(0.8f, 0.4f, 0.2f);
    
    private SoupFluidController soupController;
    
    [ContextMenu("Auto Setup Soup Simulation")]
    public void AutoSetupSoupSimulation()
    {
        Debug.Log("=== SOUP SIMULATION SETUP ===");
        
        // Step 1: Find or create SoupFluidController
        soupController = UnityEngine.Object.FindFirstObjectByType<SoupFluidController>();
        if (soupController == null)
        {
            GameObject soupObj = new GameObject("SoupSimulation");
            soupController = soupObj.AddComponent<SoupFluidController>();
            Debug.Log("✓ Created SoupFluidController");
        }
        else
        {
            Debug.Log("✓ Found existing SoupFluidController");
        }
        
        // Step 2: Auto-find bowl controller
        if (bowlController == null)
        {
            bowlController = UnityEngine.Object.FindFirstObjectByType<BowlTiltController>();
        }
        
        if (bowlController == null)
        {
            Debug.LogError("✗ No BowlTiltController found! Please assign one or make sure it exists in the scene.");
            return;
        }
        else
        {
            Debug.Log("✓ Found BowlTiltController");
        }
        
        Debug.Log("=== SETUP COMPLETE! ===");
        Debug.Log("Your soup simulation is ready! Use WASD to tilt the bowl and watch the soup flow!");
        
        // If we have a controller, restart it
        if (soupController != null)
        {
            soupController.ResetSimulation();
            Debug.Log("Restarting simulation...");
        }
    }
    
    void Update()
    {
        // Additional controls for the simulation
        if (soupController != null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                soupController.TogglePause();
                Debug.Log("Simulation " + (soupController.IsSimulationRunning ? "resumed" : "paused"));
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                soupController.ResetSimulation();
                Debug.Log("🍲 Soup simulation reset");
            }
        }
    }
    
    void OnValidate()
    {
        // Auto-find components when values change in inspector
        if (bowlController == null)
            bowlController = UnityEngine.Object.FindFirstObjectByType<BowlTiltController>();
    }
}