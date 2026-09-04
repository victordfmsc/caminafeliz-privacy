using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CaminaFeliz.VRBrowser.Editor
{
    /// <summary>
    /// Finds and configures types that this assembly deliberately does not
    /// reference at compile time.
    /// </summary>
    /// <remarks>
    /// The scene needs Meta's OVRPassthroughLayer and TLabWebView's Browser, but
    /// referencing either from here would make the whole editor tooling fail to
    /// compile whenever one of those packages is missing or still resolving -
    /// exactly when you most need a tool that explains what is missing. Looking
    /// them up by name at runtime keeps the tooling alive and lets it report
    /// precisely which piece is absent, and configuring them through
    /// SerializedObject works on any UnityEngine.Object without a typed
    /// reference.
    /// </remarks>
    public static class EditorTypeResolver
    {
        /// <summary>Find a type by full name across every loaded assembly.</summary>
        public static Type Find(string fullName)
        {
            var direct = Type.GetType(fullName);
            if (direct != null)
                return direct;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }

            return null;
        }

        /// <summary>Add a component named at runtime; returns null when the type is unavailable.</summary>
        public static Component AddComponent(GameObject target, string fullName)
        {
            var type = Find(fullName);
            if (type == null)
                return null;

            return target.AddComponent(type);
        }

        /// <summary>
        /// Set a serialized field by name. Returns false when the field does not
        /// exist, which is how we notice a package changed its layout instead of
        /// silently writing nothing.
        /// </summary>
        public static bool SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            if (target == null)
                return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
                return false;

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static bool SetBool(UnityEngine.Object target, string field, bool value)
        {
            if (target == null)
                return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
                return false;

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static bool SetEnum(UnityEngine.Object target, string field, int value)
        {
            if (target == null)
                return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
                return false;

            property.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static bool SetString(UnityEngine.Object target, string field, string value)
        {
            if (target == null)
                return false;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
                return false;

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        /// <summary>Load the first asset matching a name and type, from Assets or from any package.</summary>
        public static T FindAsset<T>(string assetName) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != assetName)
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    return asset;
            }

            return null;
        }

        /// <summary>True when a package's assembly is loaded, used to report what is missing.</summary>
        public static bool HasType(string fullName) => Find(fullName) != null;

        public static string DescribeAvailability(params (string label, string type)[] checks) =>
            string.Join("\n", checks.Select(check =>
                $"  {(HasType(check.type) ? "OK      " : "FALTA   ")}{check.label}"));
    }
}
