using System;
using System.Runtime.InteropServices;

namespace Tvg.Sys
{
    internal static class TvgLib
    {
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
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_engine_init(int threads);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_engine_term();

        public static void EngineInit()
        {
            Check(tvg_engine_init(0), "Engine Init");
        }

        public static void EngineTerm()
        {
            Check(tvg_engine_term(), "Engine Term");
        }
        
/************************************************************************/
/* Canvas API                                                           */
/************************************************************************/

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tvg_swcanvas_create(EngineOption option);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_destroy(IntPtr handle);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_push(IntPtr handle, IntPtr paint);

        public static IntPtr CanvasCreate()
        {
            return tvg_swcanvas_create(EngineOption.None);
        }

        public static void CanvasDestroy(IntPtr handle)
        {
            Check(tvg_canvas_destroy(handle), "Canvas Destroy");
        }

        public static void CanvasPush(IntPtr handle, IntPtr paint)
        {
            Check(tvg_canvas_push(handle, paint), "Canvas Push");
        }

        // Drawing APIs

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_swcanvas_set_target(IntPtr handle, IntPtr buffer, uint stride, uint w, uint h, ColorSpace colorspace);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_sync(IntPtr handle);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_update(IntPtr handle);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_canvas_draw(IntPtr handle, bool clear);

        public static void CanvasSetTarget(IntPtr handle, IntPtr buffer, int w, int h)
        {
            Check(
                tvg_swcanvas_set_target(
                        handle,
                        buffer,
                        (uint)w,
                        (uint)w,
                        (uint)h,
                        ColorSpace.Abgr8888),
                "Canvas Set Target");
            Check(tvg_canvas_sync(handle), "Canvas Sync");
        }

        public static void CanvasDraw(IntPtr handle, bool clear)
        {
            Check(tvg_canvas_update(handle), "Canvas Update");
            Check(tvg_canvas_draw(handle, clear), "Canvas Draw");
            Check(tvg_canvas_sync(handle), "Canvas Sync");
        }

/************************************************************************/
/* Animation API                                                        */
/************************************************************************/

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tvg_animation_new();

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_del(IntPtr handle);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_set_frame(IntPtr handle, float frame);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_get_total_frame(IntPtr handle, out float totalFrame);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_animation_get_duration(IntPtr handle, out float duration);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tvg_animation_get_picture(IntPtr handle);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_load_data(IntPtr handle, string data, uint size, string mimetype, string rpath, bool copy);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_set_size(IntPtr handle, float w, float h);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_get_size(IntPtr handle, out float w, out float h);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        private static extern int tvg_picture_set_origin(IntPtr handle, float x, float y);

        public static IntPtr AnimationCreate()
        {
            return tvg_animation_new();
        }

        public static void AnimationDestroy(IntPtr handle)
        {
            Check(tvg_animation_del(handle), "Animation Del");
        }

        public static IntPtr AnimationGetPicture(IntPtr handle)
        {
            return tvg_animation_get_picture(handle);
        }

        public static void AnimationSetFrame(IntPtr handle, float frame)
        {
            tvg_animation_set_frame(handle, frame);
        }
    
        public static void AnimationGetInfo(IntPtr handle, out float totalFrame, out float duration)
        {
            Check(tvg_animation_get_total_frame(handle, out totalFrame), "Animation Get Total Frame");
            Check(tvg_animation_get_duration(handle, out duration), "Animation Get Duration");
        }

        public static void PictureLoad(IntPtr handle, string data)
        {
            Check(tvg_picture_load_data(handle, data, (uint)data.Length, "", "", true), "Picture Load");
        }

        public static void PictureSetSize(IntPtr handle, float w, float h)
        {
            Check(tvg_picture_set_size(handle, w, h), "Picture Set Size");
        }

        public static void PictureGetSize(IntPtr handle, out float w, out float h)
        {
            Check(tvg_picture_get_size(handle, out w, out h), "Picture Get Size");
        }

        public static void PictureSetOrigin(IntPtr handle, float x, float y)
        {
            Check(tvg_picture_set_origin(handle, x, y), "Picture Set Origin");
        }
    }
}