using System;
using UnityEngine;
using UnityEngine.Video;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>Frame layout of a panoramic video.</summary>
    public enum PanoramaLayout
    {
        /// <summary>One image for both eyes, 360x180 equirectangular.</summary>
        Mono360 = 0,

        /// <summary>Left eye on top, right eye below.</summary>
        StereoOverUnder360 = 1,

        /// <summary>Left eye left, right eye right.</summary>
        StereoSideBySide360 = 2,

        /// <summary>Half sphere, one image for both eyes.</summary>
        Mono180 = 3,

        /// <summary>Half sphere, left eye left, right eye right. The common VR180 layout.</summary>
        StereoSideBySide180 = 4,
    }

    /// <summary>
    /// Plays an equirectangular video into the skybox.
    /// </summary>
    /// <remarks>
    /// The skybox is used rather than an inverted sphere on purpose. Unity's
    /// stock <c>Skybox/Panoramic</c> shader already handles 360 vs 180, mono vs
    /// over-under vs side-by-side, and per-eye selection, so the prototype needs
    /// no custom shader at all. The passthrough blend happens in the headset's
    /// compositor rather than in this material, which is both cheaper and
    /// sharper than blending it ourselves - see <see cref="RealityMix"/>.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/360 Video Player")]
    [RequireComponent(typeof(VideoPlayer))]
    public class Video360Player : MonoBehaviour
    {
        [Header("Output")]
        [Tooltip("Material using the Skybox/Panoramic shader. Left empty, one is created at runtime.")]
        [SerializeField] private Material m_skyboxMaterial;

        [SerializeField] private Vector2Int m_renderTextureSize = new Vector2Int(4096, 2048);

        [Header("Playback")]
        [SerializeField] private PanoramaLayout m_layout = PanoramaLayout.Mono360;
        [SerializeField] private bool m_loop = true;
        [SerializeField, Range(0f, 1f)] private float m_volume = 1f;

        [Tooltip("Guess the layout from the file name (_TB, _SBS, 180...) instead of trusting the field above.")]
        [SerializeField] private bool m_detectLayoutFromUrl = true;

        private VideoPlayer m_videoPlayer;
        private RenderTexture m_renderTexture;
        private Material m_runtimeMaterial;
        private Material m_previousSkybox;
        private bool m_skyboxInstalled;

        public event Action<string> Started;
        public event Action Finished;
        public event Action<string> Failed;

        public bool IsPlaying => m_videoPlayer != null && m_videoPlayer.isPlaying;
        public string Url => m_videoPlayer != null ? m_videoPlayer.url : string.Empty;
        public double Duration => m_videoPlayer != null ? m_videoPlayer.length : 0d;

        public double Time
        {
            get => m_videoPlayer != null ? m_videoPlayer.time : 0d;
            set { if (m_videoPlayer != null) m_videoPlayer.time = value; }
        }

        public PanoramaLayout Layout
        {
            get => m_layout;
            set
            {
                m_layout = value;
                ApplyLayout();
            }
        }

        private void Awake()
        {
            m_videoPlayer = GetComponent<VideoPlayer>();

            m_renderTexture = new RenderTexture(m_renderTextureSize.x, m_renderTextureSize.y, 0)
            {
                name = "Video360",
                wrapMode = TextureWrapMode.Repeat,
            };

            m_runtimeMaterial = m_skyboxMaterial != null
                ? new Material(m_skyboxMaterial)
                : new Material(Shader.Find("Skybox/Panoramic"));

            m_runtimeMaterial.name = "Video360 (runtime)";
            m_runtimeMaterial.SetTexture(MainTex, m_renderTexture);

            m_videoPlayer.playOnAwake = false;
            m_videoPlayer.isLooping = m_loop;
            m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            m_videoPlayer.targetTexture = m_renderTexture;
            m_videoPlayer.source = VideoSource.Url;

            // Direct output avoids wiring an AudioSource and is fine for a
            // prototype; spatialised audio would need AudioOutputMode.AudioSource.
            m_videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            m_videoPlayer.errorReceived += OnErrorReceived;
            m_videoPlayer.loopPointReached += OnLoopPointReached;
            m_videoPlayer.prepareCompleted += OnPrepareCompleted;

            ApplyLayout();
        }

        private void OnDestroy()
        {
            if (m_videoPlayer != null)
            {
                m_videoPlayer.errorReceived -= OnErrorReceived;
                m_videoPlayer.loopPointReached -= OnLoopPointReached;
                m_videoPlayer.prepareCompleted -= OnPrepareCompleted;
            }

            RestoreSkybox();

            if (m_renderTexture != null)
                m_renderTexture.Release();
        }

        /// <summary>Load and play a video URL. Local files work as file:// or a bare path.</summary>
        public void Play(string url)
        {
            if (string.IsNullOrEmpty(url) || m_videoPlayer == null)
                return;

            if (m_detectLayoutFromUrl)
                Layout = GuessLayout(url, m_layout);

            InstallSkybox();

            m_videoPlayer.url = url;
            m_videoPlayer.SetDirectAudioVolume(0, m_volume);
            m_videoPlayer.Prepare();
        }

        public void Pause() => m_videoPlayer?.Pause();

        public void Resume() => m_videoPlayer?.Play();

        public void TogglePause()
        {
            if (m_videoPlayer == null)
                return;

            if (m_videoPlayer.isPlaying)
                m_videoPlayer.Pause();
            else
                m_videoPlayer.Play();
        }

        public void Stop()
        {
            m_videoPlayer?.Stop();
            RestoreSkybox();
        }

        public void SetVolume(float volume)
        {
            m_volume = Mathf.Clamp01(volume);
            m_videoPlayer?.SetDirectAudioVolume(0, m_volume);
        }

        public void Seek(float normalized)
        {
            if (m_videoPlayer == null || m_videoPlayer.length <= 0d)
                return;

            m_videoPlayer.time = Mathf.Clamp01(normalized) * m_videoPlayer.length;
        }

        /// <summary>
        /// Dim the video without touching the passthrough layer. Driven by
        /// <see cref="RealityMix"/> so that fading in reality fades out video
        /// rather than stacking both at full brightness.
        /// </summary>
        public void SetExposure(float exposure)
        {
            if (m_runtimeMaterial != null)
                m_runtimeMaterial.SetFloat(Exposure, Mathf.Max(0f, exposure));
        }

        /// <summary>Yaw the panorama, to re-centre the video on where the user is facing.</summary>
        public void SetRotation(float degrees)
        {
            if (m_runtimeMaterial != null)
                m_runtimeMaterial.SetFloat(Rotation, Mathf.Repeat(degrees, 360f));
        }

        private void OnPrepareCompleted(VideoPlayer source)
        {
            source.Play();
            Started?.Invoke(source.url);
        }

        private void OnLoopPointReached(VideoPlayer source)
        {
            if (!source.isLooping)
                Finished?.Invoke();
        }

        private void OnErrorReceived(VideoPlayer source, string message)
        {
            Debug.LogError($"[{nameof(Video360Player)}] {message}", this);
            Failed?.Invoke(message);
        }

        private void InstallSkybox()
        {
            if (m_skyboxInstalled)
                return;

            m_previousSkybox = RenderSettings.skybox;
            RenderSettings.skybox = m_runtimeMaterial;
            m_skyboxInstalled = true;
        }

        private void RestoreSkybox()
        {
            if (!m_skyboxInstalled)
                return;

            RenderSettings.skybox = m_previousSkybox;
            m_skyboxInstalled = false;
        }

        /// <summary>
        /// Maps our layout enum onto the three properties the stock panoramic
        /// skybox shader actually reads.
        /// </summary>
        private void ApplyLayout()
        {
            if (m_runtimeMaterial == null)
                return;

            var is180 = m_layout == PanoramaLayout.Mono180 || m_layout == PanoramaLayout.StereoSideBySide180;

            var stereoLayout = m_layout switch
            {
                PanoramaLayout.StereoSideBySide360 => 1,
                PanoramaLayout.StereoSideBySide180 => 1,
                PanoramaLayout.StereoOverUnder360 => 2,
                _ => 0,
            };

            m_runtimeMaterial.SetFloat(Mapping, 1f);              // latitude/longitude
            m_runtimeMaterial.SetFloat(ImageType, is180 ? 1f : 0f);
            m_runtimeMaterial.SetFloat(LayoutProperty, stereoLayout);
        }

        /// <summary>
        /// Producers encode the layout in the file name far more reliably than
        /// in the container metadata, and reading it wrong is instantly obvious
        /// in a headset - double images or a squashed world.
        /// </summary>
        public static PanoramaLayout GuessLayout(string url, PanoramaLayout fallback)
        {
            if (string.IsNullOrEmpty(url))
                return fallback;

            var name = url.ToLowerInvariant();
            var is180 = name.Contains("180") || name.Contains("vr180");

            if (name.Contains("_tb") || name.Contains("-tb") || name.Contains("overunder") || name.Contains("_ou"))
                return PanoramaLayout.StereoOverUnder360;

            if (name.Contains("_sbs") || name.Contains("-sbs") || name.Contains("sidebyside") || name.Contains("_lr"))
                return is180 ? PanoramaLayout.StereoSideBySide180 : PanoramaLayout.StereoSideBySide360;

            if (is180)
                return PanoramaLayout.Mono180;

            if (name.Contains("360") || name.Contains("equirect"))
                return PanoramaLayout.Mono360;

            return fallback;
        }

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Mapping = Shader.PropertyToID("_Mapping");
        private static readonly int ImageType = Shader.PropertyToID("_ImageType");
        private static readonly int LayoutProperty = Shader.PropertyToID("_Layout");
        private static readonly int Exposure = Shader.PropertyToID("_Exposure");
        private static readonly int Rotation = Shader.PropertyToID("_Rotation");
    }
}
