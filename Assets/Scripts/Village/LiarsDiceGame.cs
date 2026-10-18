using Underwrite.Global;

namespace Underwrite.Village
{
    /// <summary>
    /// 여관 주사위 내기(라이어스 다이스, 2026-10-18 사장님과 합의 — 엿듣기를 갈음할 여관 놀이).
    ///
    /// 둘이 주사위를 몇 개씩 가리고 굴린다. 번갈아 「둘의 주사위를 합쳐 ○ 눈이 N개 이상」을 부르고,
    /// 다음 사람은 더 높게 부르거나 「거짓말!」을 외친다. 열어서 부른 말이 맞으면 부른 쪽이, 틀리면 외친 쪽이 그 판을 이긴다.
    /// 먼저 <see cref="Constants.DICE_ROUNDS_TO_WIN"/>판을 이기면 끝. 화면과 보상은 부르는 쪽이 맡는다.
    /// 상대는 자기 주사위와 확률로 판단하고, 대담할수록 허세를 부르고 덜 의심한다.
    /// </summary>
    public sealed class LiarsDiceGame
    {
        public int[] PlayerDice { get; private set; }
        public int[] OpponentDice { get; private set; }

        /// <summary>지금 걸려 있는 말. 개수가 0이면 아직 아무도 부르지 않았다.</summary>
        public int BidCount { get; private set; }
        public int BidFace { get; private set; }

        /// <summary>지금 걸린 말을 플레이어가 불렀는가.</summary>
        public bool BidByPlayer { get; private set; }

        public bool IsPlayerTurn { get; private set; }
        public int PlayerWins { get; private set; }
        public int OpponentWins { get; private set; }

        /// <summary>판이 열려서(거짓말!) 결과를 보여 주는 중인가.</summary>
        public bool IsRevealed { get; private set; }

        /// <summary>마지막으로 연 판: 실제 개수와 이긴 쪽.</summary>
        public int RevealedCount { get; private set; }
        public bool RevealPlayerWon { get; private set; }

        /// <summary>0~1. 높을수록 허세가 많고 의심이 적다.</summary>
        public float Boldness { get; private set; }

        public bool IsOver
        {
            get
            {
                return PlayerWins >= Constants.DICE_ROUNDS_TO_WIN || OpponentWins >= Constants.DICE_ROUNDS_TO_WIN;
            }
        }

        public bool PlayerWonMatch
        {
            get
            {
                return PlayerWins >= Constants.DICE_ROUNDS_TO_WIN;
            }
        }

        public int TotalDice
        {
            get
            {
                return PlayerDice.Length + OpponentDice.Length;
            }
        }

        private readonly System.Random Rng;
        private bool PlayerStartsNext;

        public LiarsDiceGame(System.Random rng, float boldness, int dicePerPlayer)
        {
            Rng = rng;
            Boldness = boldness;
            PlayerDice = new int[dicePerPlayer];
            OpponentDice = new int[dicePerPlayer];
            PlayerStartsNext = rng.Next(2) == 0;
            StartRound();
        }

        /// <summary>새 판. 다시 굴리고 먼저 부르는 쪽을 번갈아 정한다.</summary>
        public void StartRound()
        {
            Roll(PlayerDice);
            Roll(OpponentDice);
            BidCount = 0;
            BidFace = 0;
            IsRevealed = false;
            IsPlayerTurn = PlayerStartsNext;
            PlayerStartsNext = !PlayerStartsNext;
        }

        /// <summary>지금 걸린 말보다 높은가. 개수가 많거나, 같은 개수면 눈이 높아야 한다.</summary>
        public bool IsValidRaise(int count, int face)
        {
            if (count < 1 || count > TotalDice || face < 1 || face > Constants.DICE_FACES)
            {
                return false;
            }

            if (BidCount == 0)
            {
                return true;
            }

            return count > BidCount || (count == BidCount && face > BidFace);
        }

        /// <summary>플레이어가 부른다. 받아들여지면 참.</summary>
        public bool PlayerBid(int count, int face)
        {
            if (IsOver == true || IsRevealed == true || IsPlayerTurn == false || IsValidRaise(count, face) == false)
            {
                return false;
            }

            SetBid(count, face, true);
            return true;
        }

        /// <summary>플레이어가 「거짓말!」. 상대가 부른 말이 있어야 한다.</summary>
        public bool PlayerCall()
        {
            if (IsOver == true || IsRevealed == true || IsPlayerTurn == false || BidCount == 0)
            {
                return false;
            }

            Reveal();
            return true;
        }

        /// <summary>상대 차례. 더 높게 부르면 참, 「거짓말!」을 외치면 거짓(판이 열린다).</summary>
        public bool OpponentAct()
        {
            if (IsOver == true || IsRevealed == true || IsPlayerTurn == true)
            {
                return false;
            }

            if (BidCount == 0)
            {
                OpenWithBestGuess();
                return true;
            }

            // 플레이어가 부른 말이 맞을 확률. 낮으면 의심한다 — 대담할수록 덜 의심한다.
            var truth = ChanceAtLeast(BidCount - CountIn(OpponentDice, BidFace), PlayerDice.Length);
            var doubt = Constants.DICE_DOUBT_BASE - Boldness * Constants.DICE_DOUBT_BOLD;

            if (truth < doubt)
            {
                Reveal();
                return false;
            }

            // 가장 그럴듯하게 올릴 수 있는 말을 찾는다. 대담하면 가끔 하나 더 얹어 허세를 부린다.
            var bestCount = 0;
            var bestFace = 0;
            var bestChance = -1f;

            for (var face = 1; face <= Constants.DICE_FACES; face++)
            {
                var count = face > BidFace ? BidCount : BidCount + 1;

                if (IsValidRaise(count, face) == false)
                {
                    continue;
                }

                var chance = ChanceAtLeast(count - CountIn(OpponentDice, face), PlayerDice.Length);

                if (chance > bestChance)
                {
                    bestChance = chance;
                    bestCount = count;
                    bestFace = face;
                }
            }

            if (bestCount == 0 || bestChance < Constants.DICE_GIVE_UP_CHANCE)
            {
                Reveal();
                return false;
            }

            if (Rng.NextDouble() < Boldness * Constants.DICE_BLUFF_RATE && IsValidRaise(bestCount + 1, bestFace) == true)
            {
                bestCount++;
            }

            SetBid(bestCount, bestFace, false);
            return true;
        }

        /// <summary>그 눈이 둘의 주사위를 합쳐 몇 개인가.</summary>
        public int CountAll(int face)
        {
            return CountIn(PlayerDice, face) + CountIn(OpponentDice, face);
        }

        private void OpenWithBestGuess()
        {
            // 자기 손에 가장 많은 눈을, 상대 몫의 기대치만큼 얹어 부른다.
            var face = 1;

            for (var f = 2; f <= Constants.DICE_FACES; f++)
            {
                if (CountIn(OpponentDice, f) > CountIn(OpponentDice, face))
                {
                    face = f;
                }
            }

            var count = CountIn(OpponentDice, face) + (int)(PlayerDice.Length / (float)Constants.DICE_FACES + 0.5f);
            SetBid(System.Math.Max(1, count), face, false);
        }

        private void SetBid(int count, int face, bool byPlayer)
        {
            BidCount = count;
            BidFace = face;
            BidByPlayer = byPlayer;
            IsPlayerTurn = !byPlayer;
        }

        private void Reveal()
        {
            RevealedCount = CountAll(BidFace);
            var bidHolds = RevealedCount >= BidCount;

            // 말이 맞으면 부른 쪽이, 틀리면 외친 쪽이 이긴다.
            RevealPlayerWon = bidHolds == BidByPlayer;
            IsRevealed = true;

            if (RevealPlayerWon == true)
            {
                PlayerWins++;
            }
            else
            {
                OpponentWins++;
            }
        }

        private void Roll(int[] dice)
        {
            for (var i = 0; i < dice.Length; i++)
            {
                dice[i] = Rng.Next(1, Constants.DICE_FACES + 1);
            }
        }

        private static int CountIn(int[] dice, int face)
        {
            var count = 0;

            for (var i = 0; i < dice.Length; i++)
            {
                if (dice[i] == face)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>모르는 주사위 <paramref name="unknown"/>개 가운데 특정 눈이 <paramref name="need"/>개 이상 나올 확률.</summary>
        public static float ChanceAtLeast(int need, int unknown)
        {
            if (need <= 0)
            {
                return 1f;
            }

            if (need > unknown)
            {
                return 0f;
            }

            var p = 1.0 / Constants.DICE_FACES;
            var total = 0.0;

            for (var k = need; k <= unknown; k++)
            {
                total += Choose(unknown, k) * System.Math.Pow(p, k) * System.Math.Pow(1.0 - p, unknown - k);
            }

            return (float)total;
        }

        private static double Choose(int n, int k)
        {
            var result = 1.0;

            for (var i = 1; i <= k; i++)
            {
                result = result * (n - k + i) / i;
            }

            return result;
        }
    }
}
