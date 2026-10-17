using System;
using UnityEngine;

namespace Underwrite.Data
{
    /// <summary>
    /// 영지의 자리에서 맡을 수 있는 일 하나. 기획서 §10의 종잣돈 퀘스트.
    ///
    /// 묻는 일은 돈을 쓰고 정보를 얻는다. 맡는 일은 반대로 **시간을 쓰고 돈을 번다.**
    /// 같은 장 사이의 시간을 어느 쪽에 쓸지가 고르는 일이 된다.
    /// 돈 외의 보상(소문)을 붙이는 이유는 §10의 표가 "돈 외의 보상"을 따로 적었기 때문이다.
    /// </summary>
    [Serializable]
    public sealed class VillageQuest
    {
        [Tooltip("선택지에 적힐 일. 플레이어가 하는 행동으로 쓴다. 비어 있으면 이 자리에는 일이 없다.")]
        public string Title;

        [Tooltip("일을 마친 뒤 자리의 주인이 하는 말.")]
        [TextArea]
        public string Done;

        [Tooltip("걸리는 시간(분). 영지의 시간은 실시간 1:1로 흐른다.")]
        public int Minutes;

        [Tooltip("받는 돈.")]
        public float Reward;

        [Tooltip("일하는 동안 소문을 하나 주워듣는가. 여관 업무처럼 사람을 많이 만나는 일이다.")]
        public bool GivesRumor;
    }
}
