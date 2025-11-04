using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Tvg.Sys;

namespace Tvg
{
    public class TvgTexture : IDisposable
    {
        // Tvg Handles
        private readonly IntPtr __canvas;
        private readonly IntPtr __animation;
        private readonly IntPtr __picture;

        // Texture Data
        private uint[] __buffer;
        private GCHandle __bufferHandle;
        private Texture2D __texture;
        private bool __isDirty = true;
        private bool __disposed = false;

        // Texture Properties
        private float __frame = 0.0f;
        public float duration { get; private set; }
        public float totalFrames { get; private set; }
        public float fps { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }

        public TvgTexture(string data)
        {
            TvgSys.Init();

            // Create the canvas and animation
            __canvas = TvgLib.tvg_swcanvas_create(TvgLib.EngineOption.None);
            __animation = TvgLib.tvg_animation_new();
            __picture = TvgLib.tvg_animation_get_picture(__animation);

            // Load the animation data
            TvgSys.Check(
                TvgLib.tvg_picture_load_data(__picture, data, (uint)data.Length, "", "", true),
                "Failed to load animation data");

            TvgSys.Check(
                TvgLib.tvg_canvas_push(__canvas, __picture),
                "Failed to push picture to canvas");

            // Position the picture
            // Unity is Y-up, but ThorVG is Y-down
            TvgSys.Check(
                TvgLib.tvg_picture_set_origin(__picture, 0, 1),
                "Failed to set picture origin");

            // Get the animation info
            TvgSys.Check(
                TvgLib.tvg_picture_get_size(__picture, out float w, out float h),
                "Failed to get picture size");

            TvgSys.Check(
                TvgLib.tvg_animation_get_duration(__animation, out float d),
                "Failed to get animation duration");

            TvgSys.Check(
                TvgLib.tvg_animation_get_total_frame(__animation, out float t),
                "Failed to get animation total frame");

            width = (int)w;
            height = (int)h;
            duration = d;
            totalFrames = t;
            fps = d > 0.0f ? t / d : 0.0f;

            // Create the texture
            __texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Resize(width, height);
        }

        ~TvgTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (__disposed)
                return;

            if (disposing)
            {
                // Clean up Unity managed resources (only on main thread)
                if (__texture != null)
                {
                    // Use DestroyImmediate in edit mode, Destroy at runtime
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(__texture);
                    else
                        UnityEngine.Object.DestroyImmediate(__texture);
                    
                    __texture = null;
                }
            }

            // Clean up native ThorVG resources (safe from any thread)
            if (__animation != IntPtr.Zero)
            {
                TvgLib.tvg_animation_del(__animation);
            }

            if (__canvas != IntPtr.Zero)
            {
                TvgLib.tvg_canvas_destroy(__canvas);
            }

            // Free the GCHandle
            if (__bufferHandle.IsAllocated)
            {
                __bufferHandle.Free();
            }

            __disposed = true;
        }

        public void Resize(int w, int h)
        {
            width = w;
            height = h;

            // Set the picture size
            TvgSys.Check(
                TvgLib.tvg_picture_set_size(__picture, width, -height),
                "Failed to set picture size");

            // Free the buffer
            if (__bufferHandle.IsAllocated)
                __bufferHandle.Free();

            // Create the buffer
            __buffer = new uint[width * height];
            __bufferHandle = GCHandle.Alloc(__buffer, GCHandleType.Pinned);

            // Resize the texture
            __texture.Reinitialize(width, height);

            // Set the target
            TvgSys.Check(
                TvgLib.tvg_swcanvas_set_target(
                    __canvas,
                    __bufferHandle.AddrOfPinnedObject(),
                    (uint)width,
                    (uint)width,
                    (uint)height,
                    TvgLib.ColorSpace.Abgr8888),
                "Failed to set target");

            __isDirty = true;
        }

        public float frame {
            get => __frame;
            set
            {
                if (value == __frame || totalFrames <= 1) return;

                // Wrap the frame value
                __frame = ((value % totalFrames) + totalFrames) % totalFrames;

                TvgSys.Check(
                    TvgLib.tvg_animation_set_frame(__animation, __frame),
                    "Failed to set animation frame");
                __isDirty = true;
            }
        }

        public Texture2D Texture()
        {
            if (!__isDirty) return __texture;

            // Draw the canvas
            TvgSys.Check(
                TvgLib.tvg_canvas_update(__canvas),
                "Failed to update canvas");

            TvgSys.Check(
                TvgLib.tvg_canvas_draw(__canvas, true),
                "Failed to draw canvas");

            TvgSys.Check(
                TvgLib.tvg_canvas_sync(__canvas),
                "Failed to sync canvas");


            // Update the texture
            __texture.SetPixelData(__buffer, 0);
            __texture.Apply();

            __isDirty = false;
            return __texture;
        }
    }
}