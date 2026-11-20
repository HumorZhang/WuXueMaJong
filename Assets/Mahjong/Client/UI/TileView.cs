using System;
using UnityEngine;
using UnityEngine.UI;
using WuXueMaJong.Core;

namespace WuXueMaJong.Client
{
    /// <summary>一张牌的 UI 表现：彩色底 + 文字，可选点击。</summary>
    public sealed class TileView : MonoBehaviour
    {
        public int TileId { get; private set; }
        public int Kind { get; private set; }

        private Image _outline;
        private Text _label;
        private Text _badge;

        public static TileView Create(
            Transform parent,
            int tileId,
            int jokerKind,
            Action<int> onClick,
            float width,
            float height,
            int fontSize = 28,
            bool interactive = true)
        {
            int kind = TileCatalog.KindOf(tileId);

            RectTransform root = UiKit.CreateRect(parent, "Tile_" + tileId);
            root.sizeDelta = new Vector2(width, height);

            var outline = root.gameObject.AddComponent<Image>();
            outline.color = UiKit.TileBorder;
            outline.raycastTarget = interactive;

            Image face = UiKit.CreateImage(root, "Face", FaceColor(kind));
            UiKit.Stretch(face.rectTransform, 2f);

            Text label = UiKit.CreateText(face.rectTransform, "Label",
                TileCatalog.DisplayName(kind), fontSize, TextColor(kind));
            UiKit.Stretch(label.rectTransform);

            Text badge = null;
            if (kind == jokerKind)
            {
                badge = UiKit.CreateText(face.rectTransform, "Badge", "癞", Mathf.Max(12, fontSize - 8), UiKit.Accent);
                badge.rectTransform.anchorMin = new Vector2(1f, 1f);
                badge.rectTransform.anchorMax = new Vector2(1f, 1f);
                badge.rectTransform.pivot = new Vector2(1f, 1f);
                badge.rectTransform.anchoredPosition = new Vector2(-3f, -2f);
                badge.rectTransform.sizeDelta = new Vector2(30f, 24f);
                badge.alignment = TextAnchor.UpperRight;
            }

            var view = root.gameObject.AddComponent<TileView>();
            view.TileId = tileId;
            view.Kind = kind;
            view._outline = outline;
            view._label = label;
            view._badge = badge;

            if (interactive && onClick != null)
            {
                var button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                int captured = tileId;
                button.onClick.AddListener(() => onClick(captured));
            }

            return view;
        }

        public static TileView CreateBack(Transform parent, float width, float height)
        {
            RectTransform root = UiKit.CreateRect(parent, "TileBack");
            root.sizeDelta = new Vector2(width, height);

            var outline = root.gameObject.AddComponent<Image>();
            outline.color = new Color(0.05f, 0.12f, 0.18f, 1f);
            outline.raycastTarget = false;

            Image face = UiKit.CreateImage(root, "Face", UiKit.BackFace);
            UiKit.Stretch(face.rectTransform, 2f);

            var view = root.gameObject.AddComponent<TileView>();
            view.TileId = -1;
            view.Kind = -1;
            view._outline = outline;
            return view;
        }

        public void SetSelected(bool selected)
        {
            if (_outline == null)
            {
                return;
            }

            _outline.color = selected ? UiKit.SelectedOutline : UiKit.TileBorder;
            transform.localScale = selected ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
        }

        private static Color FaceColor(int kind)
        {
            if (kind == TileCatalog.RedCenter)
            {
                return UiKit.RedDragonFace;
            }

            if (kind == TileCatalog.GreenDragon)
            {
                return UiKit.GreenDragonFace;
            }

            return UiKit.TileFace;
        }

        private static Color TextColor(int kind)
        {
            if (kind == TileCatalog.RedCenter || kind == TileCatalog.GreenDragon)
            {
                return Color.white;
            }

            if (TileCatalog.IsSuited(kind))
            {
                switch (TileCatalog.SuitOf(kind))
                {
                    case 0: return UiKit.WanColor;
                    case 1: return UiKit.TongColor;
                    default: return UiKit.TiaoColor;
                }
            }

            return UiKit.HonorColor;
        }
    }
}
