using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using UnityEngine;

namespace Underwrite.Data
{
    /// <summary>표 한 줄에 해당하는 종목 하나.</summary>
    [Serializable]
    public sealed class StockEntry
    {
        public string Id;
        public string DisplayName;
        public StockPersonality Personality;

        [Tooltip("내재가치. 사건의 결과가 확정될 때만 움직인다.")]
        public float TrueValue;

        [Tooltip("작을수록 같은 수량의 주문이 가격을 크게 민다.")]
        public float Liquidity;

        [Tooltip("몇 주기에 상장되는가. 1이면 처음부터 올라와 있다.")]
        public int ListCycle = 1;

        [Tooltip("몇 주기에 시장을 떠나는가. 0이면 떠나지 않는다.")]
        public int DelistCycle;

        [Tooltip("떠나는 사유. 예고가 있는 이탈과 없는 이탈을 가른다.")]
        public DelistReason DelistReason = DelistReason.Delist_None;
    }

    /// <summary>
    /// 거래 대상 목록. 종목 하나에 자산 하나를 만들지 않고 한 표에 모은다.
    /// 상장이 늘어나면 수십 개가 되는데, 그걸 자산 파일로 흩어 두면 한눈에 비교할 수 없다.
    /// </summary>
    [CreateAssetMenu(fileName = "StockTable", menuName = "Underwrite/종목 표")]
    public sealed class StockTable : ScriptableObject
    {
        [SerializeField] private List<StockEntry> Entries = new List<StockEntry>();

        public IReadOnlyList<StockEntry> Rows
        {
            get
            {
                return Entries;
            }
        }

        /// <summary>표를 만들 때 쓴다.</summary>
        public void Replace(List<StockEntry> rows)
        {
            Entries = rows;
        }
    }
}
