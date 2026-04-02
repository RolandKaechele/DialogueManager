using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueManager.Runtime
{
    // -------------------------------------------------------------------------
    // DialogueChoice
    // -------------------------------------------------------------------------

    /// <summary>
    /// A player-selectable response within a dialogue node.
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        /// <summary>Display text shown on the choice button.</summary>
        public string text;

        /// <summary>Localization key for <see cref="text"/> (overrides text when resolved).</summary>
        public string localizationKey;

        /// <summary>Id of the node to jump to when this choice is selected.</summary>
        public string nextNodeId;

        /// <summary>
        /// Optional game flag that must be set for this choice to appear.
        /// Leave empty to always show.
        /// </summary>
        public string condition;

        /// <summary>Optional game flag to set when this choice is selected.</summary>
        public string flagToSet;
    }

    // -------------------------------------------------------------------------
    // DialogueNode
    // -------------------------------------------------------------------------

    /// <summary>
    /// A single node in a <see cref="DialogueSequence"/> — one line of dialogue with optional choices.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        /// <summary>Unique identifier within the sequence.</summary>
        public string id;

        /// <summary>Display name of the speaker (e.g. "Commander Ross").</summary>
        public string speakerName;

        /// <summary>Localization key for <see cref="speakerName"/>.</summary>
        public string speakerLocalizationKey;

        /// <summary>Dialogue text for this node.</summary>
        public string text;

        /// <summary>Localization key for <see cref="text"/>.</summary>
        public string textLocalizationKey;

        /// <summary>
        /// Resources-relative path to a portrait sprite (without extension).
        /// E.g. <c>"Portraits/commander_happy"</c>.
        /// </summary>
        public string portraitResource;

        /// <summary>Which side the portrait appears on. <c>"left"</c> (default) or <c>"right"</c>.</summary>
        public string portraitSide = "left";

        /// <summary>
        /// Resources-relative path to an audio clip to play with this line (without extension).
        /// Leave empty for no voice-over.
        /// </summary>
        public string audioResource;

        /// <summary>
        /// Optional Lua script filename (without extension) to run when this node displays.
        /// Requires MapLoaderFramework with MoonSharp.
        /// </summary>
        public string luaScript;

        /// <summary>
        /// Choice buttons to show. If empty, the sequence either advances to
        /// <see cref="nextNodeId"/> automatically or ends.
        /// </summary>
        public List<DialogueChoice> choices = new();

        /// <summary>
        /// Id of the next node to display when there are no choices.
        /// Leave empty to end the sequence after this node.
        /// </summary>
        public string nextNodeId;

        /// <summary>If true, wait for player input before advancing (ignored when choices are present).</summary>
        public bool waitForInput = true;

        /// <summary>
        /// Seconds after which to auto-advance if <see cref="waitForInput"/> is false. 0 = instant.
        /// </summary>
        public float autoAdvanceDelay = 0f;
    }

    // -------------------------------------------------------------------------
    // DialogueSequence
    // -------------------------------------------------------------------------

    /// <summary>
    /// A complete conversation — an ordered tree of <see cref="DialogueNode"/>s
    /// connected by <see cref="DialogueChoice"/>s.
    /// <para>
    /// Load from JSON in <c>Resources/Dialogues/</c> or the external Dialogues folder.
    /// </para>
    /// </summary>
    [Serializable]
    public class DialogueSequence
    {
        /// <summary>Unique sequence identifier referenced by <see cref="DialogueTrigger"/> and code.</summary>
        public string id;

        /// <summary>Human-readable label (Editor/debug only).</summary>
        public string label;

        /// <summary>Id of the first <see cref="DialogueNode"/> to display.</summary>
        public string startNodeId;

        /// <summary>All nodes in this conversation.</summary>
        public List<DialogueNode> nodes = new();

        /// <summary>If false, the sequence can only play once per save (requires SaveManager integration).</summary>
        public bool canBeRepeated = true;

        /// <summary>Stores the original JSON (populated at load time; not serialized to JSON).</summary>
        [NonSerialized] public string rawJson;
    }
}
