// Assets/Editor/UnityForge/Tools/IUnityForgeTool.cs
namespace UnityForge.Tools
{
    /// <summary>
    /// Jedes Tool implementiert dieses Interface.
    /// </summary>
    public interface IUnityForgeTool
    {
        /// <summary>
        /// Name für den Tab-Button.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Zeichnet das Tool-spezifische GUI.
        /// </summary>
        void OnGUI();
    }
}
