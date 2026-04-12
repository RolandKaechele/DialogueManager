#if UNITY_EDITOR
using System;
using System.IO;
using DialogueManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace DialogueManager.Editor
{
    // ────────────────────────────────────────────────────────────────────────────
    // Dialogue Sequence JSON Editor Window (per-file)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Editor window for creating and editing individual <c>DialogueSequence</c> JSON files.
    /// Files are stored in <c>Assets/Resources/Dialogues/</c> (loaded at runtime via Resources).
    /// Open via <b>JSON Editors → Dialogue Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class DialogueJsonEditorWindow : EditorWindow
    {
        private static readonly string DefaultDirectory =
            Path.Combine(Application.dataPath, "Resources", "Dialogues");

        private DialogueEditorBridge     _bridge;
        private UnityEditor.Editor       _bridgeEditor;

        private string   _directory;
        private string[] _files          = Array.Empty<string>();
        private int      _selectedIndex  = -1;
        private string   _selectedFile;
        private string   _newFileName    = "new_dialogue";
        private Vector2  _fileListScroll;
        private Vector2  _editorScroll;
        private string   _status;
        private bool     _statusError;

        [MenuItem("JSON Editors/Dialogue Manager")]
        public static void ShowWindow() =>
            GetWindow<DialogueJsonEditorWindow>("Dialogue JSON");

        private void OnEnable()
        {
            _directory = DefaultDirectory;
            _bridge    = CreateInstance<DialogueEditorBridge>();
            RefreshFileList();
        }

        private void OnDisable()
        {
            if (_bridgeEditor != null) DestroyImmediate(_bridgeEditor);
            if (_bridge      != null) DestroyImmediate(_bridge);
        }

        private void OnGUI()
        {
            DrawDirectoryBar();

            EditorGUILayout.BeginHorizontal();

            // ── Left panel: file list ────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(180));
            DrawFileList();
            EditorGUILayout.EndVertical();

            // ── Separator ───────────────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            var sepRect = GUILayoutUtility.GetRect(2, float.MaxValue, 2, float.MaxValue);
            EditorGUI.DrawRect(sepRect, Color.gray * 0.5f);
            EditorGUILayout.EndVertical();

            // ── Right panel: editor ──────────────────────────────────────────
            EditorGUILayout.BeginVertical();
            DrawEditor();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusError ? MessageType.Error : MessageType.Info);
        }

        // ── Directory bar ────────────────────────────────────────────────────

        private void DrawDirectoryBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Directory:", EditorStyles.miniLabel, GUILayout.Width(60));
            _directory = EditorGUILayout.TextField(_directory, EditorStyles.toolbarTextField);
            if (GUILayout.Button("Browse", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                var dir = EditorUtility.OpenFolderPanel("Select Dialogues folder", _directory, "");
                if (!string.IsNullOrEmpty(dir)) { _directory = dir; RefreshFileList(); }
            }
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(55)))
                RefreshFileList();
            EditorGUILayout.EndHorizontal();
        }

        // ── File list panel ──────────────────────────────────────────────────

        private void DrawFileList()
        {
            EditorGUILayout.LabelField("JSON Files", EditorStyles.boldLabel);

            _fileListScroll = EditorGUILayout.BeginScrollView(_fileListScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _files.Length; i++)
            {
                var label = Path.GetFileName(_files[i]);
                var style = i == _selectedIndex ? EditorStyles.whiteLabel : EditorStyles.label;
                var rect  = GUILayoutUtility.GetRect(new GUIContent(label), style);
                if (i == _selectedIndex)
                    EditorGUI.DrawRect(rect, new Color(0.24f, 0.49f, 0.91f, 0.35f));
                if (GUI.Button(rect, label, style))
                    SelectFile(i);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("New file name:", EditorStyles.miniLabel);
            _newFileName = EditorGUILayout.TextField(_newFileName);
            if (GUILayout.Button("+ Create New"))
                CreateNewFile();
        }

        // ── Editor panel ─────────────────────────────────────────────────────

        private void DrawEditor()
        {
            if (_selectedFile == null)
            {
                EditorGUILayout.HelpBox("Select a dialogue file on the left, or create a new one.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(Path.GetFileName(_selectedFile), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete file",
                    $"Delete '{Path.GetFileName(_selectedFile)}'?",
                    "Delete", "Cancel"))
                    DeleteSelected();
            }
            EditorGUILayout.EndHorizontal();

            if (_bridge == null) return;
            if (_bridgeEditor == null)
                _bridgeEditor = UnityEditor.Editor.CreateEditor(_bridge);

            _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);
            _bridgeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        // ── File operations ──────────────────────────────────────────────────

        private void RefreshFileList()
        {
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);

            _files = Directory.GetFiles(_directory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(_files);
            _selectedIndex = -1;
            _selectedFile  = null;
        }

        private void SelectFile(int index)
        {
            _selectedIndex = index;
            _selectedFile  = _files[index];
            LoadFile(_selectedFile);
        }

        private void CreateNewFile()
        {
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);

            var name = string.IsNullOrWhiteSpace(_newFileName) ? "new_dialogue" : _newFileName.Trim();
            if (!name.EndsWith(".json")) name += ".json";
            var path = Path.Combine(_directory, name);

            if (File.Exists(path))
            {
                _status     = $"File '{name}' already exists.";
                _statusError = true;
                return;
            }

            var seq = new DialogueSequence
            {
                id           = Path.GetFileNameWithoutExtension(name),
                label        = name,
                startNodeId  = "node_0",
                canBeRepeated = true
            };
            File.WriteAllText(path, JsonUtility.ToJson(seq, true));
            AssetDatabase.Refresh();
            RefreshFileList();

            for (int i = 0; i < _files.Length; i++)
            {
                if (_files[i] == path) { SelectFile(i); break; }
            }

            _status     = $"Created '{name}'.";
            _statusError = false;
        }

        private void LoadFile(string path)
        {
            try
            {
                var seq = JsonUtility.FromJson<DialogueSequence>(File.ReadAllText(path));
                _bridge.sequence = seq ?? new DialogueSequence();

                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }

                _status     = $"Loaded '{Path.GetFileName(path)}'.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Load error: {e.Message}";
                _statusError = true;
            }
        }

        private void Save()
        {
            if (_selectedFile == null) return;
            try
            {
                File.WriteAllText(_selectedFile, JsonUtility.ToJson(_bridge.sequence, true));
                AssetDatabase.Refresh();
                _status     = $"Saved '{Path.GetFileName(_selectedFile)}'.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Save error: {e.Message}";
                _statusError = true;
            }
        }

        private void DeleteSelected()
        {
            if (_selectedFile == null) return;
            File.Delete(_selectedFile);
            var meta = _selectedFile + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
            AssetDatabase.Refresh();
            _status     = $"Deleted '{Path.GetFileName(_selectedFile)}'.";
            _statusError = false;
            RefreshFileList();
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class DialogueEditorBridge : ScriptableObject
    {
        public DialogueSequence sequence = new DialogueSequence();
    }
}
#endif
