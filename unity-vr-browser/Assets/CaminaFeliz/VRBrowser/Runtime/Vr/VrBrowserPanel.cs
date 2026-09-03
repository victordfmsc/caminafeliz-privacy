using UnityEngine;
using UnityEngine.UI;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Owns the physical panel: how many CSS pixels the page gets, how many
    /// texture pixels we spend on it, and how big it is in metres.
    /// </summary>
    /// <remarks>
    /// These three numbers are the whole readability/performance trade-off of a
    /// VR browser and they are usually tangled together in sample projects.
    /// Keeping them explicit lets us reason about angular resolution: a 1280 px
    /// wide texture on a 1.6 m panel viewed from 1.2 m is roughly 21 px per
    /// degree, which is above what a Quest 3 can resolve at that distance and
    /// therefore not worth increasing.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/VR Browser Panel")]
    [RequireComponent(typeof(RectTransform))]
    public class VrBrowserPanel : MonoBehaviour
    {
        [Header("Engine")]
        [SerializeField] private WebViewBackend m_backend;
        [SerializeField] private RawImage m_surface;

        [Header("Page")]
        [Tooltip("Logical page size in CSS pixels. Drives layout: 1280 wide gets desktop layouts, 480 gets mobile ones.")]
        [SerializeField] private Vector2Int m_viewSize = new Vector2Int(1280, 900);

        [Tooltip("Texture the engine renders into. Equal to view size is 1:1; halving it halves the fill cost.")]
        [SerializeField] private Vector2Int m_textureSize = new Vector2Int(1280, 900);

        [Tooltip("Engine refresh rate. 30 is enough for reading and leaves GPU budget for the compositor.")]
        [SerializeField, Range(1, 90)] private int m_fps = 30;

        [SerializeField] private string m_startUrl = "https://duckduckgo.com/";

        [Header("Physical size")]
        [Tooltip("Panel width in metres at scale 1. Height follows the page aspect ratio.")]
        [SerializeField, Min(0.1f)] private float m_widthMeters = 1.6f;

        [SerializeField, Range(0.25f, 3f)] private float m_userScale = 1f;

        [Header("Startup")]
        [SerializeField] private bool m_initializeOnStart = true;

        private RectTransform m_rectTransform;
        private bool m_applied;

        public WebViewBackend Backend => m_backend;
        public Vector2Int ViewSize => m_viewSize;
        public string StartUrl => m_startUrl;

        /// <summary>Panel width in metres, including the user's scale adjustment.</summary>
        public float WidthMeters => m_widthMeters * m_userScale;

        public float AspectRatio => m_viewSize.y <= 0 ? 1f : (float)m_viewSize.x / m_viewSize.y;

        private void Awake()
        {
            m_rectTransform = (RectTransform)transform;
            ApplyLayout();
        }

        private void OnEnable()
        {
            if (m_backend != null)
                m_backend.ReadyChanged += OnBackendReadyChanged;
        }

        private void OnDisable()
        {
            if (m_backend != null)
                m_backend.ReadyChanged -= OnBackendReadyChanged;
        }

        private void Start()
        {
            if (m_initializeOnStart)
                Initialize();
        }

        public void Initialize()
        {
            if (m_backend == null)
            {
                Debug.LogError($"[{nameof(VrBrowserPanel)}] no backend assigned.", this);
                return;
            }

            ApplyLayout();
            m_backend.Initialize(m_viewSize, m_textureSize, m_fps, m_startUrl);
            TryBindTexture();
        }

        /// <summary>Rescale the panel around its own centre, e.g. from a thumbstick or a slider.</summary>
        public void SetUserScale(float scale)
        {
            m_userScale = Mathf.Clamp(scale, 0.25f, 3f);
            ApplyLayout();
        }

        public void NudgeUserScale(float delta) => SetUserScale(m_userScale + delta);

        /// <summary>
        /// Change the logical page size at runtime - this is what "desktop site"
        /// vs "mobile site" actually means to the engine.
        /// </summary>
        public void SetViewSize(Vector2Int viewSize, Vector2Int textureSize)
        {
            m_viewSize = viewSize;
            m_textureSize = textureSize;
            ApplyLayout();

            if (m_backend != null && m_backend.IsReady)
            {
                m_backend.Resize(textureSize, viewSize);
                TryBindTexture();
            }
        }

        private void OnBackendReadyChanged(bool ready)
        {
            if (ready)
                TryBindTexture();
        }

        private void TryBindTexture()
        {
            if (m_surface == null || m_backend == null)
                return;

            var texture = m_backend.Texture;
            if (texture != null)
                m_surface.texture = texture;
        }

        /// <summary>
        /// Size the RectTransform in page pixels and scale it down so that its
        /// world width is <see cref="WidthMeters"/>, regardless of what scale the
        /// parent canvas happens to be at.
        /// </summary>
        private void ApplyLayout()
        {
            if (m_rectTransform == null)
                m_rectTransform = (RectTransform)transform;

            if (m_viewSize.x <= 0 || m_viewSize.y <= 0)
                return;

            m_rectTransform.sizeDelta = new Vector2(m_viewSize.x, m_viewSize.y);

            var parent = m_rectTransform.parent;
            var parentScale = parent != null ? parent.lossyScale.x : 1f;
            if (Mathf.Approximately(parentScale, 0f))
                parentScale = 1f;

            var scale = WidthMeters / (m_viewSize.x * parentScale);
            m_rectTransform.localScale = new Vector3(scale, scale, scale);
            m_applied = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_textureSize.x = Mathf.Max(16, m_textureSize.x);
            m_textureSize.y = Mathf.Max(16, m_textureSize.y);
            m_viewSize.x = Mathf.Max(16, m_viewSize.x);
            m_viewSize.y = Mathf.Max(16, m_viewSize.y);

            if (m_applied || !Application.isPlaying)
                ApplyLayout();
        }
#endif
    }
}
