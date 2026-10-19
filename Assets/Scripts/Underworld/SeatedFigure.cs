using Underwrite.Enumeration;
using Underwrite.Village;
using UnityEngine;

namespace Underwrite.Underworld
{
    /// <summary>
    /// 지하 방에 앉은 투자꾼·카운터 뒤 정보상(2026-10-19, Codex 지하 방 사람들 v2). 걷지 않는다.
    ///
    /// 평소에는 숨쉬기(Idle)를 되풀이하고, 시장이 크게 움직이면 한 번 반응한 뒤(투자꾼: 환호/머리 감싸기,
    /// 정보상: 손짓/끄덕임) 다시 숨쉬기로 돌아간다. 방향이 하나라 Animator 없이 프레임만 넘긴다(StationNpc와 같은 방식).
    /// 시간은 실제 시간으로 흐른다 — 거래판을 멈춰도 사람은 숨을 쉰다.
    /// </summary>
    public sealed class SeatedFigure : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer Body;
        [SerializeField] private StationClip Idle = new StationClip();

        [Tooltip("오를 때의 반응(투자꾼 Cheer · 정보상 Talk).")]
        [SerializeField] private StationClip Cheer = new StationClip();

        [Tooltip("내릴 때의 반응(투자꾼 Groan · 정보상 Nod).")]
        [SerializeField] private StationClip Groan = new StationClip();

        private StationClip Current;
        private int Frame;
        private float FrameLeft;

        /// <summary>에디터 도구가 만들 때 그림을 넣는다.</summary>
        public void Configure(SpriteRenderer body, StationClip idle, StationClip cheer, StationClip groan)
        {
            Body = body;
            Idle = idle;
            Cheer = cheer;
            Groan = groan;
        }

        private void OnEnable()
        {
            Show(Idle);
            // 모두 같은 때에 숨을 쉬면 꾸민 티가 난다. 시작 프레임을 흩는다.
            Frame = Random.Range(0, Mathf.Max(1, Idle.Frames.Count));
            Apply();
        }

        /// <summary>한 번 반응한다. 끝나면 숨쉬기로 돌아간다.</summary>
        public void Play(SeatedMotion motion)
        {
            switch (motion)
            {
                case SeatedMotion.Motion_Cheer:
                {
                    Show(Cheer);
                    break;
                }
                case SeatedMotion.Motion_Groan:
                {
                    Show(Groan);
                    break;
                }
                default:
                {
                    Show(Idle);
                    break;
                }
            }
        }

        private void Update()
        {
            if (Current == null || Current.Frames.Count == 0)
            {
                return;
            }

            FrameLeft -= Time.unscaledDeltaTime;

            if (FrameLeft > 0f)
            {
                return;
            }

            Frame++;

            if (Frame >= Current.Frames.Count)
            {
                // 반응은 한 번만 — 숨쉬기로 돌아간다.
                if (Current != Idle)
                {
                    Show(Idle);
                    return;
                }

                Frame = 0;
            }

            Apply();
        }

        private void Show(StationClip clip)
        {
            Current = clip == null || clip.Frames.Count == 0 ? Idle : clip;
            Frame = 0;
            Apply();
        }

        private void Apply()
        {
            if (Body == null || Current == null || Current.Frames.Count == 0)
            {
                return;
            }

            Frame = Mathf.Clamp(Frame, 0, Current.Frames.Count - 1);
            Body.sprite = Current.Frames[Frame];
            FrameLeft = (Frame < Current.Milliseconds.Count ? Current.Milliseconds[Frame] : 150) / 1000f;
        }
    }
}
