// Assets/Editor/UnityForge/Tools/ScaleTool.cs
using UnityEditor;
using UnityEngine;

namespace UnityForge.Tools
{
    /// <summary>
    /// Scale Selected Objects: Scale manually or via reference, and reposition to reference's position.
    /// </summary>
    public class ScaleTool : IUnityForgeTool
    {
        public string Name => "Scale";

        private Vector3 _targetScale = Vector3.one;
        private GameObject _referenceObject = null;

        public void OnGUI()
        {
            // Global description
            EditorGUILayout.HelpBox(
                "Scale Tool: Scale manually or via reference object, and optionally reposition to the reference's position.",
                MessageType.Info);

            GUILayout.Label(new GUIContent("Scale Selected Objects", "Scale or reposition selected objects."), EditorStyles.boldLabel);

            // Reference Object Drag & Drop
            EditorGUI.BeginChangeCheck();
            _referenceObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Reference Object", "Drag an object to use its scale and/or position."),
                _referenceObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && _referenceObject == null)
            {
                _targetScale = Vector3.one;
            }

            // Scale input or display reference scale
            if (_referenceObject == null)
            {
                _targetScale.x = EditorGUILayout.FloatField(
                    new GUIContent("Target Scale X", "Manual scale factor on X axis."), _targetScale.x);
                _targetScale.y = EditorGUILayout.FloatField(
                    new GUIContent("Target Scale Y", "Manual scale factor on Y axis."), _targetScale.y);
                _targetScale.z = EditorGUILayout.FloatField(
                    new GUIContent("Target Scale Z", "Manual scale factor on Z axis."), _targetScale.z);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                Vector3 refScale = _referenceObject.transform.localScale;
                EditorGUILayout.FloatField(
                    new GUIContent("Reference Scale X", "Scale of reference on X axis."), refScale.x);
                EditorGUILayout.FloatField(
                    new GUIContent("Reference Scale Y", "Scale of reference on Y axis."), refScale.y);
                EditorGUILayout.FloatField(
                    new GUIContent("Reference Scale Z", "Scale of reference on Z axis."), refScale.z);
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            // Rescale Selection button
            if (GUILayout.Button(new GUIContent("Rescale Selection", "Apply scale to all selected objects.")))
            {
                int count = RescaleSelection();
                UnityForgeWindow.AppendLogStatic($"{count} objects rescaled");
            }

            // Reposition Selection button
            EditorGUI.BeginDisabledGroup(_referenceObject == null);
            if (GUILayout.Button(new GUIContent("Reposition Selection", "Move selected objects to reference position.")))
            {
                int count = RepositionSelection();
                UnityForgeWindow.AppendLogStatic($"{count} objects repositioned");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Tooltip display
            var tip = GUI.tooltip;
            if (!string.IsNullOrEmpty(tip))
            {
                EditorGUILayout.HelpBox(tip, MessageType.None);
            }
        }

        private int RescaleSelection()
        {
            var sel = Selection.gameObjects;
            if (sel == null || sel.Length == 0)
                return 0;

            Vector3 desired = _referenceObject != null
                ? _referenceObject.transform.localScale
                : _targetScale;

            foreach (var go in sel)
            {
                Undo.RecordObject(go.transform, "Scale Scale");
                go.transform.localScale = desired;
            }
            return sel.Length;
        }

        private int RepositionSelection()
        {
            var sel = Selection.gameObjects;
            if (_referenceObject == null || sel == null || sel.Length == 0)
                return 0;

            Vector3 pos = _referenceObject.transform.position;
            foreach (var go in sel)
            {
                Undo.RecordObject(go.transform, "Scale Position");
                go.transform.position = pos;
            }
            return sel.Length;
        }
    }
}
