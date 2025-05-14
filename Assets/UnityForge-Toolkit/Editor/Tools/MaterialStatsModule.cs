using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class MaterialStatsModule : StatsModuleBase
    {
        public override string Name => "Materials";

        private int materialCount;
        private int textureCount;

        public override void Update()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            HashSet<Material> uniqueMaterials = new();
            HashSet<Texture> uniqueTextures = new();

            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null)
                    {
                        uniqueMaterials.Add(mat);
                        if (mat.mainTexture != null)
                            uniqueTextures.Add(mat.mainTexture);
                    }
                }
            }

            materialCount = uniqueMaterials.Count;
            textureCount = uniqueTextures.Count;
        }

        public override void Draw()
        {
            GUILayout.Label("Material Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Total Materials:", materialCount.ToString());
            EditorGUILayout.LabelField("Loaded Textures:", textureCount.ToString());
        }
    }
}
