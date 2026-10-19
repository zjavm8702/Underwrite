using System.Collections.Generic;
using Underwrite.Global;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 장소 위에 뜨는 이름표(2026-10-17 사용자 요청 — 어떤 건물인지 직관적으로).
    /// 다가가지 않아도 화면에 들어오면 이름과 할 수 있는 일이 보인다. 대화 중에는 감춘다.
    /// </summary>
    public static class WorldSigns
    {
        private sealed class Sign
        {
            public Transform Anchor;
            public VisualElement Plate;
            public string Title;
        }

        private static readonly List<Sign> Signs = new List<Sign>();
        // 이름표가 덮으면 안 되는 화면 칸(할 일·상태 줄). 닿으면 이름표를 그 아래로 내린다(2026-10-19 사장님 — 「영주관」이 할 일과 겹침).
        private static readonly List<VisualElement> KeepOut = new List<VisualElement>();
        private static VisualElement Layer;
        private static bool IsHighlightKnown;
        private static string HighlightTitle;

        public static void Attach(VisualElement root)
        {
            Signs.Clear();
            KeepOut.Clear();
            IsHighlightKnown = false;
            Layer = new VisualElement();
            Layer.name = "world-signs";
            Layer.pickingMode = PickingMode.Ignore;
            Layer.style.position = Position.Absolute;
            Layer.style.left = 0f;
            Layer.style.right = 0f;
            Layer.style.top = 0f;
            Layer.style.bottom = 0f;
            // 말풍선·대화창·단추보다 아래.
            root.Insert(0, Layer);
        }

        public static void Add(Transform anchor, string title, string subtitle)
        {
            if (Layer == null || anchor == null)
            {
                return;
            }

            var plate = new VisualElement();
            plate.pickingMode = PickingMode.Ignore;
            plate.AddToClassList("world-sign");

            var name = new Label(title);
            name.pickingMode = PickingMode.Ignore;
            name.AddToClassList("world-sign-title");
            plate.Add(name);

            if (string.IsNullOrEmpty(subtitle) == false)
            {
                var sub = new Label(subtitle);
                sub.pickingMode = PickingMode.Ignore;
                sub.AddToClassList("world-sign-sub");
                plate.Add(sub);
            }

            Layer.Add(plate);
            Signs.Add(new Sign { Anchor = anchor, Plate = plate, Title = title });
        }

        /// <summary>이름표가 덮지 않을 화면 칸을 등록한다.</summary>
        public static void KeepClear(VisualElement element)
        {
            if (element != null && KeepOut.Contains(element) == false)
            {
                KeepOut.Add(element);
            }
        }

        /// <summary>
        /// 그 이름의 이름표만 「다음에 갈 곳」으로 밝힌다. null이면 모두 끈다(프롤로그 순찰 — 2026-10-18).
        /// </summary>
        public static void Highlight(string title)
        {
            // 매 프레임 불린다. 같은 이름이면 할 일이 없다.
            if (IsHighlightKnown == true && HighlightTitle == title)
            {
                return;
            }

            IsHighlightKnown = true;
            HighlightTitle = title;

            foreach (var sign in Signs)
            {
                sign.Plate.EnableInClassList("world-sign--target", title != null && sign.Title == title);
            }
        }

        /// <summary>문 앞 자리에서 <paramref name="height"/>유닛 위에 세운다. 영지 화면이 매 프레임 부른다.</summary>
        public static void Tick(bool hidden, float height)
        {
            if (Layer == null || Layer.panel == null)
            {
                return;
            }

            Layer.style.display = hidden == true ? DisplayStyle.None : DisplayStyle.Flex;
            var camera = Camera.main;

            if (hidden == true || camera == null)
            {
                return;
            }

            for (var i = 0; i < Signs.Count; i++)
            {
                var sign = Signs[i];

                if (sign.Anchor == null)
                {
                    continue;
                }

                var screen = camera.WorldToScreenPoint(sign.Anchor.position + Vector3.up * height);
                var panel = RuntimePanelUtils.ScreenToPanel(Layer.panel,
                    new Vector2(screen.x, Screen.height - screen.y));
                var width = sign.Plate.resolvedStyle.width;
                var plateHeight = sign.Plate.resolvedStyle.height;
                width = float.IsNaN(width) == true ? 0f : width;
                plateHeight = float.IsNaN(plateHeight) == true ? 0f : plateHeight;
                var left = panel.x - width * 0.5f;
                var top = BelowKeepOut(left, panel.y - plateHeight, width, plateHeight);
                sign.Plate.style.left = left;
                sign.Plate.style.top = top;
            }
        }

        // 화면 칸에 닿으면 그 칸 바로 아래 높이를 준다. 이름표 층은 화면 전체를 덮으므로 worldBound를 그대로 비교한다.
        private static float BelowKeepOut(float left, float top, float width, float height)
        {
            for (var i = 0; i < KeepOut.Count; i++)
            {
                var element = KeepOut[i];

                if (element.panel == null || element.resolvedStyle.display == DisplayStyle.None)
                {
                    continue;
                }

                var rect = element.worldBound;

                if (left < rect.xMax && left + width > rect.xMin && top < rect.yMax && top + height > rect.yMin)
                {
                    top = rect.yMax + Constants.SIGN_HUD_GAP;
                }
            }

            return top;
        }
    }
}
