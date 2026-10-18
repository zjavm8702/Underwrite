using System;
using System.Collections.Generic;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>한 동작의 프레임과 프레임마다 머무는 시간(ms).</summary>
    [Serializable]
    public sealed class StationClip
    {
        public List<Sprite> Frames = new List<Sprite>();
        public List<int> Milliseconds = new List<int>();
    }

    /// <summary>
    /// 한자리에서 일하는 영지 사람(2026-10-17, Codex NPC 1차). 정면 한 방향만 그렸다.
    ///
    /// 평소에는 일(Work)을 되풀이하다 가끔 숨을 돌린다(Idle). 주인공이 그 자리에서 무언가를 사거나 일을 마치면
    /// 건네기(Give)를 한 번 하고 일로 돌아간다. Animator를 쓰지 않는다 — 방향이 하나라 프레임만 넘기면 된다.
    /// </summary>
    public sealed class StationNpc : MonoBehaviour
    {
        [Tooltip("이 사람이 맡은 자리(VillageSpot의 이름). 그 자리에서 건넬 일이 생기면 이 사람이 건넨다.")]
        [SerializeField] private string Place = string.Empty;

        [SerializeField] private SpriteRenderer Body;
        [SerializeField] private StationClip Idle = new StationClip();
        [SerializeField] private StationClip Work = new StationClip();
        [SerializeField] private StationClip Give = new StationClip();

        // 일을 몇 바퀴 하고 숨을 한 번 돌리는가(범위).
        [SerializeField] private int WorkLoopsMin = 3;
        [SerializeField] private int WorkLoopsMax = 6;

        private static readonly List<StationNpc> All = new List<StationNpc>();

        private StationClip Current;
        private int Frame;
        private float FrameLeft;
        private int LoopsLeft;

        public string PlaceName
        {
            get
            {
                return Place;
            }
        }

        public void Configure(string place, SpriteRenderer body, StationClip idle, StationClip work, StationClip give)
        {
            Place = place;
            Body = body;
            Idle = idle;
            Work = work;
            Give = give;
        }

        private void OnEnable()
        {
            All.Add(this);
            // 모두 같은 때에 망치를 내리치면 꾸민 티가 난다. 시작 프레임을 흩는다.
            Play(Work);
            Frame = UnityEngine.Random.Range(0, Math.Max(1, Work.Frames.Count));
            Show();
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void Update()
        {
            if (Current == null || Current.Frames.Count == 0)
            {
                return;
            }

            FrameLeft -= Time.deltaTime;

            if (FrameLeft > 0f)
            {
                return;
            }

            Frame++;

            if (Frame >= Current.Frames.Count)
            {
                Frame = 0;
                OnClipEnd();
            }

            Show();
        }

        private void OnClipEnd()
        {
            if (Current == Give)
            {
                Play(Work);
                return;
            }

            LoopsLeft--;

            if (LoopsLeft > 0)
            {
                return;
            }

            Play(Current == Work ? Idle : Work);
        }

        private void Play(StationClip clip)
        {
            if (clip == null || clip.Frames.Count == 0)
            {
                return;
            }

            Current = clip;
            Frame = 0;
            LoopsLeft = clip == Work ? UnityEngine.Random.Range(WorkLoopsMin, WorkLoopsMax + 1) : 1;
            Show();
        }

        private void Show()
        {
            if (Body == null || Current == null || Current.Frames.Count == 0)
            {
                return;
            }

            Body.sprite = Current.Frames[Frame];
            var ms = Frame < Current.Milliseconds.Count ? Current.Milliseconds[Frame] : 120;
            FrameLeft = ms / 1000f;
        }
    }
}
