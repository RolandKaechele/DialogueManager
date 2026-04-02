#if DIALOGUEMANAGER_MGM
using System;
using UnityEngine;
using MiniGameManager.Runtime;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// Optional bridge between DialogueManager and MiniGameManager.
    /// Enable define <c>DIALOGUEMANAGER_MGM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Extends <see cref="DialogueManager.ConditionCheck"/> so that dialogue choice conditions
    /// prefixed with <c>"minigame_completed:"</c> or <c>"minigame_active:"</c> are resolved
    /// against the MiniGameManager state.
    /// </para>
    /// <para><b>Condition string formats:</b></para>
    /// <list type="bullet">
    /// <item><c>"minigame_completed:puzzle_01"</c> — true when puzzle_01 has been completed</item>
    /// <item><c>"minigame_active:puzzle_01"</c> — true when puzzle_01 is currently running</item>
    /// </list>
    /// <para>
    /// Conditions that do not match either prefix are forwarded to any previously registered
    /// handler (chain pattern).
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/Mini Game Dialogue Bridge")]
    [DisallowMultipleComponent]
    public class MiniGameDialogueBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Prefix marking a completed-check condition.")]
        [SerializeField] private string completedPrefix = "minigame_completed:";

        [Tooltip("Prefix marking an active-check condition.")]
        [SerializeField] private string activePrefix = "minigame_active:";

        // ─── References ──────────────────────────────────────────────────────────
        private DialogueManager _dialogue;
        private MiniGameManager.Runtime.MiniGameManager _mgr;
        private Func<string, bool> _previousConditionCheck;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _dialogue = GetComponent<DialogueManager>() ?? FindFirstObjectByType<DialogueManager>();
            _mgr      = GetComponent<MiniGameManager.Runtime.MiniGameManager>()
                        ?? FindFirstObjectByType<MiniGameManager.Runtime.MiniGameManager>();

            if (_dialogue == null) Debug.LogWarning("[MiniGameDialogueBridge] DialogueManager not found.");
            if (_mgr      == null) Debug.LogWarning("[MiniGameDialogueBridge] MiniGameManager not found.");

            if (_dialogue != null)
            {
                _previousConditionCheck  = _dialogue.ConditionCheck;
                _dialogue.ConditionCheck = HandleCondition;
            }
        }

        private void OnDestroy()
        {
            if (_dialogue != null && _dialogue.ConditionCheck == HandleCondition)
                _dialogue.ConditionCheck = _previousConditionCheck;
        }

        // ─── Handler ─────────────────────────────────────────────────────────────
        private bool HandleCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return true;

            if (condition.StartsWith(completedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string id = condition.Substring(completedPrefix.Length).Trim();
                return _mgr != null && _mgr.HasCompleted(id);
            }

            if (condition.StartsWith(activePrefix, StringComparison.OrdinalIgnoreCase))
            {
                string id = condition.Substring(activePrefix.Length).Trim();
                return _mgr != null && _mgr.IsPlaying && _mgr.ActiveMiniGameId == id;
            }

            // Forward to the previously registered handler
            return _previousConditionCheck?.Invoke(condition) ?? true;
        }
    }
}
#else
namespace DialogueManager.Runtime
{
    /// <summary>No-op stub. Enable DIALOGUEMANAGER_MGM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("DialogueManager/Mini Game Dialogue Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MiniGameDialogueBridge : UnityEngine.MonoBehaviour { }
}
#endif
