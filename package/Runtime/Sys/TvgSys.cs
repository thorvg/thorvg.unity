using System;
using UnityEngine;

namespace Tvg.Sys
{   
    public static class TvgSys
    {
        public static bool Initialized { get; private set; }
        
        /// <summary>
        /// Check if ThorVG is ready to use (important for WebGL)
        /// </summary>
        /// <returns>True if ready, false if still loading</returns>
        public static bool IsReady()
        {
            if (!Initialized) return false;
            
            int status = TvgLib.IsReady();
            if (status < 0)
            {
                throw new Exception("ThorVG failed to load");
            }
            return status > 0;
        }
        
        public static void Init()
        {
            if (Initialized) return;
            TvgLib.EngineInit();
            Initialized = true;
        }

        private static void Term()
        {
            TvgLib.EngineTerm();
            Initialized = false;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitScene()
        {
            Init();
            Application.quitting += TermScene;
        }

        private static void TermScene()
        {
            Term();
            Application.quitting -= TermScene;
        }
    }
}