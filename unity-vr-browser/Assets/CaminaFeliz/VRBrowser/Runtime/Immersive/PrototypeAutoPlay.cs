using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Starts a video on Play so the prototype scene shows something without any
    /// browser, any headset, or a single click.
    /// </summary>
    /// <remarks>
    /// Deliberately trivial and deliberately separate: the real entry point is
    /// <see cref="ImmersiveModeController.PlayDetected"/> driven by the page the
    /// user is on. This exists so the mix slider can be judged on its own,
    /// before any of that is wired up.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/Prototype Auto Play")]
    public class PrototypeAutoPlay : MonoBehaviour
    {
        [SerializeField] private Video360Player m_player;

        [Tooltip("Any URL Unity's VideoPlayer can open: a direct .mp4, or a local file path.")]
        [SerializeField] private string m_url;

        [SerializeField] private bool m_playOnStart = true;

        private void Start()
        {
            if (m_playOnStart)
                Play();
        }

        public void Play()
        {
            if (m_player == null || string.IsNullOrEmpty(m_url))
                return;

            m_player.Play(m_url);
        }

        public void SetUrl(string url) => m_url = url;
    }
}
