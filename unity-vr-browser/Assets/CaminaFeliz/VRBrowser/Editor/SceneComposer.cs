using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CaminaFeliz.VRBrowser.Editor
{
    /// <summary>
    /// Builds the whole application scene - rig, passthrough, browser panel and
    /// 360 player - wired and ready to run.
    /// </summary>
    /// <remarks>
    /// Hand-wiring this scene is the slowest part of the project and the easiest
    /// to get subtly wrong: a slider bound to nothing looks exactly like a
    /// slider bound to the wrong thing. Generating it puts the wiring in a file
    /// that can be reviewed and rebuilt in one click after a mistake.
    ///
    /// It degrades instead of failing. Where the Meta SDK or the web engine is
    /// present, the real components go in; where either is missing, the
    /// simulated ones do, and the summary at the end says exactly which. So the
    /// scene always opens and always plays - with real passthrough on a headset,
    /// or with stand-ins in the Editor.
    /// </remarks>
    public static class SceneComposer
    {
        private const string MenuRoot = "Tools/CaminaFeliz VR Browser/";

        public const string ScenePath = "Assets/CaminaFeliz/VRBrowser/Scenes/CaminaFelizVRBrowser.unity";

        /// <summary>A public-domain clip is worth more than a placeholder path that plays nothing.</summary>
        private const string SampleVideoUrl =
            "https://storage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4";

        private const string HomeUrl = "https://duckduckgo.com/";

        // Types resolved by name; see EditorTypeResolver for why.
        private const string PassthroughLayerType = "OVRPassthroughLayer";
        private const string OvrManagerType = "OVRManager";
        private const string TlabWebViewType = "TLab.WebView.WebView";
        private const string TlabBrowserManagerType = "TLab.WebView.BrowserManager";
        private const string TlabBackendType = "CaminaFeliz.VRBrowser.Integration.TLabWebViewBackend";

        [MenuItem(MenuRoot + "Create or Rebuild Main Scene", priority = 0)]
        public static void CreateSceneMenu()
        {
            var path = ComposeAndSave();
            EditorSceneManager.OpenScene(path);
        }

        [MenuItem(MenuRoot + "Report Installed Packages", priority = 1)]
        public static void ReportPackages()
        {
            Debug.Log("[VRBrowser] Paquetes detectados:\n" + EditorTypeResolver.DescribeAvailability(
                ("Meta XR (passthrough)", PassthroughLayerType),
                ("Meta XR (OVRManager)", OvrManagerType),
                ("TLabWebView (motor web)", TlabWebViewType),
                ("Integración TLab (nuestra)", TlabBackendType)));
        }

        /// <summary>Compose the scene, write it to disk and put it in Build Settings.</summary>
        public static string ComposeAndSave(string path = ScenePath)
        {
            var notes = new List<string>();
            var scene = Compose(notes);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            AddToBuildSettings(path);

            Debug.Log($"[VRBrowser] Escena creada en {path}\n" + string.Join("\n", notes));
            return path;
        }

        public static Scene Compose(List<string> notes = null)
        {
            notes ??= new List<string>();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camera = BuildRig(out var rig, notes);
            var passthrough = BuildPassthrough(rig, camera, notes);
            var player = BuildVideoPlayer();
            var mix = BuildMix(player, passthrough);

            var eventSystem = BuildEventSystem(notes);
            var backend = BuildBrowser(camera, out var browserRoot, out var chrome, notes);
            var detector = BuildDetector(browserRoot, backend);

            BuildImmersiveBar(camera, player, mix, out var immersiveRoot);
            BuildModeController(browserRoot, immersiveRoot, player, mix, detector, backend);

            // The engine's frame pump lives on the same object as the event
            // system, which is where TLabWebView's own docs put it.
            _ = eventSystem;

            return scene;
        }

        // ---------------------------------------------------------------- rig

        private static Camera BuildRig(out GameObject rig, List<string> notes)
        {
            var rigPrefab = EditorTypeResolver.FindAsset<GameObject>("OVRCameraRig");

            if (rigPrefab != null)
            {
                rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                rig.name = "OVRCameraRig";

                var rigCamera = rig.GetComponentInChildren<Camera>();
                if (rigCamera != null)
                {
                    notes.Add("  OK      OVRCameraRig de Meta instanciado");
                    return rigCamera;
                }

                notes.Add("  AVISO   OVRCameraRig instanciado pero sin cámara; añado una");
            }
            else
            {
                rig = new GameObject("XR Rig (sin Meta XR SDK)");
                notes.Add("  FALTA   OVRCameraRig (Meta XR SDK): monto una cámara normal");
            }

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rig.transform, worldPositionStays: false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            return camera;
        }

        private static PassthroughController BuildPassthrough(GameObject rig, Camera camera, List<string> notes)
        {
            var layer = EditorTypeResolver.AddComponent(rig, PassthroughLayerType);

            if (layer != null)
            {
                var controller = rig.AddComponent<MetaPassthroughController>();
                EditorTypeResolver.SetReference(controller, "m_passthroughLayer", layer);

                var managerType = EditorTypeResolver.Find(OvrManagerType);
                if (managerType != null)
                {
                    var manager = rig.GetComponent(managerType);
                    if (manager != null && !EditorTypeResolver.SetBool(manager, "isInsightPassthroughEnabled", true))
                        notes.Add("  AVISO   activa Passthrough Support a mano en el OVRManager");
                }

                notes.Add("  OK      passthrough real de Meta (OVRPassthroughLayer, overlay)");
                return controller;
            }

            var simulatedObject = new GameObject("Passthrough (Simulado)");
            var simulated = simulatedObject.AddComponent<SimulatedPassthroughController>();
            EditorTypeResolver.SetReference(simulated, "m_camera", camera);

            notes.Add("  FALTA   OVRPassthroughLayer: uso el passthrough simulado del Editor");
            return simulated;
        }

        // ------------------------------------------------------------- video

        private static Video360Player BuildVideoPlayer()
        {
            var go = new GameObject("360 Video Player", typeof(VideoPlayer), typeof(Video360Player));
            var player = go.GetComponent<Video360Player>();

            var autoPlay = go.AddComponent<PrototypeAutoPlay>();
            EditorTypeResolver.SetReference(autoPlay, "m_player", player);
            EditorTypeResolver.SetString(autoPlay, "m_url", SampleVideoUrl);
            EditorTypeResolver.SetBool(autoPlay, "m_playOnStart", false);

            return player;
        }

        private static RealityMix BuildMix(Video360Player player, PassthroughController passthrough)
        {
            var go = new GameObject("Reality Mix", typeof(RealityMix));
            var mix = go.GetComponent<RealityMix>();

            EditorTypeResolver.SetReference(mix, "m_passthrough", passthrough);
            EditorTypeResolver.SetReference(mix, "m_videoPlayer", player);

            return mix;
        }

        // ----------------------------------------------------------- browser

        private static GameObject BuildEventSystem(List<string> notes)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // BrowserManager collects the engine's native instances; without it
            // they are never released.
            if (EditorTypeResolver.AddComponent(go, TlabBrowserManagerType) == null)
                notes.Add("  FALTA   TLab BrowserManager (el paquete no está resuelto)");

            return go;
        }

        private static WebViewBackend BuildBrowser(
            Camera camera, out GameObject browserRoot, out VrBrowserChrome chrome, List<string> notes)
        {
            browserRoot = new GameObject("Browser");

            var canvasObject = new GameObject("Panel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(browserRoot.transform, worldPositionStays: false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1280f, 1000f);
            canvasRect.position = new Vector3(0f, 1.5f, 1.4f);
            canvasRect.localScale = Vector3.one * 0.0013f;   // ~1.7 m de ancho

            // The controller ray only reaches a world-space canvas through this.
            if (EditorTypeResolver.AddComponent(canvasObject, "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster") == null)
                notes.Add("  AVISO   sin TrackedDeviceGraphicRaycaster: añade el del XR Interaction Toolkit al Panel");

            var surface = new GameObject("Surface", typeof(RawImage));
            var surfaceRect = (RectTransform)surface.transform;
            surfaceRect.SetParent(canvasRect, worldPositionStays: false);
            surfaceRect.anchoredPosition = new Vector2(0f, -50f);

            var backend = BuildBackend(browserRoot, surface.GetComponent<RawImage>(), notes);

            var panel = surface.AddComponent<VrBrowserPanel>();
            EditorTypeResolver.SetReference(panel, "m_backend", backend);
            EditorTypeResolver.SetReference(panel, "m_surface", surface.GetComponent<RawImage>());
            EditorTypeResolver.SetString(panel, "m_startUrl", HomeUrl);

            var pointer = surface.AddComponent<VrPointerInput>();
            EditorTypeResolver.SetReference(pointer, "m_backend", backend);

            chrome = BuildChrome(canvasRect, backend, panel);

            var placement = browserRoot.AddComponent<VrPanelPlacement>();
            EditorTypeResolver.SetReference(placement, "m_head", camera.transform);

            var keyboard = browserRoot.AddComponent<VrKeyboardBridge>();
            EditorTypeResolver.SetReference(keyboard, "m_backend", backend);

            var privacy = browserRoot.AddComponent<PrivacyController>();
            EditorTypeResolver.SetReference(privacy, "m_backend", backend);
            EditorTypeResolver.SetReference(privacy, "m_chrome", chrome);

            return backend;
        }

        private static WebViewBackend BuildBackend(GameObject parent, RawImage surface, List<string> notes)
        {
            var engineObject = new GameObject("Engine");
            engineObject.transform.SetParent(parent.transform, worldPositionStays: false);

            var browser = EditorTypeResolver.AddComponent(engineObject, TlabWebViewType);
            var adapter = EditorTypeResolver.AddComponent(engineObject, TlabBackendType) as WebViewBackend;

            if (browser != null && adapter != null)
            {
                EditorTypeResolver.SetReference(adapter, "m_browser", browser);

                // The engine writes its frames straight into this RawImage.
                if (!EditorTypeResolver.SetReference(browser, "m_rawImage", surface))
                    notes.Add("  AVISO   asigna a mano el RawImage del WebView de TLab");

                notes.Add("  OK      motor web real (TLabWebView) - solo renderiza en Android");
                return adapter;
            }

            var simulated = engineObject.AddComponent<SimulatedWebViewBackend>();
            notes.Add("  FALTA   TLabWebView: uso el backend simulado (rejilla + puntero)");
            return simulated;
        }

        private static VrBrowserChrome BuildChrome(RectTransform canvas, WebViewBackend backend, VrBrowserPanel panel)
        {
            var chromeObject = new GameObject("Chrome", typeof(RectTransform));
            var chromeRect = (RectTransform)chromeObject.transform;
            chromeRect.SetParent(canvas, worldPositionStays: false);
            chromeRect.anchoredPosition = new Vector2(0f, 460f);
            chromeRect.sizeDelta = new Vector2(1280f, 80f);

            var back = UiFactory.Button(chromeRect, "Atrás", new Vector2(-540f, 0f));
            var forward = UiFactory.Button(chromeRect, "Adelante", new Vector2(-330f, 0f));
            var reload = UiFactory.Button(chromeRect, "Recargar", new Vector2(-120f, 0f));
            var home = UiFactory.Button(chromeRect, "Inicio", new Vector2(90f, 0f));

            var chrome = chromeObject.AddComponent<VrBrowserChrome>();
            EditorTypeResolver.SetReference(chrome, "m_backend", backend);
            EditorTypeResolver.SetReference(chrome, "m_panel", panel);
            EditorTypeResolver.SetReference(chrome, "m_backButton", back);
            EditorTypeResolver.SetReference(chrome, "m_forwardButton", forward);
            EditorTypeResolver.SetReference(chrome, "m_reloadButton", reload);
            EditorTypeResolver.SetReference(chrome, "m_homeButton", home);

            return chrome;
        }

        private static WebVideoDetector BuildDetector(GameObject browserRoot, WebViewBackend backend)
        {
            // UnitySendMessage addresses by GameObject name, so this one has to
            // stay unique in the scene and active.
            var go = new GameObject("Web Video Detector", typeof(WebVideoDetector));
            go.transform.SetParent(browserRoot.transform, worldPositionStays: false);

            var detector = go.GetComponent<WebVideoDetector>();
            EditorTypeResolver.SetReference(detector, "m_backend", backend);
            return detector;
        }

        // --------------------------------------------------------- immersive

        private static void BuildImmersiveBar(
            Camera camera, Video360Player player, RealityMix mix, out GameObject immersiveRoot)
        {
            immersiveRoot = new GameObject("Immersive Bar");

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(immersiveRoot.transform, worldPositionStays: false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            EditorTypeResolver.AddComponent(canvasObject, "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster");

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1000f, 260f);
            canvasRect.position = new Vector3(0f, 1.15f, 1.2f);
            canvasRect.localScale = Vector3.one * 0.0008f;   // 1000 px -> 0,8 m

            UiFactory.Background(canvasRect);

            var slider = UiFactory.Slider(canvasRect, "Realidad / Vídeo", new Vector2(0f, 45f), new Vector2(820f, 40f));
            UnityEventTools.AddPersistentListener(slider.onValueChanged, mix.SetMix);

            var playPause = UiFactory.Button(canvasRect, "Play / Pausa", new Vector2(-320f, -55f));
            UnityEventTools.AddPersistentListener(playPause.onClick, player.TogglePause);

            var onlyVideo = UiFactory.Button(canvasRect, "Solo vídeo", new Vector2(-110f, -55f));
            UnityEventTools.AddPersistentListener(onlyVideo.onClick, mix.ShowOnlyVideo);

            var onlyReality = UiFactory.Button(canvasRect, "Solo realidad", new Vector2(100f, -55f));
            UnityEventTools.AddPersistentListener(onlyReality.onClick, mix.ShowOnlyReality);

            immersiveRoot.SetActive(false);
        }

        private static void BuildModeController(
            GameObject browserRoot, GameObject immersiveRoot, Video360Player player,
            RealityMix mix, WebVideoDetector detector, WebViewBackend backend)
        {
            var go = new GameObject("Immersive Mode Controller", typeof(ImmersiveModeController));
            var controller = go.GetComponent<ImmersiveModeController>();

            EditorTypeResolver.SetReference(controller, "m_browserRoot", browserRoot);
            EditorTypeResolver.SetReference(controller, "m_immersiveRoot", immersiveRoot);
            EditorTypeResolver.SetReference(controller, "m_player", player);
            EditorTypeResolver.SetReference(controller, "m_mix", mix);
            EditorTypeResolver.SetReference(controller, "m_detector", detector);
            EditorTypeResolver.SetReference(controller, "m_backend", backend);

            // "Ver en 360" lives on the browser panel and only lights up when the
            // page turns out to have something playable.
            var watch = UiFactory.Button(
                (RectTransform)browserRoot.transform.Find("Panel"), "Ver en 360", new Vector2(400f, 460f));

            UnityEventTools.AddPersistentListener(watch.onClick, controller.PlayDetected);
            UnityEventTools.AddPersistentListener(controller.onPlayableVideoAvailable, watch.gameObject.SetActive);
            watch.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------- build

        private static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            if (scenes.Any(scene => scene.path == path))
                return;

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
