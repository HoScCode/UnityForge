using UnityEditor;
using UnityEngine;

namespace UnityForge.Tools
{
    public class ObjectStatsModule : StatsModuleBase
    {
        public override string Name => "Objects";

        private int active, inactive, scripts;

        public override void Update()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            active = 0; inactive = 0;

            foreach (var go in allObjects)
            {
                if (go.activeInHierarchy)
                    active++;
                else
                    inactive++;
            }

            scripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Length;
        }

        public override void Draw()
        {
            GUILayout.Label("Object Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active GameObjects:", active.ToString());
            EditorGUILayout.LabelField("Inactive GameObjects:", inactive.ToString());
            EditorGUILayout.LabelField("MonoBehaviours:", scripts.ToString());
        }
    }
}
