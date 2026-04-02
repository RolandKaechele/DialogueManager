#if DIALOGUEMANAGER_LM
using UnityEngine;
using LocalizationManager.Runtime;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// Optional bridge between DialogueManager and LocalizationManager.
    /// Enable define <c>DIALOGUEMANAGER_LM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Wires <see cref="DialogueManager.TextResolver"/> to route all localization key lookups
    /// through <c>LocalizationManager.GetText(key)</c>, so that node speaker names, dialogue
    /// text, and choice button labels are automatically displayed in the active language.
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/Localization Dialogue Bridge")]
    [DisallowMultipleComponent]
    public class LocalizationDialogueBridge : MonoBehaviour
    {
        private DialogueManager _dialogue;
        private LocalizationManager.Runtime.LocalizationManager _localization;

        private void Awake()
        {
            _dialogue     = GetComponent<DialogueManager>() ?? FindFirstObjectByType<DialogueManager>();
            _localization = GetComponent<LocalizationManager.Runtime.LocalizationManager>()
                            ?? FindFirstObjectByType<LocalizationManager.Runtime.LocalizationManager>();

            if (_dialogue     == null) Debug.LogWarning("[LocalizationDialogueBridge] DialogueManager not found.");
            if (_localization == null) Debug.LogWarning("[LocalizationDialogueBridge] LocalizationManager not found.");

            if (_dialogue != null && _localization != null)
                _dialogue.TextResolver = key => _localization.GetText(key);
        }

        private void OnDestroy()
        {
            if (_dialogue != null && _dialogue.TextResolver != null &&
                _dialogue.TextResolver.Target == this)
            {
                _dialogue.TextResolver = null;
            }
        }
    }
}
#else
namespace DialogueManager.Runtime
{
    /// <summary>No-op stub. Enable DIALOGUEMANAGER_LM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("DialogueManager/Localization Dialogue Bridge")]
    public class LocalizationDialogueBridge : UnityEngine.MonoBehaviour
    {
        private void Awake() =>
            UnityEngine.Debug.Log("[LocalizationDialogueBridge] LocalizationManager integration is disabled. " +
                                  "Add the scripting define DIALOGUEMANAGER_LM to enable it.");
    }
}
#endif
