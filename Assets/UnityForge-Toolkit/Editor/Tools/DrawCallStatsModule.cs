using UnityEditor;
using UnityEngine;

namespace UnityForge.Tools
{
    public class DrawCallStatsModule : StatsModuleBase
    {
        public override string Name => "Draw Calls";

        public override void Draw()
        {
            GUILayout.Label("Draw Call Statistics", EditorStyles.boldLabel);

#if UNITY_2020_2_OR_NEWER
            int drawCalls = UnityEditor.UnityStats.drawCalls;
            int batched = UnityEditor.UnityStats.batches;
            int instanced = UnityEditor.UnityStats.instancedBatches;
#else
            int drawCalls = 0;
            int batched = 0;
            int instanced = 0;
#endif

            EditorGUILayout.LabelField("Visible Draw Calls:", drawCalls.ToString());
            EditorGUILayout.LabelField("Batched:", batched.ToString());
            EditorGUILayout.LabelField("Instanced:", instanced.ToString());
        }

        public override void Update()
        {
            // UnityStats are live, no cache needed yet
        }
    }
}
