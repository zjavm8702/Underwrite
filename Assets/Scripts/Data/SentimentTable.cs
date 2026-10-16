using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Data
{
    [Serializable]
    public sealed class SentimentEntry
    {
        public MarketSentiment Sentiment;

        [Tooltip("분위기가 모든 종목에 공통으로 거는 틱당 흐름. 열풍은 양수, 공포는 음수다.")]
        public float DriftPerTick;

        [Tooltip("분위기가 거는 변동성 배율. 공포 장세가 가장 거칠다.")]
        public float VolatilityMultiplier = 1f;
    }

    /// <summary>시장 분위기별 흐름과 변동성. 기획서 §7.</summary>
    [CreateAssetMenu(fileName = "SentimentTable", menuName = "Underwrite/분위기 표")]
    public sealed class SentimentTable : ScriptableObject
    {
        [SerializeField] private List<SentimentEntry> Entries = new List<SentimentEntry>();

        public IReadOnlyList<SentimentEntry> Rows
        {
            get
            {
                return Entries;
            }
        }

        public void Replace(List<SentimentEntry> rows)
        {
            Entries = rows;
        }

        public void Apply()
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                switch (entry.Sentiment)
                {
                    case MarketSentiment.Sentiment_Fever:
                    {
                        Constants.SENTIMENT_DRIFT_FEVER = entry.DriftPerTick;
                        Constants.SENTIMENT_VOLATILITY_FEVER = entry.VolatilityMultiplier;
                        break;
                    }
                    case MarketSentiment.Sentiment_Fear:
                    {
                        Constants.SENTIMENT_DRIFT_FEAR = entry.DriftPerTick;
                        Constants.SENTIMENT_VOLATILITY_FEAR = entry.VolatilityMultiplier;
                        break;
                    }
                    case MarketSentiment.Sentiment_Drained:
                    {
                        Constants.SENTIMENT_DRIFT_DRAINED = entry.DriftPerTick;
                        Constants.SENTIMENT_VOLATILITY_DRAINED = entry.VolatilityMultiplier;
                        break;
                    }
                    default:
                    {
                        Constants.SENTIMENT_DRIFT_CALM = entry.DriftPerTick;
                        Constants.SENTIMENT_VOLATILITY_CALM = entry.VolatilityMultiplier;
                        break;
                    }
                }
            }
        }
    }
}
