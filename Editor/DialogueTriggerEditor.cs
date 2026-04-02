#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DialogueManager.Runtime;

namespace DialogueManager.Editor
{
    [CustomEditor(typeof(DialogueTrigger))]
    public class DialogueTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var trigger = (DialogueTrigger)target;

            EditorGUILayout.Space(4);
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Trigger Now (Play Mode)"))
                trigger.Trigger();
            GUI.enabled = true;
        }
    }
}
#endif
