using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Tvg.Sys;

namespace Tvg
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class TvgPlayer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Drag and drop a Lottie .json file here")]
        private TextAsset source;
        
        [SerializeField]
        [Tooltip("Starting frame of the animation")]
        private float __frame = 0.0f;
        
        [SerializeField]
        [Tooltip("Animation playback speed multiplier")]
        private float __speed = 1.0f;

        private TvgTexture __texture;
        private MeshRenderer __meshRenderer;
        private Material __material;
        private bool __loaded;
        private bool __isAnimated;
        private bool __needsReload;

        public float frame
        {
            get => __texture?.frame ?? __frame;
            set
            {
                if (__texture != null)
                    __texture.frame = value;
                else
                    __frame = value;
            }
        }

        public float speed
        {
            get => __speed;
            set => __speed = value;
        }

        private void Start()
        {
            // Wait for ThorVG to be ready (important for WebGL)
            StartCoroutine(InitializeWhenReady());
        }
        
        private IEnumerator InitializeWhenReady()
        {
            // Wait for ThorVG module to load (WebGL only, instant on native)
            while (!TvgSys.IsReady())
            {
                yield return null;
            }
            
            // Now safe to load content
            LoadSource();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                __needsReload = true;
            }
            else
            {
                // Preview first frame in edit mode
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) LoadSource();
                };
                #endif
            }
        }

        private void LoadSource()
        {
            // Clean up before loading
            Cleanup();
            
            __loaded = false;
            __needsReload = false;

            // Ensure mesh renderer is assigned
            if (__meshRenderer == null)
                __meshRenderer = GetComponent<MeshRenderer>();

            if (source == null)
            {
                if (__material != null)
                    __material.mainTexture = null;
                return;
            }

            // Get the text data from the source
            string dataString = source.text;
            
            if (string.IsNullOrEmpty(dataString))
            {
                if (__material != null)
                    __material.mainTexture = null;
                return;
            }

            try
            {
                // Load the texture - ThorVG automatically detects if it's SVG or Lottie
                __texture = new TvgTexture(dataString);
                __isAnimated = __texture.totalFrames > 1;
                __texture.frame = __frame;
                
                // Create mesh and material
                SetupMeshAndMaterial();
                
                // Set the flag
                __loaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load ThorVG content: {e.Message}");
                Cleanup();
            }
        }

        private void Cleanup()
        {
            // Clean up material
            if (__material != null)
            {
                if (Application.isPlaying)
                    Destroy(__material);
                else
                    DestroyImmediate(__material);
                __material = null;
            }

            // Clean up texture
            if (__texture != null)
            {
                __texture.Dispose();
                __texture = null;
            }

            __loaded = false;
        }

        private Mesh CreateQuadMesh(float width, float height)
        {
            var mesh = new Mesh();
            
            // Vertices (centered quad)
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfW, -halfH, 0),
                new Vector3(halfW, -halfH, 0),
                new Vector3(-halfW, halfH, 0),
                new Vector3(halfW, halfH, 0)
            };
            
            // UVs
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            
            // Triangles
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }

        private void SetupMeshAndMaterial()
        {
            // Setup mesh
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null)
            {
                // Create quad mesh sized to texture (in world units, 1 pixel = 0.01 units)
                meshFilter.mesh = CreateQuadMesh(__texture.width * 0.01f, __texture.height * 0.01f);
            }
            
            // Create material with unlit transparent shader
            __material = new Material(Shader.Find("Unlit/Transparent"));
            __material.mainTexture = __texture.Texture();
            __material.mainTextureScale = new Vector2(1, -1); // Flip texture vertically
            __meshRenderer.material = __material;
        }

        private void Play()
        {
            if (!__loaded) return;
            if (!__isAnimated) return;
            if (Mathf.Approximately(speed, 0.0f)) return;

            // Update the frame
            __texture.frame += Time.deltaTime * __texture.fps * speed;
            
            __texture.Texture();
        }

        private void Update()
        {
            // Check if we need to reload (source changed in inspector during play mode)
            if (__needsReload)
            {
                LoadSource();
            }

            // Play animation
            Play();
        }

        private void OnDestroy()
        {
            // Clean up all resources when component is destroyed
            Cleanup();
        }
    }
}
