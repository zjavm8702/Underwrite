using System;
using Underwrite.Global;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 게임 안에서 웹툰 한 장을 칸 단위로 보여 준다(2026-10-19 — 2일째 아침 웹툰 7장 「개미의 결심」).
    /// 칸마다 화면 가운데 액자에 그 칸만 잘라 띄우고, 클릭이나 스페이스로 넘긴다. 마지막 칸 뒤에 닫고 부른 쪽에 알린다.
    /// 인트로(IntroController)와 같은 방식이되 영지 위에 겹치는 가벼운 판이다.
    /// </summary>
    public sealed class UI_WebtoonPanels
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly VisualElement Frame;
        private readonly VisualElement Page;
        private readonly Label Hint;

        private Texture2D Texture;
        private int[] Bounds;
        private int Index;
        private Action Done;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_WebtoonPanels(VisualElement root)
        {
            Root = root;
            Overlay = new VisualElement();
            Overlay.AddToClassList("webtoon");
            Overlay.RegisterCallback<ClickEvent>(evt => Next());

            Frame = new VisualElement();
            Frame.AddToClassList("webtoon-frame");
            Frame.pickingMode = PickingMode.Ignore;
            Page = new VisualElement();
            Page.AddToClassList("webtoon-page");
            Page.pickingMode = PickingMode.Ignore;
            Frame.Add(Page);

            Hint = new Label(StringConstants.TEXT_WEBTOON_HINT);
            Hint.AddToClassList("webtoon-hint");
            Hint.pickingMode = PickingMode.Ignore;

            Overlay.Add(Frame);
            Overlay.Add(Hint);
            Frame.RegisterCallback<GeometryChangedEvent>(evt => Layout());
        }

        /// <param name="bounds">칸 경계 [위0, 아래0, 위1, 아래1, …] (그림 픽셀, 위 원점).</param>
        public void Open(Texture2D texture, int[] bounds, Action onDone)
        {
            Texture = texture;
            Bounds = bounds;
            Done = onDone;
            Index = 0;
            Page.style.backgroundImage = new StyleBackground(texture);

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
            Layout();
        }

        /// <summary>스페이스 등 키로 넘길 때 부른 쪽이 부른다.</summary>
        public void Next()
        {
            if (IsOpen == false)
            {
                return;
            }

            Index++;

            if (Index * 2 >= Bounds.Length)
            {
                Overlay.RemoveFromHierarchy();
                // 세로로 긴 원본 그림을 쥐고 있지 않게 놓는다. 내리는 것은 불러온 쪽이 한다.
                Page.style.backgroundImage = StyleKeyword.Null;
                Texture = null;
                var done = Done;
                Done = null;

                if (done != null)
                {
                    done();
                }

                return;
            }

            Layout();
        }

        // 액자는 그 칸의 비율로, 그림은 액자 폭에 맞춰 늘이고 칸의 위가 액자 위에 오게 올린다.
        private void Layout()
        {
            if (Texture == null || Bounds == null || Index * 2 + 1 >= Bounds.Length)
            {
                return;
            }

            var top = Bounds[Index * 2];
            var bottom = Bounds[Index * 2 + 1];
            var rootHeight = Root.resolvedStyle.height;
            var rootWidth = Root.resolvedStyle.width;

            if (float.IsNaN(rootHeight) == true || rootHeight <= 0f || float.IsNaN(rootWidth) == true)
            {
                return;
            }

            // 높이와 폭 둘 다에 들어가게 — 가로로 넓은 칸이 화면 밖으로 넘치지 않게.
            var scale = Mathf.Min(rootHeight * Constants.WEBTOON_FRAME_HEIGHT / (bottom - top),
                rootWidth * Constants.WEBTOON_FRAME_WIDTH / Texture.width);
            var frameHeight = (bottom - top) * scale;
            var frameWidth = Texture.width * scale;
            Frame.style.width = frameWidth;
            Frame.style.height = frameHeight;
            Page.style.width = frameWidth;
            Page.style.height = Texture.height * scale;
            Page.style.top = -top * scale;
        }
    }
}
