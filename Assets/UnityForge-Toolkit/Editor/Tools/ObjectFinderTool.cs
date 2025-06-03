using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityForge.Tools
{
    public class ObjectFinderTool : IUnityForgeTool
    {
        public string Name => "Finder";

        private GameObject _reference;
        private bool _searchMesh = true;
        private bool _searchMaterial = false;
        private bool _searchScript = false;
        private bool _searchIdentical = false;

        private List<GameObject> _foundObjects = new();

        public void OnGUI()
        {
            GUILayout.Label("Object Finder Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag a reference object. Choose what to compare.", MessageType.Info);

            _reference = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Reference Object", "Drag any GameObject from the scene or prefab."),
                _reference, typeof(GameObject), true);

            if (GUILayout.Button("Use Selected from Scene"))
            {
                if (Selection.gameObjects.Length == 1)
                {
                    _reference = Selection.gameObjects[0];
                    UnityForgeWindow.AppendLogStatic($"Reference set to: {_reference.name}");
                }
                else
                {
                    UnityForgeWindow.AppendLogStatic("Reference not set – please select exactly one object.");
                }
            }

            GUILayout.Label("Search by:");
            bool newSearchMesh = EditorGUILayout.Toggle("Mesh", _searchMesh);
            bool newSearchMaterial = EditorGUILayout.Toggle("Material", _searchMaterial);
            bool newSearchScript = EditorGUILayout.Toggle("Script", _searchScript);
            bool newSearchIdentical = EditorGUILayout.Toggle("Identical (all match)", _searchIdentical);

            if (newSearchIdentical && !_searchIdentical)
            {
                _searchIdentical = true;
                _searchMesh = false;
                _searchMaterial = false;
                _searchScript = false;
            }
            else if (!_searchIdentical && newSearchIdentical)
            {
                _searchIdentical = false;
            }
            else
            {
                _searchMesh = newSearchMesh;
                _searchMaterial = newSearchMaterial;
                _searchScript = newSearchScript;
                if (_searchMesh || _searchMaterial || _searchScript)
                    _searchIdentical = false;
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!IsSearchValid());
            if (GUILayout.Button("Find Matching Objects"))
            {
                RunSearch();
            }
            EditorGUI.EndDisabledGroup();

            if (_foundObjects.Count > 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField($"Found {_foundObjects.Count} matching object(s).");

                try
                {
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Select", GUILayout.Width(80)))
                        Selection.objects = _foundObjects.ToArray();

                    EditorGUILayout.EndHorizontal();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Finder] Error in OnGUI: {ex.Message}\n{ex.StackTrace}");
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private bool IsSearchValid()
        {
            if (_reference == null)
                return false;

            if (_searchIdentical)
            {
                return _reference.GetComponent<MeshFilter>() != null
                    || _reference.GetComponent<Renderer>() != null
                    || _reference.GetComponents<MonoBehaviour>().Length > 0;
            }

            if (_searchMesh && _reference.GetComponent<MeshFilter>() != null)
                return true;

            if (_searchMaterial && _reference.GetComponent<Renderer>() != null)
                return true;

            if (_searchScript && _reference.GetComponents<MonoBehaviour>().Length > 0)
                return true;

            return false;
        }

        private void RunSearch()
        {
            _foundObjects.Clear();
            if (_reference == null)
            {
                EditorUtility.DisplayDialog("Finder", "Please set a reference object.", "OK");
                return;
            }

            Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);


            var refMesh = _reference.GetComponent<MeshFilter>()?.sharedMesh;
            var refMat = _reference.GetComponent<Renderer>()?.sharedMaterial;
            var refTypes = _reference.GetComponents<MonoBehaviour>().Select(c => c.GetType()).ToList();

            foreach (var go in all)
            {
                if (go == _reference) continue;

                bool meshMatch = false, matMatch = false, scriptMatch = false;

                if ((_searchMesh || _searchIdentical) && refMesh != null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh == refMesh)
                        meshMatch = true;
                }

                if ((_searchMaterial || _searchIdentical) && refMat != null)
                {
                    var rend = go.GetComponent<Renderer>();
                    if (rend != null && rend.sharedMaterial == refMat)
                        matMatch = true;
                }

                if (_searchScript || _searchIdentical)
                {
                    var types = go.GetComponents<MonoBehaviour>().Select(c => c.GetType()).ToList();
                    if (types.Count == refTypes.Count && !types.Except(refTypes).Any())
                        scriptMatch = true;
                }

                bool match = false;

                if (_searchIdentical)
                {
                    match = true;

                    var refMF = _reference.GetComponent<MeshFilter>();
                    if (refMF != null)
                    {
                        var candMF = go.GetComponent<MeshFilter>();
                        if (candMF == null || candMF.sharedMesh != refMF.sharedMesh)
                            match = false;
                    }

                    var refRenderer = _reference.GetComponent<Renderer>();
                    if (refRenderer != null)
                    {
                        var candRenderer = go.GetComponent<Renderer>();
                        if (candRenderer == null || candRenderer.sharedMaterial != refRenderer.sharedMaterial)
                            match = false;
                    }

                    var refScripts = _reference.GetComponents<MonoBehaviour>().Select(c => c.GetType()).ToList();
                    if (refScripts.Count > 0)
                    {
                        var candScripts = go.GetComponents<MonoBehaviour>().Select(c => c.GetType()).ToList();
                        if (!refScripts.All(t => candScripts.Contains(t)))
                            match = false;
                    }
                }
                else
                {
                    match = (_searchMesh && meshMatch)
                        || (_searchMaterial && matMatch)
                        || (_searchScript && scriptMatch);
                }

                if (match)
                    _foundObjects.Add(go);
            }

            UnityForgeWindow.AppendLogStatic($"Found {_foundObjects.Count} matching object(s).");
        }
    }
}
