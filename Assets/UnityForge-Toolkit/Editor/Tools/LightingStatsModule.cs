using UnityEditor;
using UnityEngine;

namespace UnityForge.Tools
{
    public class LightingStatsModule : StatsModuleBase
    {
        public override string Name => "Lighting";

        private int total, realtime, shadowCasters;

        public override void Update()
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            total = lights.Length;
            realtime = 0;
            shadowCasters = 0;

            foreach (var light in lights)
            {
                if (light.lightmapBakeType == LightmapBakeType.Realtime)
                    realtime++;
                if (light.shadows != LightShadows.None)
                    shadowCasters++;
            }
        }

        public override void Draw()
        {
            GUILayout.Label("Lighting Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Lights:", total.ToString());
            EditorGUILayout.LabelField("Realtime Lights:", realtime.ToString());
            EditorGUILayout.LabelField("Shadow Casters:", shadowCasters.ToString());
        }
    }
}
