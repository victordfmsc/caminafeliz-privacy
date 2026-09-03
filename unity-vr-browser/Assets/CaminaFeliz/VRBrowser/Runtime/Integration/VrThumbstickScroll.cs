using UnityEngine;
using UnityEngine.InputSystem;

namespace CaminaFeliz.VRBrowser.Integration
{
    /// <summary>
    /// Scrolls the page from a controller thumbstick.
    /// </summary>
    /// <remarks>
    /// Dragging the page with the trigger is the only scrolling the reference VR
    /// sample offers, and it is genuinely tiring: reading an article means
    /// repeatedly pinching and hauling a ray across a metre-wide panel. A
    /// thumbstick maps far better, but a raw axis makes the page fly, so the
    /// input is curved (deadzone, then a square response) and expressed in CSS
    /// pixels per second rather than per frame - otherwise scroll speed silently
    /// tracks frame rate, which on a headset varies with thermals.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/VR Thumbstick Scroll")]
    public class VrThumbstickScroll : MonoBehaviour
    {
        [SerializeField] private WebViewBackend m_backend;

        [Tooltip("A Vector2 action, e.g. XRI RightHand Locomotion/Turn or a custom scroll action.")]
        [SerializeField] private InputActionProperty m_scrollAction;

        [Header("Response")]
        [SerializeField, Range(0f, 0.9f)] private float m_deadZone = 0.2f;
        [SerializeField, Min(1f)] private float m_pixelsPerSecond = 1600f;
        [SerializeField] private bool m_invertVertical;
        [SerializeField] private bool m_horizontalEnabled;

        private float m_residualX;
        private float m_residualY;

        private void OnEnable()
        {
            // A directly-authored action is ours to enable; one referencing an
            // asset is owned by the input action manager, so we leave it alone.
            if (m_scrollAction.reference == null)
                m_scrollAction.action?.Enable();
        }

        private void OnDisable()
        {
            if (m_scrollAction.reference == null)
                m_scrollAction.action?.Disable();
        }

        private void Update()
        {
            if (m_backend == null || !m_backend.IsReady)
                return;

            var action = m_scrollAction.action;
            if (action == null)
                return;

            var raw = action.ReadValue<Vector2>();
            var x = m_horizontalEnabled ? Curve(raw.x) : 0f;
            var y = Curve(raw.y);

            if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
            {
                m_residualX = 0f;
                m_residualY = 0f;
                return;
            }

            if (!m_invertVertical)
                y = -y;   // stick forward scrolls the page content up, as on a touchpad

            // ScrollBy takes integers, so sub-pixel movement is carried over
            // instead of being truncated away - without this, slow scrolling
            // simply does nothing.
            m_residualX += x * m_pixelsPerSecond * Time.deltaTime;
            m_residualY += y * m_pixelsPerSecond * Time.deltaTime;

            var stepX = (int)m_residualX;
            var stepY = (int)m_residualY;

            if (stepX == 0 && stepY == 0)
                return;

            m_residualX -= stepX;
            m_residualY -= stepY;

            m_backend.ScrollBy(stepX, stepY);
        }

        /// <summary>Deadzone, then a squared response so small pushes stay precise.</summary>
        private float Curve(float value)
        {
            var magnitude = Mathf.Abs(value);
            if (magnitude <= m_deadZone)
                return 0f;

            var normalized = (magnitude - m_deadZone) / (1f - m_deadZone);
            return Mathf.Sign(value) * normalized * normalized;
        }
    }
}
