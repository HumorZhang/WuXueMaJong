using System;
using UnityEngine;
using UnityEngine.UI;

namespace WuXueMaJong.Client
{
    /// <summary>运行时 UI 基础工具：字体、颜色、控件创建。</summary>
    public static class UiKit
    {
        public static readonly Color TableColor = new Color(0.06f, 0.22f, 0.15f, 1f);
        public static readonly Color PanelColor = new Color(0.10f, 0.30f, 0.20f, 0.92f);
        public static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.62f);
        public static readonly Color Accent = new Color(0.96f, 0.80f, 0.28f, 1f);
        public static readonly Color TextLight = new Color(0.96f, 0.96f, 0.90f, 1f);
        public static readonly Color TextDim = new Color(0.82f, 0.86f, 0.80f, 1f);

        public static readonly Color TileFace = new Color(0.97f, 0.95f, 0.88f, 1f);
        public static readonly Color TileBorder = new Color(0.20f, 0.18f, 0.12f, 0.95f);
        public static readonly Color WanColor = new Color(0.72f, 0.14f, 0.14f, 1f);
        public static readonly Color TongColor = new Color(0.10f, 0.34f, 0.66f, 1f);
        public static readonly Color TiaoColor = new Color(0.10f, 0.48f, 0.20f, 1f);
        public static readonly Color HonorColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        public static readonly Color RedDragonFace = new Color(0.80f, 0.14f, 0.14f, 1f);
        public static readonly Color GreenDragonFace = new Color(0.16f, 0.55f, 0.28f, 1f);
        public static readonly Color BackFace = new Color(0.12f, 0.28f, 0.42f, 1f);
        public static readonly Color SelectedOutline = new Color(1f, 0.86f, 0.25f, 1f);

        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null)
                {
                    return _font;
                }

                _font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "SimSun" },
                    32);

                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
            }
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rect = CreateRect(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.text = content;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string label,
            float width,
            float height,
            Action onClick,
            int fontSize = 22)
        {
            RectTransform rect = CreateRect(parent, "Button_" + label);
            rect.sizeDelta = new Vector2(width, height);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = Accent;
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.78f);
            colors.pressedColor = new Color(0.82f, 0.72f, 0.38f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            button.colors = colors;

            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(rect, "Label", label, fontSize, new Color(0.16f, 0.12f, 0.04f));
            Stretch(text.rectTransform);

            return button;
        }

        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public static float ButtonWidth(string label)
        {
            return Mathf.Max(96f, 36f + label.Length * 24f);
        }
    }
}
