using System;
using System.Reflection;
using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Drives Meta's passthrough layer from the reality/video mix slider.
    /// </summary>
    /// <remarks>
    /// <para>It talks to <c>OVRPassthroughLayer</c> without referencing it at
    /// compile time. That is not cleverness for its own sake: the Meta XR SDK
    /// ships either as a UPM package with its own assembly or from the Asset
    /// Store with none at all, and if it is missing or still resolving, a typed
    /// reference here fails to compile and takes every editor tool in the
    /// project down with it - including the ones whose job is to tell you the
    /// SDK is missing. Resolving the setter once through reflection keeps this
    /// component compiling in any project and failing loudly only at run time,
    /// where the message can be useful.</para>
    ///
    /// <para>The per-frame cost is nil: the property setter is bound to a
    /// delegate on the first call, and <see cref="RealityMix"/> only writes when
    /// the value actually changes.</para>
    ///
    /// <para><b>Why Overlay rather than Underlay.</b> As an overlay the
    /// compositor draws passthrough on top of our frame at
    /// <c>textureOpacity</c>, so the crossfade needs no alpha in any material -
    /// which matters because transparent materials blending against an underlay
    /// is a well-known source of black rectangles, especially under URP. The
    /// cost is that the control bar is tinted by the room as the slider comes
    /// up. If that becomes the problem, switch <see cref="m_placement"/> to
    /// Underlay, set the camera to Solid Color with alpha 0, and disable
    /// post-processing.</para>
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/Meta Passthrough Controller")]
    public class MetaPassthroughController : PassthroughController
    {
        public enum Placement
        {
            /// <summary>Reality composited over our frame. Needs no alpha anywhere.</summary>
            Overlay = 2,

            /// <summary>Reality behind our frame; requires a transparent camera background.</summary>
            Underlay = 1,
        }

        private const string LayerTypeName = "OVRPassthroughLayer";

        [Tooltip("The OVRPassthroughLayer component. Left empty, it is looked up in the scene at startup.")]
        [SerializeField] private MonoBehaviour m_passthroughLayer;

        [SerializeField] private Placement m_placement = Placement.Overlay;

        private Action<float> m_setOpacity;
        private bool m_setterResolved;
        private float m_opacity;

        public override bool IsSupported => Setter() != null;

        public override float Opacity
        {
            get => m_opacity;
            set
            {
                m_opacity = Mathf.Clamp01(value);
                Setter()?.Invoke(m_opacity);
            }
        }

        public override void SetEnabled(bool enabled)
        {
            // Leaving the layer enabled at zero opacity would still cost a
            // composition pass and keep the cameras running.
            if (m_passthroughLayer != null)
                m_passthroughLayer.enabled = enabled;
        }

        private void Awake()
        {
            if (m_passthroughLayer == null)
                m_passthroughLayer = FindPassthroughLayer();

            if (m_passthroughLayer == null)
            {
                Debug.LogError(
                    $"[{nameof(MetaPassthroughController)}] no encuentro un {LayerTypeName} en la escena. " +
                    "Añádelo al OVRCameraRig y activa Passthrough Support en el OVRManager.",
                    this);
                return;
            }

            ApplyPlacement();
            Opacity = m_opacity;
        }

        private static MonoBehaviour FindPassthroughLayer()
        {
            foreach (var behaviour in FindObjectsOfType<MonoBehaviour>(includeInactive: true))
            {
                if (behaviour != null && behaviour.GetType().Name == LayerTypeName)
                    return behaviour;
            }

            return null;
        }

        /// <summary>
        /// <c>overlayType</c> is an enum declared inside the Meta SDK, so the
        /// value is converted into whatever enum type the property expects.
        /// </summary>
        private void ApplyPlacement()
        {
            var property = m_passthroughLayer.GetType().GetProperty(
                "overlayType", BindingFlags.Public | BindingFlags.Instance);

            if (property == null || !property.PropertyType.IsEnum)
                return;

            try
            {
                property.SetValue(m_passthroughLayer, Enum.ToObject(property.PropertyType, (int)m_placement));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[{nameof(MetaPassthroughController)}] no he podido fijar overlayType: {exception.Message}", this);
            }
        }

        /// <summary>Bind the opacity setter once; a delegate costs the same as a direct call.</summary>
        private Action<float> Setter()
        {
            if (m_setterResolved)
                return m_setOpacity;

            m_setterResolved = true;

            if (m_passthroughLayer == null)
                return null;

            var type = m_passthroughLayer.GetType();

            var property = type.GetProperty("textureOpacity", BindingFlags.Public | BindingFlags.Instance);
            if (property?.GetSetMethod() != null)
            {
                m_setOpacity = (Action<float>)Delegate.CreateDelegate(
                    typeof(Action<float>), m_passthroughLayer, property.GetSetMethod());
                return m_setOpacity;
            }

            var field = type.GetField("textureOpacity", BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var target = m_passthroughLayer;
                m_setOpacity = value => field.SetValue(target, value);
                return m_setOpacity;
            }

            Debug.LogError(
                $"[{nameof(MetaPassthroughController)}] {type.Name} no expone textureOpacity. " +
                "¿Ha cambiado la versión del Meta XR SDK?", this);

            return null;
        }
    }
}
