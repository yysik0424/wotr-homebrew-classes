using Kingmaker;
using Kingmaker.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HomebrewClasses.Loader
{
    internal sealed class CombatPreviewPanel : MonoBehaviour
    {
        const float HorizontalPadding = 8f;
        const float VerticalPadding = 3f;
        const float Gap = 4f;
        RectTransform log, box;
        Canvas canvas;
        TextMeshProUGUI label;
        float measuredWidth = -1f;
        bool textChanged;

        internal static CombatPreviewPanel Create(RectTransform anchor, TMP_FontAsset font)
        {
            var obj = new GameObject("Homebrew combat preview", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var box = (RectTransform)obj.transform;
            box.SetParent(anchor, false);
            box.anchorMin = new Vector2(0, 1);
            box.anchorMax = Vector2.one;
            box.pivot = new Vector2(0.5f, 0);
            box.sizeDelta = Vector2.zero;
            box.anchoredPosition = new Vector2(0, Gap);

            var background = obj.GetComponent<Image>();
            background.raycastTarget = false;
            background.color = new Color(0.06f, 0.05f, 0.04f, 0.9f);
            var group = obj.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var caption = new GameObject("Checks", typeof(RectTransform), typeof(TextMeshProUGUI));
            var inner = (RectTransform)caption.transform;
            inner.SetParent(box, false);
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(HorizontalPadding, VerticalPadding);
            inner.offsetMax = new Vector2(-HorizontalPadding, -VerticalPadding);
            var label = caption.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.enableAutoSizing = false;
            label.enableWordWrapping = true;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.richText = false;
            label.raycastTarget = false;

            var panel = obj.AddComponent<CombatPreviewPanel>();
            panel.log = anchor;
            panel.canvas = anchor.GetComponentInParent<Canvas>(true)?.rootCanvas;
            panel.box = box;
            panel.label = label;
            return panel;
        }

        internal void Show(string text)
        {
            if (label.text != text)
            {
                label.text = text;
                textChanged = true;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        // The base log font is 20 on PC and 30 on a controller, before the game's text scale.
        internal static float FontSize(bool mouse, float textScale) => (mouse ? 16f : 24f) * textScale;

        internal static float BottomOffset(float roomAboveLog, float height) =>
            Mathf.Min(Gap, roomAboveLog - height - Gap);

        void LateUpdate()
        {
            if (canvas == null) return;
            float width = box.rect.width - 2f * HorizontalPadding;
            if (width <= 0f) return;
            float fontSize = FontSize(Game.Instance.IsControllerMouse, SettingsRoot.Game.Main.FontSize);
            if (textChanged || width != measuredWidth || label.fontSize != fontSize)
            {
                label.fontSize = fontSize;
                float height = Mathf.Ceil(label.GetPreferredValues(label.text, width, Mathf.Infinity).y) + 2f * VerticalPadding;
                box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                measuredWidth = width;
                textChanged = false;
            }

            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var top = new Vector2(canvas.pixelRect.center.x, Mathf.Min(canvas.pixelRect.yMax, Screen.safeArea.yMax));
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(log, top, camera, out var localTop))
            {
                float y = BottomOffset(localTop.y - log.rect.yMax, box.rect.height);
                if (box.anchoredPosition.y != y) box.anchoredPosition = new Vector2(0, y);
            }
        }
    }
}
