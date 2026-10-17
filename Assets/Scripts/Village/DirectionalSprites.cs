using System;
using System.Collections.Generic;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>한 방향·한 동작의 프레임. 단 색 마스크 프레임이 같은 순서로 붙는다.</summary>
    [Serializable]
    public sealed class DirectionalClip
    {
        /// <summary>S·SE·E·NE·N 가운데 하나. 왼쪽(SW·W·NW)은 오른쪽을 좌우로 뒤집어 쓴다.</summary>
        public string Direction;
        public List<Sprite> Frames = new List<Sprite>();
        public List<Sprite> Masks = new List<Sprite>();
        public List<int> Milliseconds = new List<int>();
    }

    /// <summary>
    /// Animator 없이 방향별 프레임을 넘기는 사람(2026-10-17, Codex NPC 2차 — 걷는 용병단 단원).
    ///
    /// 다섯 방향만 그렸다. 왼쪽은 오른쪽을 뒤집는다. 걷기와 대기 두 동작.
    /// 단 색은 몸 그림 위에 겹친 「Cloth」 마스크에 곱한다 — 회색으로 그린 천에만 색이 들고 명암은 남는다.
    /// NpcWalker·CharacterActor가 Animator 대신 이것에 방향과 빠르기를 넘긴다.
    /// </summary>
    public sealed class DirectionalSprites : MonoBehaviour
    {
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly string[] RIGHT_SIDE = { "E", "NE", "N", "S", "SE" };

        [SerializeField] private SpriteRenderer Body;
        [SerializeField] private SpriteRenderer Cloth;
        [SerializeField] private List<DirectionalClip> Idle = new List<DirectionalClip>();
        [SerializeField] private List<DirectionalClip> Walking = new List<DirectionalClip>();

        private Vector2 Facing = Vector2.down;
        private bool Moving;
        private DirectionalClip Current;
        private bool CurrentMoving;
        private bool Flip;
        private int Frame;
        private float FrameLeft;

        public void Configure(SpriteRenderer body, SpriteRenderer cloth, List<DirectionalClip> idle,
            List<DirectionalClip> walking)
        {
            Body = body;
            Cloth = cloth;
            Idle = idle;
            Walking = walking;
        }

        /// <summary>단 색. 마스크 자리(회색으로 그린 천)에 곱해진다.</summary>
        public void SetTint(Color tint)
        {
            if (Cloth == null)
            {
                return;
            }

            // 스프라이트 색은 셰이더에 정점 색으로 오지 않을 때가 있다. 렌더러마다 _Tint로 넘긴다.
            var block = new MaterialPropertyBlock();
            Cloth.GetPropertyBlock(block);
            block.SetColor(TintId, tint);
            Cloth.SetPropertyBlock(block);
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Facing = direction;
            Pick();
        }

        public void SetSpeed(float speed)
        {
            var moving = speed > 0.01f;

            if (moving == Moving)
            {
                return;
            }

            Moving = moving;
            Pick();
        }

        private void OnEnable()
        {
            Pick();
            Frame = UnityEngine.Random.Range(0, Current == null ? 1 : Math.Max(1, Current.Frames.Count));
            Show();
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

            Frame = (Frame + 1) % Current.Frames.Count;
            Show();
        }

        // 바라보는 쪽과 동작으로 클립을 고른다. 같은 클립이면 프레임을 이어 간다.
        private void Pick()
        {
            var name = DirectionName(Facing, out var flip);
            var clips = Moving == true ? Walking : Idle;
            DirectionalClip found = null;

            for (var i = 0; i < clips.Count; i++)
            {
                if (clips[i].Direction == name)
                {
                    found = clips[i];
                    break;
                }
            }

            if (found == null || (found == Current && flip == Flip))
            {
                return;
            }

            // 걷다가 방향만 바뀌면 발걸음을 이어 간다. 섰다·걸었다가 바뀌면 처음부터.
            var keep = CurrentMoving == Moving ? Frame : 0;
            Current = found;
            CurrentMoving = Moving;
            Flip = flip;
            Frame = Math.Min(keep, Math.Max(0, found.Frames.Count - 1));
            Show();
        }

        private void Show()
        {
            if (Current == null || Current.Frames.Count == 0 || Body == null)
            {
                return;
            }

            Body.sprite = Current.Frames[Frame];
            Body.flipX = Flip;

            if (Cloth != null)
            {
                Cloth.sprite = Frame < Current.Masks.Count ? Current.Masks[Frame] : null;
                Cloth.flipX = Flip;
            }

            var ms = Frame < Current.Milliseconds.Count ? Current.Milliseconds[Frame] : 100;
            FrameLeft = ms / 1000f;
        }

        // 8방향으로 맞춘 뒤, 왼쪽은 오른쪽 짝과 뒤집기로 바꾼다.
        private static string DirectionName(Vector2 direction, out bool flip)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var index = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;

            // 0:E 1:NE 2:N 3:NW 4:W 5:SW 6:S 7:SE
            switch (index)
            {
                case 3:
                {
                    flip = true;
                    return "NE";
                }
                case 4:
                {
                    flip = true;
                    return "E";
                }
                case 5:
                {
                    flip = true;
                    return "SE";
                }
                default:
                {
                    flip = false;
                    return RIGHT_SIDE[index == 6 ? 3 : index == 7 ? 4 : index];
                }
            }
        }
    }
}
