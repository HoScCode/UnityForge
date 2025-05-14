using UnityEditor;
using UnityEngine;

namespace UnityForge.Tools
{
    public class QuickTool : IUnityForgeTool
    {
        public string Name => "Quick";

        public void OnGUI()
        {
            GUILayout.Label("Quick Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Utility tools for grouping and ungrouping objects in the hierarchy.", MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Group Selected"))
                GroupSelected();

            if (GUILayout.Button("Ungroup Selected Parent"))
                UngroupSelected();

            DrawColliderChecker();
            DrawSceneCleanup();

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

            // Set parent based on first selected object
            Transform commonParent = selected[0].transform.parent;
            group.transform.SetParent(commonParent);

            // Set group position to center of selected objects
            Vector3 center = Vector3.zero;
            foreach (var obj in selected)
                center += obj.transform.position;
            center /= selected.Length;
            group.transform.position = center;

            foreach (var obj in selected)
            {
                Undo.SetTransformParent(obj.transform, group.transform, "Group Objects");
            }

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

            // Check if parent is empty (only Transform component)
            bool isEmpty = selected.GetComponents<Component>().Length == 1;

            if (!isEmpty)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Warning: Parent has components",
                    $"The selected object \"{selected.name}\" has components attached.\n" +
                    "Are you sure you want to ungroup it?",
                    "Yes", "Cancel"
                );

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
        
        private Vector2 _colliderScroll;
        private GameObject[] _skewedColliders = new GameObject[0];

        private void DrawColliderChecker()
        {
            GUILayout.Space(10);
            GUILayout.Label("Collider Checker", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Lists GameObjects where collider transform scale is not uniform (≠ 1,1,1).", MessageType.Info);

            if (GUILayout.Button("Scan Scene for Skewed Colliders"))
                ScanForSkewedColliders();

            if (_skewedColliders.Length > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Found {_skewedColliders.Length} potentially skewed colliders:");

                _colliderScroll = EditorGUILayout.BeginScrollView(_colliderScroll, GUILayout.Height(150));

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

            // Jetzt Button immer anzeigen
            GUILayout.Space(5);
            if (GUILayout.Button("Clear Results"))
            {
                _skewedColliders = new GameObject[0];
            }

        }

        private void ScanForSkewedColliders()
        {
            var allColliders = GameObject.FindObjectsOfType<Collider>(true);
            var skewed = new System.Collections.Generic.List<GameObject>();

            foreach (var col in allColliders)
            {
                Transform t = col.transform;
                Vector3 scale = t.lossyScale;

                bool isSkewed = false;

                if (col is SphereCollider sphere)
                {
                    Vector3 sphereScale = col.transform.lossyScale;
                    
                    // Normale Skalierung prüfen
                    bool unequalScale = !(Mathf.Approximately(sphereScale.x, sphereScale.y) && Mathf.Approximately(sphereScale.y, sphereScale.z));

                    
                    // Collider selbst prüfen
                    bool nonDefaultRadius = !Mathf.Approximately(sphere.radius, 0.5f);
                    bool nonZeroCenter = !Approximately(sphere.center, Vector3.zero, 0.01f);

                    isSkewed = unequalScale || nonDefaultRadius || nonZeroCenter;
                }
                else if (col is BoxCollider box)
                {
                    // Erst lossyScale prüfen
                    if (!Approximately(col.transform.lossyScale, Vector3.one, 0.01f))
                    {
                        isSkewed = true;
                    }
                    else
                    {
                        // Jetzt prüfen: Ist die Collider-Box selbst verzerrt?
                        Vector3 s = box.size;
                        float ratioXY = Mathf.Abs(s.x - s.y);
                        float ratioXZ = Mathf.Abs(s.x - s.z);
                        float ratioYZ = Mathf.Abs(s.y - s.z);

                        // Wenn Größen sich zu stark unterscheiden → verdächtig
                        if (ratioXY > 0.01f || ratioXZ > 0.01f || ratioYZ > 0.01f)
                            isSkewed = true;
                    }
                }

                else if (col is CapsuleCollider capsule)
                {
                    // Prüfen, ob die Achsen außer der Richtungsskala verändert wurden
                    int axis = capsule.direction;
                    isSkewed = axis switch
                    {
                        0 => !(Mathf.Approximately(scale.y, scale.z)), // X-Achse
                        1 => !(Mathf.Approximately(scale.x, scale.z)), // Y-Achse
                        2 => !(Mathf.Approximately(scale.x, scale.y)), // Z-Achse
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
            return  Mathf.Abs(a.x - b.x) < tolerance &&
                    Mathf.Abs(a.y - b.y) < tolerance &&
                    Mathf.Abs(a.z - b.z) < tolerance;
        }
        
        private void DrawSceneCleanup()
        {
            GUILayout.Space(10);
            GUILayout.Label("Scene Cleanup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Removes all empty GameObjects (no components, no children).", MessageType.Info);

            if (GUILayout.Button("Scan and Remove Empty GameObjects"))
            {
                CleanupEmptyGameObjects();
            }
        }

        
        private void CleanupEmptyGameObjects()
        {
            var all = GameObject.FindObjectsOfType<GameObject>(true);
            int removed = 0;

            foreach (var go in all)
            {
                if (go == null) continue;

                // Prüfen: Nur Transform-Komponente + keine Kinder
                var comps = go.GetComponents<Component>();
                if (comps.Length == 1 && comps[0] is Transform && go.transform.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(go);
                    removed++;
                }
            }

            UnityForgeWindow.AppendLogStatic($"Removed {removed} empty GameObject(s).");
        }




    }
}
