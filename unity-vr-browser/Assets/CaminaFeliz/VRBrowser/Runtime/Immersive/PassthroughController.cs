using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// How much of the real world the compositor lets through, 0..1.
    /// </summary>
    /// <remarks>
    /// Same reasoning as <see cref="IWebViewBackend"/>: one small boundary, so
    /// that the mixing logic and its UI can run in the Editor without a headset,
    /// and so that swapping Meta's passthrough for OpenXR's is one class.
    /// </remarks>
    public abstract class PassthroughController : MonoBehaviour
    {
        /// <summary>0 = pure virtual, 1 = pure reality. Values in between blend.</summary>
        public abstract float Opacity { get; set; }

        /// <summary>False when the device or the build cannot do passthrough at all.</summary>
        public abstract bool IsSupported { get; }

        public abstract void SetEnabled(bool enabled);
    }

    /// <summary>
    /// Stand-in for the Editor: there is no passthrough outside a headset, so it
    /// fades the camera background toward a neutral "room" colour instead.
    /// </summary>
    /// <remarks>
    /// It does not look like your room, and it is not supposed to. Its job is to
    /// prove the slider is wired to something and that the crossfade curve feels
    /// right, which are the two things that actually cost build cycles.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/Simulated Passthrough Controller")]
    public class SimulatedPassthroughController : PassthroughController
    {
        [SerializeField] private Camera m_camera;
        [SerializeField] private Color m_fakeRoomColor = new Color(0.62f, 0.60f, 0.56f);

        private float m_opacity;
        private bool m_enabled = true;

        public override bool IsSupported => true;

        public override float Opacity
        {
            get => m_opacity;
            set
            {
                m_opacity = Mathf.Clamp01(value);
                Apply();
            }
        }

        public override void SetEnabled(bool enabled)
        {
            m_enabled = enabled;
            Apply();
        }

        private void Awake()
        {
            if (m_camera == null)
                m_camera = Camera.main;

            Apply();
        }

        private void Apply()
        {
            if (m_camera == null)
                return;

            var amount = m_enabled ? m_opacity : 0f;

            // Above zero the camera has to stop drawing the skybox or the fake
            // room colour is never visible.
            m_camera.clearFlags = amount > 0.001f ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            m_camera.backgroundColor = Color.Lerp(Color.black, m_fakeRoomColor, amount);
        }
    }
}
