using UnityEngine;
using UnityEditor;
using UnityForge.Lazy;

namespace UnityForge.Tools.LazyModules
{
    public class AutoMoveModule
    {
        public string Name => "AutoMove";

        // Konfigurationsdaten
        private Transform pointA;
        private Transform pointB;
        private float duration = 2f;
        private AutoMove.MovementMode movementMode = AutoMove.MovementMode.Loop;
        private AutoMove.EaseType ease = AutoMove.EaseType.Linear;

        public void DrawSettings()
        {
            //GUILayout.Label("AutoMove Settings", EditorStyles.boldLabel);

            pointA = (Transform)EditorGUILayout.ObjectField("Point A", pointA, typeof(Transform), true);
            pointB = (Transform)EditorGUILayout.ObjectField("Point B", pointB, typeof(Transform), true);
            duration = EditorGUILayout.FloatField("Duration", duration);
            movementMode = (AutoMove.MovementMode)EditorGUILayout.EnumPopup("Movement", movementMode);
            ease = (AutoMove.EaseType)EditorGUILayout.EnumPopup("Ease", ease);

            GUILayout.Space(10);
            if (GUILayout.Button("Apply to Selected"))
            {
                ApplyToSelection();
            }
            GUILayout.Space(4);
            if (GUILayout.Button("Reset on Selected"))
            {
                ResetOnSelection();
            }
        }

        private void ApplyToSelection()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var mover = obj.GetComponent<AutoMove>();
                if (!mover) mover = obj.AddComponent<AutoMove>();

                // Wenn keine Targets gesetzt, auto-erstellen
                if (pointA == null)
                {
                    var a = new GameObject("MovePoint_A").transform;
                    a.position = obj.transform.position;
                    pointA = a;
                }

                if (pointB == null)
                {
                    var b = new GameObject("MovePoint_B").transform;
                    b.position = obj.transform.position + Vector3.right * 2f;
                    pointB = b;
                }

                mover.pointA = pointA;
                mover.pointB = pointB;
                mover.duration = duration;
                mover.movementMode = movementMode;
                mover.easeType = ease;

                Debug.Log($"[Lazy] Applied AutoMove to {obj.name}");
            }
        }
        private void ResetOnSelection()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var mover = obj.GetComponent<AutoMove>();
                if (mover)
                {
        #if UNITY_EDITOR
                    Object.DestroyImmediate(mover);
        #else
                    Object.Destroy(mover);
        #endif
                    Debug.Log($"[Lazy] Removed AutoMove from {obj.name}");
                }
            }
        }


    }
}
