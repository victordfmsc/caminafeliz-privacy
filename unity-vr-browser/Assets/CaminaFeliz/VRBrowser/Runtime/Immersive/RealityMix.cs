using UnityEngine;
using UnityEngine.Events;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// The slider: one value from "only the video" to "only the room".
    /// </summary>
    /// <remarks>
    /// The blend itself is done by the headset's compositor via
    /// <see cref="PassthroughController.Opacity"/>, not by a shader in our
    /// scene. That matters for a passthrough app: the compositor blends at the
    /// display's own rate after reprojection, so reality stays locked to the
    /// user's head even when our frame rate dips, and it never pays for a
    /// full-screen transparent draw.
    ///
    /// What this component adds on top is the second half of a crossfade.
    /// Raising passthrough alone stacks a bright room on top of a bright video
    /// and the middle of the slider turns into unreadable soup, so the video is
    /// dimmed by the same amount it is being covered.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/Reality Mix")]
    public class RealityMix : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PassthroughController m_passthrough;
        [SerializeField] private Video360Player m_videoPlayer;

        [Header("Mix")]
        [Tooltip("0 = only the video, 1 = only the real room.")]
        [SerializeField, Range(0f, 1f)] private float m_mix;

        [Tooltip("How much the video dims as reality comes in. 1 is a true crossfade; lower keeps the video readable through the room.")]
        [SerializeField, Range(0f, 1f)] private float m_videoDimming = 1f;

        [Tooltip("Seconds to ease to a new value. Instant jumps between reality and a bright video are unpleasant to look at.")]
        [SerializeField, Range(0f, 1f)] private float m_smoothing = 0.15f;

        [Header("Events")]
        public UnityEvent<float> onMixChanged;

        private float m_displayedMix = -1f;
        private float m_velocity;

        /// <summary>0 = only the video, 1 = only the real room.</summary>
        public float Mix
        {
            get => m_mix;
            set
            {
                m_mix = Mathf.Clamp01(value);
                onMixChanged?.Invoke(m_mix);
            }
        }

        /// <summary>Bind this to a uGUI Slider's onValueChanged.</summary>
        public void SetMix(float mix) => Mix = mix;

        public void ShowOnlyVideo() => Mix = 0f;

        public void ShowOnlyReality() => Mix = 1f;

        public void ToggleExtremes() => Mix = m_mix > 0.5f ? 0f : 1f;

        /// <summary>Step the slider, for a controller button or a thumbstick.</summary>
        public void NudgeMix(float delta) => Mix = m_mix + delta;

        private void Start() => Apply(m_mix, force: true);

        private void Update()
        {
            if (Mathf.Approximately(m_displayedMix, m_mix))
                return;

            var value = m_smoothing <= 0f
                ? m_mix
                : Mathf.SmoothDamp(m_displayedMix, m_mix, ref m_velocity, m_smoothing);

            if (Mathf.Abs(value - m_mix) < 0.001f)
                value = m_mix;

            Apply(value, force: false);
        }

        private void Apply(float value, bool force)
        {
            if (!force && Mathf.Approximately(m_displayedMix, value))
                return;

            m_displayedMix = value;

            if (m_passthrough != null)
            {
                m_passthrough.SetEnabled(value > 0.001f);
                m_passthrough.Opacity = value;
            }

            if (m_videoPlayer != null)
                m_videoPlayer.SetExposure(1f - value * m_videoDimming);
        }
    }
}
