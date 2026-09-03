using CaminaFeliz.VRBrowser;
using UnityEngine;

/// <summary>
/// Drives Meta's passthrough layer from the reality/video mix slider.
/// </summary>
/// <remarks>
/// <para><b>Why this file has no assembly definition and no namespace of its
/// own.</b> The Meta XR SDK ships two ways: as a UPM package with its own
/// assembly, or imported from the Asset Store into <c>Assets/Oculus</c> with no
/// assembly definition at all, in which case <c>OVRPassthroughLayer</c> lands in
/// <c>Assembly-CSharp</c>. An asmdef cannot reference <c>Assembly-CSharp</c>, so
/// a tidy <c>CaminaFeliz.VRBrowser.MetaXR</c> assembly would compile under one
/// installation method and fail under the other. Living in
/// <c>Assembly-CSharp</c> works under both, because that assembly sees every
/// auto-referenced asmdef as well as the loose Oculus scripts.</para>
///
/// <para><b>Why Overlay rather than Underlay.</b> As an overlay the compositor
/// draws passthrough on top of our frame at <c>textureOpacity</c>, so the
/// crossfade needs no alpha in any material - which matters because transparent
/// materials blending against an underlay is a well-known source of black
/// rectangles, especially under URP. The cost is that the control bar is tinted
/// by the room as the slider comes up. If that becomes the problem, switch
/// <see cref="m_placement"/> to Underlay, set the camera to Solid Color with
/// alpha 0, and disable post-processing.</para>
/// </remarks>
[AddComponentMenu("CaminaFeliz/VR Browser/Meta Passthrough Controller")]
public class MetaPassthroughController : PassthroughController
{
    public enum Placement
    {
        /// <summary>Reality composited over our frame. Needs no alpha anywhere.</summary>
        Overlay,

        /// <summary>Reality behind our frame; requires a transparent camera background.</summary>
        Underlay,
    }

    [SerializeField] private OVRPassthroughLayer m_layer;
    [SerializeField] private Placement m_placement = Placement.Overlay;

    private float m_opacity;

    public override bool IsSupported
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return m_layer != null && OVRManager.IsInsightPassthroughSupported();
#else
            return false;
#endif
        }
    }

    public override float Opacity
    {
        get => m_opacity;
        set
        {
            m_opacity = Mathf.Clamp01(value);

            if (m_layer != null)
                m_layer.textureOpacity = m_opacity;
        }
    }

    public override void SetEnabled(bool enabled)
    {
        if (m_layer == null)
            return;

        // Keeping the layer enabled at zero opacity would still cost a
        // composition pass and keep the cameras running.
        m_layer.enabled = enabled;
    }

    private void Awake()
    {
        if (m_layer == null)
            m_layer = FindObjectOfType<OVRPassthroughLayer>();

        if (m_layer == null)
        {
            Debug.LogError(
                $"[{nameof(MetaPassthroughController)}] no OVRPassthroughLayer in the scene. " +
                "Add one to the OVRCameraRig and enable Passthrough Support in OVRManager.",
                this);
            return;
        }

        m_layer.overlayType = m_placement == Placement.Overlay
            ? OVROverlay.OverlayType.Overlay
            : OVROverlay.OverlayType.Underlay;

        Opacity = m_opacity;
    }
}
