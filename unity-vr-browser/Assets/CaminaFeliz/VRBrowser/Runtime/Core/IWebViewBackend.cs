using System;
using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Phase of a synthesised pointer event sent to the web engine.
    /// Named to avoid colliding with <see cref="UnityEngine.TouchPhase"/>.
    /// </summary>
    public enum WebTouchPhase
    {
        Down = 0,
        Up = 1,
        Move = 2,
    }

    /// <summary>
    /// Everything the VR shell needs from a web engine.
    /// </summary>
    /// <remarks>
    /// The whole point of this interface is that nothing above it knows about
    /// Android, JNI or TLabWebView. That buys us two things:
    /// <list type="bullet">
    /// <item>we can drive the panel, the pointer mapping and the chrome inside
    /// the Unity Editor against <see cref="SimulatedWebViewBackend"/>, which the
    /// underlying Android plugin cannot do (it renders nothing outside a device);</item>
    /// <item>swapping the engine later (GeckoView, a CEF process on desktop, a
    /// future OpenXR-native browser) is a new implementation, not a rewrite.</item>
    /// </list>
    /// </remarks>
    public interface IWebViewBackend
    {
        /// <summary>True once the engine is initialised and safe to call.</summary>
        bool IsReady { get; }

        /// <summary>Logical page size in CSS pixels. Pointer coordinates are expressed against this.</summary>
        Vector2Int ViewSize { get; }

        /// <summary>Texture the engine renders into, or null before initialisation.</summary>
        Texture Texture { get; }

        event Action<bool> ReadyChanged;
        event Action<string> PageStarted;
        event Action<string> PageFinished;

        void Initialize(Vector2Int viewSize, Vector2Int texSize, int fps, string startUrl);

        void LoadUrl(string url);
        string GetUrl();

        void GoBack();
        void GoForward();
        void Reload();

        void ScrollBy(int deltaX, int deltaY);

        /// <param name="normalized">
        /// Pointer position in the panel's own space: (0,0) top-left, (1,1) bottom-right.
        /// The backend is responsible for converting to engine pixels and for
        /// threading the Android down-time through a gesture.
        /// </param>
        void SendTouch(Vector2 normalized, WebTouchPhase phase);

        void SendCharacter(char character);

        /// <summary>Send a raw Android key code (see <see cref="AndroidKeyCode"/>).</summary>
        void SendKeyCode(int androidKeyCode);

        void EvaluateJavaScript(string javascript);

        void ClearBrowsingData(bool cache, bool cookies, bool history);

        void Resize(Vector2Int texSize, Vector2Int viewSize);
    }

    /// <summary>
    /// The subset of Android key codes the browser shell needs. The engine takes
    /// raw ints, and hard-coded 67s scattered through UI code age badly.
    /// </summary>
    public static class AndroidKeyCode
    {
        public const int Back = 4;
        public const int DpadUp = 19;
        public const int DpadDown = 20;
        public const int DpadLeft = 21;
        public const int DpadRight = 22;
        public const int Enter = 66;
        public const int Delete = 67;   // Backspace
        public const int Tab = 61;
        public const int Escape = 111;
        public const int ForwardDelete = 112;
        public const int PageUp = 92;
        public const int PageDown = 93;
        public const int MoveHome = 122;
        public const int MoveEnd = 123;
    }

    /// <summary>
    /// Serialisable MonoBehaviour base for backends.
    /// Unity cannot serialise plain interface references, so every inspector slot
    /// in this project is typed as <see cref="WebViewBackend"/>, not <see cref="IWebViewBackend"/>.
    /// </summary>
    public abstract class WebViewBackend : MonoBehaviour, IWebViewBackend
    {
        public abstract bool IsReady { get; }
        public abstract Vector2Int ViewSize { get; }
        public abstract Texture Texture { get; }

        public event Action<bool> ReadyChanged;
        public event Action<string> PageStarted;
        public event Action<string> PageFinished;

        public abstract void Initialize(Vector2Int viewSize, Vector2Int texSize, int fps, string startUrl);
        public abstract void LoadUrl(string url);
        public abstract string GetUrl();
        public abstract void GoBack();
        public abstract void GoForward();
        public abstract void Reload();
        public abstract void ScrollBy(int deltaX, int deltaY);
        public abstract void SendTouch(Vector2 normalized, WebTouchPhase phase);
        public abstract void SendCharacter(char character);
        public abstract void SendKeyCode(int androidKeyCode);
        public abstract void EvaluateJavaScript(string javascript);
        public abstract void ClearBrowsingData(bool cache, bool cookies, bool history);
        public abstract void Resize(Vector2Int texSize, Vector2Int viewSize);

        protected void RaiseReadyChanged(bool ready) => ReadyChanged?.Invoke(ready);
        protected void RaisePageStarted(string url) => PageStarted?.Invoke(url);
        protected void RaisePageFinished(string url) => PageFinished?.Invoke(url);
    }
}
