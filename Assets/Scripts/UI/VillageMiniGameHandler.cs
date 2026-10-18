using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEngine;

namespace Underwrite.UI
{
    /// <summary>
    /// 영지의 정보 모으기 놀이 — 장부 맞추기·주문서 읽기·기록 짜맞추기·주사위 내기·엿듣기와 일거리(주문 나르기).
    /// 자리의 선택지를 만들고, 판을 열고, 결과를 대화로 알린다. 프롤로그의 연습 판도 여기서 연다.
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립한다(2026-10-19 정리 — 화면에서 떼어 냄).
    /// </summary>
    public sealed class VillageMiniGameHandler
    {
        private readonly VillageScreenController Host;

        public VillageMiniGameHandler(VillageScreenController host)
        {
            Host = host;
        }

        // ── 주문 나르기 (2026-10-17 사장님 결정 — 실제로 해야 끝나는 일) ───────

        private UI_WorkOrders Work;
        private VillageSpot WorkSpot;

        private bool IsWorkOpen
        {
            get
            {
                return Work != null && Work.IsOpen == true;
            }
        }

        /// <summary>손놀림·엿듣기 화면이 떠 있는가. 떠 있는 동안 주인공은 걷지 않고 휠은 카메라로 새지 않는다.</summary>
        public bool IsHandOpen
        {
            get
            {
                return IsWorkOpen == true || (Eaves != null && Eaves.IsOpen == true);
            }
        }

        // ── 여관 엿듣기 (시간구조 설계안 §7) ───────
        // 술을 나르며 테이블 사이를 돈다. 누구 곁에 머물지 고르는 판단형. 하루에 한 번.

        private UI_Eavesdrop Eaves;
        private EavesdropSession EavesSession;
        private int EavesDay;

        public DialogueChoice EavesChoice()
        {
            var choice = new DialogueChoice(StringConstants.TEXT_EAVES_CHOICE, StartEaves);

            if (EavesDay == Host.Today())
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_EAVES_DONE_TODAY;
            }
            else if (ActionPoints.Has(Constants.AP_MINIGAME) == false)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_GATHER_NO_TIME;
            }
            else
            {
                choice.Note = string.Format(StringConstants.TEXT_EAVES_CHOICE_NOTE, Constants.EAVES_TURNS,
                    Constants.AP_MINIGAME);
            }

            return choice;
        }

        private void StartEaves()
        {
            var heroesTalk = HeroRoster.Favorite() != null && PatronageLedger.HasOverhear(Host.Today()) == true;
            EavesSession = EavesdropSession.Create(BreakClock.Simulation, heroesTalk,
                new System.Random(Random.Range(int.MinValue, int.MaxValue)));

            if (EavesSession == null)
            {
                Host.Dialogue.Play(new List<DialogueLine>
                {
                    new DialogueLine(string.Empty, StringConstants.TEXT_EAVES_TITLE, StringConstants.TEXT_EAVES_NONE)
                }, null);
                return;
            }

            Host.Dialogue.Close();
            EavesDay = Host.Today();
            ActionPoints.Spend(Constants.AP_MINIGAME);

            if (Eaves == null)
            {
                Eaves = new UI_Eavesdrop(Host.Root);
                Eaves.Listened += ListenAt;
                Eaves.Left += () => Eaves.Close();
            }

            Eaves.Open(EavesSession);
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        private void ListenAt(EavesdropSession.Table table)
        {
            var heard = EavesSession.Listen(table);

            if (heard == false)
            {
                if (EavesSession.IsCaught == true)
                {
                    Eaves.AddLine(string.Format(StringConstants.TEXT_EAVES_CAUGHT, table.Name));
                    GameAudio.Play(SoundEffect.Sound_Close);
                }

                Eaves.Refresh();
                return;
            }

            if (table.IsHeroes == true)
            {
                Eaves.AddLine(PatronageLedger.Overhear(Host.Today()) ?? StringConstants.TEXT_EAVES_HEROES_NOTHING);
            }
            else if (table.IsDone == true)
            {
                var fact = BandCodex.Learn(table.BandId, CodexSource.Codex_Tavern);

                if (fact != null)
                {
                    GatherSession.Record(0f, 0f, new GatheredNote(table.BandId, fact, InfoSource.Source_TavernTalk));
                    Eaves.AddLine(string.Format(StringConstants.TEXT_EAVES_FULL, table.Name, fact));
                }
            }
            else
            {
                Eaves.AddLine(EavesSession.FragmentOf(BreakClock.Simulation, table));
            }

            GameAudio.Play(SoundEffect.Sound_Page);
            Eaves.Refresh();
        }

        public void StartWorkOrders(VillageSpot spot)
        {
            Host.Dialogue.Close();
            WorkSpot = spot;

            if (Work == null)
            {
                Work = new UI_WorkOrders(Host.Root);
                Work.Finished += accuracy =>
                {
                    Work.Close();
                    FinishWork(WorkSpot, Mathf.Lerp(Constants.WORK_MIN_PAY, 1f, accuracy));
                };
                Work.Skipped += () =>
                {
                    Work.Close();
                    FinishWork(WorkSpot, Constants.WORK_SLOPPY_PAY);
                };
            }

            string[] items;
            string[] places;
            string title;

            switch (spot.InfoSource)
            {
                case InfoSource.Source_SmithOrder:
                {
                    items = StringConstants.WORK_SMITH_ITEMS;
                    places = StringConstants.WORK_SMITH_PLACES;
                    title = StringConstants.TEXT_WORK_TITLE_SMITH;
                    break;
                }
                case InfoSource.Source_GuildRecord:
                {
                    items = StringConstants.WORK_GUILD_ITEMS;
                    places = StringConstants.WORK_GUILD_PLACES;
                    title = StringConstants.TEXT_WORK_TITLE_GUILD;
                    break;
                }
                default:
                {
                    items = StringConstants.WORK_TAVERN_ITEMS;
                    places = StringConstants.WORK_TAVERN_PLACES;
                    title = StringConstants.TEXT_WORK_TITLE_TAVERN;
                    break;
                }
            }

            var task = WorkOrderTask.Create(items, places, Constants.WORK_ORDERS,
                new System.Random(Random.Range(int.MinValue, int.MaxValue)));
            Work.Open(task, title, VillageScreenController.BackgroundOf(spot.InfoSource));
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        /// <summary>일을 마쳤다. 품삯은 손놀림만큼(<paramref name="pay"/>는 정해진 품삯에 곱하는 비율).</summary>
        private void FinishWork(VillageSpot spot, float pay)
        {
            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(spot.InfoSource));

            var quest = spot.Work;
            var reward = Mathf.Round(quest.Reward * pay);
            GameAudio.Play(SoundEffect.Sound_Coins);
            var text = quest.Done;
            GatheredNote note = null;
            string rumorStock = null;

            // 여관 일꾼의 자리. 단골인 용사단의 이야기가 들린다(후원흐름 설계안 §2-2).
            // 의뢰에 대한 반응이 있으면 그것이, 없으면 아직 못 들은 사정이 먼저다.
            var overheard = spot.HasCellar == true ? PatronageLedger.Overhear(Host.Today()) : null;

            if (overheard != null)
            {
                text = text + " " + overheard;
            }
            else if (quest.GivesRumor == true)
            {
                var rows = Host.NextSlotRows();

                if (rows.Count > 0)
                {
                    var pick = rows[Random.Range(0, rows.Count)];
                    var name = Host.StockName(pick.StockId);
                    note = new GatheredNote(pick.StockId, string.Format(StringConstants.TEXT_GATHER_VAGUE_NOTE, name), spot.InfoSource);
                    text = text + " " + string.Format(StringConstants.TEXT_WORK_RUMOR, name);
                    rumorStock = pick.StockId;
                }
            }

            GatherSession.Record(0f, reward, note);

            var line = Host.OwnerLineFor(spot, text);
            line.IsSecret = note != null;
            line.Note = string.Format(StringConstants.TEXT_WORK_RESULT, reward, ActionPoints.Current);
            var held = rumorStock == null ? null : VillageScreenController.HeldNote(rumorStock);

            line.Badge = held;

            Host.Dialogue.Play(new List<DialogueLine> { line },
                Host.Back(spot));
        }

        // ── 장부 맞추기 (시간구조 설계안 §7 — 첫 추리형 정보 모으기) ───────
        // 의뢰 기록과 지급 기록을 맞춰 어긋난 단을 찾고, 그것이 무슨 뜻인지 짐작한다. 맞히면 그 단의 대장간 사정을 안다.
        // 돈 대신 시간과 기록 담당의 의심을 건다. 하루에 한 번.

        private UI_LedgerPuzzle Ledger;
        private LedgerPuzzle Puzzle;
        private VillageSpot LedgerSpot;

        /// <summary>추리형 정보 모으기 화면(장부 맞추기·주문서 읽기·기록 짜맞추기·주사위)이 떠 있는가.</summary>
        public bool IsPuzzleOpen
        {
            get
            {
                return (Ledger != null && Ledger.IsOpen == true) || (Smith != null && Smith.IsOpen == true)
                    || (Dice != null && Dice.IsOpen == true) || (Deduce != null && Deduce.IsOpen == true);
            }
        }

        public DialogueChoice LedgerChoice(VillageSpot spot)
        {
            var choice = new DialogueChoice(StringConstants.TEXT_LEDGER_CHOICE, () => StartLedger(spot));
            if (BreakClock.Simulation == null)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_LEDGER_NONE;
            }
            else if (StoryProgress.LedgerDay == Host.Today())
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_LEDGER_DONE_TODAY;
            }
            else if (ActionPoints.Has(Constants.AP_MINIGAME) == false)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_GATHER_NO_TIME;
            }
            else
            {
                choice.Note = string.Format(StringConstants.TEXT_LEDGER_CHOICE_NOTE, Constants.AP_MINIGAME);
            }

            return choice;
        }

        private void StartLedger(VillageSpot spot)
        {
            Puzzle = LedgerPuzzle.Create(BreakClock.Simulation, new System.Random(Random.Range(int.MinValue, int.MaxValue)),
                Host.Today());

            if (Puzzle == null)
            {
                Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(spot, StringConstants.TEXT_LEDGER_NONE) }, null);
                return;
            }

            Host.Dialogue.Close();
            LedgerSpot = spot;
            StoryProgress.LedgerDay = Host.Today();
            ActionPoints.Spend(Constants.AP_MINIGAME);

            if (Ledger == null)
            {
                Ledger = new UI_LedgerPuzzle(Host.Root);
                Ledger.Picked += PickLedgerRow;
                Ledger.Left += () => Ledger.Close();
            }

            Ledger.Open(Puzzle);
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        private void PickLedgerRow(int index)
        {
            if (Puzzle == null)
            {
                return;
            }

            if (Puzzle.Check(index) == true)
            {
                Ledger.Close();
                DeduceLedger();
                return;
            }

            Ledger.MarkWrong(index, Puzzle.Suspicion);
            GameAudio.Play(SoundEffect.Sound_Close);

            if (Puzzle.IsKickedOut == true)
            {
                Ledger.Close();
                Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(LedgerSpot.InfoSource));
                Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(LedgerSpot, StringConstants.TEXT_LEDGER_KICKED) },
                    null);
            }
        }

        // 찾았다. 무슨 뜻인지 고른다. 맞는 답은 섞어서 보인다.
        private void DeduceLedger()
        {
            var name = Host.StockName(Puzzle.TargetId);
            var options = Puzzle.TargetGains == true
                ? StringConstants.TEXT_LEDGER_GAIN_OPTIONS
                : StringConstants.TEXT_LEDGER_LOSS_OPTIONS;
            var order = new List<int>();

            for (var i = 0; i < options.Length; i++)
            {
                order.Insert(Random.Range(0, order.Count + 1), i);
            }

            var choices = new List<DialogueChoice>();

            for (var i = 0; i < order.Count; i++)
            {
                var right = order[i] == 0;
                choices.Add(new DialogueChoice(options[order[i]], () => ConcludeLedger(right)));
            }

            var ask = new DialogueLine(string.Empty, StringConstants.TEXT_LEDGER_TAG, StringConstants.TEXT_LEDGER_ASK);
            ask.Choices = choices;

            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(LedgerSpot.InfoSource));
            Host.Dialogue.Play(new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_LEDGER_TAG,
                    string.Format(StringConstants.TEXT_LEDGER_FOUND, name, Puzzle.TargetReward, Puzzle.TargetPaid)),
                new DialogueLine(string.Empty, StringConstants.TEXT_LEDGER_TAG,
                    Puzzle.TargetGains == true ? StringConstants.TEXT_LEDGER_CLUE_GAIN : StringConstants.TEXT_LEDGER_CLUE_LOSS),
                ask
            }, null);
        }

        private void ConcludeLedger(bool right)
        {
            if (right == false)
            {
                var miss = new DialogueLine(string.Empty, StringConstants.TEXT_LEDGER_TAG,
                    StringConstants.TEXT_LEDGER_WRONG_GUESS);
                Host.Dialogue.Play(new List<DialogueLine> { miss }, null);
                return;
            }

            var id = Puzzle.TargetId;
            var fact = BandCodex.Learn(id, CodexSource.Codex_Smith);

            if (fact != null)
            {
                GatherSession.Record(0f, 0f, new GatheredNote(id, fact, InfoSource.Source_SmithOrder));
            }

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_LEDGER_TAG,
                string.Format(StringConstants.TEXT_LEDGER_RIGHT, Host.StockName(id), fact ?? string.Empty));
            Host.Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        // ── 주문서 읽기 (대장간 — 셋째 정보 모으기, 추리형) ───────
        // 여관에서 도는 소문 하나를 주문서로 가린다. 그 단의 이번 계절, 취소되지 않은 주문을 짚고 맞는 말인지 판정한다.
        // 맞히면 도감의 소문 줄에 확인 표시가 붙는다. 엿듣기로 듣고 여기서 가리는 짝이다. 하루에 한 번.

        private UI_SmithOrders Smith;
        private SmithOrderPuzzle Orders;
        private VillageSpot SmithSpot;

        public DialogueChoice SmithOrderChoice(VillageSpot spot)
        {
            var choice = new DialogueChoice(StringConstants.TEXT_SMITH_CHOICE, () => StartSmithOrders(spot));
            if (BreakClock.Simulation == null)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_SMITH_NONE;
            }
            else if (StoryProgress.SmithDay == Host.Today())
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_SMITH_DONE_TODAY;
            }
            else if (ActionPoints.Has(Constants.AP_MINIGAME) == false)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_GATHER_NO_TIME;
            }
            else
            {
                choice.Note = string.Format(StringConstants.TEXT_SMITH_CHOICE_NOTE, Constants.AP_MINIGAME);
            }

            return choice;
        }

        private void StartSmithOrders(VillageSpot spot)
        {
            Orders = SmithOrderPuzzle.Create(BreakClock.Simulation,
                new System.Random(Random.Range(int.MinValue, int.MaxValue)), Host.Today());

            if (Orders == null)
            {
                Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(spot, StringConstants.TEXT_SMITH_NONE) }, null);
                return;
            }

            Host.Dialogue.Close();
            SmithSpot = spot;
            StoryProgress.SmithDay = Host.Today();
            ActionPoints.Spend(Constants.AP_MINIGAME);

            EnsureSmithScreen();
            SmithPracticeDone = null;
            Smith.Open(Orders, Host.StockName(Orders.TargetId), VillageScreenController.BackgroundOf(spot.InfoSource));
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        // 연습 꽂이가 끝나면 부를 것(프롤로그 대장간 정거장).
        private System.Action SmithPracticeDone;

        private void EnsureSmithScreen()
        {
            if (Smith != null)
            {
                return;
            }

            Smith = new UI_SmithOrders(Host.Root);
            Smith.Picked += PickSmithSlip;
            Smith.Left += () =>
            {
                Smith.Close();
                EndSmithPractice();
            };
        }

        /// <summary>연습 꽂이. 시간·의심 없이, 증거를 짚으면 끝난다(중간에 그만둬도 끝).</summary>
        public void StartSmithPractice(System.Action onDone)
        {
            Orders = SmithOrderPuzzle.CreatePractice();
            SmithSpot = Host.FindSpot(InfoSource.Source_SmithOrder);
            SmithPracticeDone = onDone;
            EnsureSmithScreen();
            Smith.Open(Orders, string.Empty, VillageScreenController.BackgroundOf(InfoSource.Source_SmithOrder), StringConstants.TEXT_SMITH_TUT_TASK);
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        private void EndSmithPractice()
        {
            var done = SmithPracticeDone;
            SmithPracticeDone = null;

            if (done != null)
            {
                done();
            }
        }

        private void PickSmithSlip(int index)
        {
            if (Orders == null)
            {
                return;
            }

            var pick = Orders.Check(index);

            if (pick == SlipPick.Pick_Evidence && Orders.IsPractice == true)
            {
                Smith.Close();
                Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(InfoSource.Source_SmithOrder));
                Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(SmithSpot, StringConstants.TEXT_SMITH_TUT_DONE) },
                    EndSmithPractice);
                return;
            }

            if (pick == SlipPick.Pick_Evidence)
            {
                Smith.Close();
                JudgeRumor();
                return;
            }

            var message = StringConstants.TEXT_SMITH_WRONG_BAND;

            if (pick == SlipPick.Pick_LastSeason)
            {
                message = StringConstants.TEXT_SMITH_WRONG_OLD;
            }
            else if (pick == SlipPick.Pick_Cancelled)
            {
                message = StringConstants.TEXT_SMITH_WRONG_CANCELLED;
            }

            Smith.MarkWrong(index, Orders.Suspicion, message);
            GameAudio.Play(SoundEffect.Sound_Close);

            if (Orders.IsKickedOut == true)
            {
                Smith.Close();
                Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(SmithSpot.InfoSource));
                Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(SmithSpot, StringConstants.TEXT_SMITH_KICKED) },
                    null);
            }
        }

        // 증거를 찾았다. 맞는 말인지 헛말인지 고른다. 두 답의 순서는 섞는다.
        private void JudgeRumor()
        {
            var sayTrue = new DialogueChoice(StringConstants.TEXT_SMITH_VERDICT_TRUE, () => ConcludeRumor(true));
            var sayFalse = new DialogueChoice(StringConstants.TEXT_SMITH_VERDICT_FALSE, () => ConcludeRumor(false));
            var choices = Random.value < 0.5f
                ? new List<DialogueChoice> { sayTrue, sayFalse }
                : new List<DialogueChoice> { sayFalse, sayTrue };

            var ask = new DialogueLine(string.Empty, StringConstants.TEXT_SMITH_TAG,
                string.Format(StringConstants.TEXT_SMITH_ASK, Orders.Rumor));
            ask.Choices = choices;

            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(SmithSpot.InfoSource));
            Host.Dialogue.Play(new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_SMITH_TAG,
                    string.Format(StringConstants.TEXT_SMITH_FOUND, Host.StockName(Orders.TargetId), Orders.Evidence.Item)),
                ask
            }, null);
        }

        private void ConcludeRumor(bool saidTrue)
        {
            if (saidTrue != Orders.RumorTrue)
            {
                var miss = new DialogueLine(string.Empty, StringConstants.TEXT_SMITH_TAG,
                    StringConstants.TEXT_SMITH_WRONG_GUESS);
                Host.Dialogue.Play(new List<DialogueLine> { miss }, null);
                return;
            }

            var id = Orders.TargetId;
            BandCodex.Verify(id, Orders.RumorTrue);
            GatherSession.Record(0f, 0f, new GatheredNote(id, BandCodex.LineOf(id, CodexSource.Codex_Tavern),
                InfoSource.Source_SmithOrder));

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_SMITH_TAG,
                string.Format(Orders.RumorTrue == true ? StringConstants.TEXT_SMITH_RIGHT_TRUE : StringConstants.TEXT_SMITH_RIGHT_FALSE,
                    Host.StockName(id)));
            Host.Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        // ── 기록 짜맞추기 (길드 — 보드게임 「미니 크라임」을 줄인 추리) ───────
        // 기록 쪽지로 단 셋의 목적지·의뢰인을 짜맞춘다. 다 맞히면 먼 원정을 가는 단의 대장간 사정을 안다.
        // 장부 맞추기와 같은 보상이라 둘을 견줘 보고 하나를 고른다(2026-10-18 사장님 — 구현해 보고 정한다). 하루에 한 번.

        private UI_GuildDeduction Deduce;
        private GuildDeduction DeducePuzzle;
        private VillageSpot DeduceSpot;

        public DialogueChoice DeduceChoice(VillageSpot spot)
        {
            var choice = new DialogueChoice(StringConstants.TEXT_DEDUCE_CHOICE, () => StartDeduce(spot));
            if (BreakClock.Simulation == null)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_DEDUCE_NONE;
            }
            else if (StoryProgress.DeduceDay == Host.Today())
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_DEDUCE_DONE_TODAY;
            }
            else if (ActionPoints.Has(Constants.AP_MINIGAME) == false)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_GATHER_NO_TIME;
            }
            else
            {
                choice.Note = string.Format(StringConstants.TEXT_DEDUCE_CHOICE_NOTE, Constants.AP_MINIGAME);
            }

            return choice;
        }

        private void StartDeduce(VillageSpot spot)
        {
            DeducePuzzle = GuildDeduction.Create(BreakClock.Simulation,
                new System.Random(Random.Range(int.MinValue, int.MaxValue)));

            if (DeducePuzzle == null)
            {
                Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(spot, StringConstants.TEXT_DEDUCE_NONE) }, null);
                return;
            }

            Host.Dialogue.Close();
            DeduceSpot = spot;
            StoryProgress.DeduceDay = Host.Today();
            ActionPoints.Spend(Constants.AP_MINIGAME);

            EnsureDeduceScreen();
            DeducePracticeDone = null;
            Deduce.Open(DeducePuzzle, VillageScreenController.BackgroundOf(spot.InfoSource));
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        // 연습 기록이 끝나면 부를 것(프롤로그 길드 정거장).
        private System.Action DeducePracticeDone;

        private void EnsureDeduceScreen()
        {
            if (Deduce != null)
            {
                return;
            }

            Deduce = new UI_GuildDeduction(Host.Root);
            Deduce.Solved += () =>
            {
                Deduce.Close();

                if (DeducePuzzle.IsPractice == true)
                {
                    Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(InfoSource.Source_GuildRecord));
                    Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(DeduceSpot, StringConstants.TEXT_DEDUCE_TUT_DONE) },
                        EndDeducePractice);
                    return;
                }

                ConcludeDeduce();
            };
            Deduce.Missed += OnDeduceMissed;
            Deduce.Left += () =>
            {
                Deduce.Close();
                EndDeducePractice();
            };
        }

        /// <summary>연습 기록. 시간·기회 없이, 다 맞히면 끝난다(중간에 그만둬도 끝).</summary>
        public void StartDeducePractice(System.Action onDone)
        {
            DeducePuzzle = GuildDeduction.CreatePractice();
            DeduceSpot = Host.FindSpot(InfoSource.Source_GuildRecord);
            DeducePracticeDone = onDone;
            EnsureDeduceScreen();
            Deduce.Open(DeducePuzzle, VillageScreenController.BackgroundOf(InfoSource.Source_GuildRecord));
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        private void EndDeducePractice()
        {
            var done = DeducePracticeDone;
            DeducePracticeDone = null;

            if (done != null)
            {
                done();
            }
        }

        private void OnDeduceMissed(bool outOfTries)
        {
            if (DeducePuzzle.IsPractice == true)
            {
                return;
            }

            GameAudio.Play(SoundEffect.Sound_Close);

            if (outOfTries == false)
            {
                return;
            }

            Deduce.Close();
            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(DeduceSpot.InfoSource));
            Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(DeduceSpot, StringConstants.TEXT_DEDUCE_OUT) },
                null);
        }

        private void ConcludeDeduce()
        {
            var id = DeducePuzzle.TargetId;
            var fact = BandCodex.Learn(id, CodexSource.Codex_Smith);

            if (fact != null)
            {
                GatherSession.Record(0f, 0f, new GatheredNote(id, fact, InfoSource.Source_SmithOrder));
            }

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_DEDUCE_TAG,
                string.Format(StringConstants.TEXT_DEDUCE_RIGHT, DeducePuzzle.Destinations[0], Host.StockName(id), fact ?? string.Empty));
            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(DeduceSpot.InfoSource));
            Host.Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        // ── 주사위 내기 (여관 — 라이어스 다이스) ───────
        // 모험단 한 사람과 주사위로 겨룬다. 이기면 취한 상대가 그 단에 도는 말을 끝까지 털어놓고, 지면 술값을 낸다.
        // 엿듣기를 갈음할 여관 놀이 후보(2026-10-18 사장님과 합의 — 구현해 보고 정한다). 하루에 한 번.

        private UI_LiarsDice Dice;
        private LiarsDiceGame DiceGame;
        private string DiceBand;
        private VillageSpot DiceSpot;

        public DialogueChoice DiceChoice(VillageSpot spot)
        {
            var choice = new DialogueChoice(StringConstants.TEXT_DICE_CHOICE, () => StartDice(spot));
            var money = GatherSession.Available;

            if (BreakClock.Simulation == null)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_DICE_NONE;
            }
            else if (StoryProgress.DiceDay == Host.Today())
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_DICE_DONE_TODAY;
            }
            else if (ActionPoints.Has(Constants.AP_MINIGAME) == false)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_GATHER_NO_TIME;
            }
            else if (money < Constants.DICE_STAKE)
            {
                choice.IsEnabled = false;
                choice.Note = string.Format(StringConstants.TEXT_DICE_NO_MONEY, Constants.DICE_STAKE, money);
            }
            else
            {
                choice.Note = string.Format(StringConstants.TEXT_DICE_CHOICE_NOTE, Constants.AP_MINIGAME, Constants.DICE_STAKE);
            }

            return choice;
        }

        private void StartDice(VillageSpot spot)
        {
            DiceBand = PickDiceBand(BreakClock.Simulation);

            if (DiceBand == null)
            {
                Host.Dialogue.Play(new List<DialogueLine> { Host.OwnerLineFor(spot, StringConstants.TEXT_DICE_NONE) }, null);
                return;
            }

            Host.Dialogue.Close();
            DiceSpot = spot;
            StoryProgress.DiceDay = Host.Today();
            ActionPoints.Spend(Constants.AP_MINIGAME);

            var boldness = Random.Range(Constants.DICE_BOLDNESS_MIN, Constants.DICE_BOLDNESS_MAX);
            var moods = StringConstants.TEXT_DICE_MOODS;
            var mood = moods[Mathf.Clamp((int)(boldness * moods.Length), 0, moods.Length - 1)];
            DiceGame = new LiarsDiceGame(new System.Random(Random.Range(int.MinValue, int.MaxValue)), boldness,
                Constants.DICE_PER_PLAYER);

            EnsureDiceScreen();
            DicePracticeDone = null;
            Dice.Open(DiceGame, Host.StockName(DiceBand), mood, VillageScreenController.BackgroundOf(spot.InfoSource));
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        // 연습 판이 끝나면 부를 것. 있으면 보통 판의 결과(소문·술값) 대신 이것을 부른다.
        private System.Action DicePracticeDone;

        private void EnsureDiceScreen()
        {
            if (Dice != null)
            {
                return;
            }

            Dice = new UI_LiarsDice(Host.Root);
            Dice.Finished += won =>
            {
                Dice.Close();
                EndDice(won);
            };
            Dice.Left += () =>
            {
                Dice.Close();
                EndDice(false);
            };
        }

        private void EndDice(bool won)
        {
            var practice = DicePracticeDone;
            DicePracticeDone = null;

            if (practice != null)
            {
                practice();
                return;
            }

            ConcludeDice(won);
        }

        /// <summary>
        /// 주사위 연습 판(프롤로그 여관 정거장). 주사위와 상대의 수가 정해져 있고, 지지 않는다 — 끝나면 <paramref name="onDone"/>.
        /// 1판: 내 손을 근거로 부르기 → 상대 허세에 「거짓말!」. 2판: 그럴듯한 말엔 올려 부르기.
        /// </summary>
        public void StartDicePractice(System.Action onDone)
        {
            var script = new LiarsDiceScript();

            var first = new LiarsDiceScript.Round
            {
                PlayerDice = new[] { 4, 4, 2, 6 },
                OpponentDice = new[] { 1, 4, 5, 3 },
                PlayerFirst = true,
                OpenHint = StringConstants.TEXT_DICE_TUT_1_OPEN,
                ResponseHint = StringConstants.TEXT_DICE_TUT_1_RESPONSE,
                RevealHint = StringConstants.TEXT_DICE_TUT_1_REVEAL,
                AllowRaise = false,
                ExpectedCount = 2,
                ExpectedFace = 4
            };
            first.OpponentMoves.Add(new[] { 6, 4 });
            script.Rounds.Add(first);

            var second = new LiarsDiceScript.Round
            {
                PlayerDice = new[] { 3, 3, 5, 1 },
                OpponentDice = new[] { 3, 2, 6, 6 },
                PlayerFirst = false,
                ResponseHint = StringConstants.TEXT_DICE_TUT_2_RESPONSE,
                RevealHint = StringConstants.TEXT_DICE_TUT_2_REVEAL,
                AllowCall = false,
                ExpectedCount = 3,
                ExpectedFace = 3
            };
            second.OpponentMoves.Add(new[] { 2, 3 });
            second.OpponentMoves.Add(new[] { 0, 0 });
            script.Rounds.Add(second);

            DiceGame = new LiarsDiceGame(script);
            EnsureDiceScreen();
            DicePracticeDone = onDone;
            Dice.Open(DiceGame, StringConstants.TEXT_DICE_TUT_OPPONENT, StringConstants.TEXT_DICE_TUT_MOOD,
                VillageScreenController.BackgroundOf(InfoSource.Source_TavernTalk));
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        // 아직 도는 말을 모르는 단을 먼저 고른다. 다 알면 아무 단이나 — 술값 내기는 할 수 있다.
        private static string PickDiceBand(MarketSimulation sim)
        {
            if (sim == null)
            {
                return null;
            }

            var unknown = new List<string>();
            var any = new List<string>();

            foreach (var id in sim.CodexSubjects())
            {
                if (sim.CodexProfileOf(id) < 0)
                {
                    continue;
                }

                any.Add(id);

                if (BandCodex.IsKnown(id, CodexSource.Codex_Tavern) == false)
                {
                    unknown.Add(id);
                }
            }

            var pool = unknown.Count > 0 ? unknown : any;
            return pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
        }

        private void ConcludeDice(bool won)
        {
            Host.Dialogue.SetBackground(VillageScreenController.BackgroundOf(DiceSpot.InfoSource));

            if (won == false)
            {
                GatherSession.Record(Constants.DICE_STAKE, 0f, null);
                GameAudio.Play(SoundEffect.Sound_Coins);
                var lost = new DialogueLine(string.Empty, StringConstants.TEXT_DICE_TAG,
                    string.Format(StringConstants.TEXT_DICE_LOST, Constants.DICE_STAKE));
                Host.Dialogue.Play(new List<DialogueLine> { lost }, null);
                return;
            }

            var fact = BandCodex.Learn(DiceBand, CodexSource.Codex_Tavern);
            var lines = new List<DialogueLine>();

            if (fact == null)
            {
                lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_DICE_TAG, StringConstants.TEXT_DICE_WON_NOTHING));
            }
            else
            {
                GatherSession.Record(0f, 0f, new GatheredNote(DiceBand, fact, InfoSource.Source_TavernTalk));
                lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_DICE_TAG,
                    string.Format(StringConstants.TEXT_DICE_WON, Host.StockName(DiceBand))));
                lines.Add(new DialogueLine(string.Empty, Host.StockName(DiceBand), fact));
            }

            Host.Dialogue.Play(lines, null);
        }
    }
}
