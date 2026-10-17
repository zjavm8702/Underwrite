using System.Collections.Generic;
using Underwrite.Enumeration;
using UnityEngine;

namespace Underwrite.Data
{
    /// <summary>
    /// 표를 한 묶음으로 들고 다니는 자산.
    /// 화면마다 표 여섯 개를 따로 물리면 하나를 빠뜨렸을 때 조용히 기본값으로 돌아간다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataSet", menuName = "Underwrite/데이터 묶음")]
    public sealed class GameDataSet : ScriptableObject
    {
        [SerializeField] private MarketTuningData Tuning;
        [SerializeField] private PersonalityTable Personalities;
        [SerializeField] private SentimentTable Sentiments;
        [SerializeField] private StockTable Stocks;
        [SerializeField] private EventTable Events;
        [SerializeField] private UITextTable Texts;

        [Tooltip("소식을 전하는 주민의 표정 묶음. 순서는 InfoSource와 같다(여관·길드·서신·게시판·중개·대장간).")]
        [SerializeField] private List<PortraitSet> FolkFaces = new List<PortraitSet>();

        [Tooltip("지하 정보 타임의 정보상. 비워 두면 인물 자리를 비운다.")]
        [SerializeField] private PortraitSet Informant;

        /// <summary>지하 정보상의 얼굴. 아직 그림이 없으면 null.</summary>
        public Sprite InformantFace(PortraitEmotion emotion)
        {
            return Informant == null ? null : Informant.Get(emotion);
        }

        /// <summary>그 출처의 소식을 전하는 주민의 얼굴. 없으면 null.</summary>
        public PortraitSet FolkFaceOf(InfoSource source)
        {
            var index = (int)source;
            return index >= 0 && index < FolkFaces.Count ? FolkFaces[index] : null;
        }

        public StockTable StockRows
        {
            get
            {
                return Stocks;
            }
        }

        public EventTable EventRows
        {
            get
            {
                return Events;
            }
        }

        public UITextTable TextRows
        {
            get
            {
                return Texts;
            }
        }

        /// <summary>표의 수치를 코드 쪽에 덮어쓴다. 장을 열기 전에 한 번 부른다.</summary>
        public void Apply()
        {
            if (Tuning != null)
            {
                Tuning.Apply();
            }

            if (Personalities != null)
            {
                Personalities.Apply();
            }

            if (Sentiments != null)
            {
                Sentiments.Apply();
            }

            if (Texts != null)
            {
                Texts.Apply();
            }
        }

        /// <summary>표를 만들 때 쓴다.</summary>
        public void Assign(MarketTuningData tuning, PersonalityTable personalities,
            SentimentTable sentiments, StockTable stocks, EventTable events, UITextTable texts)
        {
            Tuning = tuning;
            Personalities = personalities;
            Sentiments = sentiments;
            Stocks = stocks;
            Events = events;
            Texts = texts;
        }
    }
}
