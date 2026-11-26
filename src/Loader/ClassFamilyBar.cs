using System;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Owlcat.Runtime.UI.VirtualListSystem;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace HomebrewClasses.Loader
{
    internal sealed class ClassFamilyBar : MonoBehaviour
    {
        // Two rows of three buttons, in canvas units so they follow the game's UI scale.
        const float Height = 62f;
        readonly Button[] buttons = new Button[ClassFamilies.English.Length];
        readonly TextMeshProUGUI[] captions = new TextMeshProUGUI[ClassFamilies.English.Length];
        RectTransform viewport;
        GameObject toolbar;
        CharGenClassSelectorPCView view;
        VirtualListComponent scroll;
        object boundGroup;
        ClassFamilyList list;
        ClassFamily? open;
        IDisposable selectionChanges;
        bool reserved;
        internal bool Filtering { get { return toolbar != null && list != null; } }
        internal bool Matches(object group) { return group != null && ReferenceEquals(group, boundGroup); }
        internal bool Shows(CharGenClassSelectorItemVM row)
        {
            ClassFamily family;
            return !list.FamilyOf(row, out family) || open == family;
        }
        internal void Bind(CharGenClassSelectorPCView selector, object group, CharGenClassPhaseVM phase, ClassFamilyList families, RectTransform area, VirtualListComponent virtualList)
        {
            ResetView();
            if (area == null || area.parent == null) throw new InvalidOperationException("Class selector viewport is missing.");
            var fontSource = selector.SlotPrefabs[0].GetComponentInChildren<TextMeshProUGUI>(true);
            if (fontSource == null) throw new InvalidOperationException("Class selector font is missing.");
            view = selector;
            scroll = virtualList;
            viewport = area;
            boundGroup = group;
            list = families;
            open = families.First;

            toolbar = new GameObject("Class families", typeof(RectTransform), typeof(LayoutElement));
            toolbar.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)toolbar.transform;
            rect.SetParent(area.parent, false);
            rect.anchorMin = new Vector2(area.anchorMin.x, area.anchorMax.y);
            rect.anchorMax = area.anchorMax;
            rect.offsetMin = new Vector2(area.offsetMin.x, area.offsetMax.y - Height);
            rect.offsetMax = area.offsetMax;
            for (int i = 0; i < buttons.Length; i++) CreateButton(rect, i, fontSource);
            Reserve(true);
            UpdateButtons();
            selectionChanges = phase.SelectedClassVM.Skip(1).Subscribe(row =>
            {
                ClassFamily family;
                if (list != null && list.FamilyOf(row, out family) && open != family) SetOpen(family);
            });
        }
        void CreateButton(RectTransform parent, int i, TextMeshProUGUI source)
        {
            var family = (ClassFamily)i;
            var obj = new GameObject(ClassFamilies.English[i], typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            int col = i % 3, row = i / 3;
            rect.anchorMin = new Vector2(col / 3f, (1 - row) / 2f);
            rect.anchorMax = new Vector2((col + 1) / 3f, (2 - row) / 2f);
            rect.offsetMin = new Vector2(1, 1);
            rect.offsetMax = new Vector2(-1, -1);
            var button = buttons[i] = obj.GetComponent<Button>();
            button.targetGraphic = obj.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => Click(family));

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)label.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(2, 0);
            textRect.offsetMax = new Vector2(-2, 0);
            var caption = captions[i] = label.GetComponent<TextMeshProUGUI>();
            caption.font = source.font;
            caption.fontSharedMaterial = source.fontSharedMaterial;
            caption.alignment = TextAlignmentOptions.Center;
            caption.enableWordWrapping = false;
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 10;
            caption.fontSizeMax = 19;
            caption.raycastTarget = false;
        }
        void Click(ClassFamily family)
        {
            if (!Filtering || !list.Available[(int)family]) return;
            SetOpen(ClassFamilies.Toggle(open, family));
        }
        void SetOpen(ClassFamily? family)
        {
            try
            {
                open = family;
                UpdateButtons();
                ClassPicker.Refresh.Invoke(view, null);
                if (scroll.ScrollController != null) scroll.ScrollController.ForceScrollToTop();
            }
            catch (Exception e) { ClassPicker.Report(e); }
        }
        void UpdateButtons()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var family = (ClassFamily)i;
                bool active = open == family, exists = list.Available[i];
                buttons[i].interactable = exists;
                buttons[i].image.color = active ? new Color(.40f, .30f, .16f) : new Color(.15f, .13f, .12f);
                captions[i].color = exists ? new Color(1f, .94f, .82f) : new Color(.65f, .62f, .56f);
                captions[i].text = (exists ? (active ? "− " : "+ ") : "") + list.Label(family);
            }
        }
        void Reserve(bool value)
        {
            if (viewport == null || reserved == value) return;
            viewport.offsetMax += new Vector2(0, value ? -Height : Height);
            reserved = value;
        }
        void OnDisable() { Reserve(false); if (toolbar != null) toolbar.SetActive(false); }
        void OnEnable() { if (toolbar != null) { Reserve(true); toolbar.SetActive(true); } }
        void OnDestroy() { ResetView(); }
        internal void ResetView()
        {
            if (selectionChanges != null) selectionChanges.Dispose();
            selectionChanges = null;
            Reserve(false);
            reserved = false;
            if (toolbar != null) { toolbar.SetActive(false); Destroy(toolbar); }
            toolbar = null;
            viewport = null;
            scroll = null;
            view = null;
            list = null;
            boundGroup = null;
        }
    }
}
