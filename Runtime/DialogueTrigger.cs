using UnityEngine;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// Triggers a <see cref="DialogueSequence"/> when a player enters a trigger zone,
    /// starts the scene, or calls <see cref="Trigger()"/> from code.
    /// </summary>
    [AddComponentMenu("DialogueManager/Dialogue Trigger")]
    public class DialogueTrigger : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        public enum TriggerMode { OnStart, OnTriggerEnter, OnInteract }

        [Tooltip("Id of the DialogueSequence to play.")]
        [SerializeField] private string sequenceId;

        [Tooltip("When to fire the trigger.")]
        [SerializeField] private TriggerMode triggerMode = TriggerMode.OnInteract;

        [Tooltip("If true, the trigger fires only once per scene load.")]
        [SerializeField] private bool playOnce = true;

        [Tooltip("Optional flag that must be set for this trigger to fire.")]
        [SerializeField] private string requireFlag;

        [Tooltip("Flag to set after the dialogue completes. Leave empty for none.")]
        [SerializeField] private string setFlagOnComplete;

        [Tooltip("Tag of the object that must enter the trigger (leave empty for 'Player').")]
        [SerializeField] private string triggerTag = "Player";

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private bool _hasFired;
        private DialogueManager _manager;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            _manager = FindObjectOfType<DialogueManager>();
        }

        private void Start()
        {
            if (triggerMode == TriggerMode.OnStart) Trigger();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerMode == TriggerMode.OnTriggerEnter &&
                (string.IsNullOrEmpty(triggerTag) || other.CompareTag(triggerTag)))
                Trigger();
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Fire the dialogue trigger. Respects <see cref="playOnce"/> and
        /// <see cref="requireFlag"/> checks.
        /// </summary>
        public void Trigger()
        {
            if (_manager == null)
            {
                Debug.LogWarning("[DialogueTrigger] DialogueManager not found in scene.");
                return;
            }
            if (playOnce && _hasFired) return;
            if (!string.IsNullOrEmpty(requireFlag))
            {
                // Flag check is handled externally via SaveManager; skip if ConditionCheck is wired
                // and the condition is not met.
                if (_manager.ConditionCheck != null && !_manager.ConditionCheck(requireFlag)) return;
            }

            _hasFired = true;

            if (!string.IsNullOrEmpty(setFlagOnComplete))
            {
                _manager.OnDialogueCompleted += OnCompleted;
            }

            _manager.PlayDialogue(sequenceId);
        }

        private void OnCompleted(string id)
        {
            if (id != sequenceId) return;
            _manager.OnDialogueCompleted -= OnCompleted;
            _manager.FlagSetCallback?.Invoke(setFlagOnComplete);
        }
    }
}
