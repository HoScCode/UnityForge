using UnityEditor;
using UnityEngine;
using UnityForge.Tools;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class DuplicateTool : IUnityForgeTool, System.IDisposable
    {
        public string Name => "Duplicate";

        private enum DuplicateMode { Linear, Radial, Array }
        private DuplicateMode _mode = DuplicateMode.Linear;

        private GameObject _referenceObject;
        private GameObject _directionMarker;
        private GameObject _previewGroup;

        private int _count = 5;
        private int _countX = 3, _countY = 1, _countZ = 3;

        private const string MarkerName = "DuplicateTool_Marker";

        public void OnGUI()
        {
            GUILayout.Label("Duplicate Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Duplicate instances of a reference object in linear, radial or array form.", MessageType.Info);

            _mode = (DuplicateMode)EditorGUILayout.EnumPopup("Mode", _mode);
            _referenceObject = (GameObject)EditorGUILayout.ObjectField("Reference Object", _referenceObject, typeof(GameObject), true);

            if (_mode == DuplicateMode.Array)
            {
                GUILayout.Label("Array Settings", EditorStyles.boldLabel);
                _countX = EditorGUILayout.IntSlider("Count X", _countX, 1, 20);
                _countY = EditorGUILayout.IntSlider("Count Y", _countY, 1, 20);
                _countZ = EditorGUILayout.IntSlider("Count Z", _countZ, 1, 20);
            }
            else
            {
                _count = EditorGUILayout.IntSlider("Duplicate Count", _count, 1, 100);
            }

            EditorGUI.BeginDisabledGroup(_referenceObject == null);
            if (GUILayout.Button("Create/Move Marker"))
                CreateOrMoveMarker();

            if (GUILayout.Button("Preview Duplicates"))
            {
                ClearPreview();
                if (_referenceObject != null && (_mode != DuplicateMode.Array && _directionMarker != null || _mode == DuplicateMode.Array))
                {
                    if (_mode == DuplicateMode.Linear)
                        CreateLinearInstances();
                    else if (_mode == DuplicateMode.Radial)
                        CreateRadialInstances();
                    else if (_mode == DuplicateMode.Array)
                        CreateArrayInstances();
                }
            }

            if (GUILayout.Button("Apply Duplicates"))
            {
                ApplyDuplicates();
                ClearPreview();
            }
            EditorGUI.EndDisabledGroup();

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void CreateOrMoveMarker()
        {
            if (_referenceObject == null) return;

            if (_directionMarker == null)
            {
                _directionMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _directionMarker.name = MarkerName;
                _directionMarker.transform.localScale = Vector3.one * 0.2f;
                _directionMarker.hideFlags = HideFlags.DontSave;
                Object.DestroyImmediate(_directionMarker.GetComponent<Collider>());

                var mat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                mat.color = new Color(1f, 0.8f, 0f, 0.8f);
                _directionMarker.GetComponent<Renderer>().sharedMaterial = mat;
            }

            _directionMarker.transform.position = _referenceObject.transform.position + Vector3.right;
            Selection.activeGameObject = _directionMarker;
        }

        private void CreateLinearInstances()
        {
            ClearPreview();
            _previewGroup = new GameObject("DuplicatePreviewGroup") { hideFlags = HideFlags.DontSave };

            Vector3 start = _referenceObject.transform.position;
            Vector3 end = _directionMarker.transform.position;

            for (int i = 1; i < _count; i++)
            {
                float t = i / (float)(_count);
                Vector3 pos = Vector3.Lerp(start, end, t);
                CreateInstanceAt(pos, _referenceObject.transform.rotation);
            }

            UnityForgeWindow.AppendLogStatic($"Previewed {_count - 1} linear duplicates.");
        }

        private void CreateRadialInstances()
        {
            ClearPreview();
            _previewGroup = new GameObject("DuplicatePreviewGroup") { hideFlags = HideFlags.DontSave };

            Vector3 markerCenter = _directionMarker.transform.position;
            Vector3 startPoint = _referenceObject.transform.position;
            float radius = Vector3.Distance(markerCenter, startPoint);
            Vector3 dir = (startPoint - markerCenter).normalized;
            float startAngle = Mathf.Atan2(dir.z, dir.x);

            for (int i = 1; i < _count; i++)
            {
                float angle = startAngle + i * Mathf.PI * 2f / _count;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                Vector3 pos = markerCenter + new Vector3(x, 0, z);
                Quaternion rot = Quaternion.LookRotation(markerCenter - pos, Vector3.up);
                CreateInstanceAt(pos, rot);
            }

            UnityForgeWindow.AppendLogStatic($"Previewed {_count - 1} radial duplicates.");
        }

        private void CreateArrayInstances()
        {
            ClearPreview();
            _previewGroup = new GameObject("DuplicatePreviewGroup") { hideFlags = HideFlags.DontSave };

            Vector3 origin = _referenceObject.transform.position;
            Vector3 marker = _directionMarker.transform.position;
            Vector3 spacing = new Vector3(
                Mathf.Abs(marker.x - origin.x) / Mathf.Max(1, _countX - 1),
                Mathf.Abs(marker.y - origin.y) / Mathf.Max(1, _countY - 1),
                Mathf.Abs(marker.z - origin.z) / Mathf.Max(1, _countZ - 1)
            );

            for (int x = 0; x < _countX; x++)
            {
                for (int y = 0; y < _countY; y++)
                {
                    for (int z = 0; z < _countZ; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        Vector3 offset = new Vector3(x * spacing.x, y * spacing.y, z * spacing.z);
                        Vector3 pos = origin + offset;
                        CreateInstanceAt(pos, _referenceObject.transform.rotation);
                    }
                }
            }

            int total = _countX * _countY * _countZ - 1;
            UnityForgeWindow.AppendLogStatic($"Previewed {total} array duplicates.");
        }

        private void CreateInstanceAt(Vector3 pos, Quaternion rotation)
        {
            var clone = Object.Instantiate(_referenceObject);
            clone.name = _referenceObject.name + "_dup";
            clone.transform.position = pos;
            clone.transform.rotation = rotation;
            clone.transform.localScale = _referenceObject.transform.localScale;
            clone.transform.SetParent(_previewGroup.transform);
        }

        private void ApplyDuplicates()
        {
            if (_previewGroup == null) return;

            var appliedGroup = new GameObject("DuplicatedObjects");
            Undo.RegisterCreatedObjectUndo(appliedGroup, "Apply Duplicates");

            var children = new List<Transform>();
            foreach (Transform child in _previewGroup.transform)
                children.Add(child);

            foreach (var child in children)
            {
                Undo.RegisterCreatedObjectUndo(child.gameObject, "Apply Duplicate");
                child.SetParent(appliedGroup.transform, true);
            }

            UnityForgeWindow.AppendLogStatic($"Applied {children.Count} duplicates.");
        }

        private void ClearPreview()
        {
            if (_previewGroup != null)
                Object.DestroyImmediate(_previewGroup);
            _previewGroup = null;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_referenceObject == null || (_mode != DuplicateMode.Array && _directionMarker == null)) return;

            if (_mode == DuplicateMode.Linear || _mode == DuplicateMode.Radial)
            {
                Vector3 markerCenter = _directionMarker.transform.position;
                Vector3 startPoint = _referenceObject.transform.position;
                float radius = Vector3.Distance(markerCenter, startPoint);
                Vector3 dir = (startPoint - markerCenter).normalized;
                float startAngle = Mathf.Atan2(dir.z, dir.x);

                if (_mode == DuplicateMode.Linear)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawAAPolyLine(3f, startPoint, markerCenter);
                    Handles.ArrowHandleCap(0, startPoint, Quaternion.LookRotation((markerCenter - startPoint).normalized), Mathf.Min(Vector3.Distance(startPoint, markerCenter), 1f), EventType.Repaint);
                }
                else if (_mode == DuplicateMode.Radial)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawWireDisc(markerCenter, Vector3.up, radius);

                    for (int i = 0; i < _count; i++)
                    {
                        float angle = startAngle + i * Mathf.PI * 2f / _count;
                        float x = Mathf.Cos(angle) * radius;
                        float z = Mathf.Sin(angle) * radius;
                        Vector3 point = markerCenter + new Vector3(x, 0, z);
                        Handles.DotHandleCap(0, point, Quaternion.identity, 0.05f, EventType.Repaint);
                    }
                }
            }
            else if (_mode == DuplicateMode.Array)
            {
                Vector3 origin = _referenceObject.transform.position;
                Vector3 marker = _directionMarker.transform.position;
                Vector3 spacing = new Vector3(
                    Mathf.Abs(marker.x - origin.x) / Mathf.Max(1, _countX - 1),
                    Mathf.Abs(marker.y - origin.y) / Mathf.Max(1, _countY - 1),
                    Mathf.Abs(marker.z - origin.z) / Mathf.Max(1, _countZ - 1)
                );

                Handles.color = Color.magenta;
                for (int x = 0; x < _countX; x++)
                {
                    for (int y = 0; y < _countY; y++)
                    {
                        for (int z = 0; z < _countZ; z++)
                        {
                            if (x == 0 && y == 0 && z == 0) continue;
                            Vector3 offset = new Vector3(x * spacing.x, y * spacing.y, z * spacing.z);
                            Vector3 pos = origin + offset;
                            Handles.DotHandleCap(0, pos, Quaternion.identity, 0.04f, EventType.Repaint);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_directionMarker != null)
                Object.DestroyImmediate(_directionMarker);
            ClearPreview();
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }
}