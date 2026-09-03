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
        }
        public static void SetScriptingBackend(BuildTargetGroup g, ScriptingImplementation i) { }
        public static ScriptingImplementation GetScriptingBackend(BuildTargetGroup g) => ScriptingImplementation.IL2CPP;
        public static void SetUseDefaultGraphicsAPIs(BuildTarget t, bool v) { }
        public static void SetGraphicsAPIs(BuildTarget t, UnityEngine.Rendering.GraphicsDeviceType[] apis) { }
        public static string GetScriptingDefineSymbolsForGroup(BuildTargetGroup g) => "";
        public static void SetScriptingDefineSymbolsForGroup(BuildTargetGroup g, string defines) { }
    }

    public static class EditorUserBuildSettings
    {
        public static BuildTarget activeBuildTarget => BuildTarget.Android;
        public static bool SwitchActiveBuildTarget(BuildTargetGroup g, BuildTarget t) => true;
    }

    public static class AssetDatabase { public static void SaveAssets() { } }
    public static class Selection { public static UnityEngine.Object activeObject { get; set; } }

    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute { public MenuItem(string path) { } }

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
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public class Scene { }
    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => new Scene();
        public static void MarkSceneDirty(Scene s) { }
    }
}
