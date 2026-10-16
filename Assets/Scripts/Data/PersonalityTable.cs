using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Data
{
    [Serializable]
    public sealed class PersonalityEntry
    {
        public StockPersonality Personality;

        [Tooltip("이 성격이 무작위 변동에 거는 배율. 베테랑은 낮고 고위험은 높다.")]
        public float VolatilityMultiplier = 1f;
    }

    /// <summary>종목 성격별 변동성 배율. 기획서 §7의 종목별 성격.</summary>
    [CreateAssetMenu(fileName = "PersonalityTable", menuName = "Underwrite/성격 표")]
    public sealed class PersonalityTable : ScriptableObject
    {
        [SerializeField] private List<PersonalityEntry> Entries = new List<PersonalityEntry>();

        public IReadOnlyList<PersonalityEntry> Rows
        {
            get
            {
                return Entries;
            }
        }

        public void Replace(List<PersonalityEntry> rows)
        {
            Entries = rows;
        }

        public void Apply()
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                switch (entry.Personality)
                {
                    case StockPersonality.Personality_Veteran:
                    {
                        Constants.VOLATILITY_VETERAN = entry.VolatilityMultiplier;
                        break;
                    }
                    case StockPersonality.Personality_Rookie:
                    {
                        Constants.VOLATILITY_ROOKIE = entry.VolatilityMultiplier;
                        break;
                    }
                    case StockPersonality.Personality_HighRisk:
                    {
                        Constants.VOLATILITY_HIGH_RISK = entry.VolatilityMultiplier;
                        break;
                    }
                    default:
                    {
                        Constants.VOLATILITY_FAMOUS = entry.VolatilityMultiplier;
                        break;
                    }
                }
            }
        }
    }
}
