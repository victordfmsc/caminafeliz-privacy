using TLab.WebView;
using UnityEngine;

namespace CaminaFeliz.VRBrowser.Integration
{
    /// <summary>
    /// Adapter from <see cref="IWebViewBackend"/> to TLabWebView's Android engine.
    /// </summary>
    /// <remarks>
    /// This is the only file in the project that knows the engine exists.
    /// It also owns three things the plugin leaves to the host application and
    /// that are easy to forget:
    /// <list type="number">
    /// <item>the per-frame pump - <c>UpdateFrame()</c> uploads the newest web
    /// frame and <c>DispatchMessageQueue()</c> drains the Java-side event queue.
    /// Neither is called by the plugin itself; without them the panel is a
    /// frozen image and no page callback ever fires;</item>
    /// <item>the Android gesture down-time, which must be threaded from the DOWN
    /// event through every MOVE and the final UP or the page sees unrelated taps
    /// instead of a drag;</item>
    /// <item>engine-specific data clearing, which lives on the concrete
    /// WebView/GeckoView types rather than on the shared base class.</item>
    /// </list>
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/TLab Web View Backend")]
    public class TLabWebViewBackend : WebViewBackend
    {
        [Tooltip("The TLabWebView Browser component (WebView or GeckoView) this adapter drives.")]
        [SerializeField] private Browser m_browser;

        private long m_downTime;
        private bool m_wasReady;

        public override bool IsReady =>
            m_browser != null && m_browser.state == FragmentCapture.State.Initialized;

        public override Vector2Int ViewSize =>
            m_browser != null ? m_browser.viewSize : Vector2Int.zero;

        public override Texture Texture =>
            m_browser != null && m_browser.rawImage != null ? m_browser.rawImage.texture : null;

        private void Reset() => m_browser = GetComponentInChildren<Browser>();

        private void OnEnable()
        {
            if (m_browser == null || m_browser.eventCallback == null)
                return;

            m_browser.eventCallback.onPageStart.AddListener(RaisePageStarted);
            m_browser.eventCallback.onPageFinish.AddListener(RaisePageFinished);
        }

        private void OnDisable()
        {
            if (m_browser == null || m_browser.eventCallback == null)
                return;

            m_browser.eventCallback.onPageStart.RemoveListener(RaisePageStarted);
            m_browser.eventCallback.onPageFinish.RemoveListener(RaisePageFinished);
        }

        private void Update()
        {
            if (m_browser == null)
                return;

            var ready = IsReady;
            if (ready != m_wasReady)
            {
                m_wasReady = ready;
                RaiseReadyChanged(ready);
            }

            if (!ready)
                return;

            m_browser.UpdateFrame();
            m_browser.DispatchMessageQueue();
        }

        public override void Initialize(Vector2Int viewSize, Vector2Int texSize, int fps, string startUrl)
        {
            if (m_browser == null)
            {
                Debug.LogError($"[{nameof(TLabWebViewBackend)}] no Browser assigned.", this);
                return;
            }

            m_browser.Init(viewSize, texSize, startUrl, fps, m_browser.downloadOption);
        }

        public override void LoadUrl(string url) => m_browser?.LoadUrl(url);

        public override string GetUrl() => m_browser != null ? m_browser.GetUrl() : string.Empty;

        public override void GoBack() => m_browser?.GoBack();

        public override void GoForward() => m_browser?.GoForward();

        public override void Reload()
        {
            if (m_browser == null)
                return;

            // The plugin has no Reload(); reloading from inside the page keeps
            // scroll restoration and POST-resubmission behaviour intact, which a
            // LoadUrl(GetUrl()) round trip would throw away.
            m_browser.EvaluateJS("location.reload();");
        }

        public override void ScrollBy(int deltaX, int deltaY) => m_browser?.ScrollBy(deltaX, deltaY);

        public override void SendTouch(Vector2 normalized, WebTouchPhase phase)
        {
            if (m_browser == null)
                return;

            var view = m_browser.viewSize;
            var x = Mathf.RoundToInt(normalized.x * view.x);
            var y = Mathf.RoundToInt(normalized.y * view.y);

            // TouchEvent returns the gesture's down-time; the DOWN event mints it
            // and every later event in the gesture has to carry the same value.
            var stamp = m_browser.TouchEvent(x, y, (int)phase, m_downTime);

            if (phase == WebTouchPhase.Down)
                m_downTime = stamp;
        }

        public override void SendCharacter(char character) => m_browser?.KeyEvent(character);

        public override void SendKeyCode(int androidKeyCode) => m_browser?.KeyEvent(androidKeyCode);

        public override void EvaluateJavaScript(string javascript) => m_browser?.EvaluateJS(javascript);

        public override void Resize(Vector2Int texSize, Vector2Int viewSize) =>
            m_browser?.Resize(texSize, viewSize);

        public override void ClearBrowsingData(bool cache, bool cookies, bool history)
        {
            switch (m_browser)
            {
                case TLab.WebView.WebView webView:
                    if (cache) webView.ClearCache(includeDiskFiles: true);
                    if (cookies) webView.ClearCookie();
                    if (history) webView.ClearHistory();
                    break;

                case GeckoView geckoView:
                    var flags = 0;
                    if (cache) flags |= GeckoClearFlags.AllCaches;
                    if (cookies) flags |= GeckoClearFlags.Cookies | GeckoClearFlags.DomStorages;
                    if (history) flags |= GeckoClearFlags.AuthSessions;
                    if (flags != 0) geckoView.ClearData(flags);
                    break;

                default:
                    Debug.LogWarning(
                        $"[{nameof(TLabWebViewBackend)}] ClearBrowsingData is not implemented for {m_browser?.GetType().Name ?? "null"}.",
                        this);
                    break;
            }
        }
    }

    /// <summary>
    /// Bit flags accepted by <c>GeckoView.ClearData</c>.
    /// </summary>
    /// <remarks>
    /// These mirror GeckoView's <c>StorageController.ClearFlags</c>. They are
    /// part of the Gecko runtime, not of this project, so re-check them against
    /// the geckoview version pinned in mainTemplate.gradle before relying on
    /// them for a privacy-facing feature.
    /// </remarks>
    public static class GeckoClearFlags
    {
        public const int Cookies = 1 << 0;
        public const int NetworkCache = 1 << 1;
        public const int ImageCache = 1 << 2;
        public const int DomStorages = 1 << 4;
        public const int AuthSessions = 1 << 5;

        public const int AllCaches = NetworkCache | ImageCache;
    }
}
