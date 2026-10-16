using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using UnityEngine;

namespace Underwrite.Data
{
    /// <summary>표 한 줄에 해당하는 사건 한 단계.</summary>
    [Serializable]
    public sealed class EventEntry
    {
        [Tooltip("정보 타임 번호. 0·1이 오전장 두 번, 2·3이 오후장 두 번이다.")]
        public int Slot;

        public string StockId;
        public EventStage Stage;

        [TextArea]
        public string Headline;

        public InfoSource Source;

        [Tooltip("Public이면 시장이 함께 알아 가격이 움직인다. PlayerOnly면 나만 안다.")]
        public InfoVisibility Visibility;

        [Tooltip("시장 기대를 움직이는 값. 소문·보고 단계에서 쓴다.")]
        public float ExpectationDelta;

        [Tooltip("실제 내재가치를 움직이는 값. 결과 단계에서만 쓴다.")]
        public float ValueDelta;
    }

    /// <summary>
    /// 사건 일정. 정보 타임마다 어떤 소식이 들어오는지가 여기 적힌다.
    /// 10년을 채우면 수백 줄이 되므로 한 표로 모아 두고 정보 타임 번호로 묶는다.
    /// </summary>
    [CreateAssetMenu(fileName = "EventTable", menuName = "Underwrite/사건 표")]
    public sealed class EventTable : ScriptableObject
    {
        [SerializeField] private List<EventEntry> Entries = new List<EventEntry>();

        public IReadOnlyList<EventEntry> Rows
        {
            get
            {
                return Entries;
            }
        }

        public void Replace(List<EventEntry> rows)
        {
            Entries = rows;
        }
    }
}
