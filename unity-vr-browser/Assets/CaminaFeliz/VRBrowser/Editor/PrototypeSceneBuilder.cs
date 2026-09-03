using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CaminaFeliz.VRBrowser.Editor
{
    /// <summary>
    /// Builds the passthrough + 360 prototype scene, wired and playable in the
    /// Editor, in one menu click.
    /// </summary>
    /// <remarks>
    /// Hand-wiring a scene is the slowest part of a Unity prototype and the
    /// easiest to get subtly wrong - a slider bound to nothing looks exactly
    /// like a slider bound to the wrong thing. Generating it means the wiring is
    /// reviewable in this file, and rebuilding after a mistake costs a click.
    ///
    /// It builds the parts that do not need a headset: player, mix, simulated
    /// passthrough, and the control bar. The OVRCameraRig and the browser panel
    /// are added by hand afterwards - see docs/05-passthrough-360.md.
    /// </remarks>
    public static class PrototypeSceneBuilder
    {
        private const string MenuRoot = "Tools/CaminaFeliz VR Browser/";

        /// <summary>A public-domain 360 clip is worth more than a placeholder path that plays nothing.</summary>
        private const string SampleUrl =
            "https://storage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4";

        /// <summary>Where the generated scene is saved when a build needs one on disk.</summary>
        public const string ScenePath = "Assets/CaminaFeliz/VRBrowser/Scenes/Prototype360.unity";

        [MenuItem(MenuRoot + "Build 360 + Passthrough Prototype Scene")]
        public static void Build() => BuildScene();

        /// <summary>
        /// Generate the scene and write it to disk. A player build needs a scene
        /// asset, not one living only in memory.
        /// </summary>
        public static string BuildAndSave(string path = ScenePath)
        {
            var scene = BuildScene();

            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                System.IO.Directory.CreateDirectory(directory);

            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();

            Debug.Log($"[VRBrowser] Escena guardada en {path}");
            return path;
        }

        private static Scene BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.transform.position = Vector3.zero;
            camera.nearClipPlane = 0.05f;

            EnsureEventSystem();

            var player = BuildPlayer();
            var passthrough = BuildPassthrough(camera);
            var mix = BuildMix(player, passthrough);

            BuildControlBar(camera, player, mix);

            Selection.activeObject = mix.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(
                "[VRBrowser] Prototype scene built. Press Play: the slider crossfades a stand-in 'room' colour " +
                "against the video, which is how the real passthrough mix behaves.\n" +
                "On device, add an OVRCameraRig with an OVRPassthroughLayer, put MetaPassthroughController on it, " +
                "and drop it into Reality Mix in place of the simulated one. See docs/05-passthrough-360.md.");

            return scene;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static Video360Player BuildPlayer()
        {
            var go = new GameObject("360 Video Player", typeof(VideoPlayer), typeof(Video360Player));
            return go.GetComponent<Video360Player>();
        }

        private static SimulatedPassthroughController BuildPassthrough(Camera camera)
        {
            var go = new GameObject("Passthrough (Simulated)", typeof(SimulatedPassthroughController));
            var controller = go.GetComponent<SimulatedPassthroughController>();

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("m_camera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        private static RealityMix BuildMix(Video360Player player, PassthroughController passthrough)
        {
            var go = new GameObject("Reality Mix", typeof(RealityMix));
            var mix = go.GetComponent<RealityMix>();

            var serialized = new SerializedObject(mix);
            serialized.FindProperty("m_passthrough").objectReferenceValue = passthrough;
            serialized.FindProperty("m_videoPlayer").objectReferenceValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return mix;
        }

        /// <summary>
        /// A world-space bar 1.2 m ahead and slightly below eye level: close
        /// enough to read, low enough not to sit on top of what you are watching.
        /// </summary>
        private static void BuildControlBar(Camera camera, Video360Player player, RealityMix mix)
        {
            var canvasObject = new GameObject("Immersive Bar", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1000f, 260f);
            canvasRect.position = new Vector3(0f, -0.35f, 1.2f);
            canvasRect.localScale = Vector3.one * 0.0008f;   // 1000 px -> 0.8 m

            AddPanelBackground(canvasRect);

            var slider = CreateSlider(canvasRect, "Reality Mix", new Vector2(0f, 45f), new Vector2(820f, 40f));
            UnityEventTools.AddPersistentListener(slider.onValueChanged, mix.SetMix);

            var playPause = CreateButton(canvasRect, "Play / Pause", new Vector2(-200f, -55f));
            UnityEventTools.AddPersistentListener(playPause.onClick, player.TogglePause);

            var toVideo = CreateButton(canvasRect, "Solo vídeo", new Vector2(0f, -55f));
            UnityEventTools.AddPersistentListener(toVideo.onClick, mix.ShowOnlyVideo);

            var toReality = CreateButton(canvasRect, "Solo realidad", new Vector2(200f, -55f));
            UnityEventTools.AddPersistentListener(toReality.onClick, mix.ShowOnlyReality);

            var starter = canvasObject.AddComponent<PrototypeAutoPlay>();
            var serialized = new SerializedObject(starter);
            serialized.FindProperty("m_player").objectReferenceValue = player;
            serialized.FindProperty("m_url").stringValue = SampleUrl;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPanelBackground(RectTransform parent)
        {
            var go = new GameObject("Background", typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.75f);
        }

        private static Slider CreateSlider(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(Slider));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var background = new GameObject("Background", typeof(Image));
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.SetParent(rect, worldPositionStays: false);
            Stretch(backgroundRect);
            background.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.SetParent(rect, worldPositionStays: false);
            Stretch(fillAreaRect);

            var fill = new GameObject("Fill", typeof(Image));
            var fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(fillAreaRect, worldPositionStays: false);
            Stretch(fillRect);
            fill.GetComponent<Image>().color = new Color(0.98f, 0.55f, 0.25f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.SetParent(rect, worldPositionStays: false);
            Stretch(handleAreaRect);

            var handle = new GameObject("Handle", typeof(Image));
            var handleRect = (RectTransform)handle.transform;
            handleRect.SetParent(handleAreaRect, worldPositionStays: false);
            handleRect.sizeDelta = new Vector2(44f, 0f);
            handle.GetComponent<Image>().color = Color.white;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            return slider;
        }

        private static Button CreateButton(RectTransform parent, string label, Vector2 position)
        {
            var go = new GameObject(label, typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(190f, 60f);

            go.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.24f);

            var font = BuiltinFont();
            if (font != null)
            {
                var textObject = new GameObject("Label", typeof(Text));
                var textRect = (RectTransform)textObject.transform;
                textRect.SetParent(rect, worldPositionStays: false);
                Stretch(textRect);

                var text = textObject.GetComponent<Text>();
                text.text = label;
                text.font = font;
                text.fontSize = 22;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }

            return go.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>The builtin font was renamed in 2022; try both rather than shipping unlabelled buttons.</summary>
        private static Font BuiltinFont()
        {
            foreach (var name in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
            {
                try
                {
                    var font = Resources.GetBuiltinResource<Font>(name);
                    if (font != null)
                        return font;
                }
                catch
                {
                    // Not present in this Editor version; try the other name.
                }
            }

            return null;
        }
    }
}
