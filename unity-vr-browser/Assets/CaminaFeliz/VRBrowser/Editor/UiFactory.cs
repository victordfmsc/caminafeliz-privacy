using UnityEngine;
using UnityEngine.UI;

namespace CaminaFeliz.VRBrowser.Editor
{
    /// <summary>
    /// Builds the handful of uGUI widgets the generated scene needs, without the
    /// Editor's own menu helpers, which drag in prefab assets and selection state.
    /// </summary>
    internal static class UiFactory
    {
        public static void Background(RectTransform parent)
        {
            var go = new GameObject("Background", typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            Stretch(rect);

            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.75f);
        }

        public static Slider Slider(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(UnityEngine.UI.Slider));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var background = Image(rect, "Background", new Color(0.25f, 0.25f, 0.3f));
            Stretch((RectTransform)background.transform);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.SetParent(rect, worldPositionStays: false);
            Stretch(fillAreaRect);

            var fill = Image(fillAreaRect, "Fill", new Color(0.98f, 0.55f, 0.25f));
            Stretch((RectTransform)fill.transform);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.SetParent(rect, worldPositionStays: false);
            Stretch(handleAreaRect);

            var handle = Image(handleAreaRect, "Handle", Color.white);
            ((RectTransform)handle.transform).sizeDelta = new Vector2(44f, 0f);

            var slider = go.GetComponent<UnityEngine.UI.Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = (RectTransform)handle.transform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            return slider;
        }

        public static Button Button(RectTransform parent, string label, Vector2 position)
        {
            var go = new GameObject(label, typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(196f, 60f);

            go.GetComponent<UnityEngine.UI.Image>().color = new Color(0.18f, 0.19f, 0.24f);

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

            return go.GetComponent<UnityEngine.UI.Button>();
        }

        private static Graphic Image(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, worldPositionStays: false);

            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            return image;
        }

        public static void Stretch(RectTransform rect)
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
