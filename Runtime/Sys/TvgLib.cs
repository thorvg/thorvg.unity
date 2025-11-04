using System;
using System.Runtime.InteropServices;

namespace Tvg.Sys
{
    internal static class TvgLib
    {
        public enum Result
        {
                Success = 0,
                InvalidArguments,
                InsufficientCondition,
                FailedAllocation,
                MemoryCorruption,
                NonSupport,
                Unknown = 255
        }

        public enum ColorSpace
        {   
                Abgr8888 = 0
        }

        public enum EngineOption
        {
                // No special features
                None = 0
        }
        
/************************************************************************/
/* Engine API                                                           */
/************************************************************************/
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_engine_init(int threads);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_engine_term();
        
/************************************************************************/
/* Canvas API                                                           */
/************************************************************************/

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tvg_swcanvas_create(EngineOption option);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_canvas_destroy(IntPtr handle);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_canvas_push(IntPtr handle, IntPtr paint);

        // Drawing APIs

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_canvas_sync(IntPtr handle);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_swcanvas_set_target(IntPtr handle, IntPtr buffer, uint stride, uint w, uint h, ColorSpace colorspace);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_canvas_update(IntPtr handle);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_canvas_draw(IntPtr handle, bool clear);

/************************************************************************/
/* Animation API                                                        */
/************************************************************************/

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tvg_animation_new();

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_animation_del(IntPtr handle);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_animation_set_frame(IntPtr handle, float frame);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_animation_get_total_frame(IntPtr handle, out float totalFrame);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_animation_get_duration(IntPtr handle, out float duration);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tvg_animation_get_picture(IntPtr handle);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_picture_load_data(IntPtr handle, string data, uint size, string mimetype, string rpath, bool copy);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_picture_set_size(IntPtr handle, float w, float h);
        
        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_picture_get_size(IntPtr handle, out float w, out float h);

        [DllImport("libthorvg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int tvg_picture_set_origin(IntPtr handle, float x, float y);
    }
}