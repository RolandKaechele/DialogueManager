#if DIALOGUEMANAGER_DLC
using System;
using UnityEngine;
using DlcManager.Runtime;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// Optional bridge between DialogueManager and DlcManager.
    /// Enable define <c>DIALOGUEMANAGER_DLC</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Extends <see cref="DialogueManager.ConditionCheck"/> so that dialogue choice conditions
    /// prefixed with <c>"has_dlc:"</c> are resolved against DlcManager pack ownership.
    /// </para>
    /// <para><b>Condition string format:</b></para>
    /// <list type="bullet">
    /// <item><c>"has_dlc:season_pass"</c> — true when the player owns the <c>season_pass</c> pack</item>
    /// </list>
    /// <para>
    /// Conditions that do not start with the prefix are forwarded to any previously registered
    /// handler (chain pattern).
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/DLC Dialogue Bridge")]
    [DisallowMultipleComponent]
    public class DlcDialogueBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Prefix marking a DLC ownership condition.")]
        [SerializeField] private string conditionPrefix = "has_dlc:";

        // ─── References ──────────────────────────────────────────────────────────
        private DialogueManager _dialogue;
        private DlcManager.Runtime.DlcManager _dlc;
        private Func<string, bool> _previousConditionCheck;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _dialogue = GetComponent<DialogueManager>() ?? FindFirstObjectByType<DialogueManager>();
            _dlc      = GetComponent<DlcManager.Runtime.DlcManager>()
                        ?? FindFirstObjectByType<DlcManager.Runtime.DlcManager>();

            if (_dialogue == null) Debug.LogWarning("[DlcDialogueBridge] DialogueManager not found.");
            if (_dlc      == null) Debug.LogWarning("[DlcDialogueBridge] DlcManager not found.");

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

            if (condition.StartsWith(conditionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string packId = condition.Substring(conditionPrefix.Length).Trim();
                return _dlc != null && _dlc.IsOwned(packId);
            }

            return _previousConditionCheck?.Invoke(condition) ?? true;
        }
    }
}
#else
namespace DialogueManager.Runtime
{
    /// <summary>No-op stub. Enable DIALOGUEMANAGER_DLC in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("DialogueManager/DLC Dialogue Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DlcDialogueBridge : UnityEngine.MonoBehaviour { }
}
#endif
