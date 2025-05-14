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
    [InitializeOnLoad]
    public static class UnityForgeWindowCleaner
    {
        static UnityForgeWindowCleaner()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<UnityForgeWindow>())
                {
                    if (!window._initialized)
                    {
                        Debug.LogWarning("[UnityForge] Schließe fehlerhafte Fensterinstanz beim Start.");
                        window.Close();
                    }
                }
            };
        }
    }

    public class UnityForgeWindow : EditorWindow
    {
        private Dictionary<string, Type> _availableToolTypes = new();
        private Dictionary<string, bool> _toolToggles = new();
        private List<IUnityForgeTool> _activeTools = new();
        private Dictionary<string, Texture2D> _iconCache = new();
        private Dictionary<string, string> _toolLabels = new();

        private int _selectedTab;
        private Vector2 _logScroll;
        private string _outputLog = string.Empty;
        internal bool _initialized = false;

        [MenuItem("Tools/UnityForge")]
        public static void ShowWindow() =>
            GetWindow<UnityForgeWindow>("UnityForge");

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            Debug.Log("[UnityForge] Initialize UnityForgeWindow");
            _initialized = true;

            var toolTypes = Assembly.GetAssembly(typeof(IUnityForgeTool))
                .GetTypes()
                .Where(t => typeof(IUnityForgeTool).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => Activator.CreateInstance(t) as IUnityForgeTool)
                .Where(inst => inst != null)
                .ToList();

            foreach (var inst in toolTypes)
            {
                _availableToolTypes[inst.Name] = inst.GetType();
                _toolToggles[inst.Name] = false;
            }

            var iconMapping = new Dictionary<string, string>
{
    { "Scale", "icon_scale.png" },
    { "Pivot", "icon_pivot.png" },
    { "Scatter", "icon_scatter.png" },
    { "Renamer", "icon_renamer.png" },
    { "UVCheck", "icon_uvcheck.png" },
    { "Duplicate", "icon_duplicate.png" }
};

foreach (var kv in _availableToolTypes)
{
    string iconName = iconMapping.ContainsKey(kv.Key) ? iconMapping[kv.Key] : null;

    if (!string.IsNullOrEmpty(iconName))
    {
        string path = $"Assets/UnityForge-Toolkit/Editor/Icons/{iconName}";
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        _iconCache[kv.Key] = icon;
        if (icon == null)
            Debug.LogWarning($"[UnityForge] Icon mapped but not found: {path}");
    }
    else
    {
        Debug.LogWarning($"[UnityForge] No icon mapping found for tool: {kv.Key}");
    }

    _toolLabels[kv.Key] = kv.Key;
}


        }

        private void OnDisable()
        {
            foreach (var tool in _activeTools)
                (tool as IDisposable)?.Dispose();
            _activeTools.Clear();
        }

        private void OnGUI()
        {
            try
            {
                if (!_initialized)
                {
                    EditorGUILayout.HelpBox("Initializing... Please wait and reopen.", MessageType.Info);
                    return;
                }

                int buttonsPerRow = 5;
                int count = 0;

                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();

                foreach (var name in _availableToolTypes.Keys)
                {
                    var icon = _iconCache[name];
                    var label = _toolLabels[name];

                    GUIContent content = new GUIContent(label, icon);

                    GUIStyle style = new GUIStyle(GUI.skin.button)
                    {
                        imagePosition = ImagePosition.ImageAbove,
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10,
                        wordWrap = true
                    };

                    if (GUILayout.Button(content, style, GUILayout.Width(64), GUILayout.Height(72)))
                    {
                        OpenTool(name);
                    }

                    count++;
                    if (count % buttonsPerRow == 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }

                EditorGUILayout.EndHorizontal(); // schließt die letzte Zeile
EditorGUILayout.EndVertical();


                GUILayout.Space(10);
                DrawActiveTool();
                GUILayout.Space(10);
                DrawLog();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityForgeWindow] Caught in OnGUI: {ex.Message}");
            }
        }

        private void OpenTool(string toolName)
        {
            foreach (var key in _toolToggles.Keys.ToList())
                _toolToggles[key] = false;

            _toolToggles[toolName] = true;
            _activeTools.Clear();
            var type = _availableToolTypes[toolName];
            var inst = Activator.CreateInstance(type) as IUnityForgeTool;
            if (inst != null)
                _activeTools.Add(inst);
            _selectedTab = 0;
            _outputLog = string.Empty;
            Debug.Log($"[UnityForge] OpenTool: {toolName}");
        }

        private void DrawActiveTool()
        {
            if (_activeTools.Count == 0)
                return;

            var names = _activeTools.Select(t => t.Name).ToArray();
            _selectedTab = GUILayout.Toolbar(_selectedTab, names);
            GUILayout.Space(5);
            try
            {
                _activeTools[_selectedTab].OnGUI();
            }
            catch (Exception ex)
            {
                Log($"Error in OnGUI of {_activeTools[_selectedTab].Name}: {ex.Message}");
            }
        }

        private void DrawLog()
        {
            GUILayout.Label("Output Log:", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(100));
            EditorGUILayout.TextArea(_outputLog);
            EditorGUILayout.EndScrollView();
        }

        public void Log(string message)
        {
            _outputLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        public static void AppendLogStatic(string message)
        {
            var wnd = GetWindow<UnityForgeWindow>();
            wnd.Log(message);
        }
    }
}
