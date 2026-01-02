/*
 * Copyright (c) 2025 - 2026 ThorVG project. All rights reserved.

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using System.IO;

namespace Tvg.Editor
{
    /// <summary>
    /// Custom importer for SVG files that creates SvgAsset instances
    /// with rendered thumbnail previews for the Unity Project window.
    /// </summary>
    [ScriptedImporter(1, "svg")]
    public class SvgImporter : ScriptedImporter
    {
        [Header("Sprite Settings")]
        [SerializeField]
        [Tooltip("Render scale factor")]
        private readonly float scale = 1.0f;

        [SerializeField]
        [Tooltip("Pixels per unit for the sprite (affects size in world space)")]
        private readonly float pixelsPerUnit = 100.0f;

        [SerializeField]
        [Tooltip("Pivot point of the sprite")]
        private readonly Vector2 pivot = new Vector2(0.5f, 0.5f);

        [SerializeField]
        [Tooltip("Sprite mesh type")]
        private readonly SpriteMeshType meshType = SpriteMeshType.Tight;

        [SerializeField]
        [Range(0, 32)]
        [Tooltip("Extrude edges for sprite mesh")]
        private readonly uint extrudeEdges = 1;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Read the SVG file
            string svgText = File.ReadAllText(ctx.assetPath);
            
            // Generate texture and sprite
            (Texture2D texture, Sprite sprite) = GenerateTextureAndSprite(svgText, ctx.assetPath);
            
            // Add texture as sub-asset
            if (texture != null)
            {
                ctx.AddObjectToAsset("texture", texture);
            }
            
            // Add sprite as MAIN asset - this makes it behave like a PNG import
            if (sprite != null)
            {
                ctx.AddObjectToAsset("sprite", sprite, texture);
                ctx.SetMainObject(sprite);  // Sprite is the main asset!
            }
            
            // Create SvgAsset to store raw SVG data (for TvgPlayer use)
            SvgAsset svgAsset = ScriptableObject.CreateInstance<SvgAsset>();
            svgAsset.SetData(svgText, texture, sprite);
            ctx.AddObjectToAsset("svgData", svgAsset);
        }

        private (Texture2D, Sprite) GenerateTextureAndSprite(string svgData, string assetPath)
        {
            try
            {   
                // Load the SVG and render at its natural size
                using (TvgTexture tvgTexture = new TvgTexture(svgData))
                {
                    int width = tvgTexture.width;
                    int height = tvgTexture.height;

                    if (Mathf.Abs(scale - 1.0f) > 1e-6f)
                    {
                        width = (int)(width * scale);
                        height = (int)(height * scale);
                        tvgTexture.Resize(width, height);
                    }
                    
                    // Get the rendered texture
                    Texture2D sourceTexture = tvgTexture.Texture();
                    
                    // Create a copy for the asset (can't use the original)
                    Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    texture.name = "texture";
                    texture.hideFlags = HideFlags.HideInHierarchy;
                    
                    // Copy the pixel data
                    Graphics.CopyTexture(sourceTexture, texture);
                    texture.Apply(false, false);
                    
                    // Create a sprite from the texture with user-defined settings
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, width, height),
                        pivot,
                        pixelsPerUnit,
                        extrudeEdges,
                        meshType
                    );
                    sprite.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    sprite.hideFlags = HideFlags.None;  // Show in hierarchy so it can be selected
                    
                    return (texture, sprite);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to generate SVG assets for {assetPath}: {e.Message}");
                
                // Return placeholder
                Texture2D placeholder = new Texture2D(2, 2);
                placeholder.hideFlags = HideFlags.HideInHierarchy;
                placeholder.SetPixels(new Color[] { Color.gray, Color.gray, Color.gray, Color.gray });
                placeholder.Apply();
                
                Sprite placeholderSprite = Sprite.Create(placeholder, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                placeholderSprite.hideFlags = HideFlags.HideInHierarchy;
                
                return (placeholder, placeholderSprite);
            }
        }
    }
}

