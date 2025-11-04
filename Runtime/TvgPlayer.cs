using System;
using UnityEngine;

namespace Tvg
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TvgPlayer : MonoBehaviour
    {   
        [SerializeField] 
        [Tooltip("Drag and drop a Lottie .json file here")]
        private TextAsset source;
        
        [SerializeField] 
        [Tooltip("Animation playback speed multiplier")]
        private float speed = 1.0f;

        private TvgTexture __texture;
        private SpriteRenderer __spriteRenderer;
        private Sprite __currentSprite;
        private bool __loaded;
        private bool __isAnimated;
        private bool __needsReload;

        private void Start()
        {
            // Load the initial content
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

            // Ensure sprite renderer is assigned
            if (__spriteRenderer == null)
                __spriteRenderer = GetComponent<SpriteRenderer>();

            if (source == null)
            {
                if (__spriteRenderer != null)
                    __spriteRenderer.sprite = null;
                return;
            }

            // Get the text data from the source
            string dataString = source.text;
            
            if (string.IsNullOrEmpty(dataString))
            {
                if (__spriteRenderer != null)
                    __spriteRenderer.sprite = null;
                return;
            }

            try
            {
                // Load the texture - ThorVG automatically detects if it's SVG or Lottie
                __texture = new TvgTexture(dataString);
                __isAnimated = __texture.totalFrames > 1;
                UpdateSprite();
                
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
            // Clean up old sprite (use appropriate destroy method)
            if (__currentSprite != null)
            {
                if (Application.isPlaying)
                    Destroy(__currentSprite);
                else
                    DestroyImmediate(__currentSprite);
                __currentSprite = null;
            }

            // Clean up old texture
            if (__texture != null)
            {
                __texture.Dispose();
                __texture = null;
            }

            __loaded = false;
        }

        private void UpdateSprite()
        {
            // Destroy old sprite if it exists (use appropriate method)
            if (__currentSprite != null)
            {
                if (Application.isPlaying)
                    Destroy(__currentSprite);
                else
                    DestroyImmediate(__currentSprite);
            }

            // Create and assign new sprite
            __currentSprite = Sprite.Create(
                __texture.Texture(),
                new Rect(0, 0, __texture.width, __texture.height),
                new Vector2(0.5f, 0.5f));
            
            __spriteRenderer.sprite = __currentSprite;
        }

        private void Play()
        {
            if (!__loaded) return;
            if (!__isAnimated) return;
            if (Mathf.Approximately(speed, 0.0f)) return;

            __texture.frame += Time.deltaTime * __texture.fps * speed;
            UpdateSprite();
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
