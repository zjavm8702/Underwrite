using System.Collections.Generic;
using Underwrite.Enumeration;

namespace Underwrite.Market
{
    /// <summary>영지에서 캐낸 소식 한 줄.</summary>
    public sealed class GatheredNote
    {
        public string StockId;
        public string Headline;
        public InfoSource Source;

        public GatheredNote(string stockId, string headline, InfoSource source)
        {
            StockId = stockId;
            Headline = headline;
            Source = source;
        }
    }

    /// <summary>
    /// 장과 장 사이에 영지에서 한 일을 적어 둔다. 기획서 §11의 "자금 및 정보 모으기".
    ///
    /// **자원은 시간이다.** 들를 수 있는 횟수를 따로 세지 않는다.
    /// 영지의 시간은 실시간 1:1로 흐르고, 한 가지 일을 하면 그만큼 시각이 간다.
    /// 다음 장이 열릴 시각에 닿으면 더는 할 수 없다. 어디에 쓸지 고르는 일이 곧 선택이다.
    ///
    /// 영지는 지갑을 직접 건드리지 않는다. 돈은 <see cref="Portfolio"/>에 있고,
    /// 영지가 그것을 따로 들고 있으면 어느 쪽이 맞는 잔고인지 알 수 없게 된다.
    /// 여기에는 **무엇을 했는지만** 적고, 거래 화면으로 돌아올 때 한꺼번에 반영한다.
    ///
    /// 씬을 넘나들어야 하므로 정적이다. 저장 기능이 들어오면 이 내용도 함께 저장한다.
    /// </summary>
    public static class GatherSession
    {
        private static readonly List<GatheredNote> Notes = new List<GatheredNote>();

        /// <summary>쓴 돈. 거래 화면으로 돌아올 때 지갑에서 뺀다.</summary>
        public static float Spent { get; private set; }

        /// <summary>번 돈. 퀘스트로 종잣돈을 모으는 길이다. 기획서 §10.</summary>
        public static float Earned { get; private set; }

        /// <summary>다음 정보 타임의 번호. 캐내는 소식은 이 묶음에서 나온다.</summary>
        public static int NextSlot { get; set; }

        public static IReadOnlyList<GatheredNote> Gathered
        {
            get
            {
                return Notes;
            }
        }

        /// <summary>지갑에 더해질 값. 번 것에서 쓴 것을 뺀다.</summary>
        public static float NetChange
        {
            get
            {
                return Earned - Spent;
            }
        }

        /// <summary>한 가지 일을 했다. 돈과 소식만 적는다. 시간은 시계가 센다.</summary>
        public static void Record(float cost, float reward, GatheredNote note)
        {
            Spent += cost;
            Earned += reward;

            if (note != null)
            {
                Notes.Add(note);
            }
        }

        /// <summary>장이 끝나 영지의 시간이 시작된다.</summary>
        public static void BeginBreak(int nextSlot)
        {
            Spent = 0f;
            Earned = 0f;
            NextSlot = nextSlot;
            Notes.Clear();
        }
    }
}
