using System.Collections;
using UnityEngine;

namespace Seb.Fluid.Simulation
{
    public class FluidSim : MonoBehaviour
    {
        [Header("Simulation Settings")]
        public ComputeShader compute;
        public Spawner3D spawner;
        public float gravity = -9.81f;
        public float viscosityStrength = 0.2f;
        public float targetDensity = 1000f;
        public float smoothingRadius = 0.15f;
        public float pressureMultiplier = 100f;
        public float nearPressureMultiplier = 5f;
        public float collisionDamping = 0.9f;
        public bool foamActive = false;
        
        [Header("Performance")]
        public int iterationsPerFrame = 2;
        public float normalTimeScale = 1f;
        
        [Header("Debug")]
        public int numParticles;
        
        // Buffers
        public ComputeBuffer positionBuffer { get; private set; }
        public ComputeBuffer velocityBuffer { get; private set; }
        public ComputeBuffer densityBuffer { get; private set; }
        
        private bool isInitialized = false;
        
        void Start()
        {
            if (spawner == null)
            {
                Debug.LogError("FluidSim: No spawner assigned!");
                return;
            }
            
            InitializeSimulation();
        }
        
        void InitializeSimulation()
        {
            var spawnData = spawner.GetSpawnData();
            numParticles = spawnData.points.Length;
            
            if (numParticles == 0)
            {
                Debug.LogWarning("FluidSim: No particles to spawn!");
                return;
            }
            
            Debug.Log($"FluidSim: Initializing {numParticles} particles");
            
            // Create buffers
            CreateBuffers();
            
            // Initialize particle data
            InitializeParticleData(spawnData);
            
            isInitialized = true;
        }
        
        void CreateBuffers()
        {
            ReleaseBuffers();
            
            positionBuffer = new ComputeBuffer(numParticles, sizeof(float) * 3);
            velocityBuffer = new ComputeBuffer(numParticles, sizeof(float) * 3);
            densityBuffer = new ComputeBuffer(numParticles, sizeof(float));
        }
        
        void InitializeParticleData(Spawner3D.SpawnData spawnData)
        {
            positionBuffer.SetData(spawnData.points);
            velocityBuffer.SetData(spawnData.velocities);
            
            float[] densities = new float[numParticles];
            for (int i = 0; i < numParticles; i++)
            {
                densities[i] = targetDensity;
            }
            densityBuffer.SetData(densities);
        }
        
        void Update()
        {
            if (!isInitialized || numParticles == 0) return;
            
            // Simple CPU simulation
            RunCPUSimulation(Time.deltaTime * normalTimeScale);
        }
        
        void RunCPUSimulation(float deltaTime)
        {
            Vector3[] positions = new Vector3[numParticles];
            Vector3[] velocities = new Vector3[numParticles];
            
            positionBuffer.GetData(positions);
            velocityBuffer.GetData(velocities);
            
            for (int i = 0; i < numParticles; i++)
            {
                velocities[i] += Vector3.up * gravity * deltaTime;
                velocities[i] *= (1f - viscosityStrength * deltaTime);
                positions[i] += velocities[i] * deltaTime;
                
                Vector3 bounds = transform.localScale * 0.5f;
                Vector3 center = transform.position;
                
                if (positions[i].x < center.x - bounds.x) 
                {
                    positions[i].x = center.x - bounds.x;
                    velocities[i].x *= -collisionDamping;
                }
                if (positions[i].x > center.x + bounds.x) 
                {
                    positions[i].x = center.x + bounds.x;
                    velocities[i].x *= -collisionDamping;
                }
                
                if (positions[i].y < center.y - bounds.y) 
                {
                    positions[i].y = center.y - bounds.y;
                    velocities[i].y *= -collisionDamping;
                }
                if (positions[i].y > center.y + bounds.y) 
                {
                    positions[i].y = center.y + bounds.y;
                    velocities[i].y *= -collisionDamping;
                }
                
                if (positions[i].z < center.z - bounds.z) 
                {
                    positions[i].z = center.z - bounds.z;
                    velocities[i].z *= -collisionDamping;
                }
                if (positions[i].z > center.z + bounds.z) 
                {
                    positions[i].z = center.z + bounds.z;
                    velocities[i].z *= -collisionDamping;
                }
            }
            
            positionBuffer.SetData(positions);
            velocityBuffer.SetData(velocities);
        }
        
        void ReleaseBuffers()
        {
            positionBuffer?.Release();
            velocityBuffer?.Release();
            densityBuffer?.Release();
        }
        
        void OnDestroy()
        {
            ReleaseBuffers();
        }
        
        void OnDisable()
        {
            ReleaseBuffers();
        }
    }
}