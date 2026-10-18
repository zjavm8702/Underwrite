using System;
using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Village;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 길드 기록 짜맞추기 화면. 왼쪽에 기록 쪽지, 오른쪽에 단마다 목적지·의뢰인을 채우는 표.
    /// 칸을 누를 때마다 보기가 돌아간다(빈칸 → 첫 보기 → … → 빈칸). 맞춰 보기는 <see cref="GuildDeduction"/>이 판정한다.
    /// </summary>
    public sealed class UI_GuildDeduction
    {
        private readonly VisualElement Root;
        private readonly VisualElement Overlay;
        private readonly VisualElement ClueList;
        private readonly VisualElement Table;
        private readonly Label Tries;
        private readonly Label Message;
        private readonly Button LeaveButton;

        private GuildDeduction Puzzle;
        private string Place;
        private int[] DestPick;
        private int[] ClientPick;
        private readonly List<Button> DestButtons = new List<Button>();
        private readonly List<Button> ClientButtons = new List<Button>();

        /// <summary>다 맞혔다.</summary>
        public event Action Solved;

        /// <summary>틀렸다. 기회가 남았으면 거짓, 다 썼으면 참을 넘긴다.</summary>
        public event Action<bool> Missed;

        /// <summary>「그만둔다」.</summary>
        public event Action Left;

        public bool IsOpen
        {
            get
            {
                return Overlay.parent != null;
            }
        }

        public UI_GuildDeduction(VisualElement root)
        {
            Root = root;
            Overlay = new VisualElement();
            Overlay.AddToClassList("deduce");

            var header = new VisualElement();
            header.AddToClassList("ledger-header");
            var title = new Label(StringConstants.TEXT_DEDUCE_TITLE);
            title.AddToClassList("ledger-title");
            Tries = new Label();
            Tries.AddToClassList("ledger-suspicion");
            header.Add(title);
            header.Add(Tries);

            var hint = new Label(StringConstants.TEXT_DEDUCE_HINT);
            hint.AddToClassList("ledger-hint");

            var body = new VisualElement();
            body.AddToClassList("deduce-body");

            var cluePage = new VisualElement();
            cluePage.AddToClassList("deduce-clues");
            var clueTitle = new Label(StringConstants.TEXT_DEDUCE_CLUES);
            clueTitle.AddToClassList("ledger-title");
            ClueList = new VisualElement();
            ClueList.AddToClassList("deduce-clue-list");
            cluePage.Add(clueTitle);
            cluePage.Add(ClueList);

            Table = new VisualElement();
            Table.AddToClassList("popup");
            Table.AddToClassList("deduce-table");

            body.Add(cluePage);
            body.Add(Table);

            var footer = new VisualElement();
            footer.AddToClassList("ledger-footer");
            Message = new Label();
            Message.AddToClassList("ledger-message");
            var check = new Button(OnCheck);
            check.text = StringConstants.TEXT_DEDUCE_CHECK;
            check.AddToClassList("popup-button");
            check.AddToClassList("popup-button--main");
            check.AddToClassList("ledger-leave");
            LeaveButton = new Button(OnLeave);
            LeaveButton.text = StringConstants.TEXT_DEDUCE_LEAVE;
            LeaveButton.AddToClassList("popup-button");
            LeaveButton.AddToClassList("ledger-leave");
            footer.Add(Message);
            footer.Add(check);
            footer.Add(LeaveButton);

            Overlay.Add(header);
            Overlay.Add(hint);
            Overlay.Add(body);
            Overlay.Add(footer);
        }

        public void Open(GuildDeduction puzzle, string placeClass)
        {
            Puzzle = puzzle;
            // 연습 기록은 기회를 세지 않는다 — 대신 첫 줄에 길잡이를 띄운다.
            Message.text = puzzle.IsPractice == true ? StringConstants.TEXT_DEDUCE_TUT_HINT : string.Empty;
            Tries.style.display = puzzle.IsPractice == true ? DisplayStyle.None : DisplayStyle.Flex;
            LeaveButton.style.display = puzzle.IsPractice == true ? DisplayStyle.None : DisplayStyle.Flex;
            DestPick = new[] { -1, -1, -1 };
            ClientPick = new[] { -1, -1, -1 };

            if (string.IsNullOrEmpty(Place) == false)
            {
                Overlay.RemoveFromClassList(Place);
            }

            Place = placeClass;
            Overlay.AddToClassList(Place);

            ClueList.Clear();
            var tilts = Constants.SMITH_SLIP_TILTS;

            for (var i = 0; i < puzzle.Clues.Count; i++)
            {
                var slip = new Label(puzzle.Clues[i]);
                slip.AddToClassList("popup");
                slip.AddToClassList("deduce-clue");
                slip.style.rotate = new Rotate(new Angle(tilts[i % tilts.Length], AngleUnit.Degree));
                ClueList.Add(slip);
            }

            BuildTable();
            ShowTries();

            if (Overlay.parent == null)
            {
                Root.Add(Overlay);
            }

            Overlay.BringToFront();
        }

        public void Close()
        {
            Overlay.RemoveFromHierarchy();
        }

        private void BuildTable()
        {
            Table.Clear();
            DestButtons.Clear();
            ClientButtons.Clear();

            var title = new Label(StringConstants.TEXT_DEDUCE_TABLE);
            title.AddToClassList("popup-title");
            Table.Add(title);

            var head = new VisualElement();
            head.AddToClassList("deduce-row");
            head.Add(Cell(string.Empty, "deduce-band"));
            head.Add(Cell(StringConstants.TEXT_DEDUCE_DEST_HEAD, "deduce-head"));
            head.Add(Cell(StringConstants.TEXT_DEDUCE_CLIENT_HEAD, "deduce-head"));
            Table.Add(head);

            for (var row = 0; row < GuildDeduction.SIZE; row++)
            {
                var index = row;
                var line = new VisualElement();
                line.AddToClassList("deduce-row");
                line.Add(Cell(Puzzle.BandNames[row], "deduce-band"));

                var dest = new Button(() => Cycle(DestPick, index, DestButtons, Puzzle.Destinations));
                dest.AddToClassList("deduce-pick");
                var client = new Button(() => Cycle(ClientPick, index, ClientButtons, Puzzle.Clients));
                client.AddToClassList("deduce-pick");
                line.Add(dest);
                line.Add(client);
                DestButtons.Add(dest);
                ClientButtons.Add(client);
                Table.Add(line);
            }

            // 고를 수 있는 보기를 늘 보이게 둔다. 칸을 눌러 봐야만 알면 추리가 아니라 뒤지기가 된다.
            var options = new Label(string.Format(StringConstants.TEXT_DEDUCE_OPTIONS,
                string.Join(StringConstants.TEXT_DEDUCE_OPTION_SEPARATOR, Puzzle.Destinations),
                string.Join(StringConstants.TEXT_DEDUCE_OPTION_SEPARATOR, Puzzle.Clients)));
            options.AddToClassList("deduce-options");
            Table.Add(options);

            RefreshPicks();
        }

        // 빈칸(-1) → 0 → 1 → 2 → 빈칸.
        private void Cycle(int[] picks, int row, List<Button> buttons, string[] options)
        {
            picks[row] = picks[row] + 1 >= options.Length ? -1 : picks[row] + 1;
            Message.text = string.Empty;
            RefreshPicks();
        }

        private void RefreshPicks()
        {
            for (var row = 0; row < GuildDeduction.SIZE; row++)
            {
                DestButtons[row].text = DestPick[row] < 0 ? StringConstants.TEXT_DEDUCE_BLANK : Puzzle.Destinations[DestPick[row]];
                ClientButtons[row].text = ClientPick[row] < 0 ? StringConstants.TEXT_DEDUCE_BLANK : Puzzle.Clients[ClientPick[row]];
                DestButtons[row].EnableInClassList("deduce-pick--blank", DestPick[row] < 0);
                ClientButtons[row].EnableInClassList("deduce-pick--blank", ClientPick[row] < 0);
            }
        }

        private void ShowTries()
        {
            Tries.text = string.Format(StringConstants.TEXT_DEDUCE_TRIES, Puzzle.TriesLeft, Constants.DEDUCE_TRIES);
        }

        private void OnCheck()
        {
            for (var row = 0; row < GuildDeduction.SIZE; row++)
            {
                if (DestPick[row] < 0 || ClientPick[row] < 0)
                {
                    Message.text = StringConstants.TEXT_DEDUCE_NEED_ALL;
                    return;
                }
            }

            if (Puzzle.Check(DestPick, ClientPick) == true)
            {
                var solved = Solved;

                if (solved != null)
                {
                    solved();
                }

                return;
            }

            Message.text = StringConstants.TEXT_DEDUCE_WRONG;
            ShowTries();
            var missed = Missed;

            if (missed != null)
            {
                missed(Puzzle.IsOutOfTries);
            }
        }

        private void OnLeave()
        {
            var handler = Left;

            if (handler != null)
            {
                handler();
            }
        }

        private static Label Cell(string text, string kind)
        {
            var cell = new Label(text);
            cell.AddToClassList(kind);
            cell.pickingMode = PickingMode.Ignore;
            return cell;
        }
    }
}
