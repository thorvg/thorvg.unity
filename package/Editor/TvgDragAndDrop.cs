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

using UnityEngine;
using UnityEditor;

namespace Tvg.Editor
{
    /// <summary>
    /// Handles drag-and-drop of SVG and Lottie files into the Scene/Hierarchy.
    /// Automatically creates a GameObject with TvgPlayer component.
    /// </summary>
    [InitializeOnLoad]
    public static class TvgDragAndDrop
    {
        static TvgDragAndDrop()
        {
            // Register for hierarchy drag-and-drop
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            
            // Register for scene view drag-and-drop
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            {
                HandleDrag(Event.current, null);
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            {
                HandleDrag(Event.current, sceneView);
            }
        }

        private static void HandleDrag(Event evt, SceneView sceneView)
        {
            // Check if we're dragging any ThorVG compatible assets
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (IsTvgAsset(obj))
                {
                    // Show visual feedback that we can accept this
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        CreateTvgPlayer(obj, sceneView);
                        evt.Use();
                    }
                    return;
                }
            }
        }

        private static bool IsTvgAsset(Object obj)
        {
            // Check if it's an SvgAsset
            if (obj is SvgAsset)
                return true;

            // Check if it's a TextAsset with .json extension (Lottie)
            if (obj is TextAsset textAsset)
            {
                string path = AssetDatabase.GetAssetPath(textAsset);
                if (path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void CreateTvgPlayer(Object asset, SceneView sceneView)
        {
            // Create a new GameObject
            GameObject go = new GameObject(asset.name);
            
            // Place at scene view focus point or world origin
            go.transform.position = sceneView != null ? sceneView.pivot : Vector3.zero;

            // Add SpriteRenderer (required by TvgPlayer)
            go.AddComponent<SpriteRenderer>();

            // Add TvgPlayer component
            TvgPlayer player = go.AddComponent<TvgPlayer>();

            // Use reflection to set the private source field
            var sourceField = typeof(TvgPlayer).GetField("source", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (sourceField != null)
            {
                sourceField.SetValue(player, asset);
            }

            // Register undo
            Undo.RegisterCreatedObjectUndo(go, "Create TvgPlayer");

            // Select the new object
            Selection.activeGameObject = go;
        }
    }
}

