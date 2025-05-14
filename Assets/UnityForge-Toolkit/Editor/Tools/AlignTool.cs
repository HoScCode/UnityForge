using UnityEditor;
using UnityEngine;

namespace UnityForge.Tools
{
    public class AlignTool : IUnityForgeTool
    {
        public string Name => "Align";

        private bool[] _pos = new bool[3];  // X,Y,Z
        private bool[] _rot = new bool[3];
        private bool[] _scl = new bool[3];
        private GameObject _referenceObject;


        private bool _allPos, _allRot, _allScl;

        public void OnGUI()
        {
            
            GUILayout.Label("Align to Zero/One", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Set Position / Rotation to zero, Scale to one – per axis or globally.", MessageType.Info);

            _referenceObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Reference", "Optional reference object to copy values from."),
                _referenceObject, typeof(GameObject), true);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(40)); // Spacer
            GUILayout.Label("POS", GUILayout.Width(40));
            GUILayout.Label("ROT", GUILayout.Width(40));
            GUILayout.Label("SCALE", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            DrawAxisRow("X", 0);
            DrawAxisRow("Y", 1);
            DrawAxisRow("Z", 2);
            DrawAllRow();

            GUILayout.Space(10);
            if (GUILayout.Button("Apply Alignment"))
            {
                ApplyTransformChanges();
            }
        }

        private void DrawAxisRow(string label, int axis)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(40));
            _pos[axis] = EditorGUILayout.Toggle(_pos[axis], GUILayout.Width(40));
            _rot[axis] = EditorGUILayout.Toggle(_rot[axis], GUILayout.Width(40));
            _scl[axis] = EditorGUILayout.Toggle(_scl[axis], GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAllRow()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ALL", GUILayout.Width(40));

            bool prevAllPos = _allPos;
            bool prevAllRot = _allRot;
            bool prevAllScl = _allScl;

            bool newAllPos = EditorGUILayout.Toggle(_allPos, GUILayout.Width(40));
            bool newAllRot = EditorGUILayout.Toggle(_allRot, GUILayout.Width(40));
            bool newAllScl = EditorGUILayout.Toggle(_allScl, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            // Änderungen erfassen → setzen oder löschen
            if (newAllPos != prevAllPos)
            {
                for (int i = 0; i < 3; i++)
                    _pos[i] = newAllPos;
            }

            if (newAllRot != prevAllRot)
            {
                for (int i = 0; i < 3; i++)
                    _rot[i] = newAllRot;
            }

            if (newAllScl != prevAllScl)
            {
                for (int i = 0; i < 3; i++)
                    _scl[i] = newAllScl;
            }

            // Update "ALL"-Flags basierend auf aktuellem Zustand
            _allPos = _pos[0] && _pos[1] && _pos[2];
            _allRot = _rot[0] && _rot[1] && _rot[2];
            _allScl = _scl[0] && _scl[1] && _scl[2];
        }


        private void ApplyTransformChanges()
{
    var selected = Selection.transforms;
    if (selected == null || selected.Length == 0)
    {
        EditorUtility.DisplayDialog("Align Tool", "Please select one or more GameObjects.", "OK");
        return;
    }

    // Bestimme Referenzwerte: entweder von Objekt oder von Welt
    Vector3 refPosition = Vector3.zero;
    Vector3 refRotation = Vector3.zero;
    Vector3 refScale    = Vector3.one;

    if (_referenceObject != null)
    {
        var refTransform = _referenceObject.transform;
        refPosition = _referenceObject.transform.position;
        refRotation = _referenceObject.transform.localEulerAngles;
        refScale    = _referenceObject.transform.localScale;
    }

    Undo.RecordObjects(selected, "Align Transforms");

    foreach (var tr in selected)
    {
        // --- POSITION ---
        if (tr.parent != null && _referenceObject != null && _referenceObject.transform.parent != null)
        {
            Vector3 localPos = tr.localPosition;
            Vector3 refLocal = _referenceObject.transform.localPosition;
            if (_pos[0]) localPos.x = refLocal.x;
            if (_pos[1]) localPos.y = refLocal.y;
            if (_pos[2]) localPos.z = refLocal.z;
            tr.localPosition = localPos;
        }
        else
        {
            Vector3 worldPos = tr.position;
            if (_pos[0]) worldPos.x = refPosition.x;
            if (_pos[1]) worldPos.y = refPosition.y;
            if (_pos[2]) worldPos.z = refPosition.z;
            tr.position = worldPos;
        }

        // --- ROTATION ---
        Vector3 euler = tr.localEulerAngles;
        if (_rot[0]) euler.x = refRotation.x;
        if (_rot[1]) euler.y = refRotation.y;
        if (_rot[2]) euler.z = refRotation.z;
        tr.localEulerAngles = euler;

        // --- SCALE ---
        Vector3 scale = tr.localScale;
        if (_scl[0]) scale.x = refScale.x;
        if (_scl[1]) scale.y = refScale.y;
        if (_scl[2]) scale.z = refScale.z;
        tr.localScale = scale;
    }

    string refName = _referenceObject ? _referenceObject.name : "World Origin";
    UnityForgeWindow.AppendLogStatic($"Aligned {selected.Length} object(s) to {(refName)}.");
}


    }
}
