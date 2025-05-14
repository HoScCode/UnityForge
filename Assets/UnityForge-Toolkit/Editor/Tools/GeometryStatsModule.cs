// File: GeometryStatsModule.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class GeometryStatsModule : StatsModuleBase
    {
        private int cachedScenePolys;
        private int cachedSelectedPolys;
        public override string Name => "Geometry";

        public override void Update()
        {
            cachedScenePolys = GetScenePolycount();
            cachedSelectedPolys = GetSelectedPolycount();
        }

        public override void Draw()
        {
            GUILayout.Label("Geometry Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scene Polycount:", cachedScenePolys.ToString("N0"));
            EditorGUILayout.LabelField("Selected Polycount:", cachedSelectedPolys.ToString("N0"));
        }

        private int GetScenePolycount()
        {
            int triangleCount = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mf.TryGetComponent(out MeshRenderer renderer) && renderer.isVisible)
                {
                    if (mf.sharedMesh != null)
                        triangleCount += mf.sharedMesh.triangles.Length / 3;
                }
            }

            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                if (smr.isVisible && smr.sharedMesh != null)
                    triangleCount += smr.sharedMesh.triangles.Length / 3;
            }

            return triangleCount;
        }

        private int GetSelectedPolycount()
        {
            int triangleCount = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                {
                    triangleCount += mf.sharedMesh.triangles.Length / 3;
                }
                else if (go.TryGetComponent(out SkinnedMeshRenderer smr) && smr.sharedMesh != null)
                {
                    triangleCount += smr.sharedMesh.triangles.Length / 3;
                }
            }
            return triangleCount;
        }
    }
}