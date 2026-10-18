using System;
using Underwrite.Global;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 장면 전환 제목 카드(2026-10-18 사장님 — 튜토리얼 연출 강화). 화면이 어두워지며 큰 글씨가 떴다가 걷힌다.
    /// 어두운 동안(<c>onDark</c>) 시계를 넘기는 등 보이면 어색한 일을 한다.
    /// 시간은 부르는 쪽이 매 프레임 <see cref="Tick"/>으로 흘린다 — UI 예약 실행은 지연을 지키지 않을 때가 있었다.
    /// </summary>
    public sealed class UI_TitleCard
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly Label Title;
        private readonly Label Subtitle;

        private float Elapsed;
        private float Fade;
        private float Hold;
        private bool DarkFired;
        private Action OnDark;
        private Action OnDone;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_TitleCard(VisualElement root)
        {
            Root = root;
            Overlay = new VisualElement();
            Overlay.AddToClassList("title-card");
            Overlay.pickingMode = PickingMode.Position;

            Title = new Label();
            Title.AddToClassList("title-card-title");
            Subtitle = new Label();
            Subtitle.AddToClassList("title-card-sub");
            Overlay.Add(Title);
            Overlay.Add(Subtitle);
        }

        /// <summary>어두워짐 → 글씨 → 걷힘. 가장 어두울 때 <paramref name="onDark"/>, 다 걷히면 <paramref name="onDone"/>.</summary>
        public void Play(string title, string subtitle, Action onDark, Action onDone)
        {
            Begin(title, subtitle, Constants.TITLE_CARD_FADE_SECONDS, Constants.TITLE_CARD_HOLD_SECONDS, onDark, onDone);
        }

        /// <summary>글씨 없는 짧은 암전. 장소를 옮길 때 쓴다.</summary>
        public void Blink(Action onDark, Action onDone)
        {
            Begin(string.Empty, null, Constants.BLINK_FADE_SECONDS, Constants.BLINK_HOLD_SECONDS, onDark, onDone);
        }

        private void Begin(string title, string subtitle, float fade, float hold, Action onDark, Action onDone)
        {
            Fade = fade;
            Hold = hold;
            Title.text = title;
            Subtitle.text = subtitle ?? string.Empty;
            Subtitle.style.display = string.IsNullOrEmpty(subtitle) == true ? DisplayStyle.None : DisplayStyle.Flex;
            OnDark = onDark;
            OnDone = onDone;
            Elapsed = 0f;
            DarkFired = false;
            Overlay.style.opacity = 0f;

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
        }

        /// <summary>매 프레임 실시간으로 흘린다.</summary>
        public void Tick(float delta)
        {
            if (IsOpen == false)
            {
                return;
            }

            Elapsed += delta;
            var fade = Fade;
            var hold = Hold;

            if (Elapsed < fade)
            {
                Overlay.style.opacity = Elapsed / fade;
                return;
            }

            if (DarkFired == false)
            {
                DarkFired = true;
                Overlay.style.opacity = 1f;
                Fire(ref OnDark);
            }

            if (Elapsed < fade + hold)
            {
                return;
            }

            var left = fade * 2f + hold - Elapsed;

            if (left > 0f)
            {
                Overlay.style.opacity = Mathf.Clamp01(left / fade);
                return;
            }

            Overlay.RemoveFromHierarchy();
            Fire(ref OnDone);
        }

        private static void Fire(ref Action action)
        {
            var callback = action;
            action = null;

            if (callback != null)
            {
                callback();
            }
        }
    }
}
