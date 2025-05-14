// Assets/UnityForge-Toolkit/Editor/Tools/UVCheckerTool.cs
using UnityEditor;
using UnityEngine;
using UnityForge.Tools;
using System.Collections.Generic;
using System.Linq;

namespace UnityForge.Tools
{
    public class UVCheckerTool : IUnityForgeTool, System.IDisposable
    {
        public string Name => "UVCheck";

        //private bool _showPreview = false;
        private Material _uvCheckerMaterial;
        private Texture2D[] _availableTextures;
        private int _selectedTextureIndex = 0;
        private Dictionary<GameObject, Material[]> _originalMaterials = new();
        private bool _checkerApplied = false;
        private GameObject[] _lastAppliedObjects = new GameObject[0];
        public void OnGUI()
        {
            

            GUILayout.Label("UV Check Preview Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Temporarily overrides materials with a UV checker to evaluate texel density or stretching. Automatically restores original materials when turned off, switched to another tool, or on window close.", MessageType.Info);

            if (_uvCheckerMaterial == null)
                LoadMaterial();

            if (_availableTextures == null || _availableTextures.Length == 0)
                LoadTextures();

            if (_uvCheckerMaterial == null || _availableTextures.Length == 0)
            {
                EditorGUILayout.HelpBox("Checker material or textures missing. Ensure 'UV_Checker.mat' and textures are in the correct folders.", MessageType.Warning);
                return;
            }

            _selectedTextureIndex = EditorGUILayout.Popup(new GUIContent("Checker Texture", "Select which checker texture to apply."), _selectedTextureIndex,
                _availableTextures.Select(t => t.name).ToArray());

            _uvCheckerMaterial.mainTexture = _availableTextures[_selectedTextureIndex];

            GUILayout.Space(8);
            bool wantPreview = GUILayout.Toggle(_checkerApplied, "Preview UV Checker", "Button");

            if (wantPreview != _checkerApplied)
            {
                if (wantPreview)
                {
                    ApplyChecker();
                }
                else
                {
                    RestoreOriginals();
                }
            }

            string tip = GUI.tooltip;
            if (!string.IsNullOrEmpty(tip))
                EditorGUILayout.HelpBox(tip, MessageType.None);
        }

        private void LoadMaterial()
        {
            _uvCheckerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityForge-Toolkit/Editor/Material/UV_Checker.mat");
        }

        private void LoadTextures()
        {
            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UnityForge-Toolkit/Editor/Texture" });
            _availableTextures = textureGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p))
                .Where(t => t != null)
                .OrderBy(t => t.name)
                .ToArray();
        }

        private void ApplyChecker()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("UV Check", "Please select one or more objects with MeshRenderer.", "OK");
                return;
            }

            foreach (var go in _lastAppliedObjects)
            {
                if (!_originalMaterials.ContainsKey(go)) continue;
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterials = _originalMaterials[go];
            }

            _originalMaterials.Clear();

            foreach (var go in selected)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    _originalMaterials[go] = renderer.sharedMaterials;
                    var mats = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = _uvCheckerMaterial;
                    renderer.sharedMaterials = mats;
                }
            }

            _lastAppliedObjects = selected;
            _checkerApplied = true;
            UnityForgeWindow.AppendLogStatic($"Applied UV Checker to {selected.Length} object(s).");
        }

        private void RestoreOriginals()
        {
            foreach (var kvp in _originalMaterials)
            {
                if (kvp.Key != null)
                {
                    var renderer = kvp.Key.GetComponent<MeshRenderer>();
                    if (renderer != null)
                        renderer.sharedMaterials = kvp.Value;
                }
            }

            _originalMaterials.Clear();
            _lastAppliedObjects = new GameObject[0];
            _checkerApplied = false;
            UnityForgeWindow.AppendLogStatic("Restored original materials.");
        }

        public void OnDeselect()
        {
            Dispose();
        }

        public void Dispose()
        {
            RestoreOriginals();
        }

        ~UVCheckerTool()
        {
            Dispose();
        }
    }
}
