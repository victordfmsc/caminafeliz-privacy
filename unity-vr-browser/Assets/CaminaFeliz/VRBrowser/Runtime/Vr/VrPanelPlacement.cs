using UnityEngine;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Keeps the panel in a comfortable place: in front of the user, at eye
    /// height, facing them, and only moving when they have clearly turned away.
    /// </summary>
    /// <remarks>
    /// A panel rigidly locked to the head is nauseating and a panel fixed in the
    /// world is lost the moment the user turns around. The usual answer, and the
    /// one implemented here, is a dead zone: hold still while the head is within
    /// <see cref="m_followAngleThreshold"/> degrees, then ease back into view.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/VR Panel Placement")]
    public class VrPanelPlacement : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to use Camera.main at startup.")]
        [SerializeField] private Transform m_head;

        [Header("Resting pose")]
        [SerializeField, Min(0.2f)] private float m_distance = 1.2f;
        [SerializeField] private float m_verticalOffset = -0.15f;

        [Header("Lazy follow")]
        [SerializeField] private bool m_followEnabled = true;
        [Tooltip("Head yaw difference, in degrees, before the panel starts catching up.")]
        [SerializeField, Range(1f, 90f)] private float m_followAngleThreshold = 35f;
        [SerializeField, Range(0.5f, 10f)] private float m_followSpeed = 3f;

        private bool m_catchingUp;

        public float Distance
        {
            get => m_distance;
            set => m_distance = Mathf.Max(0.2f, value);
        }

        public bool FollowEnabled
        {
            get => m_followEnabled;
            set => m_followEnabled = value;
        }

        private void Awake()
        {
            if (m_head == null && Camera.main != null)
                m_head = Camera.main.transform;
        }

        private void Start() => Recenter();

        private void LateUpdate()
        {
            if (!m_followEnabled || m_head == null)
                return;

            var target = TargetPosition();
            var angle = Vector3.Angle(Flatten(m_head.forward), Flatten(target - m_head.position));

            if (angle > m_followAngleThreshold)
                m_catchingUp = true;
            else if (angle < m_followAngleThreshold * 0.25f)
                m_catchingUp = false;

            if (!m_catchingUp)
                return;

            var t = 1f - Mathf.Exp(-m_followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation(), t);
        }

        /// <summary>Snap the panel back in front of the user. Bind this to a controller button.</summary>
        public void Recenter()
        {
            if (m_head == null)
                return;

            transform.position = TargetPosition();
            transform.rotation = TargetRotation();
            m_catchingUp = false;
        }

        /// <summary>Push the panel further away or pull it closer, then re-place it.</summary>
        public void NudgeDistance(float delta)
        {
            Distance += delta;
            Recenter();
        }

        private Vector3 TargetPosition()
        {
            var forward = Flatten(m_head.forward);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            return m_head.position + forward.normalized * m_distance + Vector3.up * m_verticalOffset;
        }

        private Quaternion TargetRotation()
        {
            var away = Flatten(transform.position - m_head.position);
            return away.sqrMagnitude < 0.0001f
                ? transform.rotation
                : Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        private static Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
