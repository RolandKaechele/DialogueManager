# DialogueManager

A standalone Unity package for branching NPC dialogue, portrait display, typewriter text, and optional integration with MapLoaderFramework and SaveManager.

## Features

- Load dialogue sequences from JSON (Resources folder or persistent data path)
- Branching choices with optional conditions and flag triggers
- Typewriter text effect with skip support
- Portrait display (left / right) via `Resources.Load<Sprite>`
- Audio cue delegate (`PlayAudioCallback`) for per-node voice lines or sound effects
- Fully event-driven: `OnDialogueStarted`, `OnDialogueCompleted`, `OnNodeShown`
- `DialogueTrigger` component for scene-level triggers (OnStart, OnTriggerEnter, OnInteract)
- **Optional** MapLoaderFramework bridge — auto-play `{mapId}_intro` sequences on map load
- **Optional** SaveManager bridge — gate choices behind flags; mark sequences as seen
- **Optional** LocalizationManager bridge — wire `TextResolver` to instantly localize all node text, speaker names, and choice labels (activated via `DIALOGUEMANAGER_LM`)
- **Optional** InventoryManager bridge — resolve `has_item:` choice conditions against the player inventory (activated via `DIALOGUEMANAGER_IM`)
- **Optional** MiniGameManager bridge — resolve `minigame_completed:` / `minigame_active:` choice conditions against mini-game state (activated via `DIALOGUEMANAGER_MGM`)
- **Optional** DlcManager bridge — resolve `has_dlc:` choice conditions against DLC pack ownership (activated via `DIALOGUEMANAGER_DLC`)
- **StateManager integration** — `Dialogue` state is pushed when a dialogue starts and popped on complete by StateManager's `DialogueManagerBridge` (consumed via `STATEMANAGER_DM`)
- **Odin Inspector integration** — `SerializedMonoBehaviour` base for full Inspector serialization of complex types; runtime-display fields marked `[ReadOnly]` in Play Mode (activated via `ODIN_INSPECTOR`)


## Installation

### A — Unity Package Manager (Git URL)

```
https://github.com/rolandkaechele/com.rolandkaechele.dialoguemanager.git
```

### B — Local disk

Place the `DialogueManager/` folder anywhere under your project's `Assets/` directory.  
Unity will import it automatically.

### C — npm / postinstall

```bash
npm install
```

`postinstall.js` creates the required runtime folders under `Assets/`.


## Folder Structure

```
DialogueManager/
├── Runtime/
│   ├── DialogueData.cs              # Data classes (DialogueNode, DialogueChoice, DialogueSequence)
│   ├── DialogueManager.cs           # Main orchestrator (MonoBehaviour)
│   ├── DialogueTrigger.cs           # Scene trigger component
│   ├── PortraitController.cs        # Left / right portrait Image display
│   ├── DialogueBoxController.cs     # Typewriter text + choice buttons
│   ├── MapLoaderDialogueBridge.cs   # Optional: MLF integration
│   ├── SaveDialogueBridge.cs        # Optional: SaveManager integration
│   ├── LocalizationDialogueBridge.cs # Optional: LocalizationManager integration
│   └── InventoryDialogueBridge.cs   # Optional: InventoryManager integration
│   ├── MiniGameDialogueBridge.cs    # Optional: MiniGameManager integration
│   └── DlcDialogueBridge.cs         # Optional: DlcManager integration
├── Editor/
│   ├── DialogueManagerEditor.cs     # Custom inspector for DialogueManager
│   └── DialogueTriggerEditor.cs     # Custom inspector for DialogueTrigger
├── Examples/
│   ├── Dialogues/
│   │   └── example_npc_dialogue.json
│   └── Scripts/
│       └── example_dialogue_trigger.lua
├── package.json
├── postinstall.js
├── LICENSE
└── README.md
```


## Quick Start

### 1. Scene Setup

Add `DialogueManager`, `DialogueBoxController`, and `PortraitController` components to a persistent GameObject.

Wire them up in the Inspector:

- **DialogueManager** → assign `DialogueBox` and `Portraits` references
- **DialogueBoxController** → assign `panel`, `speakerNameText`, `bodyText`, `choiceContainer`, `choiceButtonPrefab`
- **PortraitController** → assign `leftPortrait` and `rightPortrait` Image references

### 2. Dialogue JSON

Place JSON files in `Assets/Resources/Dialogues/` (or the persistent data path equivalent).

```json
{
  "id": "intro_kosta",
  "label": "Engineer Kosta Intro",
  "startNodeId": "node_01",
  "canBeRepeated": false,
  "nodes": [
    {
      "id": "node_01",
      "speakerName": "Kosta",
      "text": "Jan! Finally you made it.",
      "portraitResource": "Portraits/kosta_neutral",
      "portraitSide": "left",
      "choices": [
        { "text": "What happened here?", "nextNodeId": "node_02" },
        { "text": "We need to leave now.", "nextNodeId": "node_03" }
      ]
    },
    {
      "id": "node_02",
      "speakerName": "Kosta",
      "text": "The reactor overloaded. We have minutes.",
      "nextNodeId": null
    },
    {
      "id": "node_03",
      "speakerName": "Kosta",
      "text": "Right behind you.",
      "nextNodeId": null
    }
  ]
}
```

### 3. Play from Code

```csharp
DialogueManager dm = FindFirstObjectByType<DialogueManager.Runtime.DialogueManager>();
dm.PlayDialogue("intro_kosta");

dm.OnDialogueCompleted += id => Debug.Log($"Dialogue {id} done.");
```

### 4. DialogueTrigger Component

Add `DialogueTrigger` to any scene object:

| Field | Description |
| ----- | ----------- |
| `Sequence Id` | ID of the sequence to play |
| `Trigger Mode` | `OnStart`, `OnTriggerEnter`, or `OnInteract` |
| `Play Once` | Skip if already played |
| `Require Flag` | Only trigger if SaveManager has this flag set |
| `Set Flag On Complete` | Set this SaveManager flag when done |
| `Trigger Tag` | Collider tag filter (default: `"Player"`) |


## Dialogue JSON Format

### DialogueSequence Fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique sequence identifier |
| `label` | string | Display name (Editor only) |
| `startNodeId` | string | ID of the first node to play |
| `canBeRepeated` | bool | Whether SaveDialogueBridge allows replay (default: `true`) |
| `nodes` | array | List of DialogueNode objects |

### DialogueNode Fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique node ID within the sequence |
| `speakerName` | string | Speaker display name |
| `speakerLocalizationKey` | string | Localization key override for speaker name |
| `text` | string | Dialogue body text |
| `textLocalizationKey` | string | Localization key override for body text |
| `portraitResource` | string | `Resources.Load` path for portrait sprite |
| `portraitSide` | string | `"left"` or `"right"` (default: `"left"`) |
| `audioResource` | string | `Resources.Load` path for audio clip |
| `luaScript` | string | Custom script name (fires via `FlagSetCallback`) |
| `choices` | array | List of DialogueChoice objects |
| `nextNodeId` | string | Next node ID when no choices (or null to end) |
| `waitForInput` | bool | Wait for `ConfirmInput()` before advancing (default: `true`) |
| `autoAdvanceDelay` | float | Auto-advance delay in seconds (0 = disabled) |

### DialogueChoice Fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `text` | string | Choice button label |
| `localizationKey` | string | Localization key override |
| `nextNodeId` | string | Node to jump to when selected |
| `condition` | string | Flag name; choice is hidden if `ConditionCheck(flag)` returns false |
| `flagToSet` | string | Flag to set via `FlagSetCallback` when this choice is selected |


## MapLoaderFramework Integration

Enable the scripting define `DIALOGUEMANAGER_MLF` in Unity Player Settings.

Add `MapLoaderDialogueBridge` to the same GameObject as `DialogueManager` and `MapLoaderFramework`.

When a map loads, the bridge looks for a dialogue sequence named `{mapId}{introSuffix}` (default suffix: `"_intro"`).  
If found — and if `firstVisitOnly` is true, only if the sequence has not been seen before — it plays automatically.

### Inspector Fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `Intro Suffix` | `"_intro"` | Appended to map ID to form the sequence ID |
| `First Visit Only` | `true` | Only play if `"dialogue_seen_{introId}"` flag is not set |

### Example

Map ID `station_alpha` → sequence ID `station_alpha_intro` plays on first entry.


## SaveManager Integration

Enable the scripting define `DIALOGUEMANAGER_SM` in Unity Player Settings.

Add `SaveDialogueBridge` to the same GameObject.

- `ConditionCheck` is wired to `SaveManager.IsSet(flag)`
- `FlagSetCallback` is wired to `SaveManager.SetFlag(flag)`
- When any sequence completes, `"dialogue_seen_{sequenceId}"` is automatically set

This allows choices to be gated behind quest flags and prevents repeated intro sequences.


## LocalizationManager Integration

Enable the scripting define `DIALOGUEMANAGER_LM` in Unity Player Settings.

Add `LocalizationDialogueBridge` to the same GameObject.

The bridge wires `DialogueManager.TextResolver = key => LocalizationManager.GetText(key)`. Before every node is displayed, `DialogueManager` calls `TextResolver` to resolve `speakerLocalizationKey → speakerName`, `textLocalizationKey → text`, and each choice's `localizationKey → text`. The fallback is always the raw field value, so nodes without localization keys continue to work unchanged.

### Example node with localization keys

```json
{
  "id": "node_01",
  "speakerName": "Kosta",
  "speakerLocalizationKey": "npc.kosta.name",
  "text": "Jan! Finally you made it.",
  "textLocalizationKey": "dialogue.intro_kosta.node_01",
  "choices": [
    {
      "text": "What happened here?",
      "localizationKey": "dialogue.intro_kosta.choice_01",
      "nextNodeId": "node_02"
    }
  ]
}
```


## InventoryManager Integration

Enable the scripting define `DIALOGUEMANAGER_IM` in Unity Player Settings.

Add `InventoryDialogueBridge` to the same GameObject.

The bridge chains into `DialogueManager.ConditionCheck`. Conditions prefixed with `"has_item:"` are resolved against the player inventory; all others are forwarded to the previously-registered handler (chain pattern — safe to combine with `SaveDialogueBridge`).

### Condition format

| Condition string | Requirement |
| ---------------- | ----------- |
| `"has_item:sword"` | Player carries ≥ 1 `sword` |
| `"has_item:sword:3"` | Player carries ≥ 3 `sword` |

### Example choice

```json
{
  "text": "I can fix that — I have the part.",
  "localizationKey": "dialogue.choice_has_part",
  "nextNodeId": "node_repair",
  "condition": "has_item:reactor_part"
}
```

### Inspector Fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `Condition Prefix` | `"has_item:"` | Prefix that marks a condition as an inventory check |


## Runtime API

### DialogueManager

| Member | Description |
| ------ | ----------- |
| `PlayDialogue(string id)` | Play sequence by ID |
| `PlayDialogue(DialogueSequence seq)` | Play sequence directly |
| `StopDialogue()` | Interrupt and hide dialogue box |
| `ConfirmInput()` | Advance past a node waiting for input |
| `SelectChoice(int index)` | Select a visible choice by index |
| `IsPlaying` | Whether a dialogue is currently active |
| `OnDialogueStarted` | `Action<string>` — fired when sequence begins |
| `OnDialogueCompleted` | `Action<string>` — fired when sequence ends |
| `OnNodeShown` | `Action<string, string>` — node ID + speaker name |
| `ConditionCheck` | `Func<string, bool>` — evaluate flag condition |
| `FlagSetCallback` | `Action<string>` — set a flag by name |
| `PlayAudioCallback` | `Action<string, bool>` — play audio (resourcePath, loop) |
| `TextResolver` | `Func<string, string>` — optional localization resolver; called with a localization key, returns translated text. Set by `LocalizationDialogueBridge`. |

### DialogueTrigger

| Member | Description |
| ------ | ----------- |
| `Trigger()` | Manually fire the trigger |
| `sequenceId` | Sequence to play |
| `playOnce` | Whether to fire only once |
| `requireFlag` | Required SaveManager flag |
| `setFlagOnComplete` | Flag to set on completion |

### PortraitController

| Member | Description |
| ------ | ----------- |
| `ShowPortrait(string path, string side)` | Load and display portrait sprite |
| `HidePortrait(string side)` | Hide one side |
| `HideAll()` | Hide both sides |

### DialogueBoxController

| Member | Description |
| ------ | ----------- |
| `Show(string speaker, string text, List<DialogueChoice> choices)` | Display node |
| `Hide()` | Hide the dialogue box |
| `SkipTypewriter()` | Instantly complete the typewriter effect |
| `OnChoiceSelected` | `Action<int>` — fired when player selects a choice |

### MapLoaderDialogueBridge

| Member | Description |
| ------ | ----------- |
| `introSuffix` | Suffix appended to map ID (default `"_intro"`) |
| `firstVisitOnly` | Skip if `"dialogue_seen_{introId}"` flag is set |

### SaveDialogueBridge

Wires automatically on Awake. No public API surface beyond Inspector fields.

### LocalizationDialogueBridge *(requires `DIALOGUEMANAGER_LM`)*

Wires `DialogueManager.TextResolver` on Awake. No further configuration required.

### InventoryDialogueBridge *(requires `DIALOGUEMANAGER_IM`)*

| Member | Description |
| ------ | ----------- |
| `conditionPrefix` | Inspector — prefix marking a condition as an inventory check (default: `"has_item:"`) |

### MiniGameDialogueBridge *(requires `DIALOGUEMANAGER_MGM`)*

Chains into `ConditionCheck`. Prefix `"minigame_completed:"` checks whether the named mini-game has been completed; `"minigame_active:"` checks whether it is currently running.

| Condition | Evaluates to true when... |
| --------- | ------------------------- |
| `"minigame_completed:puzzle_01"` | mini-game `puzzle_01` has been completed |
| `"minigame_active:puzzle_01"` | mini-game `puzzle_01` is currently running |

### DlcDialogueBridge *(requires `DIALOGUEMANAGER_DLC`)*

Chains into `ConditionCheck`. Prefix `"has_dlc:"` resolves against DlcManager pack ownership.

| Condition | Evaluates to true when... |
| --------- | ------------------------- |
| `"has_dlc:season_pass"` | the player owns the `season_pass` DLC pack |


## Examples

See `Examples/Dialogues/example_npc_dialogue.json` for a complete branching dialogue with two choice paths.

See `Examples/Scripts/example_dialogue_trigger.lua` for a Lua-side example of playing a dialogue and checking the seen flag.


## Dependencies

| Dependency | Role |
| ---------- | ---- |
| Unity 2022.3+ | Required |
| TextMeshPro | Optional — used by DialogueBoxController if present |
| MapLoaderFramework | Optional — enable `DIALOGUEMANAGER_MLF` |
| SaveManager | Optional — enable `DIALOGUEMANAGER_SM` |
| LocalizationManager | Optional — enable `DIALOGUEMANAGER_LM` |
| InventoryManager | Optional — enable `DIALOGUEMANAGER_IM` |
| MiniGameManager | Optional — enable `DIALOGUEMANAGER_MGM` |
| DlcManager | Optional — enable `DIALOGUEMANAGER_DLC` |
| Odin Inspector | Optional — enable `ODIN_INSPECTOR` |


## Repository

`https://github.com/rolandkaechele/com.rolandkaechele.dialoguemanager`


## License

MIT — see [LICENSE](LICENSE)
