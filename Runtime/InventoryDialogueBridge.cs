#if DIALOGUEMANAGER_IM
using System;
using UnityEngine;
using InventoryManager.Runtime;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// Optional bridge between DialogueManager and InventoryManager.
    /// Enable define <c>DIALOGUEMANAGER_IM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Extends <see cref="DialogueManager.ConditionCheck"/> so that dialogue choice conditions
    /// prefixed with <c>"has_item:"</c> are resolved against the player's inventory instead of
    /// (or in addition to) any previously registered condition handler.
    /// </para>
    /// <para><b>Condition string format:</b></para>
    /// <list type="bullet">
    /// <item><c>"has_item:sword"</c> — true when the player carries at least 1 unit of <c>sword</c></item>
    /// <item><c>"has_item:sword:3"</c> — true when the player carries at least 3 units</item>
    /// </list>
    /// <para>
    /// Conditions that do not start with the prefix are forwarded to whichever handler was
    /// registered before this bridge (chain pattern).
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/Inventory Dialogue Bridge")]
    [DisallowMultipleComponent]
    public class InventoryDialogueBridge : MonoBehaviour
    {
        [Tooltip("Prefix that marks a dialogue condition as an inventory check.")]
        [SerializeField] private string conditionPrefix = "has_item:";

        private DialogueManager _dialogue;
        private InventoryManager.Runtime.InventoryManager _inventory;
        private Func<string, bool> _previousConditionCheck;

        private void Awake()
        {
            _dialogue  = GetComponent<DialogueManager>() ?? FindFirstObjectByType<DialogueManager>();
            _inventory = GetComponent<InventoryManager.Runtime.InventoryManager>()
                         ?? FindFirstObjectByType<InventoryManager.Runtime.InventoryManager>();

            if (_dialogue  == null) Debug.LogWarning("[InventoryDialogueBridge] DialogueManager not found.");
            if (_inventory == null) Debug.LogWarning("[InventoryDialogueBridge] InventoryManager not found.");

            if (_dialogue != null)
            {
                _previousConditionCheck  = _dialogue.ConditionCheck;
                _dialogue.ConditionCheck = HandleCondition;
            }
        }

        private void OnDestroy()
        {
            // Restore the previous handler only if no one has replaced ours after us.
            if (_dialogue != null && _dialogue.ConditionCheck == HandleCondition)
                _dialogue.ConditionCheck = _previousConditionCheck;
        }

        private bool HandleCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            if (condition.StartsWith(conditionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string payload = condition.Substring(conditionPrefix.Length);
                int separatorIndex = payload.LastIndexOf(':');

                string itemId;
                int minQty;

                if (separatorIndex >= 0 && int.TryParse(payload.Substring(separatorIndex + 1), out int parsed))
                {
                    itemId = payload.Substring(0, separatorIndex);
                    minQty = parsed;
                }
                else
                {
                    itemId = payload;
                    minQty = 1;
                }

                return _inventory != null && _inventory.GetQuantity(itemId) >= minQty;
            }

            // Not an inventory condition — forward to the previous handler.
            return _previousConditionCheck?.Invoke(condition) ?? true;
        }
    }
}
#else
namespace DialogueManager.Runtime
{
    /// <summary>No-op stub. Enable DIALOGUEMANAGER_IM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("DialogueManager/Inventory Dialogue Bridge")]
    public class InventoryDialogueBridge : UnityEngine.MonoBehaviour
    {
        private void Awake() =>
            UnityEngine.Debug.Log("[InventoryDialogueBridge] InventoryManager integration is disabled. " +
                                  "Add the scripting define DIALOGUEMANAGER_IM to enable it.");
    }
}
#endif
