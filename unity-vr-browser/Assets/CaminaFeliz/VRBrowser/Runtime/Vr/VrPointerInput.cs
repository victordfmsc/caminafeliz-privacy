using UnityEngine;
using UnityEngine.EventSystems;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Translates uGUI pointer events into web touch events.
    /// </summary>
    /// <remarks>
    /// This works with XR ray interactors for free: the XR Interaction Toolkit's
    /// input module feeds a world-space canvas the same PointerEventData that a
    /// mouse would produce, so a controller ray, a hand-tracked poke and an
    /// Editor mouse all arrive here identically. The panel needs a
    /// TrackedDeviceGraphicRaycaster on its canvas for the ray to reach it.
    ///
    /// The coordinate maths mirrors TLabWebView's own BaseInputListener - it is
    /// the part that is easy to get subtly wrong - but this version talks to
    /// <see cref="IWebViewBackend"/> instead of the Android plugin, so it also
    /// runs in the Editor against the simulated backend.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/VR Pointer Input")]
    [RequireComponent(typeof(RectTransform))]
    public class VrPointerInput : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerExitHandler
    {
        [SerializeField] private WebViewBackend m_backend;

        [Tooltip("Ignore drags shorter than this fraction of the panel; keeps a shaky ray from turning taps into drags.")]
        [SerializeField, Range(0f, 0.05f)] private float m_dragDeadZone = 0.002f;

        private RectTransform m_rectTransform;
        private int? m_activePointerId;
        private Vector2 m_lastSent;

        public WebViewBackend Backend
        {
            get => m_backend;
            set => m_backend = value;
        }

        private void Awake() => m_rectTransform = (RectTransform)transform;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_activePointerId.HasValue || !TryGetNormalized(eventData, out var normalized))
                return;

            m_activePointerId = eventData.pointerId;
            m_lastSent = normalized;
            Send(normalized, WebTouchPhase.Down);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsActive(eventData) || !TryGetNormalized(eventData, out var normalized))
                return;

            if ((normalized - m_lastSent).sqrMagnitude < m_dragDeadZone * m_dragDeadZone)
                return;

            m_lastSent = normalized;
            Send(normalized, WebTouchPhase.Move);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsActive(eventData))
                return;

            // Release at the last valid position even if the ray has drifted off
            // the panel, otherwise the page keeps a button visually pressed.
            var normalized = TryGetNormalized(eventData, out var current) ? current : m_lastSent;

            Send(normalized, WebTouchPhase.Up);
            m_activePointerId = null;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsActive(eventData))
                return;

            Send(m_lastSent, WebTouchPhase.Up);
            m_activePointerId = null;
        }

        private void OnDisable()
        {
            if (m_activePointerId.HasValue)
            {
                Send(m_lastSent, WebTouchPhase.Up);
                m_activePointerId = null;
            }
        }

        private bool IsActive(PointerEventData eventData) =>
            m_activePointerId.HasValue && m_activePointerId.Value == eventData.pointerId;

        private void Send(Vector2 normalized, WebTouchPhase phase)
        {
            if (m_backend != null && m_backend.IsReady)
                m_backend.SendTouch(normalized, phase);
        }

        /// <summary>
        /// Converts a pointer event to panel space with (0,0) at the top-left,
        /// which is what web engines expect. Returns false when the point falls
        /// outside the panel.
        /// </summary>
        private bool TryGetNormalized(PointerEventData eventData, out Vector2 normalized)
        {
            normalized = Vector2.zero;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_rectTransform, eventData.position, eventData.pressEventCamera, out var local))
                return false;

            var rect = m_rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return false;

            var x = local.x / rect.width + m_rectTransform.pivot.x;
            var y = 1f - (local.y / rect.height + m_rectTransform.pivot.y);

            if (x < 0f || x > 1f || y < 0f || y > 1f)
                return false;

            normalized = new Vector2(x, y);
            return true;
        }
    }
}
