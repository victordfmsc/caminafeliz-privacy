using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Switches between the flat browser panel and full-surround 360 playback,
    /// and carries the video found on the page across that switch.
    /// </summary>
    [AddComponentMenu("CaminaFeliz/VR Browser/Immersive Mode Controller")]
    public class ImmersiveModeController : MonoBehaviour
    {
        [Header("Scene roots")]
        [Tooltip("Everything that makes up the flat browser: panel, chrome, keyboard.")]
        [SerializeField] private GameObject m_browserRoot;

        [Tooltip("The minimal bar shown while immersed: mix slider, pause, exit.")]
        [SerializeField] private GameObject m_immersiveRoot;

        [Header("Components")]
        [SerializeField] private Video360Player m_player;
        [SerializeField] private RealityMix m_mix;
        [SerializeField] private WebVideoDetector m_detector;
        [SerializeField] private WebViewBackend m_backend;

        [Header("Behaviour")]
        [Tooltip("Mix value applied on entering. 0 shows only the video; a little reality helps people keep their bearings.")]
        [SerializeField, Range(0f, 1f)] private float m_mixOnEnter = 0.15f;

        [Tooltip("Pause the page's own player on entering, or the same audio plays twice.")]
        [SerializeField] private bool m_pausePageVideoOnEnter = true;

        [Header("Events")]
        public UnityEvent<bool> onImmersiveChanged;

        /// <summary>Raised when the page turns out to have something watchable.</summary>
        public UnityEvent<bool> onPlayableVideoAvailable;

        private WebVideoSource m_candidate;

        public bool IsImmersive { get; private set; }

        public WebVideoSource Candidate => m_candidate;

        private void OnEnable()
        {
            if (m_detector != null)
            {
                m_detector.VideosFound += OnVideosFound;
                m_detector.VideosBlocked += OnVideosBlocked;
            }

            if (m_player != null)
                m_player.Failed += OnPlaybackFailed;

            SetImmersive(false);
        }

        private void OnDisable()
        {
            if (m_detector != null)
            {
                m_detector.VideosFound -= OnVideosFound;
                m_detector.VideosBlocked -= OnVideosBlocked;
            }

            if (m_player != null)
                m_player.Failed -= OnPlaybackFailed;
        }

        /// <summary>Play whatever the current page is showing. Bind to the "watch in 360" button.</summary>
        public void PlayDetected()
        {
            if (m_candidate != null)
                Enter(m_candidate.url);
        }

        /// <summary>Play an explicit URL, for a bookmark, a local file, or a test button.</summary>
        public void Enter(string url)
        {
            if (string.IsNullOrEmpty(url) || m_player == null)
                return;

            if (m_pausePageVideoOnEnter && m_backend != null && m_backend.IsReady)
                m_backend.EvaluateJavaScript(PausePageVideosScript);

            m_player.Play(url);

            if (m_mix != null)
                m_mix.Mix = m_mixOnEnter;

            SetImmersive(true);
        }

        public void Exit()
        {
            m_player?.Stop();
            SetImmersive(false);
        }

        public void Toggle()
        {
            if (IsImmersive)
                Exit();
            else
                PlayDetected();
        }

        private void SetImmersive(bool immersive)
        {
            IsImmersive = immersive;

            if (m_browserRoot != null)
                m_browserRoot.SetActive(!immersive);

            if (m_immersiveRoot != null)
                m_immersiveRoot.SetActive(immersive);

            onImmersiveChanged?.Invoke(immersive);
        }

        private void OnVideosFound(IReadOnlyList<WebVideoSource> videos)
        {
            m_candidate = Pick(videos);
            onPlayableVideoAvailable?.Invoke(m_candidate != null);
        }

        private void OnVideosBlocked(string page)
        {
            m_candidate = null;
            onPlayableVideoAvailable?.Invoke(false);
        }

        private void OnPlaybackFailed(string message)
        {
            // Falling back to the flat browser beats leaving the user inside a
            // black sphere with no way out but the system menu.
            Exit();
        }

        /// <summary>
        /// Prefer a 2:1 frame - that aspect ratio is what equirectangular
        /// footage looks like - and fall back to the largest video on the page.
        /// </summary>
        private static WebVideoSource Pick(IReadOnlyList<WebVideoSource> videos)
        {
            if (videos == null || videos.Count == 0)
                return null;

            WebVideoSource best = null;
            var bestScore = -1L;

            foreach (var video in videos)
            {
                if (video == null || string.IsNullOrEmpty(video.url))
                    continue;

                var pixels = (long)video.width * video.height;
                var score = video.LooksPanoramic ? pixels + 1_000_000_000L : pixels;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = video;
            }

            return best;
        }

        private const string PausePageVideosScript =
            "try{var v=document.querySelectorAll('video');for(var i=0;i<v.length;i++){v[i].pause();}}catch(e){}";
    }
}
