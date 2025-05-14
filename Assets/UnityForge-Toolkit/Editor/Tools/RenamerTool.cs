// Assets/UnityForge-Toolkit/Editor/Tools/RenamerTool.cs
using UnityEditor;
using UnityEngine;
using UnityForge.Tools;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class RenamerTool : IUnityForgeTool
    {
        public string Name => "Renamer";

        private string _prefix = "";
        private string _baseName = "";
        private string _suffix = "_01";

        private GameObject _referenceObject;

        public void OnGUI()
        {
            GUILayout.Label("Batch Renamer Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select objects in the hierarchy. Use prefix, name, and suffix. Drag an object into the field to autofill the base name.", MessageType.Info);

            _prefix = EditorGUILayout.TextField(new GUIContent("Prefix", "Optional text before the name."), _prefix);

            EditorGUILayout.BeginHorizontal();
            _referenceObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Name From", "Drag an object here to copy its name as base name."),
                _referenceObject, typeof(GameObject), true);
            if (_referenceObject != null)
            {
                if (GUILayout.Button("Use", GUILayout.Width(50)))
                {
                    _baseName = _referenceObject.name;
                    GUI.FocusControl(null); // clear input focus
                }
            }
            EditorGUILayout.EndHorizontal();

            _baseName = EditorGUILayout.TextField(new GUIContent("Base Name", "The main part of the name."), _baseName);
            _suffix = EditorGUILayout.TextField(new GUIContent("Suffix", "Default: _01. Use _a or _A for letter-based suffix. Auto-increments if multiple objects selected."), _suffix);

            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("Apply Rename to Selected", "Renames all selected GameObjects using prefix, base name and suffix.")))
            {
                ApplyRename();
            }

            // Tooltip display at the bottom
            string tip = GUI.tooltip;
            if (!string.IsNullOrEmpty(tip))
            {
                EditorGUILayout.HelpBox(tip, MessageType.None);
            }
        }

        private void ApplyRename()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Renamer", "Please select one or more GameObjects in the Hierarchy.", "OK");
                return;
            }

            Undo.RecordObjects(selected, "Batch Rename");

            bool isAlpha = _suffix.EndsWith("_a") || _suffix.EndsWith("_A");
            bool isUpper = _suffix.EndsWith("_A");

            for (int i = 0; i < selected.Length; i++)
            {
                string suffix;

                if (isAlpha)
                {
                    char start = isUpper ? 'A' : 'a';
                    suffix = "_" + (char)(start + i);
                }
                else
                {
                    // extract starting number from suffix like _01, _05 etc.
                    string numericPart = System.Text.RegularExpressions.Regex.Match(_suffix, "\\d+").Value;
                    int start = 1;
                    int.TryParse(numericPart, out start);
                    suffix = "_" + (start + i).ToString("D2");
                }

                selected[i].name = _prefix + _baseName + suffix;
            }

            UnityForgeWindow.AppendLogStatic($"Renamed {selected.Length} object(s).");
        }
    }
}
