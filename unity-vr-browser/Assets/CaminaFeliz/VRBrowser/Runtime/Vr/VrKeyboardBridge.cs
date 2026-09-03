using UnityEngine;
using UnityEngine.Events;

namespace CaminaFeliz.VRBrowser
{
    /// <summary>
    /// Single entry point for text input, whatever produced it.
    /// </summary>
    /// <remarks>
    /// There are at least four keyboards a Quest browser might use: TLabVKeyborad,
    /// the XR Interaction Toolkit spatial keyboard, the Meta system keyboard
    /// overlay, and a physical Bluetooth keyboard. Binding the web engine
    /// directly to any one of them - which is what the reference VR sample does -
    /// makes the other three a rewrite. Every method here is public and
    /// UnityEvent-friendly, so a keyboard is wired in the inspector and swapped
    /// without touching code.
    /// </remarks>
    [AddComponentMenu("CaminaFeliz/VR Browser/VR Keyboard Bridge")]
    public class VrKeyboardBridge : MonoBehaviour
    {
        [SerializeField] private WebViewBackend m_backend;

        [Tooltip("Raised when Enter/Go is pressed, carrying the accumulated text. Wire it to the address bar.")]
        public UnityEvent<string> onSubmit;

        /// <summary>Send a single character. Multi-character strings are sent in order.</summary>
        public void TypeString(string text)
        {
            if (m_backend == null || string.IsNullOrEmpty(text))
                return;

            foreach (var character in text)
                m_backend.SendCharacter(character);
        }

        public void TypeCharacter(char character) => m_backend?.SendCharacter(character);

        public void Backspace() => m_backend?.SendKeyCode(AndroidKeyCode.Delete);

        public void Enter() => m_backend?.SendKeyCode(AndroidKeyCode.Enter);

        public void Tab() => m_backend?.SendKeyCode(AndroidKeyCode.Tab);

        public void Escape() => m_backend?.SendKeyCode(AndroidKeyCode.Escape);

        public void Submit(string text) => onSubmit?.Invoke(text);

        /// <summary>
        /// Handles the control sequences TLabVKeyborad and the XRI spatial
        /// keyboard emit ("\b", "\r", "\s"...) alongside plain characters, so a
        /// keyboard can be pointed at this one method.
        /// </summary>
        public void HandleKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            switch (key)
            {
                case "\\b":
                case "\b":
                    Backspace();
                    return;
                case "\\r":
                case "\r":
                case "\n":
                    Enter();
                    return;
                case "\\t":
                case "\t":
                    Tab();
                    return;
                case "\\c":
                case "\\s":
                case "\\caps":
                case "\\cl":
                case "\\h":
                    // Shift / caps / cancel / clear / hide are keyboard-local state.
                    return;
                default:
                    TypeString(key);
                    return;
            }
        }
    }
}
