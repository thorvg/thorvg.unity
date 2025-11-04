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
            __canvas = TvgLib.CanvasCreate();
            __animation = TvgLib.AnimationCreate();
            __picture = TvgLib.AnimationGetPicture(__animation);

            // Load the animation data
            TvgLib.PictureLoad(__picture, data);
            TvgLib.CanvasPush(__canvas, __picture);

            // Position the picture
            // Unity is Y-up, but ThorVG is Y-down
            TvgLib.PictureSetOrigin(__picture, 0, 1);

            // Get the animation info
            TvgLib.PictureGetSize(__picture, out float w, out float h);
            TvgLib.AnimationGetInfo(__animation, out float t, out float d);

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

            if (disposing && __texture != null)
            {
                // Clean up Unity managed resources (only on main thread)
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(__texture);
                else
                    UnityEngine.Object.DestroyImmediate(__texture);
                
                __texture = null;
            }

            // Clean up native ThorVG resources (safe from any thread)
            if (__animation != IntPtr.Zero)
            {
                TvgLib.AnimationDestroy(__animation);
            }

            if (__canvas != IntPtr.Zero)
            {
                TvgLib.CanvasDestroy(__canvas);
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
            TvgLib.PictureSetSize(__picture, width, -height);

            // Free the buffer
            if (__bufferHandle.IsAllocated)
                __bufferHandle.Free();

            // Create the buffer
            __buffer = new uint[width * height];
            __bufferHandle = GCHandle.Alloc(__buffer, GCHandleType.Pinned);

            // Resize the texture
            __texture.Reinitialize(width, height);

            // Set the target
            TvgLib.CanvasSetTarget(__canvas, __bufferHandle.AddrOfPinnedObject(), width, height);

            __isDirty = true;
        }

        public float frame
        {
            get => __frame;
            set
            {
                if (Mathf.Abs(value - __frame) < 1e-6f) return;
                __frame = value;

                // Wrap the frame value
                if (totalFrames <= 1) return;
                float wrappedFrame = ((__frame % totalFrames) + totalFrames) % totalFrames;

                TvgLib.AnimationSetFrame(__animation, wrappedFrame);
                __isDirty = true;
            }
        }

        public Texture2D Texture()
        {
            if (!__isDirty) return __texture;

            // Draw the canvas
            TvgLib.CanvasDraw(__canvas, true);

            // Update the texture
            __texture.SetPixelData(__buffer, 0);
            __texture.Apply();

            __isDirty = false;
            return __texture;
        }
    }
}