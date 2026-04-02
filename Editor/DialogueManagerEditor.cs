#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DialogueManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="DialogueManager.Runtime.DialogueManager"/>.
    /// </summary>
    [CustomEditor(typeof(DialogueManager.Runtime.DialogueManager))]
    public class DialogueManagerEditor : UnityEditor.Editor
    {
        private string _playId = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mgr = (DialogueManager.Runtime.DialogueManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            var ids = mgr.GetSequenceIds();
            if (ids.Count == 0)
            {
                EditorGUILayout.HelpBox("No sequences loaded. Place dialogue JSON files in Resources/Dialogues/.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"{ids.Count} sequence(s) loaded.", MessageType.None);
                foreach (var id in ids)
                    EditorGUILayout.LabelField("  •", id);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Play by ID (Play Mode only)", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            _playId = EditorGUILayout.TextField(_playId);
            GUI.enabled = Application.isPlaying && !string.IsNullOrEmpty(_playId);
            if (GUILayout.Button("Play", GUILayout.Width(60)))
                mgr.PlayDialogue(_playId);
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Stop", GUILayout.Width(60)))
                mgr.StopDialogue();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Reload Sequences (Play Mode)"))
            {
                if (Application.isPlaying) mgr.LoadAllSequences();
                else Debug.Log("[DialogueManager] Reload available in Play Mode only.");
            }
        }
    }
}
#endif
