using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RecipeSearch
{
    /// <summary>
    /// Adds a search box above the crafting recipe list. The recipe scroll view is shrunk from the top to make room.
    /// </summary>
    public static class SearchUi
    {
        private const float SearchHeight = 26f;
        private const float Gap = 3f;

        private static InventoryGui _gui;
        private static RectTransform _bar;
        private static TMP_InputField _input;
        private static readonly List<(RectTransform rt, Vector2 offsetMax)> _shrunk = new List<(RectTransform, Vector2)>();
        private static float _origBaseSize;

        // Frame until which game hotkeys are suppressed (covers the frame the field loses focus on Escape/Enter).
        private static int _blockInputUntilFrame = -1;
        public static bool BlockGameInput => Time.frameCount <= _blockInputUntilFrame;

        public static void Build(InventoryGui gui)
        {
            Destroy();
            _gui = gui;

            var scroll = gui.m_recipeListRoot.GetComponentInParent<ScrollRect>();
            var target = scroll != null ? (RectTransform)scroll.transform : (RectTransform)gui.m_recipeListRoot.parent;
            var parent = (RectTransform)target.parent;

            float barHeight = SearchHeight + Gap * 2f;

            Shrink(target, barHeight);
            var sb = gui.m_recipeListScroll != null ? (RectTransform)gui.m_recipeListScroll.transform : null;
            if (sb != null && !sb.IsChildOf(target)) Shrink(sb, barHeight);

            _origBaseSize = gui.m_recipeListBaseSize;
            gui.m_recipeListBaseSize = Mathf.Max(0f, _origBaseSize - barHeight);

            var barGo = new GameObject("RecipeSearch_Bar", typeof(RectTransform));
            _bar = (RectTransform)barGo.transform;
            _bar.SetParent(parent, false);
            _bar.anchorMin = new Vector2(target.anchorMin.x, target.anchorMax.y);
            _bar.anchorMax = new Vector2(target.anchorMax.x, target.anchorMax.y);
            _bar.pivot = new Vector2(0.5f, 1f);
            _bar.offsetMin = new Vector2(target.offsetMin.x, target.offsetMax.y + Gap);
            _bar.offsetMax = new Vector2(target.offsetMax.x, target.offsetMax.y + barHeight - Gap);
            _bar.SetSiblingIndex(target.GetSiblingIndex() + 1);

            var font = gui.m_tabCraft.GetComponentInChildren<TMP_Text>(true)?.font;
            _input = CreateInput(_bar, font);
            var inputRt = (RectTransform)_input.transform;
            inputRt.anchorMin = Vector2.zero;
            inputRt.anchorMax = Vector2.one;
            inputRt.offsetMin = Vector2.zero;
            inputRt.offsetMax = Vector2.zero;

            _input.SetTextWithoutNotify(RecipeQuery.SearchText);
        }

        public static void Destroy()
        {
            foreach (var (rt, offsetMax) in _shrunk)
                if (rt != null) rt.offsetMax = offsetMax;
            _shrunk.Clear();
            if (_gui != null) _gui.m_recipeListBaseSize = _origBaseSize;
            if (_bar != null) Object.Destroy(_bar.gameObject);
            _bar = null; _input = null; _gui = null;
        }

        public static void OnInventoryHidden()
        {
            if (Plugin.ClearSearchOnClose.Value && _input != null)
            {
                _input.SetTextWithoutNotify(string.Empty);
                RecipeQuery.SearchText = string.Empty;
            }
            if (_input != null && _input.isFocused) _input.DeactivateInputField();
            RecipeQuery.ClearCache(); // picks up language changes / modded recipes next time
        }

        /// <summary>Called every frame from InventoryGui.Update.</summary>
        public static void Tick()
        {
            if (_input != null && _input.isFocused) _blockInputUntilFrame = Time.frameCount + 1;
        }

        private static void Refresh()
        {
            if (_gui == null || Player.m_localPlayer == null || !InventoryGui.IsVisible()) return;
            _gui.UpdateCraftingPanel(false);
        }

        private static void Shrink(RectTransform rt, float amount)
        {
            _shrunk.Add((rt, rt.offsetMax));
            rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y - amount);
        }

        private static TMP_InputField CreateInput(Transform parent, TMP_FontAsset font)
        {
            var go = new GameObject("Search", typeof(RectTransform), typeof(Image));
            go.SetActive(false); // configure before TMP_InputField.OnEnable runs
            go.transform.SetParent(parent, false);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            var areaRt = (RectTransform)area.transform;
            areaRt.SetParent(go.transform, false);
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(8f, 2f);
            areaRt.offsetMax = new Vector2(-8f, -2f);

            var placeholder = CreateText(areaRt, "Placeholder", font);
            placeholder.text = "Search name, ingredient or type...";
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);

            var text = CreateText(areaRt, "Text", font);
            text.color = Color.white;

            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = font;
            input.pointSize = 16f;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 64;
            input.caretColor = Color.white;
            input.customCaretColor = true;
            input.onFocusSelectAll = true;
            input.restoreOriginalTextOnEscape = false;
            input.onValueChanged.AddListener(new UnityAction<string>(OnSearchChanged));

            go.SetActive(true);
            return input;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = 16f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false;
            return t;
        }

        private static void OnSearchChanged(string value)
        {
            RecipeQuery.SearchText = value ?? string.Empty;
            Refresh();
        }
    }
}
