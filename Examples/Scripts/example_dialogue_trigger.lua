-- DialogueManager example Lua trigger
-- Requires MapLoaderFramework (Lua scripting) + DialogueManager.
-- Trigger a dialogue sequence from a map event or warp zone.

-- Play a dialogue by its sequence id
dialogue_manager_play("npc_engineer_intro")

-- Check if a dialogue has already been seen (requires SaveDialogueBridge)
if save_manager_is_flag_set("dialogue_seen_npc_engineer_intro") then
    print("[DialogueTrigger] Kosta already introduced.")
else
    dialogue_manager_play("npc_engineer_intro")
end
