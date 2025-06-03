// File: StatsTool.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class StatsTool : EditorWindow, IUnityForgeTool
    {
        public string Name => "Stats";
        private readonly List<StatsModuleBase> modules = new();
        private int selectedModule = 0;
        private string[] moduleNames;
        private bool modulesInitialized = false;

        [MenuItem("Window/UnityForge/Stats", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<StatsTool>("Stats");
            window.minSize = new Vector2(300, 400);
        }

        private void OnEnable()
        {
            InitializeModules();
        }

        public void InitializeModules()
        {
            if (modulesInitialized)
                return;

            modules.Clear();

            modules.Add(new GeometryStatsModule());
            modules.Add(new DrawCallStatsModule());
            modules.Add(new MaterialStatsModule());
            modules.Add(new LightingStatsModule());
            modules.Add(new ObjectStatsModule());
            modules.Add(new MemoryStatsModule());

            moduleNames = modules.ConvertAll(m => m.Name).ToArray();

            // Update modules once so initial stats are populated when
            // the window first opens
            foreach (var module in modules)
            {
                module.Update();
            }

            modulesInitialized = true;
        }

        public void OnGUI()
        {
            if (!modulesInitialized)
                InitializeModules();

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
            {
                modules[selectedModule].Draw();
            }
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
                margin = new RectOffset(4, 4, 4, 4),
                normal = {
                    textColor = Color.white,
                    background = isSelected
                        ? MakeColorTexture(new Color(0.3f, 0.5f, 0.8f))  // Aktiver Button-Hintergrund
                        : GUI.skin.button.normal.background
                }
            };

            if (GUILayout.Button(modules[index].Name, buttonStyle))
            {
                selectedModule = index;
            }
        }
        private Texture2D MakeColorTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }


    }
} 