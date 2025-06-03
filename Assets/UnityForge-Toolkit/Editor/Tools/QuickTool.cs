using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityForge.Tools
{
    public class QuickTool : IUnityForgeTool
    {
        public string Name => "Quick";
        public string DisplayName => "Quick Tool Section"; // GUI-Zweck
        private bool _showQuickTools = false;
        private bool _showColliderChecker = false;
        private bool _showSceneCleanup = false;
        private bool _showStaticChecker = false;

        private Vector2 _colliderScroll;
        private GameObject[] _skewedColliders = new GameObject[0];

        private Vector2 _staticCheckerScroll;
        private GameObject[] _nonStaticObjects = new GameObject[0];
        private MonoScript _scriptAssetToAdd;
        
        private int _selectedColliderType = 0;
        private readonly string[] _colliderOptions = new string[] { "None", "BoxCollider", "SphereCollider", "CapsuleCollider", "MeshCollider" };
        
        private GameObject _componentCopySource;
        private List<Component> _copiableComponents = new();
        private Vector2 _componentScroll;
        private List<bool> _componentCopyFlags = new();
        private bool _showResetItem = false;



        public void OnGUI()
{
    // ▸ Modify Item (einklappbar oben)
    _showResetItem = EditorGUILayout.Foldout(_showResetItem, "Modify Item", true);
    if (_showResetItem)
        DrawResetItemTool();

    EditorGUILayout.Space(10);

    // ▸ Erste Zeile: Group Tools & Collider Checker (nebeneinander)
    EditorGUILayout.BeginHorizontal();

    // ▸ Group Tools Box (linke Spalte)
    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 25));
    _showQuickTools = EditorGUILayout.Foldout(_showQuickTools, "Group Tools", true);
    if (_showQuickTools)
        DrawGroupTools();
    EditorGUILayout.EndVertical();

    // ▸ Collider Checker Box (rechte Spalte)
    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 25));
    _showColliderChecker = EditorGUILayout.Foldout(_showColliderChecker, "Collider Checker", true);
    if (_showColliderChecker)
        DrawColliderChecker();
    EditorGUILayout.EndVertical();

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space(10);

    // ▸ Zweite Zeile: Scene Cleanup & Static Checker (nebeneinander)
    EditorGUILayout.BeginHorizontal();

    // ▸ Scene Cleanup Box (linke Spalte)
    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 25));
    _showSceneCleanup = EditorGUILayout.Foldout(_showSceneCleanup, "Scene Cleanup", true);
    if (_showSceneCleanup)
        DrawSceneCleanup();
    EditorGUILayout.EndVertical();

    // ▸ Static Checker Box (rechte Spalte)
    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 25));
    _showStaticChecker = EditorGUILayout.Foldout(_showStaticChecker, "Static Checker", true);
    if (_showStaticChecker)
        DrawStaticChecker();
    EditorGUILayout.EndVertical();

    EditorGUILayout.EndHorizontal();
}


        private bool _resetTransform = true;
        private bool _removeScripts = true;
        private bool _removeRenderers = false;
        private bool _removeColliders = false;

        private void DrawResetItemTool()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Modify Item", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Modify selected GameObject to default state. Configure removals, additions, or transfer components from another object.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            // LEFT SIDE
            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 30));

            GUILayout.Label("Remove Components", EditorStyles.boldLabel);
            _resetTransform = EditorGUILayout.Toggle("Reset Transform", _resetTransform);
            _removeScripts = EditorGUILayout.Toggle("Remove Scripts", _removeScripts);
            _removeRenderers = EditorGUILayout.Toggle("Remove Renderers", _removeRenderers);
            _removeColliders = EditorGUILayout.Toggle("Remove Colliders", _removeColliders);

            GUILayout.Space(8);
            if (GUILayout.Button("Remove Selected", GUILayout.Width(200)) && Selection.activeGameObject != null)
            {
                ApplyReset(Selection.activeGameObject, onlyRemove: true);
            }

            GUILayout.Space(10);
            GUILayout.Label("Add Components", EditorStyles.boldLabel);
            _selectedColliderType = EditorGUILayout.Popup("Add Collider", _selectedColliderType, _colliderOptions);
            _scriptAssetToAdd = (MonoScript)EditorGUILayout.ObjectField("Add Script", _scriptAssetToAdd, typeof(MonoScript), false);

            GUILayout.Space(5);
            if (GUILayout.Button("Add Components", GUILayout.Width(200)) && Selection.activeGameObject != null)
            {
                ApplyReset(Selection.activeGameObject, onlyAdd: true);
            }

            EditorGUILayout.EndVertical();

            // VERTICAL SEPARATOR
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            GUILayout.Space(10);

            // RIGHT SIDE
            EditorGUILayout.BeginVertical();

            GUILayout.Label("Copy From Object", EditorStyles.boldLabel);
            GameObject newSource = (GameObject)EditorGUILayout.ObjectField("Source", _componentCopySource, typeof(GameObject), true);
            if (newSource != _componentCopySource)
            {
                _componentCopySource = newSource;
                UpdateCopiableComponents();
            }

            GUILayout.Space(5);

            if (_copiableComponents.Count > 0)
            {
                GUILayout.Label("Transferable Components:");
                _componentScroll = EditorGUILayout.BeginScrollView(_componentScroll, GUILayout.Height(90));
                for (int i = 0; i < _copiableComponents.Count; i++)
                {
                    var comp = _copiableComponents[i];
                    if (comp == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    _componentCopyFlags[i] = EditorGUILayout.Toggle(_componentCopyFlags[i], GUILayout.Width(20));
                    EditorGUILayout.LabelField(comp.GetType().Name);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No components to transfer.", EditorStyles.miniLabel);
            }

            GUILayout.Space(5);
            GUI.enabled = _componentCopySource != null && _copiableComponents.Count > 0;
            if (GUILayout.Button("Copy To Selected", GUILayout.Width(200)) && Selection.activeGameObject != null)
            {
                ApplyComponentCopyToTarget(_componentCopySource, Selection.activeGameObject);
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }


        
        
        private void ApplyCopiedComponents(GameObject source, GameObject target)
        {
            foreach (var comp in _copiableComponents)
            {
                if (comp == null) continue;
                System.Type type = comp.GetType();

                if (target.GetComponent(type) != null)
                    continue; // nicht doppelt

                Component newComp = Undo.AddComponent(target, type);

                // Kopiere Felder
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.IsDefined(typeof(System.NonSerializedAttribute), true)) continue;
                    field.SetValue(newComp, field.GetValue(comp));
                }

                // Kopiere Properties (optional)
                var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (!prop.CanWrite || !prop.CanRead || prop.Name == "name") continue;
                    if (prop.GetIndexParameters().Length > 0) continue;
                    try { prop.SetValue(newComp, prop.GetValue(comp)); }
                    catch { /* überspringe inkompatible */ }
                }
            }

            UnityForgeWindow.AppendLogStatic($"Transferred {_copiableComponents.Count} component(s) to: {target.name}");
        }

       private void ApplyReset(GameObject go, bool onlyRemove = false, bool onlyAdd = false)
        {
            if (!onlyAdd)
            {
                if (_resetTransform)
                {
                    Undo.RecordObject(go.transform, "Reset Transform");
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                }

                if (_removeScripts)
                {
                    foreach (var comp in go.GetComponents<MonoBehaviour>())
                    {
                        if (comp != null)
                            Undo.DestroyObjectImmediate(comp);
                    }
                }

                if (_removeRenderers)
                {
                    foreach (var renderer in go.GetComponents<Renderer>())
                        Undo.DestroyObjectImmediate(renderer);
                }

                if (_removeColliders)
                {
                    foreach (var collider in go.GetComponents<Collider>())
                        Undo.DestroyObjectImmediate(collider);
                }
            }

            if (!onlyRemove)
            {
                // Add Collider
                if (_selectedColliderType > 0)
                {
                    string typeName = "UnityEngine." + _colliderOptions[_selectedColliderType];
                    var colliderType = System.Type.GetType(typeName + ", UnityEngine");
                    if (colliderType != null && go.GetComponent(colliderType) == null)
                    {
                        Undo.AddComponent(go, colliderType);
                    }
                    else if (colliderType == null)
                    {
                        Debug.LogWarning("Collider type not found: " + typeName);
                    }
                }

                // Add Script
                if (_scriptAssetToAdd != null)
                {
                    var scriptType = _scriptAssetToAdd.GetClass();
                    if (scriptType != null && scriptType.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        if (go.GetComponent(scriptType) == null)
                            Undo.AddComponent(go, scriptType);
                    }
                    else
                    {
                        Debug.LogWarning("Selected script is not a valid MonoBehaviour: " + _scriptAssetToAdd.name);
                    }
                }


            }

            UnityForgeWindow.AppendLogStatic($"Reset/Add applied to: {go.name}");
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
            var allColliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
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
            var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
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
            var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
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
        
        

        private void UpdateCopiableComponents()
        {
            _copiableComponents.Clear();
            _componentCopyFlags.Clear();

            if (_componentCopySource == null) return;

            foreach (var comp in _componentCopySource.GetComponents<Component>())
            {
                if (comp is Transform || comp is MeshFilter || comp is MeshRenderer)
                    continue;
                if (comp == null) continue;

                _copiableComponents.Add(comp);
                _componentCopyFlags.Add(true); // standardmäßig aktiviert
            }
        }


        private void ApplyComponentCopyToTarget(GameObject source, GameObject target)
{
    for (int i = 0; i < _copiableComponents.Count; i++)
    {
        if (!_componentCopyFlags[i]) continue;

        var comp = _copiableComponents[i];
        if (comp == null) continue;

        System.Type type = comp.GetType();
        if (target.GetComponent(type) != null)
            continue;

        Component newComp = Undo.AddComponent(target, type);

        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsDefined(typeof(System.NonSerializedAttribute), true)) continue;
            field.SetValue(newComp, field.GetValue(comp));
        }

        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in props)
        {
            if (!prop.CanWrite || !prop.CanRead || prop.Name == "name") continue;
            if (prop.GetIndexParameters().Length > 0) continue;
            try { prop.SetValue(newComp, prop.GetValue(comp)); }
            catch { }
        }
    }

    UnityForgeWindow.AppendLogStatic("Component transfer completed.");
}

    }
}
