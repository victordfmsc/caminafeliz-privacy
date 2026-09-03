using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CaminaFeliz.VRBrowser.Editor
{
    /// <summary>
    /// Produces a sideloadable development APK for Quest, from the Editor menu
    /// or from a headless command line.
    /// </summary>
    /// <remarks>
    /// A Quest build has a dozen settings that must all be right, and getting
    /// one wrong usually fails at install time or, worse, at run time with a
    /// black screen. Scripting them means the build is reproducible on any
    /// machine and diffable in review, and it makes a CI build possible later
    /// without rediscovering the list.
    ///
    /// Headless use:
    /// <code>
    /// Unity -quit -batchmode -nographics -projectPath . \
    ///       -executeMethod CaminaFeliz.VRBrowser.Editor.QuestBuildPipeline.BuildFromCommandLine \
    ///       -apkOutput Build/CaminaFelizVRBrowser.apk
    /// </code>
    /// </remarks>
    public static class QuestBuildPipeline
    {
        private const string MenuRoot = "Tools/CaminaFeliz VR Browser/";

        public const string DefaultOutputPath = "Build/CaminaFelizVRBrowser.apk";
        private const string ApplicationIdentifier = "com.vertey.caminafelizvrbrowser";
        private const string ProductName = "CaminaFeliz VR Browser";
        private const string CompanyName = "Vertey";

        [MenuItem(MenuRoot + "Build Development APK")]
        public static void BuildDevelopmentApkMenu() => BuildApk(DefaultOutputPath, development: true);

        [MenuItem(MenuRoot + "Build Release APK")]
        public static void BuildReleaseApkMenu() => BuildApk(DefaultOutputPath, development: false);

        /// <summary>Entry point for -executeMethod. Exits the Editor with the build's status.</summary>
        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var output = ArgumentValue(args, "-apkOutput") ?? DefaultOutputPath;
            var development = ArgumentValue(args, "-buildType") != "release";

            var succeeded = BuildApk(output, development);

            // In batch mode nothing else will stop the Editor, and a build
            // failure has to reach the shell as a non-zero status or a broken
            // build silently looks like a good one.
            if (Application.isBatchMode)
                EditorApplication.Exit(succeeded ? 0 : 1);
        }

        public static bool BuildApk(string outputPath, bool development)
        {
            var problems = VrBrowserProjectSetup.Collect();
            if (problems.Count > 0)
            {
                // Applying is safe and is what a fresh clone needs; the audit
                // below then reports anything this tool cannot fix itself.
                Debug.Log("[VRBrowser] Aplicando ajustes de build de Quest antes de compilar.");
                VrBrowserProjectSetup.Apply();
            }

            ApplyIdentity(development);

            var scenes = ResolveScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[VRBrowser] No hay ninguna escena que compilar.");
                return false;
            }

            var absoluteOutput = Path.IsPathRooted(outputPath)
                ? outputPath
                : Path.Combine(Directory.GetCurrentDirectory(), outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutput) ?? ".");

            // An .aab cannot be sideloaded with adb; this pipeline always makes an APK.
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = absoluteOutput,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            Debug.Log($"[VRBrowser] Compilando {(development ? "development" : "release")} APK -> {absoluteOutput}");
            foreach (var scene in scenes)
                Debug.Log($"[VRBrowser]   escena: {scene}");

            var report = BuildPipeline.BuildPlayer(options);
            return Report(report, absoluteOutput);
        }

        private static void ApplyIdentity(bool development)
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, ApplicationIdentifier);

            // Unity signs with its own debug keystore when no keystore is set.
            // That is exactly what sideloading in developer mode wants, and it
            // keeps a signing key out of the repository.
            PlayerSettings.Android.useCustomKeystore = false;

            if (development)
                PlayerSettings.Android.bundleVersionCode++;
        }

        /// <summary>
        /// Use whatever is in Build Settings; if that is empty, generate the
        /// prototype scene rather than failing. A first build on a fresh clone
        /// should produce something you can put on a headset.
        /// </summary>
        private static string[] ResolveScenes()
        {
            var configured = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path) && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (configured.Length > 0)
                return configured;

            Debug.Log("[VRBrowser] Build Settings vacío: genero la escena prototipo.");
            var generated = PrototypeSceneBuilder.BuildAndSave();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(generated, true) };
            return new[] { generated };
        }

        private static bool Report(BuildReport report, string outputPath)
        {
            var summary = report.summary;
            var succeeded = summary.result == BuildResult.Succeeded;

            if (succeeded)
            {
                var megabytes = summary.totalSize / (1024f * 1024f);
                Debug.Log(
                    $"[VRBrowser] Build OK en {summary.totalTime.TotalSeconds:F0}s, " +
                    $"{megabytes:F1} MB -> {outputPath}\n" +
                    $"Instalar con: adb install -r \"{outputPath}\"");
                return true;
            }

            Debug.LogError($"[VRBrowser] Build {summary.result} con {summary.totalErrors} error(es).");

            foreach (var message in FailureMessages(report))
                Debug.LogError("[VRBrowser]   " + message);

            return false;
        }

        /// <summary>
        /// Pull the actual errors out of the report. Unity's own console dump is
        /// unreadable in batch mode, and "Build failed" alone helps nobody.
        /// </summary>
        private static IEnumerable<string> FailureMessages(BuildReport report)
        {
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                        yield return $"{step.name}: {message.content}";
                }
            }
        }

        private static string ArgumentValue(string[] args, string name)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
