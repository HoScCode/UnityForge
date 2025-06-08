using UnityEngine;
using UnityEditor;
using UnityForge.Tools.LazyModules;

namespace UnityForge.Tools
{
    public class LazyTool : IUnityForgeTool
    {
        private AutoMoveModule _autoMoveModule = new AutoMoveModule();

        public string Name => "Lazy";

        private string[] _modules = new[]
        {
            "AutoMove",
            "AutoRotate",
            "TriggerZone",
            "PulseMaterial",
            "LightFlicker",
            "ScalePulse",
            "Billboard",
            "ToggleActive",
            "PlaySound"
        };

        private int _selectedIndex = 0;

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Linkes Menü (Modul-Auswahl)
            EditorGUILayout.BeginVertical(GUILayout.Width(140));
            GUILayout.Label("Lazy Modules", EditorStyles.boldLabel);
            _selectedIndex = GUILayout.SelectionGrid(_selectedIndex, _modules, 1);
            EditorGUILayout.EndVertical();

            // Rechter Bereich (Settings)
            EditorGUILayout.BeginVertical();
            GUILayout.Label($"Settings: {_modules[_selectedIndex]}", EditorStyles.boldLabel);

            switch (_modules[_selectedIndex])
            {
                case "AutoMove":
                    _autoMoveModule.DrawSettings();
                    break;

                default:
                    EditorGUILayout.HelpBox("Dieses Modul ist noch nicht implementiert.", MessageType.Info);
                    break;
            }

            EditorGUILayout.EndVertical();


            EditorGUILayout.EndHorizontal();
        }
    }
}
