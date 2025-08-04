using UnityEngine;
using Seb.Fluid.Simulation;
using Seb.Fluid.Rendering;

public class SoupFluidController : MonoBehaviour
{
    [Header("Fluid Position & Size Controls")]
    [SerializeField] private Vector3 fluidPosition = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 fluidSize = new Vector3(2f, 1f, 2f);
    [SerializeField] private float fluidScale = 1f;
    
    [Header("Soup Simulation Settings")]
    [SerializeField] private float soupGravity = -1.0f;
    [SerializeField] private float soupViscosity = 0.3f;
    [SerializeField] private float soupDensity = 1000f;
    [SerializeField] private int particleCount = 2000;
    [Range(0f, 1f)]
    [SerializeField] private float soupThickness = 0.7f;
    
    [Header("Visual Settings")]
    [SerializeField] private ParticleDisplay3D.DisplayMode renderMode = ParticleDisplay3D.DisplayMode.Billboard;
    [SerializeField] private float particleSize = 3.0f;
    [SerializeField] private Gradient soupColor;
    [SerializeField] private bool enableFoam = true;
    
    [Header("Bowl Integration")]
    [SerializeField] private BowlTiltController bowlController;
    [SerializeField] private float bowlInfluenceStrength = 1f;
    [SerializeField] private bool followBowlRotation = true;
    
    [Header("Runtime Controls")]
    [SerializeField] private bool pauseSimulation = false;
    [SerializeField] private bool resetSimulation = false;
    
    // Internal references
    public FluidSim fluidSim;
    public Spawner3D spawner;
    public ParticleDisplay3D particleRenderer;
    private GameObject fluidSimContainer;
    
    // Bowl tilt tracking
    private Vector3 lastBowlRotation;
    private Vector3 gravityDirection = Vector3.down;

    void Start()
    {
        Invoke(nameof(DelayedSetup), 0.1f);
    }
    
    void DelayedSetup()
    {
        try
        {
            SetupFluidSimulation();
            SetupDefaultSoupColor();
            
            if (bowlController == null)
                bowlController = UnityEngine.Object.FindFirstObjectByType<BowlTiltController>();
                
            lastBowlRotation = bowlController ? bowlController.transform.eulerAngles : Vector3.zero;
            
            Debug.Log("Soup simulation setup completed successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to setup soup simulation: {e.Message}");
        }
    }

    void SetupFluidSimulation()
    {
        // Create container for fluid simulation
        fluidSimContainer = new GameObject("FluidSimulation");
        fluidSimContainer.transform.SetParent(transform);
        
        // Setup Spawner
        GameObject spawnerObj = new GameObject("FluidSpawner");
        spawnerObj.transform.SetParent(fluidSimContainer.transform);
        spawner = spawnerObj.AddComponent<Spawner3D>();
        
        // Setup spawn region for soup
        spawner.particleSpawnDensity = particleCount;
        spawner.jitterStrength = 0.05f;
        spawner.initialVel = new Vector3(0, -0.5f, 0);
        spawner.spawnRegions = new Spawner3D.SpawnRegion[1];
        spawner.spawnRegions[0] = new Spawner3D.SpawnRegion
        {
            centre = transform.position + fluidPosition,
            size = 1.5f,
            debugDisplayCol = Color.cyan
        };
        
        // Setup FluidSim
        GameObject fluidSimObj = new GameObject("FluidSimulator");
        fluidSimObj.transform.SetParent(fluidSimContainer.transform);
        fluidSim = fluidSimObj.AddComponent<FluidSim>();
        
        // Configure simulation for soup
        fluidSim.spawner = spawner;
        fluidSim.gravity = soupGravity;
        fluidSim.viscosityStrength = soupViscosity;
        fluidSim.targetDensity = soupDensity;
        fluidSim.collisionDamping = 0.95f;
        
        fluidSim.smoothingRadius = 0.12f;
        fluidSim.pressureMultiplier = 200f;
        fluidSim.nearPressureMultiplier = 8f;
        fluidSim.normalTimeScale = 1f;
        fluidSim.iterationsPerFrame = 4;
        
        fluidSimObj.transform.position = transform.position + fluidPosition;
        fluidSimObj.transform.localScale = fluidSize;
        
        // Setup Renderer
        GameObject particleRendererObj = new GameObject("FluidRenderer");
        particleRendererObj.transform.SetParent(fluidSimContainer.transform);
        particleRenderer = particleRendererObj.AddComponent<ParticleDisplay3D>();
        
        Invoke(nameof(SetupRenderer), 0.2f);
        
        UpdateFluidTransform();
    }
    
    void SetupRenderer()
    {
        if (particleRenderer == null || fluidSim == null) return;
        
        particleRenderer.sim = fluidSim;
        particleRenderer.mode = renderMode;
        particleRenderer.scale = particleSize;
        particleRenderer.colourMap = soupColor;
        particleRenderer.gradientResolution = 64;
        particleRenderer.meshResolution = 4;
        particleRenderer.velocityDisplayMax = 15f;
        
        Debug.Log($"🎨 Renderer setup complete! Mode: {renderMode}, Particles: {CurrentParticleCount}");
    }
    
    void SetupDefaultSoupColor()
    {
        if (soupColor == null)
        {
            soupColor = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.8f, 0.4f, 0.2f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.9f, 0.6f, 0.3f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.8f, 0.4f), 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            soupColor.SetKeys(colorKeys, alphaKeys);
        }
    }

    void Update()
    {
        HandleRuntimeControls();
        UpdateBowlIntegration();
        UpdateFluidTransform();
        UpdateSimulationParameters();
    }
    
    void HandleRuntimeControls()
    {
        if (resetSimulation)
        {
            resetSimulation = false;
            ResetSimulation();
        }
    }
    
    void UpdateBowlIntegration()
    {
        if (bowlController == null || fluidSim == null) return;
        
        Vector3 currentBowlRotation = bowlController.transform.eulerAngles;
        
        if (followBowlRotation)
        {
            Vector3 bowlUp = bowlController.transform.up;
            gravityDirection = -bowlUp * bowlInfluenceStrength;
            
            Vector3 totalGravity = gravityDirection * Mathf.Abs(soupGravity);
            fluidSim.gravity = totalGravity.magnitude * Mathf.Sign(soupGravity);
            
            if (fluidSimContainer != null)
            {
                fluidSimContainer.transform.position = bowlController.transform.position + fluidPosition;
                fluidSimContainer.transform.rotation = bowlController.transform.rotation;
            }
        }
        
        lastBowlRotation = currentBowlRotation;
    }
    
    void UpdateFluidTransform()
    {
        if (fluidSimContainer == null) return;
        
        if (!followBowlRotation)
        {
            fluidSimContainer.transform.position = transform.position + fluidPosition;
            fluidSimContainer.transform.localScale = fluidSize * fluidScale;
        }
        else
        {
            fluidSimContainer.transform.localScale = fluidSize * fluidScale;
        }
    }
    
    void UpdateSimulationParameters()
    {
        if (fluidSim == null) return;
        
        if (!followBowlRotation)
            fluidSim.gravity = soupGravity;
            
        fluidSim.viscosityStrength = soupViscosity;
        fluidSim.targetDensity = soupDensity;
        fluidSim.collisionDamping = soupThickness;
        
        if (particleRenderer != null)
        {
            particleRenderer.mode = renderMode;
            particleRenderer.scale = particleSize;
            particleRenderer.colourMap = soupColor;
        }
    }
    
    public void ResetSimulation()
    {
        if (fluidSimContainer != null)
        {
            Object.DestroyImmediate(fluidSimContainer);
            Invoke(nameof(DelayedSetup), 0.1f);
            Debug.Log("🔄 Completely restarting fluid simulation...");
        }
    }

    public void SetFluidPosition(Vector3 newPosition)
    {
        fluidPosition = newPosition;
    }
    
    public void SetFluidSize(Vector3 newSize)
    {
        fluidSize = newSize;
    }
    
    public void SetFluidScale(float newScale)
    {
        fluidScale = newScale;
    }
    
    public void SetSoupViscosity(float viscosity)
    {
        soupViscosity = Mathf.Max(0f, viscosity);
    }
    
    public void SetSoupDensity(float density)
    {
        soupDensity = Mathf.Max(1f, density);
    }
    
    public void SetParticleCount(int count)
    {
        particleCount = Mathf.Max(1, count);
        if (Application.isPlaying)
        {
            Debug.Log("Particle count changed. Simulation will be reset.");
            ResetSimulation();
        }
    }
    
    public void EnableFoam(bool enable)
    {
        enableFoam = enable;
    }
    
    public void SetRenderMode(ParticleDisplay3D.DisplayMode mode)
    {
        renderMode = mode;
        if (particleRenderer != null)
        {
            particleRenderer.mode = mode;
        }
    }
    
    public void SetParticleSize(float size)
    {
        particleSize = size;
        if (particleRenderer != null)
        {
            particleRenderer.scale = size;
        }
    }
    
    public void TogglePause()
    {
        pauseSimulation = !pauseSimulation;
        if (fluidSim != null)
        {
            fluidSim.enabled = !pauseSimulation;
        }
    }
    
    // Public properties for external access
    public bool IsSimulationRunning => fluidSim != null && fluidSim.enabled;
    public int CurrentParticleCount => fluidSim?.positionBuffer?.count ?? 0;
    public Vector3 CurrentGravityDirection => gravityDirection;
    
    void OnDestroy()
    {
        if (fluidSimContainer != null)
        {
            DestroyImmediate(fluidSimContainer);
        }
    }
}