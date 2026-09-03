using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Navigation state that the underlying engine does not expose.
    /// </summary>
    /// <remarks>
    /// TLabWebView gives us GoBack()/GoForward() but no canGoBack/canGoForward,
    /// so the chrome has no way to grey out its buttons. We mirror the history
    /// depth here from page-finished events. It is an approximation - in-page
    /// fragment navigation and history.pushState do not always surface - so the
    /// buttons are advisory, never a gate on calling the engine.
    /// </remarks>
    [Serializable]
    public class BrowserSession
    {
        [SerializeField] private string m_homeUrl = "https://duckduckgo.com/";
        [SerializeField] private string m_searchTemplate = UrlUtility.DefaultSearchTemplate;
        [SerializeField, Min(1)] private int m_maxHistory = 100;

        private readonly List<string> m_history = new List<string>();
        private int m_cursor = -1;

        /// <summary>Set while we are replaying our own back/forward, so the echo is not recorded.</summary>
        private bool m_suppressRecord;

        public event Action StateChanged;

        public string HomeUrl
        {
            get => m_homeUrl;
            set => m_homeUrl = value;
        }

        public string SearchTemplate
        {
            get => string.IsNullOrEmpty(m_searchTemplate) ? UrlUtility.DefaultSearchTemplate : m_searchTemplate;
            set => m_searchTemplate = value;
        }

        public IReadOnlyList<string> History => m_history;

        public string CurrentUrl => m_cursor >= 0 && m_cursor < m_history.Count ? m_history[m_cursor] : string.Empty;

        public bool CanGoBack => m_cursor > 0;

        public bool CanGoForward => m_cursor >= 0 && m_cursor < m_history.Count - 1;

        /// <summary>Resolve address-bar text into a URL this session would load.</summary>
        public string Resolve(string rawInput) => UrlUtility.Resolve(rawInput, SearchTemplate);

        /// <summary>Record a committed navigation reported by the engine.</summary>
        public void RecordNavigation(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            if (m_suppressRecord)
            {
                m_suppressRecord = false;
                SyncCursorTo(url);
                StateChanged?.Invoke();
                return;
            }

            if (CurrentUrl == url)
                return;

            // A new navigation truncates anything ahead of the cursor.
            if (m_cursor < m_history.Count - 1)
                m_history.RemoveRange(m_cursor + 1, m_history.Count - m_cursor - 1);

            m_history.Add(url);

            if (m_history.Count > m_maxHistory)
                m_history.RemoveAt(0);

            m_cursor = m_history.Count - 1;
            StateChanged?.Invoke();
        }

        /// <summary>Call immediately before asking the engine to go back or forward.</summary>
        public void NotifyHistoryTraversal(int direction)
        {
            m_cursor = Mathf.Clamp(m_cursor + direction, 0, Mathf.Max(0, m_history.Count - 1));
            m_suppressRecord = true;
            StateChanged?.Invoke();
        }

        public void Clear()
        {
            m_history.Clear();
            m_cursor = -1;
            m_suppressRecord = false;
            StateChanged?.Invoke();
        }

        /// <summary>
        /// After a traversal the engine tells us where it actually landed, which
        /// may not be where we predicted (redirects, skipped entries).
        /// </summary>
        private void SyncCursorTo(string url)
        {
            var index = m_history.LastIndexOf(url);
            if (index >= 0)
            {
                m_cursor = index;
                return;
            }

            m_history.Add(url);
            m_cursor = m_history.Count - 1;
        }
    }
}
