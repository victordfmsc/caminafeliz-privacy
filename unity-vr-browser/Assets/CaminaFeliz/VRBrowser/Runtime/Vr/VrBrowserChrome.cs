using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// The browser's own UI: address bar, navigation buttons, loading state.
    /// </summary>
    /// <remarks>
    /// Everything here goes through <see cref="BrowserSession"/> and
    /// <see cref="IWebViewBackend"/>, never through the Android plugin, so the
    /// whole chrome is clickable in the Editor against the simulated backend.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/VR Browser Chrome")]
    public class VrBrowserChrome : MonoBehaviour
    {
        [Header("Engine")]
        [SerializeField] private WebViewBackend m_backend;
        [SerializeField] private VrBrowserPanel m_panel;

        [Header("Address bar")]
        [SerializeField] private TMP_InputField m_addressField;
        [SerializeField] private TMP_Text m_titleLabel;
        [SerializeField] private GameObject m_secureIndicator;
        [SerializeField] private GameObject m_loadingIndicator;

        [Header("Navigation")]
        [SerializeField] private Button m_backButton;
        [SerializeField] private Button m_forwardButton;
        [SerializeField] private Button m_reloadButton;
        [SerializeField] private Button m_homeButton;

        [Header("Session")]
        [SerializeField] private BrowserSession m_session = new BrowserSession();

        [Tooltip("Collapse the address bar to the host name while not being edited.")]
        [SerializeField] private bool m_compactAddress = true;

        private bool m_editing;

        public BrowserSession Session => m_session;

        private void Awake()
        {
            if (m_panel != null && m_backend == null)
                m_backend = m_panel.Backend;
        }

        private void OnEnable()
        {
            if (m_backend != null)
            {
                m_backend.PageStarted += OnPageStarted;
                m_backend.PageFinished += OnPageFinished;
            }

            m_session.StateChanged += RefreshNavigationButtons;

            if (m_addressField != null)
            {
                m_addressField.onSelect.AddListener(OnAddressSelected);
                m_addressField.onDeselect.AddListener(OnAddressDeselected);
                m_addressField.onSubmit.AddListener(Navigate);
            }

            Bind(m_backButton, GoBack);
            Bind(m_forwardButton, GoForward);
            Bind(m_reloadButton, Reload);
            Bind(m_homeButton, GoHome);

            RefreshNavigationButtons();
            SetLoading(false);
        }

        private void OnDisable()
        {
            if (m_backend != null)
            {
                m_backend.PageStarted -= OnPageStarted;
                m_backend.PageFinished -= OnPageFinished;
            }

            m_session.StateChanged -= RefreshNavigationButtons;

            if (m_addressField != null)
            {
                m_addressField.onSelect.RemoveListener(OnAddressSelected);
                m_addressField.onDeselect.RemoveListener(OnAddressDeselected);
                m_addressField.onSubmit.RemoveListener(Navigate);
            }

            Unbind(m_backButton, GoBack);
            Unbind(m_forwardButton, GoForward);
            Unbind(m_reloadButton, Reload);
            Unbind(m_homeButton, GoHome);
        }

        /// <summary>Load whatever is currently typed in the address bar.</summary>
        public void NavigateToAddressBar() => Navigate(m_addressField != null ? m_addressField.text : string.Empty);

        /// <summary>Resolve raw text (URL or search terms) and load it.</summary>
        public void Navigate(string rawInput)
        {
            if (m_backend == null)
                return;

            var url = m_session.Resolve(rawInput);
            if (string.IsNullOrEmpty(url))
                return;

            m_editing = false;
            m_backend.LoadUrl(url);
        }

        public void GoBack()
        {
            if (m_backend == null)
                return;

            m_session.NotifyHistoryTraversal(-1);
            m_backend.GoBack();
        }

        public void GoForward()
        {
            if (m_backend == null)
                return;

            m_session.NotifyHistoryTraversal(1);
            m_backend.GoForward();
        }

        public void Reload() => m_backend?.Reload();

        public void GoHome() => Navigate(m_session.HomeUrl);

        /// <summary>Swap between a desktop-width and a phone-width page layout.</summary>
        public void SetDesktopLayout(bool desktop)
        {
            if (m_panel == null)
                return;

            m_panel.SetViewSize(
                desktop ? new Vector2Int(1280, 900) : new Vector2Int(480, 900),
                desktop ? new Vector2Int(1280, 900) : new Vector2Int(720, 1350));
        }

        private void OnPageStarted(string url)
        {
            SetLoading(true);
            ShowUrl(url);
        }

        private void OnPageFinished(string url)
        {
            SetLoading(false);
            m_session.RecordNavigation(url);
            ShowUrl(url);
        }

        private void OnAddressSelected(string _)
        {
            m_editing = true;

            // Editing starts from the full URL, not the collapsed host name.
            if (m_addressField != null && m_backend != null)
                m_addressField.text = m_backend.GetUrl();
        }

        private void OnAddressDeselected(string _)
        {
            m_editing = false;
            ShowUrl(m_backend != null ? m_backend.GetUrl() : string.Empty);
        }

        private void ShowUrl(string url)
        {
            if (m_secureIndicator != null)
                m_secureIndicator.SetActive(UrlUtility.IsSecure(url));

            if (m_titleLabel != null)
                m_titleLabel.text = UrlUtility.DisplayName(url);

            if (m_addressField == null || m_editing)
                return;

            m_addressField.SetTextWithoutNotify(
                m_compactAddress ? UrlUtility.DisplayName(url) : url);
        }

        private void SetLoading(bool loading)
        {
            if (m_loadingIndicator != null)
                m_loadingIndicator.SetActive(loading);
        }

        private void RefreshNavigationButtons()
        {
            if (m_backButton != null)
                m_backButton.interactable = m_session.CanGoBack;

            if (m_forwardButton != null)
                m_forwardButton.interactable = m_session.CanGoForward;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }
    }
}
