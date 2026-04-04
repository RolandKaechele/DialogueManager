#if DIALOGUEMANAGER_MLF
using MapLoaderFramework.Runtime;
using UnityEngine;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderDialogueBridge</b> connects DialogueManager to MapLoaderFramework without creating
    /// a hard compile-time dependency in either package.
    /// <para>
    /// When <c>DIALOGUEMANAGER_MLF</c> is defined:
    /// <list type="bullet">
    /// <item>Listens to <c>MapLoaderFramework.OnMapLoaded</c> and plays the dialogue sequence
    /// <c>"{mapId}_intro"</c> if one exists — useful for map-entry cutscene dialogues.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/Map Loader Dialogue Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderDialogueBridge : MonoBehaviour
    {
        private DialogueManager _dialogue;
        private MapLoaderFramework _framework;

        [Tooltip("Suffix appended to the map id to look up the intro dialogue (e.g. '_intro').")]
        [SerializeField] private string introSuffix = "_intro";

        [Tooltip("If true, only play the intro dialogue the first visit (requires SaveDialogueBridge flag tracking).")]
        [SerializeField] private bool firstVisitOnly = true;

        private void Awake()
        {
            _dialogue  = GetComponent<DialogueManager>() ?? FindObjectOfType<DialogueManager>();
            _framework = GetComponent<MapLoaderFramework>() ?? FindObjectOfType<MapLoaderFramework>();

            if (_dialogue == null)
            {
                Debug.LogWarning("[MapLoaderDialogueBridge] DialogueManager not found in scene.");
                return;
            }

            if (_framework != null)
            {
                _framework.OnMapLoaded += OnMapLoaded;
                Debug.Log("[MapLoaderDialogueBridge] Hooked into MapLoaderFramework.OnMapLoaded.");
            }
            else
            {
                Debug.LogWarning("[MapLoaderDialogueBridge] MapLoaderFramework not found.");
            }
        }

        private void OnDestroy()
        {
            if (_framework != null)
                _framework.OnMapLoaded -= OnMapLoaded;
        }

        private void OnMapLoaded(MapData mapData)
        {
            if (mapData == null) return;

            string introId = mapData.id + introSuffix;
            var seq = _dialogue.GetSequence(introId);
            if (seq == null) return;

            if (firstVisitOnly && _dialogue.ConditionCheck != null)
            {
                // Use the seen-dialogue flag if SaveDialogueBridge is wired
                string seenFlag = $"dialogue_seen_{introId}";
                if (_dialogue.ConditionCheck(seenFlag)) return;
            }

            _dialogue.PlayDialogue(introId);
        }
    }
}
#endif
