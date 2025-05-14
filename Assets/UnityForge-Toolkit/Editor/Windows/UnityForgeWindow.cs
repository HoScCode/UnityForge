// Assets/Editor/UnityForge/Windows/UnityForgeWindow.cs
using UnityEditor;
using UnityEngine;
using UnityForge.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityForge
{
    public class UnityForgeWindow : EditorWindow
    {
        private Dictionary<string, Type> _availableToolTypes;
        private Dictionary<string, bool> _toolToggles;
        private List<IUnityForgeTool> _activeTools;
        private Dictionary<string, Texture2D> _iconCache;
        private Dictionary<string, string> _toolLabels;

        private int _selectedTab;
        private Vector2 _logScroll;
        private string _outputLog = string.Empty;

        [MenuItem("Tools/UnityForge")]
        public static void ShowWindow() =>
            GetWindow<UnityForgeWindow>("UnityForge");

        private void OnEnable()
        {
            var toolTypes = Assembly.GetAssembly(typeof(IUnityForgeTool))
                .GetTypes()
                .Where(t => typeof(IUnityForgeTool).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t =>
                {
                    var inst = Activator.CreateInstance(t) as IUnityForgeTool;
                    return (ToolName: inst.Name, ToolType: t);
                })
                .ToList();

            _availableToolTypes = toolTypes.ToDictionary(x => x.ToolName, x => x.ToolType);

            if (_toolToggles == null)
                _toolToggles = new Dictionary<string, bool>();

            foreach (var name in _availableToolTypes.Keys)
                if (!_toolToggles.ContainsKey(name))
                    _toolToggles[name] = false;

            var removed = _toolToggles.Keys.Except(_availableToolTypes.Keys).ToList();
            foreach (var name in removed)
                _toolToggles.Remove(name);

            _activeTools = new List<IUnityForgeTool>();

            _iconCache = new Dictionary<string, Texture2D>
            {
                { "RandomPlacement", LoadIcon("icon_scatter") },
                { "Adjust", LoadIcon("icon_scale") },
                { "BoundingBox", LoadIcon("icon_pivot") }
            };

            _toolLabels = new Dictionary<string, string>
            {
                { "RandomPlacement", "Scatter" },
                { "Adjust", "Scale" },
                { "BoundingBox", "Pivot" }
            };
        }

        private Texture2D LoadIcon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/UnityForge-Toolkit/Editor/Icons/" + name + ".png");
        }

        private void OnGUI()
        {
            GUILayout.Label("Click a tool to open it:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            foreach (var kv in _availableToolTypes)
            {
                var toolName = kv.Key;
                Texture2D icon = _iconCache.ContainsKey(toolName) ? _iconCache[toolName] : null;
                string label = _toolLabels.ContainsKey(toolName) ? _toolLabels[toolName] : toolName;

                GUIStyle style = new GUIStyle(GUI.skin.button)
                {
                    fixedWidth = 64,
                    fixedHeight = 64,
                    imagePosition = ImagePosition.ImageAbove,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10
                };

                if (GUILayout.Button(new GUIContent(label, icon), style))
                {
                    foreach (var key in _toolToggles.Keys.ToList())
                        _toolToggles[key] = false;
                    _toolToggles[toolName] = true;
                    ApplyToolSelection();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (_activeTools.Count > 0)
            {
                var names = _activeTools.Select(t => _toolLabels.ContainsKey(t.Name) ? _toolLabels[t.Name] : t.Name).ToArray();
                _selectedTab = GUILayout.Toolbar(_selectedTab, names);
                GUILayout.Space(8);

                try
                {
                    _activeTools[_selectedTab].OnGUI();
                }
                catch (Exception ex)
                {
                    AppendLog($"Error in tool '{_activeTools[_selectedTab].Name}': {ex.Message}");
                }

                GUILayout.Space(10);
                GUILayout.Label("Output Log", EditorStyles.boldLabel);
                _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(100));
                EditorGUILayout.TextArea(_outputLog, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void ApplyToolSelection()
        {
            _activeTools.Clear();
            _outputLog = string.Empty;

            foreach (var kv in _toolToggles)
            {
                if (!kv.Value) continue;
                var type = _availableToolTypes[kv.Key];
                var tool = Activator.CreateInstance(type) as IUnityForgeTool;
                if (tool != null)
                    _activeTools.Add(tool);
            }

            _selectedTab = 0;
        }

        protected void AppendLog(string message)
        {
            _outputLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            _logScroll.y = float.MaxValue;
        }
        public static void AppendLogStatic(string message)
        {
            var window = GetWindow<UnityForgeWindow>();
            window.AppendLog(message);
        }
    }
}
