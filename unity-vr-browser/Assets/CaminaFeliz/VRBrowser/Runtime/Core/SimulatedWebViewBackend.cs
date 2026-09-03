using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// A backend that renders no web content at all - it draws a grid, a scroll
    /// offset and the last pointer position into a texture.
    /// </summary>
    /// <remarks>
    /// The Android engine renders nothing inside the Unity Editor, so without
    /// this every change to panel sizing, pointer mapping, scrolling or chrome
    /// layout costs a full APK build and deploy. With it, everything above
    /// <see cref="IWebViewBackend"/> is iterable at Editor frame rate, and the
    /// grid makes an off-by-one in the pointer mapping obvious: the dot lands
    /// where you pointed, or the mapping is wrong.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/Simulated Web View Backend")]
    public class SimulatedWebViewBackend : WebViewBackend
    {
        [Header("Simulation")]
        [SerializeField] private Color m_background = new Color(0.11f, 0.12f, 0.14f);
        [SerializeField] private Color m_grid = new Color(0.20f, 0.22f, 0.26f);
        [SerializeField] private Color m_cursor = new Color(1f, 0.45f, 0.2f);
        [SerializeField, Min(8)] private int m_gridSpacing = 64;

        private Texture2D m_texture;
        private Vector2Int m_viewSize = new Vector2Int(1280, 900);
        private Vector2Int m_texSize = new Vector2Int(1280, 900);
        private Vector2 m_pointer = new Vector2(-1f, -1f);
        private Vector2Int m_scroll;
        private string m_url = string.Empty;
        private bool m_dirty;

        public override bool IsReady => m_texture != null;
        public override Vector2Int ViewSize => m_viewSize;
        public override Texture Texture => m_texture;

        public override void Initialize(Vector2Int viewSize, Vector2Int texSize, int fps, string startUrl)
        {
            m_viewSize = viewSize;
            m_texSize = texSize;

            m_texture = new Texture2D(texSize.x, texSize.y, TextureFormat.RGBA32, mipChain: false)
            {
                name = "SimulatedWebView",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            m_dirty = true;
            Redraw();
            RaiseReadyChanged(true);

            if (!string.IsNullOrEmpty(startUrl))
                LoadUrl(startUrl);
        }

        public override void LoadUrl(string url)
        {
            m_url = url;
            m_scroll = Vector2Int.zero;
            m_dirty = true;

            Debug.Log($"[{nameof(SimulatedWebViewBackend)}] LoadUrl: {url}", this);

            RaisePageStarted(url);
            RaisePageFinished(url);
        }

        public override string GetUrl() => m_url;

        public override void GoBack() => Debug.Log($"[{nameof(SimulatedWebViewBackend)}] GoBack", this);

        public override void GoForward() => Debug.Log($"[{nameof(SimulatedWebViewBackend)}] GoForward", this);

        public override void Reload() => LoadUrl(m_url);

        public override void ScrollBy(int deltaX, int deltaY)
        {
            m_scroll += new Vector2Int(deltaX, deltaY);
            m_scroll.y = Mathf.Max(0, m_scroll.y);
            m_dirty = true;
        }

        public override void SendTouch(Vector2 normalized, WebTouchPhase phase)
        {
            m_pointer = phase == WebTouchPhase.Up ? new Vector2(-1f, -1f) : normalized;
            m_dirty = true;
        }

        public override void SendCharacter(char character) =>
            Debug.Log($"[{nameof(SimulatedWebViewBackend)}] char '{character}'", this);

        public override void SendKeyCode(int androidKeyCode) =>
            Debug.Log($"[{nameof(SimulatedWebViewBackend)}] keyCode {androidKeyCode}", this);

        public override void EvaluateJavaScript(string javascript) =>
            Debug.Log($"[{nameof(SimulatedWebViewBackend)}] JS: {javascript}", this);

        public override void ClearBrowsingData(bool cache, bool cookies, bool history) =>
            Debug.Log($"[{nameof(SimulatedWebViewBackend)}] ClearBrowsingData cache:{cache} cookies:{cookies} history:{history}", this);

        public override void Resize(Vector2Int texSize, Vector2Int viewSize)
        {
            m_viewSize = viewSize;

            if (m_texture == null || texSize == m_texSize)
                return;

            m_texSize = texSize;
            m_texture.Reinitialize(texSize.x, texSize.y);
            m_dirty = true;
        }

        private void LateUpdate()
        {
            if (m_dirty)
                Redraw();
        }

        private void OnDestroy()
        {
            if (m_texture != null)
                Destroy(m_texture);
        }

        private void Redraw()
        {
            m_dirty = false;

            var width = m_texture.width;
            var height = m_texture.height;
            var pixels = new Color32[width * height];

            var background = (Color32)m_background;
            var grid = (Color32)m_grid;

            // Grid offset by the scroll position, so scrolling is visibly working.
            var offsetX = Mod(m_scroll.x, m_gridSpacing);
            var offsetY = Mod(m_scroll.y, m_gridSpacing);

            for (var y = 0; y < height; y++)
            {
                var horizontal = Mod(y + offsetY, m_gridSpacing) == 0;
                for (var x = 0; x < width; x++)
                {
                    var vertical = Mod(x + offsetX, m_gridSpacing) == 0;
                    pixels[y * width + x] = horizontal || vertical ? grid : background;
                }
            }

            if (m_pointer.x >= 0f)
                DrawCursor(pixels, width, height);

            m_texture.SetPixels32(pixels);
            m_texture.Apply(updateMipmaps: false);
        }

        private void DrawCursor(Color32[] pixels, int width, int height)
        {
            // Normalized space is top-left origin; texture space is bottom-left.
            var centerX = Mathf.RoundToInt(m_pointer.x * (width - 1));
            var centerY = Mathf.RoundToInt((1f - m_pointer.y) * (height - 1));
            var radius = Mathf.Max(4, width / 128);
            var cursor = (Color32)m_cursor;

            for (var y = centerY - radius; y <= centerY + radius; y++)
            {
                if (y < 0 || y >= height)
                    continue;

                for (var x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || x >= width)
                        continue;

                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx + dy * dy <= radius * radius)
                        pixels[y * width + x] = cursor;
                }
            }
        }

        private static int Mod(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
