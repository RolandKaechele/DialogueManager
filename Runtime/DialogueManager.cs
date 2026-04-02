using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// <b>DialogueManager</b> is the central orchestrator for NPC conversations.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Load <see cref="DialogueSequence"/> JSON from <c>Resources/Dialogues/</c>
    /// and an optional external folder.</item>
    /// <item>Play sequences node-by-node; handle branching choices.</item>
    /// <item>Drive <see cref="DialogueBoxController"/> and <see cref="PortraitController"/>.</item>
    /// <item>Expose events and delegate hooks for flag checks and audio playback.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add to a persistent manager GameObject. Wire the optional sub-controllers
    /// in the Inspector.
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/Dialogue Manager")]
    [DisallowMultipleComponent]
    public class DialogueManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Sub-controllers (auto-resolved if not assigned)")]
        [SerializeField] private DialogueBoxController dialogueBoxController;
        [SerializeField] private PortraitController     portraitController;

        [Header("Loaded sequences (read-only)")]
        [SerializeField] private List<string> loadedSequenceIds = new();

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when a sequence starts. Parameter: sequence id.</summary>
        public event Action<string> OnDialogueStarted;

        /// <summary>Fired when a sequence ends (normally or forced). Parameter: sequence id.</summary>
        public event Action<string> OnDialogueCompleted;

        /// <summary>Fired every time a node is displayed. Parameters: (sequenceId, nodeId).</summary>
        public event Action<string, string> OnNodeShown;

        // -------------------------------------------------------------------------
        // Delegate hooks for external systems
        // -------------------------------------------------------------------------

        /// <summary>
        /// Optional callback to check whether a <see cref="DialogueChoice"/> condition is met.
        /// Signature: (conditionFlag) → bool.
        /// Defaults to always-true when not set. Set by <c>SaveDialogueBridge</c>.
        /// </summary>
        public Func<string, bool> ConditionCheck;

        /// <summary>
        /// Optional callback invoked when a choice sets a flag.
        /// Signature: (flagName). Set by <c>SaveDialogueBridge</c>.
        /// </summary>
        public Action<string> FlagSetCallback;

        /// <summary>
        /// Optional callback for node audio playback.
        /// Signature: (resourcePath, loop).
        /// When set, overrides the built-in AudioSource fallback.
        /// </summary>
        public Action<string, bool> PlayAudioCallback;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private readonly Dictionary<string, DialogueSequence> _sequences = new();
        private Coroutine _activeCoroutine;
        private string _activeSequenceId;
        private bool _awaitingInput;
        private int _pendingChoiceIndex = -1;

        /// <summary>True while a conversation is playing.</summary>
        public bool IsPlaying => _activeCoroutine != null;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (dialogueBoxController == null)
                dialogueBoxController = FindObjectOfType<DialogueBoxController>();
            if (portraitController == null)
                portraitController = FindObjectOfType<PortraitController>();

            LoadAllSequences();
        }

        // -------------------------------------------------------------------------
        // Loading
        // -------------------------------------------------------------------------

        /// <summary>
        /// Reload all dialogue JSON files from <c>Resources/Dialogues/</c> and the external folder.
        /// </summary>
        public void LoadAllSequences()
        {
            _sequences.Clear();
            loadedSequenceIds.Clear();

            var assets = Resources.LoadAll<TextAsset>("Dialogues");
            foreach (var asset in assets)
                RegisterJson(asset.text);

            string externalDir = Path.Combine(Application.persistentDataPath, "Dialogues");
            if (Directory.Exists(externalDir))
            {
                foreach (var file in Directory.GetFiles(externalDir, "*.json", SearchOption.AllDirectories))
                {
                    try   { RegisterJson(File.ReadAllText(file)); }
                    catch (Exception ex) { Debug.LogError($"[DialogueManager] Failed to load {file}: {ex.Message}"); }
                }
            }

            Debug.Log($"[DialogueManager] Loaded {_sequences.Count} dialogue sequence(s).");
        }

        private void RegisterJson(string json)
        {
            try
            {
                var seq = JsonUtility.FromJson<DialogueSequence>(json);
                if (seq == null || string.IsNullOrEmpty(seq.id)) return;
                seq.rawJson = json;
                _sequences[seq.id] = seq;
                if (!loadedSequenceIds.Contains(seq.id))
                    loadedSequenceIds.Add(seq.id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueManager] Failed to parse sequence JSON: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // Playback
        // -------------------------------------------------------------------------

        /// <summary>Play the dialogue sequence with <paramref name="id"/>.</summary>
        public void PlayDialogue(string id)
        {
            if (!_sequences.TryGetValue(id, out var seq))
            {
                Debug.LogWarning($"[DialogueManager] Sequence '{id}' not found.");
                return;
            }
            PlayDialogue(seq);
        }

        /// <summary>Play a <see cref="DialogueSequence"/> directly.</summary>
        public void PlayDialogue(DialogueSequence sequence)
        {
            if (sequence == null) return;
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _pendingChoiceIndex = -1;
            _awaitingInput = false;
            _activeSequenceId = sequence.id;
            _activeCoroutine  = StartCoroutine(RunSequence(sequence));
        }

        /// <summary>Stop the currently playing dialogue.</summary>
        public void StopDialogue()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
            dialogueBoxController?.Hide();
            portraitController?.HideAll();
            _activeSequenceId = null;
        }

        /// <summary>
        /// Advance a node that is waiting for input.
        /// Call from a UI confirm button or keyboard handler.
        /// </summary>
        public void ConfirmInput() => _awaitingInput = false;

        /// <summary>
        /// Select a choice by index.
        /// Call from <see cref="DialogueBoxController"/> choice button callbacks.
        /// </summary>
        public void SelectChoice(int index)
        {
            _pendingChoiceIndex = index;
            _awaitingInput = false;
        }

        // -------------------------------------------------------------------------
        // Query helpers
        // -------------------------------------------------------------------------

        /// <summary>Return all loaded sequence ids.</summary>
        public IReadOnlyList<string> GetSequenceIds() => loadedSequenceIds;

        /// <summary>Return a sequence by id, or null.</summary>
        public DialogueSequence GetSequence(string id) =>
            _sequences.TryGetValue(id, out var s) ? s : null;

        // -------------------------------------------------------------------------
        // Coroutine
        // -------------------------------------------------------------------------

        private IEnumerator RunSequence(DialogueSequence sequence)
        {
            OnDialogueStarted?.Invoke(sequence.id);

            if (sequence.nodes == null || sequence.nodes.Count == 0)
            {
                FinishSequence(sequence.id);
                yield break;
            }

            // Build node lookup
            var nodeMap = sequence.nodes.ToDictionary(n => n.id, n => n);

            string currentNodeId = sequence.startNodeId;
            if (string.IsNullOrEmpty(currentNodeId))
                currentNodeId = sequence.nodes[0].id;

            while (!string.IsNullOrEmpty(currentNodeId) && nodeMap.TryGetValue(currentNodeId, out var node))
            {
                yield return ShowNode(node, sequence.id);
                currentNodeId = _pendingChoiceIndex >= 0 ? null : node.nextNodeId;
            }

            FinishSequence(sequence.id);
        }

        private IEnumerator ShowNode(DialogueNode node, string sequenceId)
        {
            OnNodeShown?.Invoke(sequenceId, node.id);

            // Portraits
            if (portraitController != null)
            {
                if (!string.IsNullOrEmpty(node.portraitResource))
                    portraitController.ShowPortrait(node.portraitResource, node.portraitSide);
                else
                    portraitController.HideAll();
            }

            // Audio
            if (!string.IsNullOrEmpty(node.audioResource))
            {
                if (PlayAudioCallback != null)
                    PlayAudioCallback(node.audioResource, false);
            }

            // Text
            if (dialogueBoxController != null)
            {
                var visibleChoices = node.choices?
                    .Where(c => string.IsNullOrEmpty(c.condition) ||
                                (ConditionCheck?.Invoke(c.condition) ?? true))
                    .ToList();

                dialogueBoxController.Show(node.speakerName, node.text, visibleChoices);
            }

            // Wait
            if (node.choices != null && node.choices.Count > 0)
            {
                _pendingChoiceIndex = -1;
                _awaitingInput = true;
                while (_awaitingInput) yield return null;

                if (_pendingChoiceIndex >= 0)
                {
                    var choice = node.choices[_pendingChoiceIndex];
                    if (!string.IsNullOrEmpty(choice.flagToSet))
                        FlagSetCallback?.Invoke(choice.flagToSet);

                    // Jump to the chosen next node by injecting into nextNodeId via pending
                    node = new DialogueNode { nextNodeId = choice.nextNodeId };
                    _pendingChoiceIndex = -1;
                }
            }
            else if (node.waitForInput)
            {
                _awaitingInput = true;
                while (_awaitingInput) yield return null;
            }
            else if (node.autoAdvanceDelay > 0f)
            {
                yield return new WaitForSeconds(node.autoAdvanceDelay);
            }

            dialogueBoxController?.Hide();
        }

        private void FinishSequence(string id)
        {
            dialogueBoxController?.Hide();
            portraitController?.HideAll();
            _activeCoroutine  = null;
            _activeSequenceId = null;
            OnDialogueCompleted?.Invoke(id);
        }
    }
}
