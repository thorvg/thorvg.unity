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
            Check(TvgLib.tvg_engine_init(0), "Engine Init");
            Initialized = true;
        }

        private static void Term()
        {
            TvgLib.tvg_engine_term();
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
        
        internal static void Check(int code, string msg)
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
    }
}