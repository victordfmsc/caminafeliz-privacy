using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CaminaFeliz.VRBrowser.Editor
{
    /// <summary>
    /// Applies and audits the project settings the Android web engine requires.
    /// </summary>
    /// <remarks>
    /// The engine's requirements (Linear colour space, API 26+, a specific set
    /// of scripting defines, Internet permission not stripped) are documented as
    /// a checklist of inspector clicks. Checklists rot: someone opens the project
    /// on a new machine, one box is unticked, and the panel renders a black
    /// rectangle with no error in the log. Encoding them here makes the setup
    /// reproducible and, more usefully, auditable - Validate reports what is
    /// wrong without changing anything.
    /// </remarks>
    public static class VrBrowserProjectSetup
    {
        private const string MenuRoot = "Tools/CaminaFeliz VR Browser/";

        /// <summary>Defines the TLabWebView build post-processor reads to patch the Android manifest.</summary>
        private static readonly string[] RequiredDefines =
        {
            "UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC",
            "UNITYWEBVIEW_ANDROID_ENABLE_CAMERA",
            "UNITYWEBVIEW_ANDROID_ENABLE_MICROPHONE",
        };

        private const AndroidSdkVersions MinSdk = AndroidSdkVersions.AndroidApiLevel26;

        /// <summary>API 33 is the floor for the GeckoView engine; the WebView engine is happy lower.</summary>
        private const AndroidSdkVersions TargetSdk = AndroidSdkVersions.AndroidApiLevel33;

        [MenuItem(MenuRoot + "Apply Quest Build Settings")]
        public static void Apply()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            // The engine uploads sRGB frames; in Gamma space every page renders washed out.
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.Android.minSdkVersion = MinSdk;
            PlayerSettings.Android.targetSdkVersion = TargetSdk;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

            // A browser without Internet permission is a black panel and no error.
            // The XR plug-in's "Force Remove Internet Permission" is the usual culprit.
            PlayerSettings.Android.forceInternetPermission = true;

            // OpenGLES3 is the combination the engine is best tested against.
            // Vulkan works with HardwareBuffer capture on Quest but is reported
            // to blank the panel on some other Adreno parts - see docs/03-setup-quest.md.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            ApplyDefines();

            AssetDatabase.SaveAssets();
            Debug.Log("[VRBrowser] Quest build settings applied. Run Validate to confirm.");
        }

        [MenuItem(MenuRoot + "Validate Setup")]
        public static void Validate()
        {
            var problems = Collect();

            if (problems.Count == 0)
            {
                Debug.Log("[VRBrowser] Setup looks correct.");
                return;
            }

            var report = new StringBuilder("[VRBrowser] Setup problems found:\n");
            foreach (var problem in problems)
                report.Append("  - ").AppendLine(problem);

            report.AppendLine($"Run {MenuRoot}Apply Quest Build Settings to fix the ones this tool owns.");
            Debug.LogWarning(report.ToString());
        }

        /// <summary>Read-only audit, safe to call from a build script or a CI check.</summary>
        public static List<string> Collect()
        {
            var problems = new List<string>();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                problems.Add($"Active build target is {EditorUserBuildSettings.activeBuildTarget}, expected Android.");

            if (PlayerSettings.colorSpace != ColorSpace.Linear)
                problems.Add("Colour space is Gamma; the engine expects Linear or pages render washed out.");

            if (PlayerSettings.Android.minSdkVersion < MinSdk)
                problems.Add($"Minimum API level is {PlayerSettings.Android.minSdkVersion}, expected {MinSdk} or higher.");

            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                problems.Add("ARM64 is not among the target architectures; Quest will refuse the build.");

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
                problems.Add("Scripting backend is not IL2CPP, which ARM64 requires.");

            if (!PlayerSettings.Android.forceInternetPermission)
                problems.Add("Internet permission is not forced; check XR Plug-in Management's 'Force Remove Internet Permission'.");

            if (EditorBuildSettings.scenes.All(scene => !scene.enabled))
                problems.Add("No hay ninguna escena activa en Build Settings; usa Create or Rebuild Main Scene.");

            var defines = CurrentDefines();
            foreach (var define in RequiredDefines.Where(define => !defines.Contains(define)))
                problems.Add($"Missing scripting define symbol: {define}");

            return problems;
        }

        private static void ApplyDefines()
        {
            var defines = CurrentDefines();
            var added = RequiredDefines.Where(define => !defines.Contains(define)).ToArray();

            if (added.Length == 0)
                return;

            defines.AddRange(added);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Android, string.Join(";", defines));
        }

        private static List<string> CurrentDefines() =>
            PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)
                .Split(';')
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToList();
    }
}
