using System;
using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Village;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 여관 주사위 내기 화면. 위에 상대(가린 주사위·분위기), 가운데 걸린 말, 아래 내 주사위와 부르기 칸.
    /// 판정은 <see cref="LiarsDiceGame"/>이 하고, 이 화면은 보여 주고 받아 넘긴다. 상대 차례에는 잠깐 뜸을 들인다.
    /// </summary>
    public sealed class UI_LiarsDice
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly Label Score;
        private readonly Label Mood;
        private readonly VisualElement OpponentRow;
        private readonly VisualElement PlayerRow;
        private readonly Label Bid;
        private readonly Label Coach;
        private readonly Label Message;
        private readonly Label CountLabel;
        private readonly VisualElement FaceRow;
        private readonly VisualElement Controls;
        private readonly Button BidButton;
        private readonly Button CallButton;
        private readonly Button NextButton;
        private readonly List<Button> FaceButtons = new List<Button>();

        private LiarsDiceGame Game;
        private string OpponentName;
        private string Place;
        private int PickCount = 1;
        private int PickFace = 1;
        private IVisualElementScheduledItem Thinking;

        /// <summary>한 판이 끝까지 끝났다. 플레이어가 이겼으면 참.</summary>
        public event Action<bool> Finished;

        /// <summary>도중에 「그만둔다」. 진 것으로 친다.</summary>
        public event Action Left;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_LiarsDice(VisualElement root)
        {
            Root = root;
            Overlay = new VisualElement();
            Overlay.AddToClassList("dice");

            var header = new VisualElement();
            header.AddToClassList("ledger-header");
            var title = new Label(StringConstants.TEXT_DICE_TITLE);
            title.AddToClassList("ledger-title");
            Score = new Label();
            Score.AddToClassList("ledger-suspicion");
            header.Add(title);
            header.Add(Score);

            var hint = new Label(StringConstants.TEXT_DICE_HINT);
            hint.AddToClassList("ledger-hint");

            var table = new VisualElement();
            table.AddToClassList("popup");
            table.AddToClassList("dice-table");

            Mood = new Label();
            Mood.AddToClassList("dice-mood");
            OpponentRow = new VisualElement();
            OpponentRow.AddToClassList("dice-row");
            Bid = new Label();
            Bid.AddToClassList("dice-bid");

            // 연습 판의 코치, 실전의 판단 재료 한 줄.
            Coach = new Label();
            Coach.AddToClassList("dice-coach");
            PlayerRow = new VisualElement();
            PlayerRow.AddToClassList("dice-row");

            Controls = new VisualElement();
            Controls.AddToClassList("dice-controls");
            var countBox = new VisualElement();
            countBox.AddToClassList("dice-count");
            var minus = new Button(() => ChangeCount(-1));
            minus.text = StringConstants.TEXT_DICE_MINUS;
            minus.AddToClassList("popup-button");
            minus.AddToClassList("dice-step");
            CountLabel = new Label();
            CountLabel.AddToClassList("dice-count-label");
            var plus = new Button(() => ChangeCount(1));
            plus.text = StringConstants.TEXT_DICE_PLUS;
            plus.AddToClassList("popup-button");
            plus.AddToClassList("dice-step");
            countBox.Add(minus);
            countBox.Add(CountLabel);
            countBox.Add(plus);

            FaceRow = new VisualElement();
            FaceRow.AddToClassList("dice-faces");

            for (var face = 1; face <= Constants.DICE_FACES; face++)
            {
                var pick = face;
                var button = new Button(() => PickFaceOf(pick));
                button.text = face.ToString();
                button.AddToClassList("dice-face-button");
                FaceRow.Add(button);
                FaceButtons.Add(button);
            }

            BidButton = new Button(OnBid);
            BidButton.text = StringConstants.TEXT_DICE_BTN_BID;
            BidButton.AddToClassList("popup-button");
            BidButton.AddToClassList("popup-button--main");
            BidButton.AddToClassList("dice-action");
            CallButton = new Button(OnCall);
            CallButton.text = StringConstants.TEXT_DICE_BTN_CALL;
            CallButton.AddToClassList("popup-button");
            CallButton.AddToClassList("dice-action");

            var pickRow = new VisualElement();
            pickRow.AddToClassList("dice-pick");
            pickRow.Add(countBox);
            pickRow.Add(FaceRow);
            var actionRow = new VisualElement();
            actionRow.AddToClassList("dice-actions");
            actionRow.Add(BidButton);
            actionRow.Add(CallButton);
            Controls.Add(pickRow);
            Controls.Add(actionRow);

            NextButton = new Button(OnNext);
            NextButton.AddToClassList("popup-button");
            NextButton.AddToClassList("popup-button--main");
            NextButton.AddToClassList("dice-next");

            table.Add(Mood);
            table.Add(OpponentRow);
            table.Add(Bid);
            table.Add(Coach);
            table.Add(PlayerRow);
            table.Add(Controls);
            table.Add(NextButton);

            var footer = new VisualElement();
            footer.AddToClassList("ledger-footer");
            Message = new Label();
            Message.AddToClassList("ledger-message");
            var leave = new Button(OnLeave);
            leave.text = StringConstants.TEXT_DICE_LEAVE;
            leave.AddToClassList("popup-button");
            leave.AddToClassList("ledger-leave");
            footer.Add(Message);
            footer.Add(leave);

            Overlay.Add(header);
            Overlay.Add(hint);
            Overlay.Add(table);
            Overlay.Add(footer);
        }

        /// <summary>판을 편다. <paramref name="placeClass"/>는 배경 그림 클래스(대화 배경과 같다).</summary>
        public void Open(LiarsDiceGame game, string opponentName, string mood, string placeClass)
        {
            Game = game;
            OpponentName = opponentName;
            Mood.text = string.Format(StringConstants.TEXT_DICE_MOOD_LINE, opponentName, mood);

            if (string.IsNullOrEmpty(Place) == false)
            {
                Overlay.RemoveFromClassList(Place);
            }

            Place = placeClass;
            Overlay.AddToClassList(Place);

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
            BeginTurn();
        }

        public void Close()
        {
            StopThinking();
            Overlay.RemoveFromHierarchy();
        }

        // 판의 지금 상태대로 다시 그리고, 상대 차례면 뜸을 들인 뒤 상대가 움직인다.
        private void BeginTurn()
        {
            Refresh();

            if (Game.IsRevealed == false && Game.IsPlayerTurn == false)
            {
                Message.text = StringConstants.TEXT_DICE_THINKING;
                StopThinking();
                Thinking = Overlay.schedule.Execute(OpponentMove)
                    .StartingIn((long)(Constants.DICE_THINK_SECONDS * 1000f));
            }
        }

        private void OpponentMove()
        {
            Thinking = null;

            if (Game == null)
            {
                return;
            }

            var raised = Game.OpponentAct();

            if (raised == true)
            {
                // 다음 내 말의 기본값은 상대 말 바로 위.
                PickCount = Game.BidCount;
                PickFace = Game.BidFace;
                StepToValid();
                Message.text = StringConstants.TEXT_DICE_YOUR_TURN;
                Refresh();
                return;
            }

            Message.text = string.Format(StringConstants.TEXT_DICE_CALL, OpponentName);
            Refresh();
        }

        private void Refresh()
        {
            Score.text = string.Format(StringConstants.TEXT_DICE_SCORE, Game.PlayerWins, Game.OpponentWins, OpponentName);
            FillRow(OpponentRow, Game.OpponentDice, Game.IsRevealed == false);
            FillRow(PlayerRow, Game.PlayerDice, false);

            if (Game.BidCount == 0)
            {
                Bid.text = StringConstants.TEXT_DICE_NO_BID;
            }
            else
            {
                var who = Game.BidByPlayer == true ? StringConstants.TEXT_DICE_ME : OpponentName;
                Bid.text = string.Format(StringConstants.TEXT_DICE_BID, who, Game.BidFace, Game.BidCount);
            }

            if (Game.IsRevealed == true)
            {
                Bid.text = string.Format(StringConstants.TEXT_DICE_REVEAL, Game.BidFace, Game.RevealedCount,
                    Game.RevealPlayerWon == true ? StringConstants.TEXT_DICE_ROUND_WON : StringConstants.TEXT_DICE_ROUND_LOST);
            }

            var myTurn = Game.IsRevealed == false && Game.IsPlayerTurn == true;
            Controls.style.display = myTurn == true ? DisplayStyle.Flex : DisplayStyle.None;
            NextButton.style.display = Game.IsRevealed == true ? DisplayStyle.Flex : DisplayStyle.None;
            NextButton.text = Game.IsOver == true ? StringConstants.TEXT_DICE_BTN_END : StringConstants.TEXT_DICE_BTN_NEXT;
            CallButton.SetEnabled(Game.BidCount > 0);

            if (myTurn == true && Game.BidCount == 0)
            {
                Message.text = StringConstants.TEXT_DICE_YOUR_OPEN;
            }

            CountLabel.text = string.Format(StringConstants.TEXT_DICE_COUNT, PickCount);

            for (var i = 0; i < FaceButtons.Count; i++)
            {
                FaceButtons[i].EnableInClassList("dice-face-button--on", i + 1 == PickFace);
            }

            BidButton.SetEnabled(Game.IsValidRaise(PickCount, PickFace));
            RefreshCoach(myTurn);
        }

        // 연습 판이면 각본의 코치를 띄우고 배울 동작 말고는 잠근다. 실전이면 내 차례에 판단 재료만 준다.
        private void RefreshCoach(bool myTurn)
        {
            var scripted = Game.CurrentScriptRound;
            var text = string.Empty;

            if (scripted != null)
            {
                if (Game.IsRevealed == true)
                {
                    text = scripted.RevealHint;
                }
                else if (myTurn == true && Game.BidCount == 0)
                {
                    text = scripted.OpenHint;
                }
                else if (myTurn == true)
                {
                    text = scripted.ResponseHint;
                    BidButton.SetEnabled(scripted.AllowRaise == true && Game.IsValidRaise(PickCount, PickFace));
                    CallButton.SetEnabled(scripted.AllowCall);
                }
            }
            else if (myTurn == true && Game.BidCount > 0)
            {
                var mine = 0;

                foreach (var die in Game.PlayerDice)
                {
                    if (die == Game.BidFace)
                    {
                        mine++;
                    }
                }

                text = string.Format(StringConstants.TEXT_DICE_HELP, Game.BidFace, mine, Game.OpponentDice.Length);
            }

            Coach.text = text ?? string.Empty;
            Coach.style.display = string.IsNullOrEmpty(text) == true ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // 주사위 한 줄. 가린 주사위는 「?」, 판이 열리면 걸린 눈과 같은 주사위를 밝힌다.
        private void FillRow(VisualElement row, int[] dice, bool hidden)
        {
            row.Clear();

            for (var i = 0; i < dice.Length; i++)
            {
                var die = new Label(hidden == true ? StringConstants.TEXT_DICE_HIDDEN : dice[i].ToString());
                die.AddToClassList("dice-die");
                die.EnableInClassList("dice-die--hidden", hidden);
                die.EnableInClassList("dice-die--match", hidden == false && Game.BidCount > 0 && dice[i] == Game.BidFace);
                row.Add(die);
            }
        }

        private void ChangeCount(int delta)
        {
            PickCount = Math.Max(1, Math.Min(Game.TotalDice, PickCount + delta));
            Refresh();
        }

        private void PickFaceOf(int face)
        {
            PickFace = face;
            Refresh();
        }

        // 지금 고른 말이 걸린 말보다 낮으면 가장 가까운 높은 말로 올린다.
        private void StepToValid()
        {
            if (Game.IsValidRaise(PickCount, PickFace) == true)
            {
                return;
            }

            if (PickFace < Constants.DICE_FACES)
            {
                PickFace++;
            }
            else
            {
                PickCount = Math.Min(Game.TotalDice, PickCount + 1);
                PickFace = 1;
            }
        }

        private void OnBid()
        {
            if (Game.PlayerBid(PickCount, PickFace) == true)
            {
                BeginTurn();
            }
        }

        private void OnCall()
        {
            if (Game.PlayerCall() == true)
            {
                Message.text = string.Format(StringConstants.TEXT_DICE_CALL, StringConstants.TEXT_DICE_ME);
                Refresh();
            }
        }

        private void OnNext()
        {
            if (Game.IsOver == true)
            {
                var handler = Finished;

                if (handler != null)
                {
                    handler(Game.PlayerWonMatch);
                }

                return;
            }

            Game.StartRound();
            PickCount = 1;
            PickFace = 1;
            Message.text = string.Empty;
            BeginTurn();
        }

        private void OnLeave()
        {
            StopThinking();
            var handler = Left;

            if (handler != null)
            {
                handler();
            }
        }

        private void StopThinking()
        {
            if (Thinking != null)
            {
                Thinking.Pause();
                Thinking = null;
            }
        }
    }
}
