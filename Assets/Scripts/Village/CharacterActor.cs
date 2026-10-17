using System;
using System.Collections;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.UI;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 필드 캐릭터의 연기. 바라보기, 몸짓, 머리 위 감정 표시와 말풍선.
    ///
    /// 2026-10-17 사용자: "애니메이션을 많이 만든다. 선택지를 넘기는 것뿐 아니라 상호작용이 많이 일어났으면."
    /// 대화창은 화면을 덮으므로, 상호작용은 대화 **앞뒤로** 필드에서 보인다 — 다가가면 돌아보고,
    /// 일을 마치면 기뻐하고, 지나가면 말을 건다.
    ///
    /// 몸짓은 <see cref="ActorMotion"/>과 같은 이름의 Animator 상태가 있으면 그것을 재생한다
    /// (예: Motion_Cheer → "Cheer"). 없으면 Body를 코드로 조금 움직여 대신한다.
    /// 새 애니메이션이 들어오면 Animator에 상태만 추가하면 된다.
    /// </summary>
    public sealed class CharacterActor : MonoBehaviour
    {
        private static readonly int DirectionXHash = Animator.StringToHash(StringConstants.ANIM_PARAM_DIRECTION_X);
        private static readonly int DirectionYHash = Animator.StringToHash(StringConstants.ANIM_PARAM_DIRECTION_Y);
        private static readonly int IdleHash = Animator.StringToHash(StringConstants.ANIM_STATE_IDLE);

        private Animator Body;
        private Transform BodyTransform;
        private SpriteRenderer BodySprite;
        private Vector3 BodyRest;
        private Coroutine Running;

        /// <summary>지금 몸짓 중인가. 걷기와 겹치지 않게 쓴다.</summary>
        public bool IsPerforming
        {
            get
            {
                return Running != null;
            }
        }

        /// <summary>머리 위 한 점(월드). 말풍선이 여기 붙는다.</summary>
        public Vector3 HeadPoint
        {
            get
            {
                if (BodySprite != null)
                {
                    var bounds = BodySprite.bounds;
                    return new Vector3(bounds.center.x, bounds.max.y, 0f);
                }

                return transform.position + Vector3.up;
            }
        }

        private void Awake()
        {
            Body = GetComponentInChildren<Animator>();
            BodyTransform = Body != null ? Body.transform : transform;
            BodySprite = BodyTransform.GetComponent<SpriteRenderer>();

            // Animator 없이 프레임만 넘기는 사람(StationNpc)은 몸 그림이 자식 「Body」에 있다.
            // 못 찾으면 머리 위가 아니라 허리쯤에 말풍선이 떴다(2026-10-17 사용자 보고).
            if (BodySprite == null)
            {
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.gameObject.name == "Body")
                    {
                        BodySprite = renderer;

                        // 몸짓(끄덕임·흔들기)은 몸 그림만 움직인다. 사람 전체(루트)를 움직이면, 처음 선 자리를
                        // 기준으로 되돌아가 걷던 용병단 단원이 말을 걸자마자 출발점으로 튀었다(2026-10-18 사용자 보고).
                        BodyTransform = renderer.transform;
                        break;
                    }
                }
            }

            BodyRest = BodyTransform.localPosition;

            // 사람은 모두 외곽선을 두른다(2026-10-17 사용자 결정). 사물과 그림이 섞인 화면에서 사람이 떠 보이게.
            if (Constants.OUTLINE_ENABLED == true && GetComponent<SpriteOutline>() == null)
            {
                gameObject.AddComponent<SpriteOutline>();
            }
        }

        /// <summary>그쪽을 바라본다. 8방향으로 맞춘다.</summary>
        public void Face(Vector2 direction)
        {
            var frames = GetComponent<DirectionalSprites>();

            if (frames != null)
            {
                frames.SetDirection(direction);
            }

            if (Body == null || direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var angle = Mathf.Atan2(direction.y, direction.x);
            var snap = Mathf.PI / 4f;
            var snapped = Mathf.Round(angle / snap) * snap;
            Body.SetFloat(DirectionXHash, Mathf.Cos(snapped));
            Body.SetFloat(DirectionYHash, Mathf.Sin(snapped));
        }

        /// <summary>그 사람을 바라본다.</summary>
        public void FaceTowards(Vector3 worldPoint)
        {
            Face(worldPoint - transform.position);
        }

        /// <summary>머리 위에 감정 표시를 띄운다.</summary>
        public void Emote(ActorEmote emote)
        {
            WorldBubbles.Show(this, EmoteText(emote), Constants.ACTOR_EMOTE_SECONDS, true);
        }

        /// <summary>머리 위 말풍선으로 짧게 말한다. 대화창을 열지 않는다.</summary>
        public void Say(string text)
        {
            if (string.IsNullOrEmpty(text) == true)
            {
                return;
            }

            WorldBubbles.Show(this, text, Constants.ACTOR_SAY_SECONDS, false);
        }

        /// <summary>몸짓을 한다. 끝나면 <paramref name="onDone"/>을 부른다. 하던 몸짓은 끊는다.</summary>
        public void Play(ActorMotion motion, Action onDone = null)
        {
            if (Running != null)
            {
                StopCoroutine(Running);
                BodyTransform.localPosition = BodyRest;
                BodyTransform.localRotation = Quaternion.identity;
            }

            Running = StartCoroutine(Perform(motion, onDone));
        }

        private IEnumerator Perform(ActorMotion motion, Action onDone)
        {
            var state = Animator.StringToHash(StateName(motion));

            if (Body != null && Body.HasState(0, state) == true)
            {
                Body.Play(state, 0, 0f);

                // Play는 다음 갱신에 적용된다. 상태가 실제로 바뀔 때까지 기다린다.
                // 바로 상태를 검사하면 아직 Idle이라 몸짓이 시작도 못 하고 끝나 버린다.
                for (var wait = 0; wait < 10 && Body.GetCurrentAnimatorStateInfo(0).shortNameHash != state; wait++)
                {
                    yield return null;
                }

                // 한 번 도는 상태다. 끝나면 Idle로 돌린다. Controller에 나가는 전이가 없어서다.
                while (Body.GetCurrentAnimatorStateInfo(0).shortNameHash == state
                    && Body.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                {
                    yield return null;
                }

                Body.Play(IdleHash, 0, 0f);
            }
            else
            {
                yield return Fallback(motion);
            }

            BodyTransform.localPosition = BodyRest;
            BodyTransform.localRotation = Quaternion.identity;
            Running = null;

            if (onDone != null)
            {
                onDone();
            }
        }

        // 그림이 아직 없는 몸짓. 몸을 조금 움직여 뜻만 전한다.
        private IEnumerator Fallback(ActorMotion motion)
        {
            var duration = Constants.ACTOR_FALLBACK_SECONDS;
            var scale = transform.lossyScale.y <= 0f ? 1f : 1f / transform.lossyScale.y;

            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var k = t / duration;
                var offset = Vector3.zero;
                var tilt = 0f;

                switch (motion)
                {
                    case ActorMotion.Motion_Nod:
                    {
                        offset.y = -Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * 0.08f;
                        break;
                    }
                    case ActorMotion.Motion_Shake:
                    {
                        tilt = Mathf.Sin(k * Mathf.PI * 6f) * 5f * (1f - k);
                        break;
                    }
                    default:
                    {
                        // Hop, 그리고 그림이 없는 Cheer
                        offset.y = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * 0.25f;
                        break;
                    }
                }

                BodyTransform.localPosition = BodyRest + offset * scale;
                BodyTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                yield return null;
            }
        }

        private static string StateName(ActorMotion motion)
        {
            switch (motion)
            {
                case ActorMotion.Motion_Cheer:
                {
                    return StringConstants.ANIM_STATE_CHEER;
                }
                case ActorMotion.Motion_Nod:
                {
                    return StringConstants.ANIM_STATE_NOD;
                }
                case ActorMotion.Motion_Shake:
                {
                    return StringConstants.ANIM_STATE_SHAKE;
                }
                default:
                {
                    return StringConstants.ANIM_STATE_HOP;
                }
            }
        }

        private static string EmoteText(ActorEmote emote)
        {
            switch (emote)
            {
                case ActorEmote.Emote_Exclaim:
                {
                    return StringConstants.TEXT_EMOTE_EXCLAIM;
                }
                case ActorEmote.Emote_Question:
                {
                    return StringConstants.TEXT_EMOTE_QUESTION;
                }
                case ActorEmote.Emote_Music:
                {
                    return StringConstants.TEXT_EMOTE_MUSIC;
                }
                case ActorEmote.Emote_Heart:
                {
                    return StringConstants.TEXT_EMOTE_HEART;
                }
                case ActorEmote.Emote_Coin:
                {
                    return StringConstants.TEXT_EMOTE_COIN;
                }
                default:
                {
                    return StringConstants.TEXT_EMOTE_DOTS;
                }
            }
        }
    }
}
