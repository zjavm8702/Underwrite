using System.Collections.Generic;
using Underwrite.Village;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 필드 캐릭터 머리 위의 말풍선과 감정 표시. 화면 위 UI 한 겹에 그리고 매 프레임 머리 위치를 따라간다.
    ///
    /// 월드에 글자를 그리려면 다른 글꼴 체계를 들여야 한다. 화면 UI에 그리면 대화창과 같은 글꼴을 쓴다.
    /// 한 사람에 하나만 뜬다. 새로 말하면 앞의 것을 바꾼다.
    /// </summary>
    public static class WorldBubbles
    {
        private sealed class Bubble
        {
            public CharacterActor Actor;
            public Label Label;
            public float Until;
        }

        private static readonly List<Bubble> Bubbles = new List<Bubble>();
        private static VisualElement Layer;

        /// <summary>그릴 자리를 붙인다. 화면이 준비되면 한 번 부른다. 씬이 바뀌면 다시 붙인다.</summary>
        public static void Attach(VisualElement root)
        {
            Bubbles.Clear();
            Layer = new VisualElement();
            Layer.name = "world-bubbles";
            Layer.pickingMode = PickingMode.Ignore;
            Layer.style.position = Position.Absolute;
            Layer.style.left = 0f;
            Layer.style.right = 0f;
            Layer.style.top = 0f;
            Layer.style.bottom = 0f;
            // 대화창보다 아래, 영지 화면 단추보다 아래에 둔다.
            root.Insert(0, Layer);
        }

        public static void Show(CharacterActor actor, string text, float seconds, bool isEmote)
        {
            if (Layer == null || actor == null)
            {
                return;
            }

            Bubble bubble = null;

            for (var i = 0; i < Bubbles.Count; i++)
            {
                if (Bubbles[i].Actor == actor)
                {
                    bubble = Bubbles[i];
                    break;
                }
            }

            if (bubble == null)
            {
                bubble = new Bubble { Actor = actor, Label = new Label() };
                bubble.Label.pickingMode = PickingMode.Ignore;
                bubble.Label.AddToClassList("world-bubble");
                Layer.Add(bubble.Label);
                Bubbles.Add(bubble);
            }

            bubble.Label.text = text;
            bubble.Label.EnableInClassList("world-bubble--emote", isEmote);
            bubble.Until = Time.time + seconds;
        }

        /// <summary>
        /// 위치를 맞추고 시간이 다 된 것을 걷는다. 영지 화면이 매 프레임 부른다.
        /// <paramref name="hidden"/>이 참이면(대화 중) 모두 감춘다 — 대화창 뒤에 비쳐 보이면 산만하다.
        /// </summary>
        public static void Tick(bool hidden)
        {
            if (Layer == null || Layer.panel == null)
            {
                return;
            }

            Layer.style.display = hidden == true ? DisplayStyle.None : DisplayStyle.Flex;
            var camera = Camera.main;

            for (var i = Bubbles.Count - 1; i >= 0; i--)
            {
                var bubble = Bubbles[i];

                if (bubble.Actor == null || Time.time > bubble.Until)
                {
                    bubble.Label.RemoveFromHierarchy();
                    Bubbles.RemoveAt(i);
                    continue;
                }

                if (camera == null)
                {
                    continue;
                }

                var screen = camera.WorldToScreenPoint(bubble.Actor.HeadPoint);
                // 화면 좌표는 아래가 0, 패널 좌표는 위가 0이다.
                var panel = RuntimePanelUtils.ScreenToPanel(Layer.panel,
                    new Vector2(screen.x, Screen.height - screen.y));
                var width = bubble.Label.resolvedStyle.width;
                var height = bubble.Label.resolvedStyle.height;
                bubble.Label.style.left = panel.x - (float.IsNaN(width) == true ? 0f : width * 0.5f);
                bubble.Label.style.top = panel.y - (float.IsNaN(height) == true ? 0f : height) - 6f;
            }
        }
    }
}
