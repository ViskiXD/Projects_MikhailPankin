using UnityEngine;
using Seb.Fluid.Simulation;

namespace Seb.Fluid.Rendering
{
    public class ParticleDisplay3D : MonoBehaviour
    {
        [Header("Rendering Mode")]
        public DisplayMode mode = DisplayMode.Billboard;
        
        [Header("Rendering Settings")]
        public float scale = 1f;
        public Gradient colourMap;
        public int gradientResolution = 64;
        public float velocityDisplayMax = 15f;
        public int meshResolution = 4;
        
        [Header("Shaders")]
        public Shader shaderShaded;
        public Shader shaderBillboard;
        
        [Header("Simulation Reference")]
        public FluidSim sim;
        
        public Mesh mesh { get; private set; }
        public Material mat { get; private set; }
        private Texture2D colourTexture;
        private ComputeBuffer argsBuffer;
        
        public enum DisplayMode
        {
            Billboard,
            Mesh3D
        }
        
        void Start()
        {
            if (sim == null)
            {
                sim = FindFirstObjectByType<FluidSim>();
                if (sim == null)
                {
                    Debug.LogError("ParticleDisplay3D: No FluidSim found!");
                    return;
                }
            }
            
            InitializeRendering();
        }
        
        void InitializeRendering()
        {
            CreateMesh();
            CreateMaterial();
            CreateColourTexture();
            CreateArgsBuffer();
        }
        
        void CreateMesh()
        {
            mesh = CreateQuadMesh();
        }
        
        Mesh CreateQuadMesh()
        {
            Mesh quadMesh = new Mesh();
            quadMesh.name = "Billboard Quad";
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0),
                new Vector3(0.5f, 0.5f, 0)
            };
            
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            
            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            
            quadMesh.vertices = vertices;
            quadMesh.uv = uvs;
            quadMesh.triangles = triangles;
            quadMesh.RecalculateNormals();
            
            return quadMesh;
        }
        
        void CreateMaterial()
        {
            Shader targetShader = Shader.Find("Sprites/Default");
            
            if (targetShader == null)
            {
                Debug.LogError("ParticleDisplay3D: No suitable shader found!");
                return;
            }
            
            mat = new Material(targetShader);
            mat.name = $"Particle Material ({mode})";
        }
        
        void CreateColourTexture()
        {
            if (colourMap == null)
            {
                colourMap = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(new Color(0.8f, 0.4f, 0.2f), 0f);
                colorKeys[1] = new GradientColorKey(new Color(0.9f, 0.6f, 0.3f), 0.5f);
                colorKeys[2] = new GradientColorKey(new Color(1f, 0.8f, 0.4f), 1f);
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                
                colourMap.SetKeys(colorKeys, alphaKeys);
            }
            
            colourTexture = new Texture2D(gradientResolution, 1, TextureFormat.RGBA32, false);
            colourTexture.wrapMode = TextureWrapMode.Clamp;
            
            Color[] colors = new Color[gradientResolution];
            for (int i = 0; i < gradientResolution; i++)
            {
                float t = i / (float)(gradientResolution - 1);
                colors[i] = colourMap.Evaluate(t);
            }
            
            colourTexture.SetPixels(colors);
            colourTexture.Apply();
        }
        
        void CreateArgsBuffer()
        {
            if (mesh == null) return;
            
            argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            
            uint[] args = new uint[]
            {
                (uint)mesh.GetIndexCount(0),
                0,
                (uint)mesh.GetIndexStart(0),
                (uint)mesh.GetBaseVertex(0),
                0
            };
            
            argsBuffer.SetData(args);
        }
        
        void Update()
        {
            if (sim == null || sim.positionBuffer == null || mat == null || mesh == null)
                return;
                
            RenderParticles();
        }
        
        void RenderParticles()
        {
            if (sim.numParticles == 0) return;
            
            uint[] args = new uint[5];
            argsBuffer.GetData(args);
            args[1] = (uint)sim.numParticles;
            argsBuffer.SetData(args);
            
            Bounds bounds = new Bounds(transform.position, Vector3.one * 1000f);
            Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argsBuffer);
        }
        
        void OnDestroy()
        {
            if (mat != null)
            {
                if (Application.isPlaying)
                    Destroy(mat);
                else
                    DestroyImmediate(mat);
            }
            
            if (colourTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(colourTexture);
                else
                    DestroyImmediate(colourTexture);
            }
            
            argsBuffer?.Release();
        }
    }
}