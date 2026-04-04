#if DIALOGUEMANAGER_SM
using SaveManager.Runtime;
using UnityEngine;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// <b>SaveDialogueBridge</b> connects DialogueManager to SaveManager without creating
    /// a hard compile-time dependency in either package.
    /// <para>
    /// When <c>DIALOGUEMANAGER_SM</c> is defined:
    /// <list type="bullet">
    /// <item>Wires <c>DialogueManager.ConditionCheck</c> to read flags from SaveManager.</item>
    /// <item>Wires <c>DialogueManager.FlagSetCallback</c> to write flags to SaveManager.</item>
    /// <item>Records a <c>"dialogue_seen_{sequenceId}"</c> flag when each sequence completes,
    /// enabling once-per-save dialogue gating.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/Save Dialogue Bridge")]
    [DisallowMultipleComponent]
    public class SaveDialogueBridge : MonoBehaviour
    {
        private DialogueManager _dialogue;
        private SaveManager _save;

        private void Awake()
        {
            _dialogue = GetComponent<DialogueManager>() ?? FindObjectOfType<DialogueManager>();
            _save     = GetComponent<SaveManager>() ?? FindObjectOfType<SaveManager>();

            if (_dialogue == null)
            {
                Debug.LogWarning("[SaveDialogueBridge] DialogueManager not found in scene.");
                return;
            }

            if (_save == null)
            {
                Debug.LogWarning("[SaveDialogueBridge] SaveManager not found in scene.");
                return;
            }

            _dialogue.ConditionCheck  = flag => _save.IsSet(flag);
            _dialogue.FlagSetCallback = flag => _save.SetFlag(flag);
            _dialogue.OnDialogueCompleted += OnDialogueCompleted;

            Debug.Log("[SaveDialogueBridge] Hooked DialogueManager conditions and flags into SaveManager.");
        }

        private void OnDestroy()
        {
            if (_dialogue != null)
                _dialogue.OnDialogueCompleted -= OnDialogueCompleted;
        }

        private void OnDialogueCompleted(string sequenceId)
        {
            // Record that this dialogue has been seen
            _save?.SetFlag($"dialogue_seen_{sequenceId}");
        }
    }
}
#endif
