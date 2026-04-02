using UnityEngine;
using UnityEngine.UI;

namespace DialogueManager.Runtime
{
    /// <summary>
    /// <b>PortraitController</b> manages left and right character portrait display slots.
    /// </summary>
    [AddComponentMenu("DialogueManager/Portrait Controller")]
    [DisallowMultipleComponent]
    public class PortraitController : MonoBehaviour
    {
        [Tooltip("Left-side portrait Image component.")]
        [SerializeField] private Image leftPortrait;

        [Tooltip("Right-side portrait Image component.")]
        [SerializeField] private Image rightPortrait;

        /// <summary>
        /// Load a sprite from <paramref name="resourcePath"/> and display it on the specified side.
        /// </summary>
        /// <param name="resourcePath">Resources-relative path (without extension).</param>
        /// <param name="side"><c>"left"</c> or <c>"right"</c>.</param>
        public void ShowPortrait(string resourcePath, string side = "left")
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[PortraitController] Portrait not found at Resources/{resourcePath}");
                return;
            }

            var target = side == "right" ? rightPortrait : leftPortrait;
            if (target == null) return;

            target.sprite  = sprite;
            target.enabled = true;
        }

        /// <summary>Hide the portrait on the specified side.</summary>
        public void HidePortrait(string side = "left")
        {
            var target = side == "right" ? rightPortrait : leftPortrait;
            if (target != null) target.enabled = false;
        }

        /// <summary>Hide both portrait slots.</summary>
        public void HideAll()
        {
            if (leftPortrait  != null) leftPortrait.enabled  = false;
            if (rightPortrait != null) rightPortrait.enabled = false;
        }
    }
}
