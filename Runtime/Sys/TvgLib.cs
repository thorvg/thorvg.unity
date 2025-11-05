using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tvg.Sys
{
    internal static class TvgLib
    {
        /// <summary>
        /// Unified handle structure that works for both WebGL and Native platforms
        /// </summary>
        internal struct AnimationHandle
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Simple integer ID
            public int Id;
            public IntPtr Buffer;
            public int BufferSize;
#else
            // Native: Separate handles for canvas, animation, and picture
            public IntPtr Canvas;
            public IntPtr Animation;
            public IntPtr Picture;
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        private const string DLL_NAME = "__Internal";
#else
        private const string DLL_NAME = "libthorvg";
#endif

        private enum Result
        {
                Success = 0,
                InvalidArguments,
                InsufficientCondition,
                FailedAllocation,
                MemoryCorruption,
                NonSupport,
                Unknown = 255
        }

        private enum ColorSpace
        {   
                Abgr8888 = 0
        }

        private enum EngineOption
        {
                // No special features
                None = 0
        }

        private static void Check(int code, string msg)
        {
            switch ((TvgLib.Result)code)
            {
                case TvgLib.Result.Success:
                    return;
                case TvgLib.Result.InvalidArguments:
                    msg += " (Invalid Arguments)";
                    break;
                case TvgLib.Result.InsufficientCondition:
                    msg += " (Insufficient Condition)";
                    break;
                case TvgLib.Result.FailedAllocation:
                    msg += " (Failed Allocation)";
                    break;
                case TvgLib.Result.MemoryCorruption:
                    msg += " (Memory Corruption)";
                    break;
                case TvgLib.Result.NonSupport:
                    msg += " (Non Support)";
                    break;
                case TvgLib.Result.Unknown:
                    msg += " (Unknown)";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
            throw new Exception("ThorVG: " + msg);
        }
        
/************************************************************************/
/* Engine API                                                           */
/************************************************************************/
        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport(DLL_NAME)]
        private static extern int ThorVG_Init();
        
        [DllImport(DLL_NAME)]
        private static extern void ThorVG_Term();
        
        [DllImport(DLL_NAME)]
        private static extern int ThorVG_IsReady();
#else
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_engine_init(int threads);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_engine_term();
#endif

        public static void EngineInit()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ThorVG_Init();
#else
            Check(tvg_engine_init(0), "Engine Init");
#endif
        }

        public static void EngineTerm()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ThorVG_Term();
#else
            Check(tvg_engine_term(), "Engine Term");
#endif
        }

        public static int IsReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return ThorVG_IsReady();
#else
            return 1; // Native is always ready
#endif
        }
        
/************************************************************************/
/* WebGL API                                                            */
/************************************************************************/

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL native functions
        [DllImport(DLL_NAME)]
        private static extern int ThorVG_CreateAnimation(string data);
        
        [DllImport(DLL_NAME)]
        private static extern void ThorVG_DestroyAnimation(int id);
        
        [DllImport(DLL_NAME)]
        private static extern int ThorVG_GetSize(int id, out float width, out float height);
        
        [DllImport(DLL_NAME)]
        private static extern float ThorVG_GetDuration(int id);
        
        [DllImport(DLL_NAME)]
        private static extern float ThorVG_GetTotalFrame(int id);
        
        [DllImport(DLL_NAME)]
        private static extern int ThorVG_SetFrame(int id, float frame);
        
        [DllImport(DLL_NAME)]
        private static extern int ThorVG_Resize(int id, int width, int height);
        
        [DllImport(DLL_NAME)]
        private static extern int ThorVG_RenderToBuffer(int id, IntPtr bufferPtr, int bufferSize);
#else
/************************************************************************/
/* Canvas API                                                           */
/************************************************************************/

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tvg_swcanvas_create(EngineOption option);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_destroy(IntPtr handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_push(IntPtr handle, IntPtr paint);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_swcanvas_set_target(IntPtr handle, IntPtr buffer, uint stride, uint w, uint h, ColorSpace colorspace);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_sync(IntPtr handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_update(IntPtr handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_draw(IntPtr handle, bool clear);

/************************************************************************/
/* Animation API                                                        */
/************************************************************************/

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tvg_animation_new();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_del(IntPtr handle);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_set_frame(IntPtr handle, float frame);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_get_total_frame(IntPtr handle, out float totalFrame);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_get_duration(IntPtr handle, out float duration);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tvg_animation_get_picture(IntPtr handle);

/************************************************************************/
/* Picture API                                                          */
/************************************************************************/
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_load_data(IntPtr handle, string data, uint size, string mimetype, string rpath, bool copy);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_set_size(IntPtr handle, float w, float h);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_get_size(IntPtr handle, out float w, out float h);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_set_origin(IntPtr handle, float x, float y);
#endif

/************************************************************************/
/* Public API                                                           */
/************************************************************************/

        public static AnimationHandle CreateAnimation(string data)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int id = ThorVG_CreateAnimation(data);
            if (id == 0)
            {
                throw new Exception("Failed to create animation");
            }
            return new AnimationHandle { Id = id };
#else
            // Create canvas and animation
            IntPtr canvas = tvg_swcanvas_create(EngineOption.None);
            IntPtr animation = tvg_animation_new();
            IntPtr picture = tvg_animation_get_picture(animation);

            // Load the animation data
            Check(tvg_picture_load_data(picture, data, (uint)data.Length, "", "", true), "Picture Load");
            Check(tvg_canvas_push(canvas, picture), "Canvas Push");

            // Position the picture (Unity is Y-up, but ThorVG is Y-down)
            Check(tvg_picture_set_origin(picture, 0, 1), "Picture Set Origin");

            return new AnimationHandle 
            { 
                Canvas = canvas,
                Animation = animation,
                Picture = picture
            };
#endif
        }

        public static void DestroyAnimation(in AnimationHandle handle)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ThorVG_DestroyAnimation(handle.Id);
#else
            Check(tvg_animation_del(handle.Animation), "Animation Del");
            Check(tvg_canvas_destroy(handle.Canvas), "Canvas Destroy");
#endif
        }

        public static void GetSize(in AnimationHandle handle, out float width, out float height)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Check(ThorVG_GetSize(handle.Id, out width, out height), "Get Size");
#else
            Check(tvg_picture_get_size(handle.Picture, out width, out height), "Picture Get Size");
#endif
        }

        public static void GetDuration(in AnimationHandle handle, out float duration)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            duration = ThorVG_GetDuration(handle.Id);
#else
            Check(tvg_animation_get_duration(handle.Animation, out duration), "Animation Get Duration");
#endif
        }

        public static void GetTotalFrame(in AnimationHandle handle, out float totalFrame)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            totalFrame = ThorVG_GetTotalFrame(handle.Id);
#else
            Check(tvg_animation_get_total_frame(handle.Animation, out totalFrame), "Animation Get Total Frame");
#endif
        }

        public static void SetFrame(in AnimationHandle handle, float frame)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Check(ThorVG_SetFrame(handle.Id, frame), "Set Frame");
#else
            Check(tvg_animation_set_frame(handle.Animation, frame), "Animation Set Frame");
#endif
        }

        public static void Resize(in AnimationHandle handle, int width, int height)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Check(ThorVG_Resize(handle.Id, width, height), "Resize");
#else
            Check(tvg_picture_set_size(handle.Picture, width, -height), "Picture Set Size");
#endif
        }

        public static void SetCanvasTarget(ref AnimationHandle handle, IntPtr buffer, int w, int h)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            handle.Buffer = buffer;
            handle.BufferSize = w * h * 4;
#else
            Check(
                tvg_swcanvas_set_target(
                        handle.Canvas,
                        buffer,
                        (uint)w,
                        (uint)w,
                        (uint)h,
                        ColorSpace.Abgr8888),
                "Canvas Set Target");
            Check(tvg_canvas_sync(handle.Canvas), "Canvas Sync");
#endif
        }

        public static void DrawCanvas(in AnimationHandle handle)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Check(ThorVG_RenderToBuffer(handle.Id, handle.Buffer, handle.BufferSize), "Render To Buffer");
#else
            Check(tvg_canvas_update(handle.Canvas), "Canvas Update");
            Check(tvg_canvas_draw(handle.Canvas, true), "Canvas Draw");
            Check(tvg_canvas_sync(handle.Canvas), "Canvas Sync");
#endif
        }
    }
}
