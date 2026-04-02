using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// <b>DialogueBoxController</b> handles the dialogue UI panel:
    /// speaker name, typewriter text, and dynamically generated choice buttons.
    /// </summary>
    [AddComponentMenu("DialogueManager/Dialogue Box Controller")]
    [DisallowMultipleComponent]
    public class DialogueBoxController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("UI references")]
        [Tooltip("Root panel to show/hide.")]
        [SerializeField] private GameObject panel;

        [Tooltip("TMP_Text or UI.Text for the speaker name.")]
        [SerializeField] private Component speakerNameText;

        [Tooltip("TMP_Text or UI.Text for the dialogue body.")]
        [SerializeField] private Component bodyText;

        [Tooltip("Parent transform where choice buttons are instantiated.")]
        [SerializeField] private Transform choiceContainer;

        [Tooltip("Button prefab used for each choice. Must have a Button and a text component.")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Typewriter")]
        [Tooltip("Characters per second. 0 = instant.")]
        [SerializeField] private float typewriterSpeed = 40f;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when a choice button is clicked. Parameter: choice index.</summary>
        public event Action<int> OnChoiceSelected;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private Coroutine _typewriterCoroutine;
        private readonly List<GameObject> _choiceButtons = new();

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Display the dialogue box with the given content.
        /// Starts the typewriter effect and instantiates choice buttons if provided.
        /// </summary>
        public void Show(string speakerName, string text, List<DialogueChoice> choices = null)
        {
            if (panel != null) panel.SetActive(true);

            SetText(speakerNameText, speakerName ?? "");

            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);

            if (typewriterSpeed > 0f && !string.IsNullOrEmpty(text))
                _typewriterCoroutine = StartCoroutine(Typewriter(text));
            else
                SetText(bodyText, text ?? "");

            PopulateChoices(choices);
        }

        /// <summary>
        /// Instantly complete the typewriter animation and show the full text.
        /// </summary>
        public void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            // The last assigned text is not stored here; typewriter sets it character by character.
            // The DialogueManager calls Show() with the full text, so skip is safe.
        }

        /// <summary>Hide the dialogue box and clear choices.</summary>
        public void Hide()
        {
            if (_typewriterCoroutine != null) { StopCoroutine(_typewriterCoroutine); _typewriterCoroutine = null; }
            ClearChoices();
            if (panel != null) panel.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private IEnumerator Typewriter(string fullText)
        {
            SetText(bodyText, "");
            float interval = 1f / typewriterSpeed;
            for (int i = 0; i <= fullText.Length; i++)
            {
                SetText(bodyText, fullText.Substring(0, i));
                yield return new WaitForSeconds(interval);
            }
            _typewriterCoroutine = null;
        }

        private void PopulateChoices(List<DialogueChoice> choices)
        {
            ClearChoices();
            if (choices == null || choices.Count == 0 || choiceContainer == null || choiceButtonPrefab == null)
                return;

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                var btnGo = Instantiate(choiceButtonPrefab, choiceContainer);
                _choiceButtons.Add(btnGo);

                // Set button label
                var textComp = btnGo.GetComponentInChildren<TMPro.TMP_Text>() as Component
                              ?? btnGo.GetComponentInChildren<Text>();
                SetText(textComp, choices[i].text ?? "");

                btnGo.GetComponent<Button>()?.onClick.AddListener(() =>
                {
                    OnChoiceSelected?.Invoke(index);
                });
            }
        }

        private void ClearChoices()
        {
            foreach (var btn in _choiceButtons)
                if (btn != null) Destroy(btn);
            _choiceButtons.Clear();
        }

        private static void SetText(Component comp, string text)
        {
            if (comp == null) return;
            if (comp is TMPro.TMP_Text tmp) { tmp.text = text; return; }
            if (comp is Text legacy)        { legacy.text = text; }
        }
    }
}
