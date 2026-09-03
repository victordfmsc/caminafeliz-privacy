using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    [Serializable]
    public class WebVideoSource
    {
        public string url;
        public int width;
        public int height;
        public double duration;

        /// <summary>A 2:1 frame is the giveaway for equirectangular 360 content.</summary>
        public bool LooksPanoramic =>
            width > 0 && height > 0 && Mathf.Abs((float)width / height - 2f) < 0.15f;

        public override string ToString() => $"{url} ({width}x{height})";
    }

    [Serializable]
    public class WebVideoScanResult
    {
        public WebVideoSource[] items;
        public string page;
    }

    /// <summary>
    /// Finds the video playing on the current page and hands its URL to the
    /// 360 player.
    /// </summary>
    /// <remarks>
    /// <para>The engine will not tell us what a page is playing, so we ask the
    /// page: a script is injected on load that walks the DOM (shadow roots
    /// included, which is where most players hide) and posts back every
    /// <c>&lt;video&gt;</c> source it can see.</para>
    ///
    /// <para><b>What this cannot do.</b> It reports the URL the page is actually
    /// pulling frames from. That works for a direct .mp4, and for sites serving
    /// progressive files. It does not work for YouTube, Vimeo or anything else
    /// using segmented DASH/HLS or encrypted media: <c>currentSrc</c> there is a
    /// <c>blob:</c> handle to a MediaSource buffer that only that page can read,
    /// and Unity's VideoPlayer cannot open it. Making those work means shipping
    /// a stream extractor, which is a different project with its own legal
    /// questions. For the prototype: direct 360 .mp4 URLs.</para>
    ///
    /// <para>The bridge is <c>window.tlab</c>, which the Android WebView engine
    /// provides and GeckoView does not - with GeckoView selected this component
    /// finds nothing.</para>
    ///
    /// <para>Replies arrive through Android's UnitySendMessage, addressed by
    /// GameObject name, so this object's name must be unique in the scene and
    /// the object must stay active.</para>
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/Web Video Detector")]
    public class WebVideoDetector : MonoBehaviour
    {
        [SerializeField] private WebViewBackend m_backend;

        [Tooltip("Scan every page as it finishes loading. Off means only manual Scan() calls.")]
        [SerializeField] private bool m_scanOnPageFinish = true;

        [Tooltip("Ignore sources the page cannot hand over (blob:, MediaSource) instead of failing later in the player.")]
        [SerializeField] private bool m_hidePlayableOnlyBlockers = true;

        /// <summary>Raised with every playable source found on the page.</summary>
        public event Action<IReadOnlyList<WebVideoSource>> VideosFound;

        /// <summary>Raised when a page has videos but none of them can be handed over.</summary>
        public event Action<string> VideosBlocked;

        private readonly List<WebVideoSource> m_found = new List<WebVideoSource>();

        public IReadOnlyList<WebVideoSource> Found => m_found;

        private void OnEnable()
        {
            if (m_backend != null && m_scanOnPageFinish)
                m_backend.PageFinished += OnPageFinished;
        }

        private void OnDisable()
        {
            if (m_backend != null)
                m_backend.PageFinished -= OnPageFinished;
        }

        /// <summary>Ask the current page what it is playing.</summary>
        public void Scan()
        {
            if (m_backend == null || !m_backend.IsReady)
                return;

            var preamble =
                $"var go = '{gameObject.name}';\n" +
                $"var method = '{nameof(OnWebVideoFound)}';\n";

            m_backend.EvaluateJavaScript(preamble + ScanScript);
        }

        /// <summary>
        /// Called from the page through Android's UnitySendMessage. Public and
        /// exactly this name by contract with the injected script.
        /// </summary>
        public void OnWebVideoFound(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            WebVideoScanResult result;
            try
            {
                result = JsonUtility.FromJson<WebVideoScanResult>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(WebVideoDetector)}] unparseable payload: {exception.Message}", this);
                return;
            }

            if (result?.items == null || result.items.Length == 0)
                return;

            m_found.Clear();
            var blocked = 0;

            foreach (var item in result.items)
            {
                if (item == null || string.IsNullOrEmpty(item.url))
                    continue;

                if (m_hidePlayableOnlyBlockers && !IsPlayableByUnity(item.url))
                {
                    blocked++;
                    continue;
                }

                m_found.Add(item);
            }

            if (m_found.Count > 0)
            {
                VideosFound?.Invoke(m_found);
                return;
            }

            if (blocked > 0)
            {
                VideosBlocked?.Invoke(result.page);
                Debug.Log(
                    $"[{nameof(WebVideoDetector)}] {blocked} video(s) on {result.page} are served through MediaSource " +
                    "and cannot be handed to Unity's VideoPlayer. Use a direct file URL.",
                    this);
            }
        }

        /// <summary>
        /// A blob: or MediaSource URL only means something inside that page, and
        /// data: URLs are whole files inlined - neither survives the handover.
        /// </summary>
        public static bool IsPlayableByUnity(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return !url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("mediasource:", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Walks shadow roots as well as the main document, reports once
        /// immediately, again when metadata arrives (dimensions are only known
        /// then, and they are what tells us the frame is 2:1) and once more on a
        /// timer for players that swap their source after load.
        /// </summary>
        private const string ScanScript = @"
(function () {
  function allRoots() {
    var out = [document];
    (function walk(node) {
      if (!node) return;
      if (node.shadowRoot) { out.push(node.shadowRoot); walk(node.shadowRoot); }
      var kids = node.childNodes;
      for (var i = 0; i < kids.length; i++) walk(kids[i]);
    })(document);
    return out;
  }
  function collect() {
    var seen = {}, items = [], roots = allRoots();
    for (var i = 0; i < roots.length; i++) {
      var videos = roots[i].querySelectorAll('video');
      for (var j = 0; j < videos.length; j++) {
        var v = videos[j];
        var src = v.currentSrc || v.src;
        if (!src) { var s = v.querySelector('source'); if (s) src = s.src; }
        if (!src || seen[src]) continue;
        seen[src] = 1;
        items.push({
          url: src,
          width: v.videoWidth || 0,
          height: v.videoHeight || 0,
          duration: isFinite(v.duration) ? v.duration : 0
        });
      }
    }
    return items;
  }
  function send() {
    try {
      var items = collect();
      if (!items.length) return;
      window.tlab.unitySendMessage(go, method, JSON.stringify({ items: items, page: location.href }));
    } catch (e) { }
  }
  send();
  var roots = allRoots();
  for (var i = 0; i < roots.length; i++) {
    var videos = roots[i].querySelectorAll('video');
    for (var j = 0; j < videos.length; j++) {
      videos[j].removeEventListener('loadedmetadata', send);
      videos[j].addEventListener('loadedmetadata', send);
    }
  }
  setTimeout(send, 1500);
})();
";

        private void OnPageFinished(string url) => Scan();
    }
}
