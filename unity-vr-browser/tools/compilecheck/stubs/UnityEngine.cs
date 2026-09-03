// Minimal stand-ins for the Unity API surface the project uses, so the project's
// own C# can be compiled and its logic executed outside the Editor.
// Signatures mirror Unity's; bodies are inert unless a test needs the behaviour.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.x * s, a.y * s);
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public override bool Equals(object o) => o is Vector2Int v && this == v;
        public override int GetHashCode() => x * 397 ^ y;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized => this;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public static float Angle(Vector3 a, Vector3 b) => 0f;
    }

    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion LookRotation(Vector3 f, Vector3 u) => identity;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color black => new Color(0, 0, 0);
        public static Color white => new Color(1, 1, 1);
        public static Color Lerp(Color a, Color b, float t) => a;
        public static implicit operator Color32(Color c) => new Color32();
    }

    public struct Color32 { public byte r, g, b, a; }

    public static class Mathf
    {
        public const float Epsilon = 1e-5f;
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Abs(float v) => Math.Abs(v);
        public static float Sign(float v) => v < 0 ? -1f : 1f;
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static float Repeat(float t, float len) => t - (float)Math.Floor(t / len) * len;
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 1e-6f;
        public static float Exp(float v) => (float)Math.Exp(v);
        public static float SmoothDamp(float cur, float target, ref float vel, float time) => target;
    }

    public class Object
    {
        public string name { get; set; } = "";
        public static void Destroy(Object o) { }
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public override bool Equals(object o) => ReferenceEquals(this, o);
        public override int GetHashCode() => base.GetHashCode();
        public static T FindObjectOfType<T>() where T : Object => null;
        public static T[] FindObjectsOfType<T>() where T : Object => new T[0];
    }

    public class Component : Object
    {
        public Transform transform { get; set; }
        public GameObject gameObject { get; set; }
        public T GetComponent<T>() where T : Component => null;
        public T GetComponentInChildren<T>() where T : Component => null;
        public T GetComponentInParent<T>() where T : Component => null;
        public T AddComponent<T>() where T : Component => null;
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale { get; set; }
        public Vector3 forward { get; set; }
        public Quaternion rotation { get; set; }
        public Transform parent { get; set; }
        public Vector3 InverseTransformPoint(Vector3 p) => p;
        public void SetParent(Transform p, bool worldPositionStays) { }
        public IEnumerator GetEnumerator() => new List<Transform>().GetEnumerator();
    }

    public class RectTransform : Transform
    {
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Rect rect { get; set; }
    }

    public struct Rect { public float width, height; }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public GameObject(string name, params Type[] components) { this.name = name; }
        public Transform transform { get; set; } = new Transform();
        public string tag { get; set; }
        public void SetActive(bool active) { }
        public T GetComponent<T>() where T : Component => null;
        public T AddComponent<T>() where T : Component => null;
    }

    public class MonoBehaviour : Component
    {
        public bool enabled { get; set; } = true;
        public Coroutine StartCoroutine(IEnumerator routine) => null;
    }

    public class Coroutine { }
    public class AudioListener : Component { }
    public class ScriptableObject : Object { }

    public class Texture : Object { }

    public class Texture2D : Texture
    {
        public int width { get; private set; }
        public int height { get; private set; }
        public TextureWrapMode wrapMode { get; set; }
        public FilterMode filterMode { get; set; }
        public Texture2D(int w, int h, TextureFormat f, bool mipChain) { width = w; height = h; }
        public void Reinitialize(int w, int h) { width = w; height = h; }
        public void SetPixels32(Color32[] pixels) { }
        public void Apply(bool updateMipmaps) { }
    }

    public class RenderTexture : Texture
    {
        public TextureWrapMode wrapMode { get; set; }
        public RenderTexture(int w, int h, int depth) { }
        public void Release() { }
    }

    public enum TextureFormat { RGBA32 }
    public enum TextureWrapMode { Clamp, Repeat }
    public enum FilterMode { Point, Bilinear }

    public class Shader : Object
    {
        public static Shader Find(string name) => null;
        public static int PropertyToID(string name) => 0;
    }

    public class Material : Object
    {
        public Material(Material source) { }
        public Material(Shader shader) { }
        public void SetTexture(int id, Texture value) { }
        public void SetFloat(int id, float value) { }
    }

    public class Camera : Component
    {
        public static Camera main => null;
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float nearClipPlane { get; set; }
    }

    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }

    public class Font : Object { }

    public static class RenderSettings { public static Material skybox { get; set; } }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
        public static T Load<T>(string path) where T : Object => null;
    }

    public static class Debug
    {
        public static void Log(object m) => Console.WriteLine("[log] " + m);
        public static void Log(object m, Object c) => Log(m);
        public static void LogWarning(object m) => Console.WriteLine("[warn] " + m);
        public static void LogWarning(object m, Object c) => LogWarning(m);
        public static void LogError(object m) => Console.WriteLine("[error] " + m);
        public static void LogError(object m, Object c) => LogError(m);
    }

    public static class Time { public static float deltaTime => 0.016f; }

    public static class Application
    {
        public static bool isPlaying => false;
        public static bool isBatchMode => false;
    }

    public enum LogType { Error, Assert, Warning, Log, Exception }

    public static class JsonUtility
    {
        public static T FromJson<T>(string json) => StubJson.FromJson<T>(json);
        public static string ToJson(object o) => "{}";
        public static void FromJsonOverwrite(string json, object target) { }
    }

    // Attributes and helpers used purely for inspector metadata.
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.All)] public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.All)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.All)] public class MinAttribute : Attribute { public MinAttribute(float m) { } }
    [AttributeUsage(AttributeTargets.Class)] public class AddComponentMenu : Attribute { public AddComponentMenu(string m) { } }
    [AttributeUsage(AttributeTargets.Class)] public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    [AttributeUsage(AttributeTargets.All)] public class SerializableAttribute2 : Attribute { }
}

namespace UnityEngine.Rendering
{
    public enum GraphicsDeviceType { OpenGLES3, Vulkan }
}

namespace UnityEngine.Events
{
    public class UnityEventBase { }
    public class UnityEvent : UnityEventBase
    {
        private readonly List<UnityAction> m_calls = new List<UnityAction>();
        public void AddListener(UnityAction a) => m_calls.Add(a);
        public void RemoveListener(UnityAction a) => m_calls.Remove(a);
        public void Invoke() { foreach (var c in m_calls.ToArray()) c(); }
    }
    public class UnityEvent<T> : UnityEventBase
    {
        private readonly List<UnityAction<T>> m_calls = new List<UnityAction<T>>();
        public void AddListener(UnityAction<T> a) => m_calls.Add(a);
        public void RemoveListener(UnityAction<T> a) => m_calls.Remove(a);
        public void Invoke(T arg) { foreach (var c in m_calls.ToArray()) c(arg); }
    }
    public delegate void UnityAction();
    public delegate void UnityAction<T>(T arg);
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;
    public class Graphic : Component { public Color color { get; set; } }
    public class Image : Graphic { }
    public class RawImage : Graphic { public Texture texture { get; set; } }
    public class Text : Graphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
    }
    public class Selectable : Component { public bool interactable { get; set; } = true; }
    public class Button : Selectable { public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent(); public class ButtonClickedEvent : UnityEvent { } }
    public class Slider : Selectable
    {
        public enum Direction { LeftToRight }
        public class SliderEvent : UnityEvent<float> { }
        public SliderEvent onValueChanged { get; } = new SliderEvent();
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public Graphic targetGraphic { get; set; }
        public Direction direction { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float value { get; set; }
    }
    public class Canvas : Component
    {
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
    }
    public class CanvasScaler : Component { }
    public class GraphicRaycaster : Component { }
}

namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public enum TextAnchor { MiddleCenter }
    public enum UIOrientation { LandscapeLeft }
    public enum ColorSpace { Gamma, Linear }
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform r, Vector2 screen, Camera cam, out Vector2 local)
        { local = Vector2.zero; return true; }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Component { }
    public class StandaloneInputModule : Component { }
    public class PointerEventData
    {
        public Vector2 position { get; set; }
        public int pointerId { get; set; }
        public Camera pressEventCamera { get; set; }
    }
    public interface IPointerDownHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler { void OnPointerUp(PointerEventData e); }
    public interface IDragHandler { void OnDrag(PointerEventData e); }
    public interface IPointerExitHandler { void OnPointerExit(PointerEventData e); }
}

namespace UnityEngine.Video
{
    public enum VideoSource { VideoClip, Url }
    public enum VideoRenderMode { RenderTexture }
    public enum VideoAudioOutputMode { None, AudioSource, Direct }
    public class VideoPlayer : Component
    {
        public string url { get; set; }
        public bool playOnAwake { get; set; }
        public bool isLooping { get; set; }
        public bool isPlaying { get; set; }
        public double time { get; set; }
        public double length { get; set; }
        public VideoRenderMode renderMode { get; set; }
        public RenderTexture targetTexture { get; set; }
        public VideoSource source { get; set; }
        public VideoAudioOutputMode audioOutputMode { get; set; }
        public event EventHandler errorReceived;
        public event EventHandler2 loopPointReached;
        public event EventHandler2 prepareCompleted;
        public delegate void EventHandler(VideoPlayer source, string message);
        public delegate void EventHandler2(VideoPlayer source);
        public void Prepare() { }
        public void Play() { }
        public void Pause() { }
        public void Stop() { }
        public void SetDirectAudioVolume(ushort track, float volume) { }
    }
}

namespace UnityEngine.InputSystem
{
    public class InputAction
    {
        public bool enabled { get; private set; }
        public void Enable() => enabled = true;
        public void Disable() => enabled = false;
        public T ReadValue<T>() => default;
    }
    public class InputActionReference { }
    public struct InputActionProperty
    {
        public InputAction action => null;
        public InputActionReference reference => null;
    }
}

namespace TMPro
{
    using UnityEngine;
    using UnityEngine.Events;
    public class TMP_Text : Component { public string text { get; set; } }
    public class TMP_InputField : Component
    {
        public string text { get; set; }
        public class SelectionEvent : UnityEvent<string> { }
        public class SubmitEvent : UnityEvent<string> { }
        public SelectionEvent onSelect { get; } = new SelectionEvent();
        public SelectionEvent onDeselect { get; } = new SelectionEvent();
        public SubmitEvent onSubmit { get; } = new SubmitEvent();
        public void SetTextWithoutNotify(string v) { text = v; }
    }
}
