using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 영지 화면. 자리에 다가서면 안내를 띄우고, 누르면 정보를 캔다.
    ///
    /// 기획서 §11의 두 층 가운데 아래쪽이다.
    /// 장중 소식은 지하세계가 동시에 알지만, 여기서 캔 것은 **나만 안다**.
    /// 그 차이가 플레이어의 유일한 우위다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class VillageScreenController : MonoBehaviour
    {
        [Tooltip("대화상자 한 벌. 캐낸 소식을 인물이 말해 준다.")]
        [SerializeField] private VisualTreeAsset DialogueTree;

        [Tooltip("표에서 사건을 읽는다. 비어 있으면 아무것도 캐지 못한다.")]
        [SerializeField] private GameDataSet Data;

        private VisualElement Root;
        private UI_Dialogue Dialogue;
        private VillageSpot Here;
        private VillageNpc NearNpc;
        private Transform Player;
        private bool LeaveBound;

        /// <summary>영지에서도 시각이 흐른다. 거래 화면의 시계와 같은 것을 쓴다.</summary>
        private MarketClock Clock
        {
            get
            {
                return BreakClock.Current;
            }
        }

        /// <summary>
        /// 거래 화면을 거치지 않고 영지 씬만 띄웠을 때의 시계를 마련한다.
        ///
        /// 시계가 없으면 남은 시간이 0분이라 아무 데도 들를 수 없다. 영지만 따로
        /// 시험할 수 없게 되므로, 없을 때만 오전장이 막 끝난 상태를 만들어 둔다.
        /// 거래 화면에서 넘어온 경우에는 이미 시계가 있으므로 건드리지 않는다.
        /// </summary>
        private void EnsureClock()
        {
            if (BreakClock.Current != null)
            {
                return;
            }

            var clock = new MarketClock();
            clock.BeginBreak();
            BreakClock.Current = clock;
            GatherSession.BeginBreak(0, Constants.STARTING_CASH);

            UnderwriteLog.Info("영지 씬을 단독으로 띄워 시계를 새로 만들었습니다.");
        }

        private void OnEnable()
        {
            VillageSpotEvents.Entered += OnEnter;
            VillageSpotEvents.Exited += OnExit;
        }

        private void OnDisable()
        {
            VillageSpotEvents.Entered -= OnEnter;
            VillageSpotEvents.Exited -= OnExit;
        }

        private void Update()
        {
            if (Root == null)
            {
                var document = GetComponent<UIDocument>();
                Root = document == null ? null : document.rootVisualElement;

                if (Root == null)
                {
                    return;
                }
            }

            if (Dialogue == null && DialogueTree != null)
            {
                Dialogue = new UI_Dialogue(Root, DialogueTree);
            }

            BindLeave();
            EnsureClock();

            // 영지의 시간은 실시간 1:1로 흐른다. 기획서 §11.
            if (Clock != null)
            {
                Clock.AdvanceBreak(Time.unscaledDeltaTime);
            }

            NearNpc = FindNearNpc();
            RefreshPrompt();

            var keyboard = Keyboard.current;

            if (Dialogue != null && Dialogue.IsChoosing == true)
            {
                PickByNumber(keyboard);
                return;
            }

            if ((Here == null && NearNpc == null) || Dialogue == null || Dialogue.IsOpen == true)
            {
                return;
            }

            // 상호작용은 한 키로 끝낸다. 자리에 서서 누르면 그만이다.
            if (keyboard == null || keyboard.eKey.wasPressedThisFrame == false)
            {
                return;
            }

            // 장소의 문 앞이 사람보다 먼저다. 문 앞에 주민이 서 있어도 볼일은 그 장소에 있다.
            if (Here != null)
            {
                Ask(Here);
                return;
            }

            TalkTo(NearNpc);
        }

        /// <summary>말을 걸 수 있을 만큼 가까운 사람 가운데 가장 가까운 사람. 없으면 null.</summary>
        private VillageNpc FindNearNpc()
        {
            if (Player == null)
            {
                var input = FindAnyObjectByType<PlayerCharacterInput>();

                if (input == null)
                {
                    return null;
                }

                Player = input.transform;
            }

            var foot = (Vector2)Player.position + new Vector2(0f, Constants.PROTAGONIST_FOOT_OFFSET);
            var best = Constants.NPC_TALK_RANGE * Constants.NPC_TALK_RANGE;
            VillageNpc found = null;
            var all = VillageNpc.All;

            for (var i = 0; i < all.Count; i++)
            {
                var distance = (all[i].Foot - foot).sqrMagnitude;

                if (distance < best)
                {
                    best = distance;
                    found = all[i];
                }
            }

            return found;
        }

        /// <summary>
        /// 영지 사람에게 말을 건다. 짧은 한마디로 끝난다.
        /// 모험단과는 관계를 맺지 않는다(설계안 §1-2). 이름과 얼굴만 아는 사이다.
        /// </summary>
        private void TalkTo(VillageNpc npc)
        {
            var line = new DialogueLine(npc.Name, TagOf(npc), npc.PickLine());
            Dialogue.SetBackground(null);
            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private static string TagOf(VillageNpc npc)
        {
            string role;

            switch (npc.Kind)
            {
                case NpcRole.Role_Vendor:
                {
                    role = StringConstants.TEXT_NPC_ROLE_VENDOR;
                    break;
                }
                case NpcRole.Role_Adventurer:
                {
                    role = StringConstants.TEXT_NPC_ROLE_ADVENTURER;
                    break;
                }
                default:
                {
                    role = StringConstants.TEXT_NPC_ROLE_RESIDENT;
                    break;
                }
            }

            return string.IsNullOrEmpty(npc.Group) == true ? role : $"{role} · {npc.Group}";
        }

        /// <summary>선택지 앞에 붙은 번호를 키보드 숫자로 고른다.</summary>
        private void PickByNumber(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return;
            }

            for (var i = 0; i < Constants.DIALOGUE_CHOICE_KEYS.Length; i++)
            {
                if (keyboard[Constants.DIALOGUE_CHOICE_KEYS[i]].wasPressedThisFrame == true)
                {
                    Dialogue.TryPick(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 묻기 전에 고르게 한다. 돈과 시간이 드는 일이므로 E 한 번에 바로 써 버리지 않는다.
        ///
        /// 돈이 모자라면 선택지를 잠그고 가진 돈을 보여 준다. 예전에는 영지가 지갑을 보지 않아
        /// 가진 것보다 많이 쓸 수 있었고, 거래 화면으로 돌아가면 현금이 음수가 됐다.
        /// </summary>
        private void Ask(VillageSpot spot)
        {
            // 들어가는 문, 값도 일도 창고도 창구도 없는 곳, 시간이 없는데 창고도 없는 곳은 고를 것이 없다.
            // 창고가 있는 여관은 시간이 없어도 내려갈 수 있어야 한다. 다음 장이 거기서 열린다.
            if (spot.Kind == VillageActivity.Activity_Enter
                || (spot.Price <= 0f && spot.HasWork == false && spot.HasCellar == false
                    && spot.HasRequestDesk == false)
                || (HasTime() == false && spot.HasCellar == false))
            {
                Gather(spot);
                return;
            }

            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            var choices = new List<DialogueChoice>();
            var money = GatherSession.Available;

            if (spot.Price > 0f)
            {
                var pay = new DialogueChoice(string.Format(StringConstants.TEXT_GATHER_PAY, spot.Price),
                    () => Gather(spot));
                pay.IsEnabled = money >= spot.Price && HasTime() == true;

                if (HasTime() == false)
                {
                    pay.Note = StringConstants.TEXT_GATHER_NO_TIME;
                }
                else if (money < spot.Price)
                {
                    pay.Note = string.Format(StringConstants.TEXT_GATHER_NO_MONEY, money);
                }
                else
                {
                    pay.Note = string.Format(StringConstants.TEXT_GATHER_PAY_NOTE,
                        Constants.GATHER_MINUTES_PER_VISIT, money);
                }
                choices.Add(pay);
            }

            if (spot.HasWork == true)
            {
                var quest = spot.Work;
                var left = Clock == null ? 0 : Clock.BreakMinutesLeft;
                var work = new DialogueChoice(quest.Title, () => DoWork(spot));
                work.IsEnabled = left >= quest.Minutes;
                work.Note = work.IsEnabled == true
                    ? string.Format(StringConstants.TEXT_WORK_NOTE, quest.Minutes, quest.Reward)
                    : string.Format(StringConstants.TEXT_WORK_NO_TIME, left);
                choices.Add(work);
            }

            if (spot.HasRequestDesk == true)
            {
                choices.Add(PostChoice(spot));
            }

            if (spot.HasCellar == true)
            {
                // 웹툰 6화. 술통을 옮기러 내려간 창고 안쪽에 지하거래소가 있다.
                var cellar = new DialogueChoice(StringConstants.TEXT_CELLAR_GO,
                    () => SceneManager.LoadScene(SceneRoutes.MARKET));
                cellar.Note = StringConstants.TEXT_CELLAR_NOTE;
                choices.Add(cellar);
            }

            choices.Add(new DialogueChoice(StringConstants.TEXT_GATHER_LEAVE, null));

            var line = new DialogueLine(SpeakerOf(spot.InfoSource), spot.Name, StringConstants.TEXT_GATHER_ASK);
            line.Choices = choices;

            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private int Today()
        {
            return Clock == null ? 1 : Clock.Day;
        }

        /// <summary>길드 창구의 「익명 의뢰를 낸다」. 들은 사정이 있으면 기한을 붙여 보여 준다.</summary>
        private DialogueChoice PostChoice(VillageSpot spot)
        {
            var post = new DialogueChoice(StringConstants.TEXT_REQUEST_POST, () => OpenRequestForm(spot));
            post.IsEnabled = HasTime() == true;

            if (HasTime() == false)
            {
                post.Note = StringConstants.TEXT_GATHER_NO_TIME;
                return post;
            }

            var deadline = PatronageLedger.NearestDeadline(Today());
            post.Note = deadline > 0
                ? string.Format(StringConstants.TEXT_REQUEST_POST_DEADLINE, deadline)
                : StringConstants.TEXT_REQUEST_POST_NOTE;
            return post;
        }

        /// <summary>
        /// 길드 창구에서 익명 의뢰를 꾸민다. 후원흐름 설계안 §3-2.
        /// 지금은 "무엇을 위한 의뢰인가" 하나만 고른다. 나머지 항목(선지급 물자, 보수, 위험도)은 하나씩 연다.
        /// 고르는 근거는 여관에서 엿들은 말뿐이다. 무엇이 필요한지는 화면에 적지 않는다.
        /// </summary>
        private void OpenRequestForm(VillageSpot spot)
        {
            var money = GatherSession.Available;
            var choices = new List<DialogueChoice>
            {
                RequestChoice(RequestPurpose.Purpose_Healing, StringConstants.TEXT_REQUEST_PURPOSE_HEALING,
                    Constants.REQUEST_COST_HEALING, money),
                RequestChoice(RequestPurpose.Purpose_Gear, StringConstants.TEXT_REQUEST_PURPOSE_GEAR,
                    Constants.REQUEST_COST_GEAR, money),
                RequestChoice(RequestPurpose.Purpose_Supplies, StringConstants.TEXT_REQUEST_PURPOSE_SUPPLIES,
                    Constants.REQUEST_COST_SUPPLIES, money),
                RequestChoice(RequestPurpose.Purpose_Training, StringConstants.TEXT_REQUEST_PURPOSE_TRAINING,
                    Constants.REQUEST_COST_TRAINING, money),
                new DialogueChoice(StringConstants.TEXT_REQUEST_CANCEL, null)
            };

            var line = new DialogueLine(StringConstants.TEXT_REQUEST_CLERK, spot.Name, StringConstants.TEXT_REQUEST_ASK);
            line.Choices = choices;
            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private DialogueChoice RequestChoice(RequestPurpose purpose, string text, float cost, float money)
        {
            var choice = new DialogueChoice(text, () => PostRequest(purpose, cost));
            choice.IsEnabled = money >= cost;
            choice.Note = choice.IsEnabled == true
                ? string.Format(StringConstants.TEXT_REQUEST_COST_NOTE, cost, money)
                : string.Format(StringConstants.TEXT_GATHER_NO_MONEY, money);
            return choice;
        }

        /// <summary>
        /// 의뢰를 맡긴다. 돈은 영지에서 쓴 돈으로 적혀 거래 화면으로 돌아갈 때 빠진다.
        /// 맞았는지는 여기서 알려 주지 않는다. 다음에 여관에서 일할 때 그들의 말로 듣는다.
        /// </summary>
        private void PostRequest(RequestPurpose purpose, float cost)
        {
            if (Clock != null)
            {
                Clock.SpendBreakMinutes(Constants.REQUEST_MINUTES);
            }

            GatherSession.Record(cost, 0f, null);
            PatronageLedger.Post(purpose, Today());

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_REQUEST_CLERK,
                string.Format(StringConstants.TEXT_REQUEST_DONE, StringConstants.TEXT_HERO_PARTY));
            line.Note = string.Format(StringConstants.TEXT_REQUEST_DONE_NOTE, cost);
            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        /// <summary>
        /// 하루를 닫는다. 기획서 §11의 넷째 단계 — 정보 정리 및 취침.
        ///
        /// 오늘 영지에서 한 일을 숫자로 돌려준다. 쉬는 것과 마찬가지로, 무엇을 얻고 무엇을 썼는지
        /// 보이지 않으면 하루를 어디에 썼는지가 고르는 일로 느껴지지 않는다.
        /// 다 읽으면 거래소로 간다. 다음 장이 곧 열리기 때문이다. 날짜는 시장이 장을 열 때 넘긴다.
        /// </summary>
        private void Sleep()
        {
            Clock.SkipBreak();
            Dialogue.SetBackground(BackgroundOf(InfoSource.Source_TavernTalk));

            var notes = GatherSession.Gathered.Count;
            var body = string.Format(StringConstants.TEXT_DAY_END_MONEY, GatherSession.Earned, GatherSession.Spent)
                + " "
                + (notes > 0
                    ? string.Format(StringConstants.TEXT_DAY_END_NOTES, notes)
                    : StringConstants.TEXT_DAY_END_NO_NOTES);

            // 기한은 보여 주고 금액은 숨긴다(상장흐름 설계안 §1-5 완화책 2).
            // 못 들은 사정이 급해지면 경고만 띄운다. 마지막 단계는 숨기지 않는다(완화책 1).
            var failed = PatronageLedger.EndOfDay(Clock.Day);
            var deadline = PatronageLedger.NearestDeadline(Clock.Day + 1);

            if (failed > 0)
            {
                body = body + " " + StringConstants.TEXT_DAY_END_COLLAPSE;
            }
            else if (deadline > 0)
            {
                body = body + " " + string.Format(StringConstants.TEXT_DAY_END_DEADLINE, deadline);
            }

            if (failed == 0 && PatronageLedger.HasUnheardUrgent(Clock.Day + 1) == true)
            {
                body = body + " " + StringConstants.TEXT_DAY_END_WARNING;
            }

            var line = new DialogueLine(string.Format(StringConstants.TEXT_DAY_END_TITLE, Clock.Day),
                StringConstants.TEXT_DAY_END_TAG, body);
            line.Note = string.Format(StringConstants.TEXT_DAY_END_NEXT, Constants.MARKET_MORNING_START_HOUR);

            Dialogue.Play(new List<DialogueLine> { line }, () => SceneManager.LoadScene(SceneRoutes.MARKET));
        }

        /// <summary>
        /// 자리의 일을 맡는다. 기획서 §10 — 시간을 쓰고 돈을 번다.
        ///
        /// 사람을 많이 만나는 일이면 소문 하나를 덤으로 주워듣는다. 다만 여관에 돈을 내고
        /// 묻는 것만큼 또렷하지는 않다. 누구 이야기인지까지만 안다.
        /// </summary>
        private void DoWork(VillageSpot spot)
        {
            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            var quest = spot.Work;

            if (Clock != null)
            {
                Clock.SpendBreakMinutes(quest.Minutes);
            }

            var text = quest.Done;
            GatheredNote note = null;

            // 여관 일꾼의 자리. 단골인 용사단의 이야기가 들린다(후원흐름 설계안 §2-2).
            // 의뢰에 대한 반응이 있으면 그것이, 없으면 아직 못 들은 사정이 먼저다.
            var overheard = spot.HasCellar == true ? PatronageLedger.Overhear(Today()) : null;

            if (overheard != null)
            {
                text = text + " " + overheard;
            }
            else if (quest.GivesRumor == true)
            {
                var rows = NextSlotRows();

                if (rows.Count > 0)
                {
                    var pick = rows[Random.Range(0, rows.Count)];
                    var name = StockName(pick.StockId);
                    note = new GatheredNote(pick.StockId, $"{name} 쪽에 무슨 일이 있다.", spot.InfoSource);
                    text = text + " " + string.Format(StringConstants.TEXT_WORK_RUMOR, name);
                }
            }

            GatherSession.Record(0f, quest.Reward, note);

            var minutes = Clock == null ? 0 : Clock.BreakMinutesLeft;
            var line = new DialogueLine(SpeakerOf(spot.InfoSource), spot.Name, text);
            line.IsSecret = note != null;
            line.Note = string.Format(StringConstants.TEXT_WORK_RESULT, quest.Reward, minutes / 60, minutes % 60);

            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        /// <summary>거래소로 돌아가는 단추. 화면이 준비된 뒤에 한 번만 묶는다.</summary>
        private void BindLeave()
        {
            if (LeaveBound == true)
            {
                return;
            }

            var button = Root.Q<Button>("village-leave");

            if (button == null)
            {
                return;
            }

            LeaveBound = true;
            button.clicked += () => SceneManager.LoadScene(SceneRoutes.MARKET);

            var skip = Root.Q<Button>("village-skip");

            if (skip != null)
            {
                // 볼일을 다 봤는데 실시간으로 기다리게 할 이유가 없다.
                // 밤이면 같은 단추가 하루를 닫는다.
                skip.clicked += () =>
                {
                    if (Clock == null || (Dialogue != null && Dialogue.IsOpen == true))
                    {
                        return;
                    }

                    if (Clock.NextSessionIsMorning == true)
                    {
                        Sleep();
                        return;
                    }

                    Clock.SkipBreak();
                };
            }

            var rest = Root.Q<Button>("village-rest");

            if (rest != null)
            {
                rest.clicked += ToggleRest;
            }
        }

        /// <summary>
        /// 오늘 장에 나가지 않기로 한다. 기획서 §11 — 장에 나가는 것은 선택이다.
        ///
        /// 쉬면 그 장이 열리고 닫히는 동안의 시간이 통째로 영지의 것이 된다.
        /// 거래로 벌 수 있었던 하루를, 정보와 종잣돈으로 바꾸는 것이다.
        ///
        /// 아직 그 시간을 쓰지 않았다면 다시 눌러 무를 수 있다.
        /// 이미 써 버린 뒤에는 무를 수 없다. 시간을 빚질 수는 없기 때문이다.
        /// </summary>
        private void ToggleRest()
        {
            if (Clock == null)
            {
                return;
            }

            if (Clock.SkippedSessions > 0)
            {
                if (Clock.UndoSkippedSession() == false)
                {
                    UnderwriteLog.Info("이미 쓴 시간이 있어 쉬기를 무를 수 없습니다.");
                }

                return;
            }

            Clock.SkipNextSession();
        }

        private void OnEnter(VillageSpot spot)
        {
            Here = spot;
        }

        private void OnExit(VillageSpot spot)
        {
            if (ReferenceEquals(Here, spot) == true)
            {
                Here = null;
            }
        }

        private void RefreshPrompt()
        {
            var prompt = Root.Q<VisualElement>("village-prompt");

            if (prompt == null)
            {
                return;
            }

            // 대화 중에는 안내를 감춘다. 두 개가 같이 떠 있으면 무엇을 누르라는지 모른다.
            var talking = Dialogue != null && Dialogue.IsOpen == true;
            prompt.EnableInClassList("hidden", (Here == null && NearNpc == null) || talking);
            var left = Clock == null ? 0 : Clock.BreakMinutesLeft;
            var hour = Clock == null ? 0 : Clock.BreakHour;
            var minute = Clock == null ? 0 : Clock.BreakMinute;
            var resting = Clock != null && Clock.SkippedSessions > 0;

            SetText("village-visits", resting == true
                ? $"{hour:00}:{minute:00} · 오늘 장을 쉽니다 · 다음 장까지 {left / 60}시간 {left % 60}분"
                : $"{hour:00}:{minute:00} · 다음 장까지 {left / 60}시간 {left % 60}분");

            var skip = Root.Q<Button>("village-skip");

            if (skip != null && Clock != null)
            {
                skip.text = Clock.NextSessionIsMorning == true
                    ? StringConstants.TEXT_SLEEP
                    : StringConstants.TEXT_SKIP_BREAK;
            }

            var rest = Root.Q<Button>("village-rest");

            if (rest != null)
            {
                // 누르면 무엇이 되는지를 단추가 말한다. 상태를 따로 읽게 하지 않는다.
                rest.text = resting == true ? "장 쉬기 취소" : "오늘 장 쉬기";
                rest.EnableInClassList("village-rest--on", resting);
            }

            if (Here == null)
            {
                if (NearNpc != null)
                {
                    SetText("village-place", NearNpc.Name);
                    SetText("village-cost", TagOf(NearNpc));
                    SetText("village-key", StringConstants.TEXT_NPC_TALK_KEY);
                }

                return;
            }

            SetText("village-place", Here.Name);
            SetText("village-cost", Here.Price > 0f ? $"{Here.Price:N0}을 낸다" : "값은 없다");
            SetText("village-key", HasTime() == true
                ? "E — 묻는다"
                : "다음 장이 코앞이다");
        }

        /// <summary>
        /// 자리에 맞는 정보를 캔다.
        ///
        /// 값이 비쌀수록 또렷하다. 여관은 누구 이야기가 돌지만 알려 주고,
        /// 길드 기록은 내용까지 알려 준다. 게시판은 공짜지만 이미 다 아는 것만 있다.
        /// </summary>
        private void Gather(VillageSpot spot)
        {
            // 정보를 캐는 곳만 있는 것이 아니다. 지하시장 입구는 걸어 들어가는 문이다.
            if (spot.Kind == VillageActivity.Activity_Enter)
            {
                SceneManager.LoadScene(SceneRoutes.MARKET);
                return;
            }

            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            if (HasTime() == false)
            {
                Say(spot, "다음 장이 코앞입니다. 지금 가면 개장을 놓칩니다.", null, false);
                return;
            }

            var rows = NextSlotRows();

            if (rows.Count == 0)
            {
                SpendTime(spot);
                GatherSession.Record(spot.Price, 0f, null);
                Say(spot, "아직 돌고 있는 이야기가 없습니다.", null, false);
                return;
            }

            var pick = rows[Random.Range(0, rows.Count)];
            var name = StockName(pick.StockId);

            string title;
            string body;
            GatheredNote note;

            switch (spot.InfoSource)
            {
                case InfoSource.Source_GuildRecord:
                {
                    title = "기록에 적혀 있다";
                    body = pick.Headline;
                    note = new GatheredNote(pick.StockId, pick.Headline, spot.InfoSource);
                    break;
                }
                case InfoSource.Source_MarketBoard:
                {
                    title = "게시판을 훑는다";
                    body = "새로 붙은 종이는 없습니다. 이미 시장에 돈 이야기뿐입니다.";
                    note = null;
                    break;
                }
                case InfoSource.Source_SmithOrder:
                {
                    // 대장간은 내용이 아니라 **움직임의 크기**를 알려 준다.
                    // 누가 크게 준비하는지는 알지만 왜인지는 모른다.
                    title = "주문 장부를 넘겨 본다";
                    body = $"{name} 쪽에서 장비를 크게 맞췄습니다. 큰 원정을 준비하는 모양입니다.";
                    note = new GatheredNote(pick.StockId, $"{name} 쪽이 장비를 크게 맞췄다", spot.InfoSource);
                    break;
                }
                case InfoSource.Source_PrivateLetter:
                {
                    // 가장 비싼 만큼 가장 또렷하다. 소식의 내용이 그대로 나온다.
                    title = "서신을 읽는다";
                    body = pick.Headline;
                    note = new GatheredNote(pick.StockId, pick.Headline, spot.InfoSource);
                    break;
                }
                default:
                {
                    title = "술자리에서 들었다";
                    body = $"{name} 쪽 이야기가 돈다는 것까지는 알아냈습니다. 내용까지는 못 들었습니다.";
                    note = new GatheredNote(pick.StockId, $"{name} 쪽에 무슨 일이 있다.", spot.InfoSource);
                    break;
                }
            }

            SpendTime(spot);
            GatherSession.Record(spot.Price, 0f, note);
            Say(spot, body, PortraitOf(pick.StockId), note != null);
        }

        /// <summary>
        /// 자리의 주인이 말한다. 묻는 쪽은 플레이어이므로 말하는 이는 늘 그 자리의 사람이다.
        /// </summary>
        private void Say(VillageSpot spot, string text, Sprite portrait, bool secret)
        {
            var line = new DialogueLine(SpeakerOf(spot.InfoSource), spot.Name, text);
            line.Portrait = portrait;
            line.IsSecret = secret;
            line.Note = BuildNote(spot, secret);

            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        /// <summary>다음 장까지 한 가지 일을 할 시간이 남아 있는가.</summary>
        private bool HasTime()
        {
            return Clock != null && Clock.BreakMinutesLeft >= Constants.GATHER_MINUTES_PER_VISIT;
        }

        /// <summary>한 가지 일에 쓴 시간만큼 시각을 민다.</summary>
        private void SpendTime(VillageSpot spot)
        {
            if (Clock == null)
            {
                return;
            }

            Clock.SpendBreakMinutes(Constants.GATHER_MINUTES_PER_VISIT);
        }

        private string BuildNote(VillageSpot spot, bool secret)
        {
            var spent = spot.Price > 0f ? $"{spot.Price:N0}을 썼습니다. " : string.Empty;
            var minutes = Clock == null ? 0 : Clock.BreakMinutesLeft;
            var left = $"다음 장까지 {minutes / 60}시간 {minutes % 60}분.";

            if (secret == false)
            {
                return spent + left;
            }

            return spent + "시장은 아직 모르는 이야기입니다. " + left;
        }

        private static string SpeakerOf(InfoSource source)
        {
            switch (source)
            {
                case InfoSource.Source_GuildRecord:
                {
                    return "기록 담당";
                }
                case InfoSource.Source_MarketBoard:
                {
                    return "게시판";
                }
                case InfoSource.Source_SmithOrder:
                {
                    return "대장장이";
                }
                case InfoSource.Source_PrivateLetter:
                {
                    return "영주관 집사";
                }
                default:
                {
                    return "여관 주인";
                }
            }
        }

        private static string BackgroundOf(InfoSource source)
        {
            switch (source)
            {
                case InfoSource.Source_GuildRecord:
                {
                    return "dialogue-root--guild";
                }
                case InfoSource.Source_MarketBoard:
                {
                    return "dialogue-root--board";
                }
                case InfoSource.Source_SmithOrder:
                {
                    return "dialogue-root--smith";
                }
                case InfoSource.Source_PrivateLetter:
                {
                    return "dialogue-root--manor";
                }
                default:
                {
                    return "dialogue-root--tavern";
                }
            }
        }

        private Sprite PortraitOf(string stockId)
        {
            if (Data == null || Data.StockRows == null)
            {
                return null;
            }

            var rows = Data.StockRows.Rows;

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Id == stockId)
                {
                    return rows[i].Portrait;
                }
            }

            return null;
        }

        /// <summary>다음 정보 타임에 들어올 소식. 그것을 미리 캐는 것이 조사다.</summary>
        private List<EventEntry> NextSlotRows()
        {
            var found = new List<EventEntry>();

            if (Data == null || Data.EventRows == null)
            {
                return found;
            }

            var rows = Data.EventRows.Rows;

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Slot == GatherSession.NextSlot)
                {
                    found.Add(rows[i]);
                }
            }

            return found;
        }

        private string StockName(string stockId)
        {
            if (Data == null || Data.StockRows == null)
            {
                return stockId;
            }

            var rows = Data.StockRows.Rows;

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Id == stockId)
                {
                    return rows[i].DisplayName;
                }
            }

            return stockId;
        }

        private void SetText(string elementName, string value)
        {
            var label = Root.Q<Label>(elementName);

            if (label == null)
            {
                return;
            }

            label.text = value;
        }
    }
}
