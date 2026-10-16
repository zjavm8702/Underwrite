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
    /// 영지는 지갑을 직접 건드리지 않는다. 돈은 <see cref="Portfolio"/>에 있고,
    /// 영지가 그것을 따로 들고 있으면 어느 쪽이 맞는 잔고인지 알 수 없게 된다.
    /// 여기에는 **무엇을 했는지만** 적고, 거래 화면으로 돌아올 때 한꺼번에 반영한다.
    ///
    /// 씬을 넘나들어야 하므로 정적이다. 저장 기능이 들어오면 이 내용도 함께 저장한다.
    /// </summary>
    public static class GatherSession
    {
        private static readonly List<GatheredNote> Notes = new List<GatheredNote>();

        /// <summary>이번 쉬는 동안 더 들를 수 있는 곳의 수. 시간이 자원이다.</summary>
        public static int VisitsLeft { get; private set; } = Global.Constants.GATHER_VISITS_PER_BREAK;

        /// <summary>쓴 돈. 거래 화면으로 돌아올 때 지갑에서 뺀다.</summary>
        public static float Spent { get; private set; }

        /// <summary>다음 정보 타임의 번호. 캐내는 소식은 이 묶음에서 나온다.</summary>
        public static int NextSlot { get; set; }

        public static IReadOnlyList<GatheredNote> Gathered
        {
            get
            {
                return Notes;
            }
        }

        public static bool CanVisit
        {
            get
            {
                return VisitsLeft > 0;
            }
        }

        /// <summary>한 곳을 들렀다. 돈과 횟수를 함께 쓴다.</summary>
        public static void Visit(float cost, GatheredNote note)
        {
            if (VisitsLeft <= 0)
            {
                return;
            }

            VisitsLeft--;
            Spent += cost;

            if (note != null)
            {
                Notes.Add(note);
            }
        }

        /// <summary>장이 열릴 때 부른다. 쉬는 시간이 끝났으므로 횟수를 되돌린다.</summary>
        public static void BeginBreak(int nextSlot)
        {
            VisitsLeft = Global.Constants.GATHER_VISITS_PER_BREAK;
            Spent = 0f;
            NextSlot = nextSlot;
            Notes.Clear();
        }
    }
}
