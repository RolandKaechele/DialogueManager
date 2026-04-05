#if DIALOGUEMANAGER_DOTWEEN
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// Optional bridge that adds DOTween-driven animations to the dialogue UI:
    /// the dialogue box fades in/out on sequence start/end, and portraits slide into view
    /// when a new node is shown.
    /// Enable define <c>DIALOGUEMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Assign <see cref="dialogueBoxGroup"/> to the <see cref="CanvasGroup"/> on the root of your
    /// dialogue panel and <see cref="leftPortraitImage"/> / <see cref="rightPortraitImage"/> to the
    /// respective portrait <see cref="Image"/> components.
    /// </para>
    /// </summary>
    [AddComponentMenu("DialogueManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenDialogueBridge : MonoBehaviour
    {
        [Header("Dialogue Box")]
        [Tooltip("CanvasGroup on the root of the dialogue panel. Faded in/out on sequence start/end.")]
        [SerializeField] private CanvasGroup dialogueBoxGroup;

        [Tooltip("Duration for the dialogue box to fade in when a sequence starts.")]
        [SerializeField] private float boxFadeInDuration = 0.2f;

        [Tooltip("Duration for the dialogue box to fade out when a sequence ends.")]
        [SerializeField] private float boxFadeOutDuration = 0.15f;

        [Tooltip("DOTween ease applied to dialogue box fade transitions.")]
        [SerializeField] private Ease boxFadeEase = Ease.OutQuad;

        [Header("Portraits")]
        [Tooltip("Left portrait Image — animated on portrait change.")]
        [SerializeField] private Image leftPortraitImage;

        [Tooltip("Right portrait Image — animated on portrait change.")]
        [SerializeField] private Image rightPortraitImage;

        [Tooltip("Horizontal slide offset (pixels) from which a newly shown portrait enters.")]
        [SerializeField] private float portraitSlideOffset = 40f;

        [Tooltip("Duration of the portrait slide-in animation.")]
        [SerializeField] private float portraitSlideDuration = 0.25f;

        [Tooltip("DOTween ease for portrait slide-in.")]
        [SerializeField] private Ease portraitSlideEase = Ease.OutBack;

        [Header("Choice Buttons")]
        [Tooltip("When true, each choice button in the choice container is punched on show.")]
        [SerializeField] private bool punchChoiceButtons = true;

        [Tooltip("Transform that holds dynamically spawned choice buttons (same as DialogueBoxController.choiceContainer).")]
        [SerializeField] private Transform choiceContainer;

        [Tooltip("Stagger delay between successive choice button punch animations.")]
        [SerializeField] private float choiceStagger = 0.05f;

        // -------------------------------------------------------------------------

        private DialogueManager _dm;

        private void Awake()
        {
            _dm = GetComponent<DialogueManager>() ?? FindFirstObjectByType<DialogueManager>();
            if (_dm == null) Debug.LogWarning("[DialogueManager/DotweenDialogueBridge] DialogueManager not found.");
        }

        private void OnEnable()
        {
            if (_dm == null) return;
            _dm.OnDialogueStarted   += OnDialogueStarted;
            _dm.OnDialogueCompleted += OnDialogueCompleted;
            _dm.OnNodeShown         += OnNodeShown;
        }

        private void OnDisable()
        {
            if (_dm == null) return;
            _dm.OnDialogueStarted   -= OnDialogueStarted;
            _dm.OnDialogueCompleted -= OnDialogueCompleted;
            _dm.OnNodeShown         -= OnNodeShown;
        }

        // -------------------------------------------------------------------------

        private void OnDialogueStarted(string sequenceId)
        {
            if (dialogueBoxGroup != null)
            {
                DOTween.Kill(dialogueBoxGroup);
                dialogueBoxGroup.alpha = 0f;
                dialogueBoxGroup.DOFade(1f, boxFadeInDuration).SetEase(boxFadeEase);
            }
        }

        private void OnDialogueCompleted(string sequenceId)
        {
            if (dialogueBoxGroup != null)
            {
                DOTween.Kill(dialogueBoxGroup);
                dialogueBoxGroup.DOFade(0f, boxFadeOutDuration).SetEase(boxFadeEase);
            }
        }

        private void OnNodeShown(string sequenceId, string nodeId)
        {
            AnimatePortraitIn(leftPortraitImage,  -portraitSlideOffset);
            AnimatePortraitIn(rightPortraitImage,  portraitSlideOffset);

            if (punchChoiceButtons && choiceContainer != null)
                PunchChoiceButtons();
        }

        // -------------------------------------------------------------------------

        private void AnimatePortraitIn(Image portrait, float xOffset)
        {
            if (portrait == null || !portrait.enabled) return;
            var rt = portrait.rectTransform;
            DOTween.Kill(rt);
            Vector2 dest = rt.anchoredPosition;
            rt.anchoredPosition = dest + new Vector2(xOffset, 0f);
            rt.DOAnchorPos(dest, portraitSlideDuration).SetEase(portraitSlideEase);
        }

        private void PunchChoiceButtons()
        {
            for (int i = 0; i < choiceContainer.childCount; i++)
            {
                var child = choiceContainer.GetChild(i);
                float delay = i * choiceStagger;
                DOTween.Kill(child);
                child.DOPunchScale(Vector3.one * 0.08f, 0.3f, 5, 0.3f).SetDelay(delay);
            }
        }
    }
}
#else
namespace DialogueManager.Runtime
{
    /// <summary>No-op stub — enable define <c>DIALOGUEMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("DialogueManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenDialogueBridge : UnityEngine.MonoBehaviour { }
}
#endif
