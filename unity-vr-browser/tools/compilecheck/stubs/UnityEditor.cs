// Stand-ins for the Editor API used by the project's editor tooling.
using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor
{
    public enum BuildTarget { Android, StandaloneWindows64 }
    public enum BuildTargetGroup { Android, Standalone }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2 }
    public enum AndroidSdkVersions { AndroidApiLevel26 = 26, AndroidApiLevel30 = 30, AndroidApiLevel33 = 33 }

    public static class PlayerSettings
    {
        public static ColorSpace colorSpace { get; set; }
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static class Android
        {
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static AndroidSdkVersions targetSdkVersion { get; set; }
            public static AndroidArchitecture targetArchitectures { get; set; }
            public static bool forceInternetPermission { get; set; }
            public static bool useCustomKeystore { get; set; }
            public static int bundleVersionCode { get; set; }
        }
        public static void SetScriptingBackend(BuildTargetGroup g, ScriptingImplementation i) { }
        public static ScriptingImplementation GetScriptingBackend(BuildTargetGroup g) => ScriptingImplementation.IL2CPP;
        public static void SetUseDefaultGraphicsAPIs(BuildTarget t, bool v) { }
        public static void SetGraphicsAPIs(BuildTarget t, UnityEngine.Rendering.GraphicsDeviceType[] apis) { }
        public static string GetScriptingDefineSymbolsForGroup(BuildTargetGroup g) => "";
        public static void SetScriptingDefineSymbolsForGroup(BuildTargetGroup g, string defines) { }
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static void SetApplicationIdentifier(BuildTargetGroup g, string id) { }
    }

    public static class EditorUserBuildSettings
    {
        public static BuildTarget activeBuildTarget => BuildTarget.Android;
        public static bool buildAppBundle { get; set; }
        public static bool SwitchActiveBuildTarget(BuildTargetGroup g, BuildTarget t) => true;
    }

    public static class AssetDatabase
    {
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static string[] FindAssets(string filter) => new string[0];
        public static string GUIDToAssetPath(string guid) => "";
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => null;
    }

    public static class EditorApplication
    {
        public static void Exit(int code) { }
        public static Action delayCall { get; set; }
    }

    [Flags]
    public enum BuildOptions { None = 0, Development = 1, AllowDebugging = 2 }

    public struct BuildPlayerOptions
    {
        public string[] scenes;
        public string locationPathName;
        public BuildTarget target;
        public BuildTargetGroup targetGroup;
        public BuildOptions options;
    }

    public class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
        public string path { get; set; }
        public bool enabled { get; set; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; } = new EditorBuildSettingsScene[0];
    }

    public static class BuildPipeline
    {
        public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options) =>
            new Build.Reporting.BuildReport();
    }
    public static class Selection { public static UnityEngine.Object activeObject { get; set; } }

    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string path) { }
        public int priority { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoad : Attribute { }

    public static class EditorPrefs
    {
        public static bool GetBool(string key, bool fallback) => fallback;
        public static void SetBool(string key, bool value) { }
    }

    public static class PrefabUtility
    {
        public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object prefab) => new UnityEngine.GameObject();
    }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object o) { }
        public SerializedProperty FindProperty(string path) => new SerializedProperty();
        public void ApplyModifiedPropertiesWithoutUndo() { }
    }

    public class SerializedProperty
    {
        public UnityEngine.Object objectReferenceValue { get; set; }
        public string stringValue { get; set; }
        public bool boolValue { get; set; }
        public int enumValueIndex { get; set; }
    }
}

namespace UnityEditor.Events
{
    public static class UnityEventTools
    {
        public static void AddPersistentListener(UnityEvent e, UnityAction call) { }
        public static void AddPersistentListener<T>(UnityEvent<T> e, UnityAction<T> call) { }
    }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;

    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => new Scene();
        public static void MarkSceneDirty(Scene s) { }
        public static bool SaveScene(Scene s, string path) => true;
        public static Scene OpenScene(string path) => new Scene();
    }
}

namespace UnityEditor.Build.Reporting
{
    using System;
    using UnityEngine;

    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }

    public class BuildSummary
    {
        public BuildResult result = BuildResult.Succeeded;
        public ulong totalSize = 0;
        public TimeSpan totalTime = TimeSpan.Zero;
        public int totalErrors = 0;
    }

    public struct BuildStepMessage { public LogType type; public string content; }

    public class BuildStep { public string name; public BuildStepMessage[] messages = new BuildStepMessage[0]; }

    public class BuildReport
    {
        public BuildSummary summary { get; } = new BuildSummary();
        public BuildStep[] steps { get; } = new BuildStep[0];
    }
}
