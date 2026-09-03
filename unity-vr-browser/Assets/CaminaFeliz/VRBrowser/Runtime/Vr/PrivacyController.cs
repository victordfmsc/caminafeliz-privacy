using UnityEngine;
using UnityEngine.Events;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Privacy actions surfaced as one component, so the UI does not have to
    /// know which of them the engine supports natively.
    /// </summary>
    /// <remarks>
    /// The Android engine exposes cache, cookie and history clearing, and a
    /// settable user agent. It has no private-browsing mode, so "private
    /// session" here means: send a do-not-track hint, and wipe cache, cookies
    /// and history when the session ends. That is weaker than a real
    /// incognito profile and the UI copy should say so rather than imply
    /// isolation we do not provide.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/Privacy Controller")]
    public class PrivacyController : MonoBehaviour
    {
        [SerializeField] private WebViewBackend m_backend;
        [SerializeField] private VrBrowserChrome m_chrome;

        [Header("Private session")]
        [Tooltip("Wipe cache, cookies and history when the app quits or the session is ended.")]
        [SerializeField] private bool m_clearOnExit;

        [Tooltip("Set navigator.doNotTrack on every page load. Advisory only - most sites ignore it.")]
        [SerializeField] private bool m_sendDoNotTrack = true;

        public UnityEvent onBrowsingDataCleared;

        public bool ClearOnExit
        {
            get => m_clearOnExit;
            set => m_clearOnExit = value;
        }

        private void OnEnable()
        {
            if (m_backend != null && m_sendDoNotTrack)
                m_backend.PageFinished += OnPageFinished;
        }

        private void OnDisable()
        {
            if (m_backend != null)
                m_backend.PageFinished -= OnPageFinished;
        }

        public void ClearAll() => Clear(cache: true, cookies: true, history: true);

        public void ClearCookies() => Clear(cache: false, cookies: true, history: false);

        public void ClearCache() => Clear(cache: true, cookies: false, history: false);

        public void ClearHistory() => Clear(cache: false, cookies: false, history: true);

        /// <summary>Wipe browsing data and return to the home page.</summary>
        public void EndPrivateSession()
        {
            ClearAll();
            m_chrome?.GoHome();
        }

        private void Clear(bool cache, bool cookies, bool history)
        {
            if (m_backend == null)
                return;

            m_backend.ClearBrowsingData(cache, cookies, history);

            if (history)
                m_chrome?.Session.Clear();

            onBrowsingDataCleared?.Invoke();
        }

        private void OnPageFinished(string url)
        {
            // navigator.doNotTrack is read-only in the page; redefining it is the
            // only way to set it from an injected script.
            m_backend?.EvaluateJavaScript(
                "try{Object.defineProperty(navigator,'doNotTrack',{get:function(){return '1';},configurable:true});}catch(e){}");
        }

        private void OnApplicationQuit()
        {
            if (m_clearOnExit)
                ClearAll();
        }

        private void OnApplicationPause(bool paused)
        {
            // Quest suspends rather than quits when the user takes the headset
            // off, so OnApplicationQuit alone would often never run.
            if (paused && m_clearOnExit)
                ClearAll();
        }
    }
}
