using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityForge.Tools
{
    public class MemoryStatsModule : StatsModuleBase
    {
        public override string Name => "Memory";

        private long totalAllocated;
        private long gcMemory;

        public override void Update()
        {
            totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            gcMemory = System.GC.GetTotalMemory(false);
        }

        public override void Draw()
        {
            GUILayout.Label("Memory Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("GC Allocations:", FormatBytes(gcMemory));
            EditorGUILayout.LabelField("Used RAM:", FormatBytes(totalAllocated));
        }

        private string FormatBytes(long bytes)
        {
            float mb = bytes / (1024f * 1024f);
            return mb.ToString("F2") + " MB";
        }
    }
}
