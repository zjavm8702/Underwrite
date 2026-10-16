using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using UnityEngine;

namespace Underwrite.Data
{
    /// <summary>표정 하나에 그림 하나.</summary>
    [Serializable]
    public sealed class PortraitFace
    {
        public PortraitEmotion Emotion;
        public Sprite Sprite;
    }

    /// <summary>
    /// 인물 한 명의 상반신 그림 묶음.
    ///
    /// 대화에서 바뀌는 것은 표정이지 사람이 아니다. 표정마다 자산을 따로 물리면
    /// 인물이 늘어날 때마다 연결할 자리가 표정 수만큼 늘어난다. 한 묶음으로 든다.
    ///
    /// 전신 그림을 잘라 쓰지 않는다. 상반신은 따로 그린다.
    /// 잘라 맞추면 표정이 바뀔 때 얼굴 위치가 어긋난다.
    /// </summary>
    [CreateAssetMenu(fileName = "PortraitSet", menuName = "Underwrite/인물 표정 묶음")]
    public sealed class PortraitSet : ScriptableObject
    {
        [Tooltip("표정 그림이 아직 없을 때 쓸 그림. 비워 두면 인물 자리를 비운다.")]
        [SerializeField] private Sprite Fallback;

        [SerializeField] private List<PortraitFace> Faces = new List<PortraitFace>();

        public IReadOnlyList<PortraitFace> Rows
        {
            get
            {
                return Faces;
            }
        }

        /// <summary>
        /// 그 표정의 그림을 찾는다. 없으면 평상시 표정, 그것도 없으면 대체 그림을 준다.
        /// 표정이 빠졌다고 인물이 사라지면 대화가 비어 보인다.
        /// </summary>
        public Sprite Get(PortraitEmotion emotion)
        {
            var neutral = default(Sprite);

            for (var i = 0; i < Faces.Count; i++)
            {
                var face = Faces[i];

                if (face == null || face.Sprite == null)
                {
                    continue;
                }

                if (face.Emotion == emotion)
                {
                    return face.Sprite;
                }

                if (face.Emotion == PortraitEmotion.Emotion_Neutral)
                {
                    neutral = face.Sprite;
                }
            }

            if (neutral != null)
            {
                return neutral;
            }

            return Fallback;
        }

        /// <summary>표를 만들 때 쓴다.</summary>
        public void Assign(Sprite fallback, List<PortraitFace> faces)
        {
            Fallback = fallback;
            Faces = faces;
        }
    }
}
