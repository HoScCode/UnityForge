// File: StatsTool.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class StatsTool : IUnityForgeTool
    {
        public string Name => "Stats";

        private readonly List<StatsModuleBase> modules = new();
        private int selectedModule = 0;
        private string[] moduleNames;

        public StatsTool()
        {
            modules.Add(new GeometryStatsModule());
            modules.Add(new DrawCallStatsModule());
            modules.Add(new MaterialStatsModule());
            modules.Add(new LightingStatsModule());
            modules.Add(new ObjectStatsModule());
            modules.Add(new MemoryStatsModule());

            moduleNames = modules.ConvertAll(m => m.Name).ToArray();

            foreach (var module in modules)
                module.Update();
        }

        public void OnGUI()
        {
            GUILayout.Space(5);
            GUILayout.Label("Select Module", EditorStyles.boldLabel);
            GUILayout.Space(5);

            for (int i = 0; i < modules.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                DrawModuleButton(i);

                if (i + 1 < modules.Count)
                    DrawModuleButton(i + 1);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(15);
            if (selectedModule >= 0 && selectedModule < modules.Count)
                modules[selectedModule].Draw();

            GUILayout.Space(10);
            if (GUILayout.Button(" Refresh", GUILayout.Height(28)))
            {
                foreach (var module in modules)
                    module.Update();
            }
        }

        private void DrawModuleButton(int index)
        {
            bool isSelected = index == selectedModule;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                fixedHeight = 32,
                fixedWidth = 140,
                margin = new RectOffset(4, 4, 4, 4)
            };

            if (GUILayout.Button(moduleNames[index], buttonStyle))
            {
                selectedModule = index;
            }
        }
    }
}
