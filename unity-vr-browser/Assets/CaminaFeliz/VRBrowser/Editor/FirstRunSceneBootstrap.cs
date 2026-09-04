using System.IO;
using UnityEditor;
using UnityEngine;

namespace CaminaFeliz.VRBrowser.Editor
{
    /// <summary>
    /// Creates the main scene the first time the project is opened.
    /// </summary>
    /// <remarks>
    /// A cloned Unity project with no scene asset opens on an empty untitled
    /// scene and an empty Build Settings, which reads as "nothing here" even
    /// when every script is present. Generating the scene on first load removes
    /// that step and the confusion with it.
    ///
    /// It runs at most once per project: it does nothing if the scene file
    /// already exists, and records the attempt so deleting the scene on purpose
    /// does not bring it back. It never touches an existing scene.
    /// </remarks>
    [InitializeOnLoad]
    public static class FirstRunSceneBootstrap
    {
        private const string PreferenceKey = "CaminaFeliz.VRBrowser.SceneBootstrapped";

        static FirstRunSceneBootstrap()
        {
            // Creating a scene during a domain reload is not safe; wait for the
            // Editor to be idle.
            EditorApplication.delayCall += TryBootstrap;
        }

        private static void TryBootstrap()
        {
            // A batch build has its own scene handling and should never be
            // surprised by a new asset appearing mid-run.
            if (Application.isBatchMode)
                return;

            var key = PreferenceKey + ":" + Application.dataPath;
            if (EditorPrefs.GetBool(key, false))
                return;

            if (File.Exists(SceneComposer.ScenePath))
            {
                EditorPrefs.SetBool(key, true);
                return;
            }

            EditorPrefs.SetBool(key, true);

            Debug.Log("[VRBrowser] No hay escena todavía: creando la principal.");
            SceneComposer.ComposeAndSave();
        }
    }
}
