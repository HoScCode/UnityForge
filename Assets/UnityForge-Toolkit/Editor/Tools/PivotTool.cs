// Assets/Editor/UnityForge/Tools/PivotTool.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityForge.Tools
{
    /// <summary>
    /// The tool is used to create a pivot in the middle of the selected object.
    /// You can set a pivot and press Apply. To revert, select the original object and press Revert.
    /// </summary>
    public class PivotTool : IUnityForgeTool
    {
        public string Name => "Pivot";

        private float _markerSize = 0.1f;
        private bool _showPivotMarker = false;

        // Maps original object to its marker
        private readonly Dictionary<GameObject, GameObject> _markers = new Dictionary<GameObject, GameObject>();
        // Maps original object to its pivot parent (allow only one)
        private readonly Dictionary<GameObject, GameObject> _pivotParents = new Dictionary<GameObject, GameObject>();
        private GameObject[] _lastSelection;

        public void OnGUI()
        {
            GUILayout.Label("Pivot Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The tool is used to create a pivot in the middle of the selected object. You can set a pivot and press Apply. To revert, select the original object and press Revert.",
                MessageType.Info);
            
            // Hide markers if selection changed to a new original
            var selection = Selection.gameObjects;
            bool selectingMarkers = selection.Length > 0 && selection.All(o => _markers.Values.Contains(o));
            if (_showPivotMarker && _lastSelection != null && !selectingMarkers && !_lastSelection.SequenceEqual(selection))
            {
                RemoveAllMarkers();
                _showPivotMarker = false;
            }

            // Identify current original object from selection (original, marker or pivot)
            GameObject currentOriginal = null;
            if (selection.Length > 0)
            {
                var first = selection[0];
                // If a pivot parent is selected, get its original
                if (_pivotParents.Values.Contains(first))
                {
                    currentOriginal = _pivotParents.First(kv => kv.Value == first).Key;
                }
                // If an original with an existing pivot is selected
                else if (_pivotParents.ContainsKey(first))
                {
                    currentOriginal = first;
                }
                // If a marker is selected (original selected by marker)
                else if (_markers.ContainsValue(first))
                {
                    currentOriginal = _markers.First(kv => kv.Value == first).Key;
                }
                // If the original itself is selected without pivot yet
                else if (_markers.ContainsKey(first))
                {
                    currentOriginal = first;
                }
            }

            // Marker controls
            _markerSize = EditorGUILayout.FloatField("Marker Size", _markerSize);
            bool newShow = EditorGUILayout.ToggleLeft("Show Pivot Marker", _showPivotMarker);
            if (newShow != _showPivotMarker)
            {
                _showPivotMarker = newShow;
                if (_showPivotMarker) CreateMarkersForSelection();
                else RemoveAllMarkers();
            }

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            // Apply Pivot button: only if original selected and no pivot exists
            bool canApply = currentOriginal != null && !_pivotParents.ContainsKey(currentOriginal);
            EditorGUI.BeginDisabledGroup(!canApply);
            if (GUILayout.Button("Apply Pivot")) ApplyPivot(currentOriginal);
            EditorGUI.EndDisabledGroup();

            // Revert Pivot button: only if original selected and pivot exists
            bool canRevert = currentOriginal != null && _pivotParents.ContainsKey(currentOriginal);
            EditorGUI.BeginDisabledGroup(!canRevert);
            if (GUILayout.Button("Revert Pivot")) RevertPivot(currentOriginal);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Save last selection
            _lastSelection = selection;
        }

        private void CreateMarkersForSelection()
        {
            RemoveAllMarkers();
            var sel = Selection.gameObjects;
            var toSelect = new List<UnityEngine.Object>();

            foreach (var go in sel)
            {
                // Compute center
                var renderers = go.GetComponentsInChildren<Renderer>();
                if (!renderers.Any()) continue;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                Vector3 center = bounds.center;

                // Create marker sphere
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = go.name + "_PivotMarker";
                marker.transform.SetParent(go.transform, true);
                marker.transform.position = center;
                marker.transform.localScale = Vector3.one * _markerSize;
                marker.hideFlags = HideFlags.DontSave;

                // Transparent magenta material
                var shader = Shader.Find("Hidden/Internal-Colored");
                var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                mat.color = new Color(1f, 0f, 1f, 0.5f);
                marker.GetComponent<Renderer>().sharedMaterial = mat;

                _markers[go] = marker;
                toSelect.Add(marker);
            }

            if (toSelect.Any())
            {
                Selection.objects = toSelect.ToArray();
            }
        }

        private void RemoveAllMarkers()
        {
            foreach (var m in _markers.Values)
                if (m != null)
                    UnityEngine.Object.DestroyImmediate(m);
            _markers.Clear();
        }

        private void ApplyPivot(GameObject original)
        {
            if (original == null || !_markers.ContainsKey(original)) return;
            var marker = _markers[original];

            // Compute old center
            var renderers = original.GetComponentsInChildren<Renderer>();
            var bounds = renderers.Any() ? renderers[0].bounds : new Bounds(original.transform.position, Vector3.zero);
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            Vector3 oldCenter = bounds.center;
            Vector3 newPivot = marker.transform.position;

            // Create pivot parent
            var pivot = new GameObject($"Pivot_{original.name}");
            pivot.transform.SetParent(original.transform.parent, true);
            pivot.transform.position = newPivot;
            Undo.RegisterCreatedObjectUndo(pivot, "Create Pivot Parent");

            // Reparent original
            Undo.SetTransformParent(original.transform, pivot.transform, "Apply Pivot");
            original.transform.SetParent(pivot.transform, true);

            _pivotParents[original] = pivot;

            // Clean up marker
            UnityEngine.Object.DestroyImmediate(marker);
            _markers.Remove(original);

            // Log
            UnityForgeWindow.AppendLogStatic($"Old Pivot: {oldCenter:F3}, New Pivot: {newPivot:F3}");
        }

        private void RevertPivot(GameObject original)
        {
            if (original == null || !_pivotParents.ContainsKey(original)) return;
            var pivot = _pivotParents[original];

            // Reparent original back
            Undo.SetTransformParent(original.transform, pivot.transform.parent, "Revert Pivot");
            original.transform.SetParent(pivot.transform.parent, true);

            // Destroy pivot
            Undo.DestroyObjectImmediate(pivot);
            _pivotParents.Remove(original);

            // Log
            UnityForgeWindow.AppendLogStatic($"Reverted pivot for {original.name}");
        }
    }
}
