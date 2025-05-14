// Assets/Editor/UnityForge/Tools/ScatterTool.cs
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace UnityForge.Tools
{
    /// <summary>
    /// Random Placement Tool: Scatter a reference object over a specified surface with live preview.
    /// Supports multiple noise patterns (Perlin, Uniform Random, Grid Jitter) and positional offset.
    /// </summary>
    public class ScatterTool : IUnityForgeTool
    {
        public string Name => "Scatter";

        private enum NoisePattern { Perlin, UniformRandom, GridJitter }
        private NoisePattern _noisePattern = NoisePattern.Perlin;

        private GameObject _referenceObject;
        private GameObject _scatterSurface;
        private int _quantity = 10;
        private float _noiseScale = 1f;
        private float _heightRange = 0f;
        private float _offsetRange = 0f;
        private Color _previewColor = Color.magenta;

        private GameObject _previewGroup;

        // Last parameters for live preview
        private int _lastQuantity;
        private float _lastNoiseScale;
        private float _lastHeightRange;
        private float _lastOffsetRange;
        private Color _lastPreviewColor;
        private GameObject _lastReferenceObject;
        private GameObject _lastScatterSurface;
        private NoisePattern _lastNoisePattern;

        public void OnGUI()
        {
            GUILayout.Label("Random Placement Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scatter instances of a reference object over a surface using selectable noise patterns and offset.",
                MessageType.Info);

            // Detect parameter changes
            EditorGUI.BeginChangeCheck();

            // Noise pattern selection
            GUILayout.Label("Noise Pattern:");
            _noisePattern = (NoisePattern)GUILayout.Toolbar((int)_noisePattern,
                new[] { "Perlin", "Uniform", "Grid" });

            // Reference and surface
            _referenceObject = EditorGUILayout.ObjectField("Reference Object", _referenceObject, typeof(GameObject), true) as GameObject;
            _scatterSurface = EditorGUILayout.ObjectField("Scatter Surface", _scatterSurface, typeof(GameObject), true) as GameObject;

            // Parameters
            _quantity = EditorGUILayout.IntSlider(
                new GUIContent("Quantity", "Adjust the number of instances to place."),
                _quantity, 1, 500);
            if (_noisePattern == NoisePattern.Perlin)
                _noiseScale = EditorGUILayout.Slider(
                    new GUIContent("Noise Scale", "Controls randomness intensity for Perlin noise pattern."),
                    _noiseScale, 0.1f, 10f);
            _heightRange = EditorGUILayout.Slider(
                    new GUIContent("Height Range", "Vertical variation range above the surface."),
                    _heightRange, 0f, 10f);
            _offsetRange = EditorGUILayout.Slider(
                    new GUIContent("Offset Range", "Horizontal random offset radius per instance."),
                    _offsetRange, 0f, 5f);
            _previewColor = EditorGUILayout.ColorField(
                    new GUIContent("Preview Color", "Tint color for preview objects."),
                    _previewColor);

            bool paramsChanged = EditorGUI.EndChangeCheck();
            GUILayout.Space(8);

            // Preview toggle (disabled if missing reference or surface)
            EditorGUI.BeginDisabledGroup(_referenceObject == null || _scatterSurface == null);
            bool previewActive = _previewGroup != null;
            bool wantPreview = GUILayout.Toggle(previewActive, "Preview", "Button");
            EditorGUI.EndDisabledGroup();

            if (wantPreview && !previewActive)
            {
                ClearPreview();
                if (_referenceObject != null && _scatterSurface != null)
                {
                    GeneratePreview();
                    SaveLastParameters();
                }
            }
            else if (!wantPreview && previewActive)
            {
                ClearPreview();
            }
            else if (wantPreview && previewActive && paramsChanged)
            {
                if (ParametersDiffer())
                {
                    ClearPreview();
                    GeneratePreview();
                    SaveLastParameters();
                }
            }

            GUILayout.Space(8);
            EditorGUI.BeginDisabledGroup(_previewGroup == null);
            if (GUILayout.Button("Apply Scatter"))
            {
                ApplyScatter();
                ClearPreview();
            }
            EditorGUI.EndDisabledGroup();

            // Status/info display
            string status = GUI.tooltip;
            if (string.IsNullOrEmpty(status))
                status = "Hover over a control to see details.";
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        #region Preview & Apply Methods

        private void GeneratePreview()
        {
            if (_referenceObject == null || _scatterSurface == null)
                return;

            var rend = _scatterSurface.GetComponentInChildren<Renderer>();
            if (rend == null)
                return;

            Bounds bounds = rend.bounds;
            int gridRows = Mathf.CeilToInt(Mathf.Sqrt(_quantity));

            _previewGroup = new GameObject("ScatterPreview");
            for (int i = 0; i < _quantity; i++)
            {
                float u = 0f, v = 0f;
                switch (_noisePattern)
                {
                    case NoisePattern.Perlin:
                        u = Mathf.PerlinNoise(i * _noiseScale, 0f);
                        v = Mathf.PerlinNoise(0f, i * _noiseScale);
                        break;
                    case NoisePattern.UniformRandom:
                        u = Random.value;
                        v = Random.value;
                        break;
                    case NoisePattern.GridJitter:
                        int row = i / gridRows;
                        int col = i % gridRows;
                        u = (col + Random.value) / gridRows;
                        v = (row + Random.value) / gridRows;
                        break;
                }
                float x = Mathf.Lerp(bounds.min.x, bounds.max.x, u) + Random.insideUnitCircle.x * _offsetRange;
                float z = Mathf.Lerp(bounds.min.z, bounds.max.z, v) + Random.insideUnitCircle.y * _offsetRange;
                float y = bounds.max.y + Random.value * _heightRange;

                var instance = Object.Instantiate(_referenceObject);
                instance.name = _referenceObject.name + "_preview";
                instance.transform.SetParent(_previewGroup.transform, true);
                instance.transform.position = new Vector3(x, y, z);
                instance.transform.rotation = Quaternion.identity;

                foreach (var r in instance.GetComponentsInChildren<Renderer>())
                {
                    var mat = new Material(r.sharedMaterial) { color = _previewColor };
                    r.sharedMaterial = mat;
                }
            }
        }

        private void ApplyScatter()
        {
            if (_previewGroup == null) return;

            var refMats = _referenceObject
                .GetComponentsInChildren<Renderer>()
                .Select(r => r.sharedMaterial)
                .ToArray();
            int matCount = refMats.Length;

            var children = _previewGroup.transform.Cast<Transform>().ToList();
            var group = new GameObject("ScatterGroup");
            Undo.RegisterCreatedObjectUndo(group, "Create Scatter Group");

            foreach (var child in children)
            {
                var obj = child.gameObject;
                Undo.RegisterCreatedObjectUndo(obj, "Apply Scatter");
                obj.transform.SetParent(group.transform, true);
                obj.name = _referenceObject.name;

                var rends = obj.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rends.Length; i++)
                {
                    Undo.RecordObject(rends[i], "Apply Reference Material");
                    rends[i].sharedMaterial = refMats[Mathf.Min(i, matCount - 1)];
                }
            }
        }

        private void ClearPreview()
        {
            if (_previewGroup != null)
                Object.DestroyImmediate(_previewGroup);
            _previewGroup = null;
        }

        private void SaveLastParameters()
        {
            _lastQuantity = _quantity;
            _lastNoiseScale = _noiseScale;
            _lastHeightRange = _heightRange;
            _lastOffsetRange = _offsetRange;
            _lastPreviewColor = _previewColor;
            _lastReferenceObject = _referenceObject;
            _lastScatterSurface = _scatterSurface;
            _lastNoisePattern = _noisePattern;
        }

        private bool ParametersDiffer()
        {
            return _quantity != _lastQuantity ||
                   _noiseScale != _lastNoiseScale ||
                   _heightRange != _lastHeightRange ||
                   _offsetRange != _lastOffsetRange ||
                   _previewColor != _lastPreviewColor ||
                   _referenceObject != _lastReferenceObject ||
                   _scatterSurface != _lastScatterSurface ||
                   _noisePattern != _lastNoisePattern;
        }
        #endregion
    }
}
