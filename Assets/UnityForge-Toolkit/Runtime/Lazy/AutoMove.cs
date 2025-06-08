// Assets/Runtime/Lazy/AutoMove.cs
using UnityEngine;

namespace UnityForge.Lazy
{
    public class AutoMove : MonoBehaviour
    {
        public Transform pointA;
        public Transform pointB;
        public float duration = 2f;
        public MovementMode movementMode = MovementMode.Loop;
        public EaseType easeType = EaseType.Linear;

        private float _t;
        private bool _forward = true;

        public enum EaseType { Linear, EaseIn, EaseOut, EaseInOut }
        public enum MovementMode { Loop, PingPong, Once }

        void Update()
        {
            if (pointA == null || pointB == null || duration <= 0f) return;

            _t += Time.deltaTime / duration * (_forward ? 1 : -1);
            float easedT = ApplyEase(Mathf.Clamp01(_t), easeType);
            transform.position = Vector3.Lerp(pointA.position, pointB.position, easedT);

            switch (movementMode)
            {
                case MovementMode.PingPong:
                    if (_t >= 1f) _forward = false;
                    if (_t <= 0f) _forward = true;
                    break;

                case MovementMode.Loop:
                    if (_t >= 1f) _t = 0f;
                    break;

                case MovementMode.Once:
                    _t = Mathf.Min(_t, 1f);
                    break;
            }
        }

        private float ApplyEase(float t, EaseType ease)
        {
            return ease switch
            {
                EaseType.EaseIn => t * t,
                EaseType.EaseOut => t * (2 - t),
                EaseType.EaseInOut => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t,
                _ => t
            };
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (pointA != null && pointB != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pointA.position, pointB.position);
                Gizmos.DrawSphere(pointA.position, 0.1f);
                Gizmos.DrawSphere(pointB.position, 0.1f);
            }
        }
#endif
    }
}
