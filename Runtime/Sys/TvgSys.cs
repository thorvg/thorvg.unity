using System;
using UnityEngine;

namespace Tvg.Sys
{   
    public static class TvgSys
    {
        public static bool Initialized { get; private set; }
        
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