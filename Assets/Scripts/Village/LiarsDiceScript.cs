using System.Collections.Generic;

namespace Underwrite.Village
{
    /// <summary>
    /// 주사위 내기 연습 판의 각본(2026-10-18 — 프롤로그 여관 정거장). 판마다 주사위와 상대의 수를 미리 정해 두고,
    /// 단계마다 코치 한 줄을 띄우며, 배워야 할 동작 말고는 잠근다. 사장님: 「잘 모르겠다 — 그게 튜토리얼이 메울 곳」.
    /// </summary>
    public sealed class LiarsDiceScript
    {
        public sealed class Round
        {
            public int[] PlayerDice;
            public int[] OpponentDice;
            public bool PlayerFirst;

            /// <summary>상대의 수를 차례로. 개수가 0이면 「거짓말!」.</summary>
            public readonly List<int[]> OpponentMoves = new List<int[]>();

            /// <summary>내가 먼저 부를 때 / 상대 말에 답할 때 / 판이 열렸을 때 코치.</summary>
            public string OpenHint;
            public string ResponseHint;
            public string RevealHint;

            /// <summary>상대 말에 답할 때 허락하는 동작.</summary>
            public bool AllowCall = true;
            public bool AllowRaise = true;
        }

        public readonly List<Round> Rounds = new List<Round>();
    }
}
