using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class QuickTool : IUnityForgeTool
    {
        public string Name => "Quick";

        private bool _showQuickTools = true;
        private bool _showColliderChecker = true;
        private bool _showSceneCleanup = true;
        private bool _showStaticChecker = true;

        private Vector2 _colliderScroll;
        private GameObject[] _skewedColliders = new GameObject[0];

        private Vector2 _staticCheckerScroll;
        private GameObject[] _nonStaticObjects = new GameObject[0];

        public void OnGUI()
        {
            _showQuickTools = EditorGUILayout.Foldout(_showQuickTools, "Quick Tools", true);
            if (_showQuickTools)
                DrawGroupTools();

            _showColliderChecker = EditorGUILayout.Foldout(_showColliderChecker, "Collider Checker", true);
            if (_showColliderChecker)
                DrawColliderChecker();

            _showSceneCleanup = EditorGUILayout.Foldout(_showSceneCleanup, "Scene Cleanup", true);
            if (_showSceneCleanup)
                DrawSceneCleanup();

            _showStaticChecker = EditorGUILayout.Foldout(_showStaticChecker, "Static Checker", true);
            if (_showStaticChecker)
                DrawStaticChecker();
        }

        private void DrawGroupTools()
        {
            EditorGUILayout.HelpBox("Utility tools for grouping and ungrouping objects in the hierarchy.", MessageType.Info);
            GUILayout.Space(5);

            if (GUILayout.Button("Group Selected"))
                GroupSelected();

            if (GUILayout.Button("Ungroup Selected Parent"))
                UngroupSelected();
        }

        private void GroupSelected()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Quick Tools", "Please select at least one object.", "OK");
                return;
            }

            GameObject group = new GameObject("Group");
            Undo.RegisterCreatedObjectUndo(group, "Create Group");

            Transform commonParent = selected[0].transform.parent;
            group.transform.SetParent(commonParent);

            Vector3 center = Vector3.zero;
            foreach (var obj in selected)
                center += obj.transform.position;
            center /= selected.Length;
            group.transform.position = center;

            foreach (var obj in selected)
                Undo.SetTransformParent(obj.transform, group.transform, "Group Objects");

            Selection.activeGameObject = group;
            UnityForgeWindow.AppendLogStatic("Grouped selected objects.");
        }

        private void UngroupSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null || selected.transform.childCount == 0)
            {
                EditorUtility.DisplayDialog("Quick Tools", "Please select a parent object with children.", "OK");
                return;
            }

            bool isEmpty = selected.GetComponents<Component>().Length == 1;

            if (!isEmpty)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Warning: Parent has components",
                    $"The selected object \"{selected.name}\" has components attached.\nAre you sure you want to ungroup it?",
                    "Yes", "Cancel");

                if (!proceed)
                    return;
            }

            Transform parent = selected.transform;
            Transform grandParent = parent.parent;

            int moved = 0;
            while (parent.childCount > 0)
            {
                Transform child = parent.GetChild(0);
                Undo.SetTransformParent(child, grandParent, "Ungroup Objects");
                moved++;
            }

            Undo.DestroyObjectImmediate(parent.gameObject);
            UnityForgeWindow.AppendLogStatic($"Ungrouped {moved} object(s).");
        }

        private void DrawColliderChecker()
        {
            EditorGUILayout.HelpBox("Lists GameObjects where collider transform scale is not uniform (≠ 1,1,1).", MessageType.Info);

            if (GUILayout.Button("Scan Scene for Skewed Colliders"))
                ScanForSkewedColliders();

            if (_skewedColliders.Length > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Found {_skewedColliders.Length} potentially skewed colliders:");

                int visibleCount = Mathf.Min(5, _skewedColliders.Length);
                float rowHeight = 22f;
                float totalHeight = rowHeight * visibleCount + 5;

                _colliderScroll = EditorGUILayout.BeginScrollView(_colliderScroll, GUILayout.Height(totalHeight));
                foreach (var go in _skewedColliders)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeGameObject = go;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            if (GUILayout.Button("Clear Results"))
                _skewedColliders = new GameObject[0];
        }

        private void ScanForSkewedColliders()
        {
            var allColliders = GameObject.FindObjectsOfType<Collider>(true);
            var skewed = new List<GameObject>();

            foreach (var col in allColliders)
            {
                Transform t = col.transform;
                Vector3 scale = t.lossyScale;
                bool isSkewed = false;

                if (col is SphereCollider sphere)
                {
                    Vector3 sphereScale = t.lossyScale;
                    bool unequalScale = !(Mathf.Approximately(sphereScale.x, sphereScale.y) && Mathf.Approximately(sphereScale.y, sphereScale.z));
                    bool nonDefaultRadius = !Mathf.Approximately(sphere.radius, 0.5f);
                    bool nonZeroCenter = !Approximately(sphere.center, Vector3.zero, 0.01f);
                    isSkewed = unequalScale || nonDefaultRadius || nonZeroCenter;
                }
                else if (col is BoxCollider box)
                {
                    if (!Approximately(scale, Vector3.one, 0.01f))
                    {
                        isSkewed = true;
                    }
                    else
                    {
                        Vector3 s = box.size;
                        float ratioXY = Mathf.Abs(s.x - s.y);
                        float ratioXZ = Mathf.Abs(s.x - s.z);
                        float ratioYZ = Mathf.Abs(s.y - s.z);
                        if (ratioXY > 0.01f || ratioXZ > 0.01f || ratioYZ > 0.01f)
                            isSkewed = true;
                    }
                }
                else if (col is CapsuleCollider capsule)
                {
                    int axis = capsule.direction;
                    isSkewed = axis switch
                    {
                        0 => !(Mathf.Approximately(scale.y, scale.z)),
                        1 => !(Mathf.Approximately(scale.x, scale.z)),
                        2 => !(Mathf.Approximately(scale.x, scale.y)),
                        _ => true
                    };
                }

                if (isSkewed)
                    skewed.Add(col.gameObject);
            }

            _skewedColliders = skewed.ToArray();
            UnityForgeWindow.AppendLogStatic($"Collider scan complete: {_skewedColliders.Length} skewed collider(s) found.");
        }

        private bool Approximately(Vector3 a, Vector3 b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.x - b.x) < tolerance &&
                   Mathf.Abs(a.y - b.y) < tolerance &&
                   Mathf.Abs(a.z - b.z) < tolerance;
        }

        private void DrawSceneCleanup()
        {
            EditorGUILayout.HelpBox("Removes all empty GameObjects (no components, no children).", MessageType.Info);

            if (GUILayout.Button("Scan and Remove Empty GameObjects"))
                CleanupEmptyGameObjects();
        }

        private void CleanupEmptyGameObjects()
        {
            var all = GameObject.FindObjectsOfType<GameObject>(true);
            int removed = 0;

            foreach (var go in all)
            {
                if (go == null) continue;

                var comps = go.GetComponents<Component>();
                if (comps.Length == 1 && comps[0] is Transform && go.transform.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(go);
                    removed++;
                }
            }

            UnityForgeWindow.AppendLogStatic($"Removed {removed} empty GameObject(s).");
        }

        private void DrawStaticChecker()
        {
            EditorGUILayout.HelpBox("Lists all objects with Renderer that are not marked as Static.", MessageType.Info);

            if (GUILayout.Button("Scan Scene for Non-Static Renderers"))
                RunStaticCheck();

            if (_nonStaticObjects.Length > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Found {_nonStaticObjects.Length} non-static object(s):");

                int visibleCount = Mathf.Min(5, _nonStaticObjects.Length);
                float rowHeight = 22f;
                float totalHeight = rowHeight * visibleCount + 5;

                _staticCheckerScroll = EditorGUILayout.BeginScrollView(_staticCheckerScroll, GUILayout.Height(totalHeight));
                foreach (var go in _nonStaticObjects)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeGameObject = go;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Select All"))
                    Selection.objects = _nonStaticObjects;
            }

            if (GUILayout.Button("Clear Results"))
                _nonStaticObjects = new GameObject[0];
        }

        private void RunStaticCheck()
        {
            var all = GameObject.FindObjectsOfType<Renderer>(true);
            var list = new List<GameObject>();

            foreach (var r in all)
            {
                if (r == null || r.gameObject == null)
                    continue;

                if (!GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic))
                    list.Add(r.gameObject);
            }

            _nonStaticObjects = list.ToArray();
            UnityForgeWindow.AppendLogStatic($"Static Checker found {_nonStaticObjects.Length} non-static object(s).");
        }
    }
}
