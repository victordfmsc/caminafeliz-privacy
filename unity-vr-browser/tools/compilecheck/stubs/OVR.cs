// Stand-ins for the Meta XR Core SDK types the passthrough controller uses.
using UnityEngine;

public class OVROverlay : MonoBehaviour
{
    public enum OverlayType { None, Underlay, Overlay }
    public OverlayType overlayType { get; set; }
}

public class OVRPassthroughLayer : OVROverlay
{
    public float textureOpacity { get; set; }
}

public static class OVRManager
{
    public static bool IsInsightPassthroughSupported() => true;
}
