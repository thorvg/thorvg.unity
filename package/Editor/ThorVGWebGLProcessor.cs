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
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

#if UNITY_WEBGL
namespace Tvg.Editor
{
    public class ThorVGWebGLProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Only process WebGL builds
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            // Source: Package StreamingAssets
            string packagePath = "Packages/com.thorvg.unity/StreamingAssets/WebGL";
            
            // Destination: Project StreamingAssets (Unity copies this to build)
            string destPath = "Assets/StreamingAssets/Packages/com.thorvg.unity/WebGL";
            
            if (!Directory.Exists(packagePath))
            {
                UnityEngine.Debug.LogWarning("[ThorVG] WebGL WASM files not found. Run build script to generate them.");
                return;
            }
            
            // Create destination directory
            Directory.CreateDirectory(destPath);
            
            // Copy files
            foreach (string file in Directory.GetFiles(packagePath))
            {
                if (file.EndsWith(".meta")) continue;
                
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destPath, fileName);
                File.Copy(file, destFile, true);
                UnityEngine.Debug.Log($"[ThorVG] Copied {fileName} for WebGL build");
            }
            
            AssetDatabase.Refresh();
        }
    }
    
    /// <summary>
    /// Cleanup after build
    /// </summary>
    public class ThorVGWebGLPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            // Clean up temporary StreamingAssets
            string tempPath = "Assets/StreamingAssets/Packages";
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
                File.Delete(tempPath + ".meta");
                
                // Remove parent if empty
                if (Directory.GetFileSystemEntries("Assets/StreamingAssets").Length == 0)
                {
                    Directory.Delete("Assets/StreamingAssets");
                    File.Delete("Assets/StreamingAssets.meta");
                }
                
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
