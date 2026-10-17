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

        [Tooltip("용사단 다섯의 표정 묶음. 학자 · 검사 · 마도사 · 전사 · 요정 순서다.")]
        [SerializeField] private List<PortraitSet> HeroFaces = new List<PortraitSet>();

        private VisualElement Root;
        private UI_Dialogue Dialogue;
        private VillageSpot Here;
        private VillageNpc NearNpc;
        private TimeDial Dial;

        // 지금 선택지를 띄운 자리. 용사 테이블처럼 자리를 넘겨받지 않는 곳에서 돌아갈 곳이다.
        private VillageSpot CurrentSpot;

        // 시계를 연 뒤 토글로 다음 장을 건너뛰었는가. 「그만둔다」로 닫으면 되돌린다.
        private bool SkipFromClock;
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

            // 대화·시계·메뉴가 떠 있으면 휠이 카메라로 새지 않게 한다.
            VillageCameraZoom.Blocked = (Dialogue != null && Dialogue.IsOpen == true) || ClockOpen == true
                || MenuOpen == true;

            var keyboard = Keyboard.current;

            if (Dialogue != null && Dialogue.IsChoosing == true)
            {
                PickByNumber(keyboard);
                return;
            }

            if ((Here == null && NearNpc == null) || Dialogue == null || Dialogue.IsOpen == true
                || ClockOpen == true)
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
        /// <summary>
        /// 그 자리에서 고를 것이 있는가.
        /// 들어가는 문, 값도 일도 창고도 창구도 없는 곳, 시간이 없는데 창고도 없는 곳은 고를 것이 없다.
        /// 창고가 있는 여관은 시간이 없어도 내려갈 수 있어야 한다. 다음 장이 거기서 열린다.
        /// </summary>
        private bool OffersChoices(VillageSpot spot)
        {
            return spot.Kind != VillageActivity.Activity_Enter
                && (spot.Price > 0f || spot.HasWork == true || spot.HasCellar == true || spot.HasRequestDesk == true)
                && (HasTime() == true || spot.HasCellar == true);
        }

        /// <summary>
        /// 한 가지 일을 마친 뒤 그 자리의 선택지로 돌아간다(2026-10-17 사용자 요청).
        /// 대화가 끝날 때마다 마을로 튕겨 나가면 같은 자리에서 여러 일을 할 때 E를 다시 눌러야 한다.
        /// 고를 것이 없어진 자리(시간이 다 됨 등)면 그냥 닫는다.
        /// </summary>
        private System.Action Back(VillageSpot spot)
        {
            return () =>
            {
                if (spot != null && OffersChoices(spot) == true)
                {
                    Ask(spot);
                }
            };
        }

        private void Ask(VillageSpot spot)
        {
            CurrentSpot = spot;

            if (OffersChoices(spot) == false)
            {
                Gather(spot);
                return;
            }

            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            // 용사단이 원정에서 돌아온 날, 여관 문을 열면 재회 장면이 먼저다.
            if (spot.HasCellar == true && HeroRoster.HasReturn(Today()) == true)
            {
                PlayReturn(() => Ask(spot));
                return;
            }

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
                choices.Add(HeroTableChoice(spot));
            }

            // 창고 안쪽을 아직 모른다. 웹툰 5~6화의 사건을 겪어야 길이 열린다.
            if (spot.HasCellar == true && StoryProgress.CellarFound == true)
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

        /// <summary>
        /// 시찰 달력. 기획서 §11 — 정기 조사는 달력에 미리 공개한다. 미리 알아야 대비할 수 있다.
        /// 문자열을 매 프레임 만들지만 영지 화면 한 줄뿐이라 그대로 둔다.
        /// </summary>
        private string InspectionTag()
        {
            var today = Today();

            if (MarketClock.IsInspectionDay(today) == true)
            {
                return " · " + StringConstants.TEXT_VILLAGE_INSPECTION_TODAY;
            }

            var days = MarketClock.NextInspectionDay(today) - today;
            return " · " + string.Format(StringConstants.TEXT_VILLAGE_INSPECTION_SOON, days);
        }

        /// <summary>
        /// 지하거래소를 찾는 장면. 웹툰 5~6화를 플레이어가 직접 겪는다(2026-10-17 사용자 결정).
        /// 인트로 다음에 곧바로 주식 창이 열리면 뜬금없다. 마을에서 일하다 우연히 찾아야 한다.
        /// </summary>
        private void PlayDiscovery(VillageSpot spot)
        {
            StoryProgress.CellarFound = true;

            var owner = SpeakerOf(spot.InfoSource);
            var lines = new List<DialogueLine>
            {
                new DialogueLine(owner, StringConstants.TEXT_DISCOVER_TAG, StringConstants.TEXT_DISCOVER_OWNER),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_OVERHEAR),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_THINK),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_CELLAR),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_END)
            };

            lines[lines.Count - 1].Note = StringConstants.TEXT_DISCOVER_NOTE;

            // 다 보면 바로 고르게 한다. 창고로 내려갈지, 일단 둘러볼지.
            Dialogue.Play(lines, () => Ask(spot));
        }

        // ── 용사단 (루틴 설계안) ──────────────────

        /// <summary>
        /// 여관의 용사단 테이블. 시찰일에는 하루를 함께 보내는 장면으로 바뀐다.
        /// 원정 중이면 잠그고 언제 돌아오는지 적는다.
        /// </summary>
        private DialogueChoice HeroTableChoice(VillageSpot spot)
        {
            var day = Today();

            if (HeroRoster.HasDayOff(day) == true)
            {
                var dayOff = new DialogueChoice(StringConstants.TEXT_HERO_DAY_OFF, PlayDayOff);
                dayOff.Note = StringConstants.TEXT_HERO_DAY_OFF_NOTE;
                return dayOff;
            }

            var table = new DialogueChoice(StringConstants.TEXT_HERO_TABLE, () => OpenHeroTable(spot));

            if (HeroRoster.IsAway(day) == true)
            {
                table.IsEnabled = false;
                table.Note = string.Format(StringConstants.TEXT_HERO_TABLE_AWAY, HeroRoster.ReturnDay);
                return table;
            }

            table.IsEnabled = HasTime() == true;
            table.Note = HasTime() == true
                ? string.Format(StringConstants.TEXT_HERO_TABLE_NOTE, Constants.HERO_TALK_MINUTES)
                : StringConstants.TEXT_GATHER_NO_TIME;
            return table;
        }

        private void OpenHeroTable(VillageSpot spot)
        {
            var day = Today();
            var choices = new List<DialogueChoice>();
            var heroes = HeroRoster.All;

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                var choice = new DialogueChoice(hero.Name, () => TalkToHero(hero));
                choice.IsEnabled = hero.LastTalkDay != day;
                choice.Note = choice.IsEnabled == true ? hero.Role : StringConstants.TEXT_HERO_TALKED_TODAY;
                choices.Add(choice);
            }

            choices.Add(new DialogueChoice(StringConstants.TEXT_REQUEST_CANCEL, () => Ask(spot)));

            var line = new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_HERO_TABLE_ASK);
            line.Choices = choices;
            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        /// <summary>
        /// 일꾼으로서 말을 건다. 기획서 §8의 두 관계 가운데 **일상의 주인공** 쪽이다.
        /// 대답에 따라 호감이 오르고 표정이 바뀐다. 숫자는 보여 주지 않는다.
        /// </summary>
        private void TalkToHero(HeroProfile hero)
        {
            if (Clock != null)
            {
                Clock.SpendBreakMinutes(Constants.HERO_TALK_MINUTES);
            }

            hero.LastTalkDay = Today();

            var greet = HeroLine(hero, StringConstants.HERO_GREET[hero.FaceIndex], PortraitEmotion.Emotion_Neutral);
            greet.Choices = new List<DialogueChoice>
            {
                new DialogueChoice(StringConstants.TEXT_HERO_REPLY_WARM, () => ReactToHero(hero, 2,
                    StringConstants.HERO_REACT_WARM[hero.FaceIndex], PortraitEmotion.Emotion_Relieved)),
                new DialogueChoice(StringConstants.TEXT_HERO_REPLY_CURIOUS, () => ReactToHero(hero, 1,
                    StringConstants.HERO_REACT_CURIOUS[hero.FaceIndex], PortraitEmotion.Emotion_Anxious)),
                new DialogueChoice(StringConstants.TEXT_HERO_REPLY_PLAIN, () => ReactToHero(hero, 0,
                    StringConstants.HERO_REACT_PLAIN[hero.FaceIndex], PortraitEmotion.Emotion_Neutral))
            };

            Dialogue.Play(new List<DialogueLine> { greet }, null);
        }

        /// <summary>
        /// 대답에 대한 반응. 충분히 친하면 다른 용병단의 현장 이야기를 덤으로 흘린다.
        /// 사랑하는 사람들과 가까워질수록 모르는 사람들에 대한 정보가 생긴다(루틴 설계안 — 관계와 투자의 다리).
        /// 다만 덤일 뿐이다. 용사를 정보 도구로 만들지 않도록 확률로만 나온다.
        /// </summary>
        private void ReactToHero(HeroProfile hero, int affinity, string text, PortraitEmotion emotion)
        {
            hero.Affinity += affinity;

            var lines = new List<DialogueLine> { HeroLine(hero, text, emotion) };

            if (hero.Affinity >= Constants.HERO_FIELD_TALK_AFFINITY
                && UnityEngine.Random.value < Constants.HERO_FIELD_TALK_CHANCE)
            {
                var rows = NextSlotRows();

                if (rows.Count > 0)
                {
                    var pick = rows[UnityEngine.Random.Range(0, rows.Count)];
                    var name = StockName(pick.StockId);
                    var field = HeroLine(hero, string.Format(StringConstants.TEXT_HERO_FIELD_TALK, name, pick.Headline),
                        PortraitEmotion.Emotion_Neutral);
                    field.IsSecret = true;
                    field.Note = HeldNote(pick.StockId);
                    lines.Add(field);
                    GatherSession.Record(0f, 0f, new GatheredNote(pick.StockId, pick.Headline,
                        InfoSource.Source_TavernTalk));
                }
            }

            // 이야기를 마치면 테이블로 돌아간다. 다른 사람에게도 말을 걸 수 있다.
            Dialogue.Play(lines, () =>
            {
                if (CurrentSpot != null)
                {
                    OpenHeroTable(CurrentSpot);
                }
            });
        }

        /// <summary>
        /// 영주 시찰일. 지하가 닫히고 용사단도 쉰다. 큰 관계 장면의 날이다(루틴 설계안 — 일주일의 리듬).
        /// 가장 친한 사람이 마지막 한마디를 한다. 하루의 남은 시간을 함께 보낸다.
        /// </summary>
        private void PlayDayOff()
        {
            var day = Today();
            HeroRoster.MarkDayOff(day);
            HeroRoster.AddAffinityToAll(1);

            var favorite = HeroRoster.Favorite();
            var sword = HeroRoster.All[1];
            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_DAY_OFF_TAG, StringConstants.TEXT_DAY_OFF_OPEN),
                HeroLine(sword, StringConstants.TEXT_DAY_OFF_INVITE, PortraitEmotion.Emotion_Neutral),
                HeroLine(favorite, StringConstants.HERO_DAY_OFF_FAVORITE[favorite.FaceIndex],
                    PortraitEmotion.Emotion_Happy),
                new DialogueLine(string.Empty, StringConstants.TEXT_DAY_OFF_TAG, StringConstants.TEXT_DAY_OFF_CLOSE)
            };

            if (Clock != null)
            {
                Clock.SkipBreak();
            }

            Dialogue.Play(lines, Back(CurrentSpot));
        }

        /// <summary>
        /// 원정에서 돌아온 날. 후원이 닿았는지가 그들의 몸으로 돌아온다(루틴 설계안 — 한 주기의 리듬).
        /// 의뢰가 두 번 이상 맞았으면 학자가 의뢰주를 의심하기 시작한다(후원흐름 설계안 §2-6).
        /// </summary>
        private void PlayReturn(System.Action onDone)
        {
            var day = Today();
            HeroRoster.MarkReturn(day);
            var heroes = HeroRoster.All;
            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_EXPEDITION_TAG, StringConstants.TEXT_RETURN_OPEN)
            };

            if (PatronageLedger.Collapsed == true)
            {
                lines[0].Text = StringConstants.TEXT_RETURN_COLLAPSE;
            }
            else if (PatronageLedger.IsMet("need_healer") == true)
            {
                lines.Add(HeroLine(heroes[3], StringConstants.TEXT_RETURN_GOOD, PortraitEmotion.Emotion_Happy));
            }
            else
            {
                lines.Add(HeroLine(heroes[4], StringConstants.TEXT_RETURN_HURT, PortraitEmotion.Emotion_Sad));
            }

            if (PatronageLedger.Collapsed == false && PatronageLedger.Clues >= 2)
            {
                lines.Add(HeroLine(heroes[0], StringConstants.TEXT_RETURN_SUSPECT, PortraitEmotion.Emotion_Shocked));
            }

            Dialogue.Play(lines, onDone);
        }

        private DialogueLine HeroLine(HeroProfile hero, string text, PortraitEmotion emotion)
        {
            var line = new DialogueLine(hero.Name, hero.Role, text);
            line.Emotion = emotion;

            if (hero.FaceIndex < HeroFaces.Count && HeroFaces[hero.FaceIndex] != null)
            {
                line.Portrait = HeroFaces[hero.FaceIndex].Get(emotion);
            }

            return line;
        }

        private int Today()
        {
            return Clock == null ? 1 : Clock.CalendarDay;
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
                new DialogueChoice(StringConstants.TEXT_REQUEST_CANCEL, () => Ask(spot))
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
            Dialogue.Play(new List<DialogueLine> { line }, Back(CurrentSpot));
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
            var today = Today();
            var failed = PatronageLedger.EndOfDay(today);
            var deadline = PatronageLedger.NearestDeadline(today + 1);

            if (failed > 0)
            {
                body = body + " " + StringConstants.TEXT_DAY_END_COLLAPSE;
            }
            else if (deadline > 0)
            {
                body = body + " " + string.Format(StringConstants.TEXT_DAY_END_DEADLINE, deadline);
            }

            if (failed == 0 && PatronageLedger.HasUnheardUrgent(today + 1) == true)
            {
                body = body + " " + StringConstants.TEXT_DAY_END_WARNING;
            }

            if (MarketClock.IsInspectionDay(today + 1) == true)
            {
                body = body + " " + StringConstants.TEXT_DAY_END_INSPECTION;
            }

            var line = new DialogueLine(string.Format(StringConstants.TEXT_DAY_END_TITLE, today),
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

            // 처음으로 여관 일을 거들면 웹툰 5~6화가 벌어진다. 창고 안쪽에서 지하거래소를 찾는다.
            if (spot.HasCellar == true && StoryProgress.CellarFound == false)
            {
                GatherSession.Record(0f, quest.Reward, null);
                PlayDiscovery(spot);
                return;
            }

            var text = quest.Done;
            GatheredNote note = null;
            string rumorStock = null;

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
                    rumorStock = pick.StockId;
                }
            }

            GatherSession.Record(0f, quest.Reward, note);

            var minutes = Clock == null ? 0 : Clock.BreakMinutesLeft;
            var line = new DialogueLine(SpeakerOf(spot.InfoSource), spot.Name, text);
            line.IsSecret = note != null;
            line.Note = string.Format(StringConstants.TEXT_WORK_RESULT, quest.Reward, minutes / 60, minutes % 60);
            var held = rumorStock == null ? null : HeldNote(rumorStock);

            if (held != null)
            {
                line.Note = held + " · " + line.Note;
            }

            Dialogue.Play(new List<DialogueLine> { line }, Back(spot));
        }

        /// <summary>메뉴 단추와 원형 시계. 화면이 준비된 뒤에 한 번만 묶는다.</summary>
        private void BindLeave()
        {
            if (LeaveBound == true)
            {
                return;
            }

            var menu = Root.Q<Button>("village-menu");

            if (menu == null)
            {
                return;
            }

            LeaveBound = true;
            menu.text = StringConstants.TEXT_MENU;
            menu.clicked += ToggleMenu;

            var slot = Root.Q<VisualElement>("village-clock-dial");

            if (slot != null)
            {
                Dial = new TimeDial();
                Dial.style.flexGrow = 1f;
                Dial.Changed += ShowClockPick;
                slot.Add(Dial);
            }

            var cancel = Root.Q<Button>("village-clock-cancel");

            if (cancel != null)
            {
                cancel.text = StringConstants.TEXT_CLOCK_CANCEL;
                cancel.clicked += CancelClock;
            }

            var confirm = Root.Q<Button>("village-clock-confirm");

            if (confirm != null)
            {
                confirm.text = StringConstants.TEXT_CLOCK_CONFIRM;
                confirm.clicked += ConfirmClock;
            }

            SetText("village-clock-title", StringConstants.TEXT_CLOCK_TITLE);

            var skipToggle = Root.Q<Toggle>("village-clock-skip");

            if (skipToggle != null)
            {
                skipToggle.RegisterValueChangedCallback(evt => ToggleSkipSession(evt.newValue));
            }
        }

        // ── 메뉴 (드롭업) ─────────────────────────

        private bool MenuOpen
        {
            get
            {
                var list = Root.Q<VisualElement>("village-menu-list");
                return list != null && list.ClassListContains("hidden") == false;
            }
        }

        private bool ClockOpen
        {
            get
            {
                var clock = Root.Q<VisualElement>("village-clock");
                return clock != null && clock.ClassListContains("hidden") == false;
            }
        }

        private void ToggleMenu()
        {
            if (Dialogue != null && Dialogue.IsOpen == true)
            {
                return;
            }

            if (MenuOpen == true)
            {
                CloseMenu();
                return;
            }

            BuildMenu();
        }

        /// <summary>
        /// 메뉴 항목을 연다. 열 때마다 새로 채운다 — 잠자리처럼 때에 따라 잠기는 항목이 있다.
        /// 항목을 늘릴 때는 여기에 MenuItem 한 줄을 더한다.
        /// </summary>
        private void BuildMenu()
        {
            var list = Root.Q<VisualElement>("village-menu-list");

            if (list == null || Clock == null)
            {
                return;
            }

            list.Clear();

            var canSkip = Clock.LatestBreakMinute > Clock.BreakMinuteOfDay;
            MenuItem(list, StringConstants.TEXT_SKIP_BREAK, canSkip, StringConstants.TEXT_MENU_NO_TIME, OpenClock);
            MenuItem(list, StringConstants.TEXT_SLEEP, Clock.CanSleep, StringConstants.TEXT_MENU_SLEEP_LOCKED, Sleep);
            MenuItem(list, StringConstants.TEXT_MENU_MARKET, StoryProgress.CellarFound,
                StringConstants.TEXT_MENU_MARKET_LOCKED, () => SceneManager.LoadScene(SceneRoutes.MARKET));

            list.RemoveFromClassList("hidden");
            Root.Q<Button>("village-menu").AddToClassList("village-button--on");
        }

        private void MenuItem(VisualElement list, string text, bool enabled, string lockedNote, System.Action action)
        {
            var button = new Button(() =>
            {
                CloseMenu();
                action();
            });

            button.AddToClassList("village-button");
            button.AddToClassList("village-menu-item");
            // 잠긴 항목도 보여 준다. 언제 열리는지를 작은 글씨로 아랫줄에 붙인다.
            button.text = enabled == true ? text : $"{text}\n<size=13>{lockedNote}</size>";
            button.SetEnabled(enabled);

            list.Add(button);
        }

        private void CloseMenu()
        {
            var list = Root.Q<VisualElement>("village-menu-list");

            if (list != null)
            {
                list.AddToClassList("hidden");
            }

            var menu = Root.Q<Button>("village-menu");

            if (menu != null)
            {
                menu.RemoveFromClassList("village-button--on");
            }
        }

        // ── 원형 시계 ─────────────────────────────

        /// <summary>
        /// 몇 시까지 보낼지 고른다. 지금부터 다음 장 직전까지가 고를 수 있는 구간이다(2026-10-17 사용자 요청).
        /// </summary>
        private void OpenClock()
        {
            if (Clock == null || Dial == null)
            {
                return;
            }

            SkipFromClock = false;
            RefreshClockRange();
            Root.Q<VisualElement>("village-clock").RemoveFromClassList("hidden");
        }

        /// <summary>
        /// 고를 수 있는 구간과 토글을 시계의 지금 상태에 맞춘다.
        /// 다음 장을 건너뛰었으면 구간이 그다음 장 직전까지 늘어난다(2026-10-17 사용자 요청).
        /// </summary>
        private void RefreshClockRange()
        {
            Dial.SetRange(Clock.BreakMinuteOfDay, Clock.LatestBreakMinute);

            var next = Clock.NextSessionMinute;
            SetText("village-clock-limit", string.Format(StringConstants.TEXT_CLOCK_LIMIT, next / 60 % 24, next % 60));

            var toggle = Root.Q<Toggle>("village-clock-skip");

            if (toggle == null)
            {
                return;
            }

            // 건너뛸 장의 이름. 이미 하나 건너뛰었으면 그 장을 가리킨다.
            var skipped = Clock.SkippedSessions > 0;
            var morning = skipped == true ? Clock.NextSessionIsMorning == false : Clock.NextSessionIsMorning;
            var session = morning == true ? StringConstants.TEXT_SESSION_MORNING : StringConstants.TEXT_SESSION_EVENING;
            toggle.text = string.Format(StringConstants.TEXT_CLOCK_SKIP_SESSION, session);
            toggle.SetValueWithoutNotify(skipped);
        }

        /// <summary>
        /// 다음 장을 건너뛴다. 기획서 §11 — 장에 나가는 것은 선택이다.
        /// 「오늘 장 쉬기」 단추를 없애고 시계 안의 토글로 옮겼다(2026-10-17 사용자 결정).
        /// 쉬어도 시장은 돈다. 거래소로 돌아가면 쉰 장을 치르고 그동안 자산이 얼마나 움직였는지 알린다.
        /// </summary>
        private void ToggleSkipSession(bool skip)
        {
            if (Clock == null)
            {
                return;
            }

            if (skip == true && Clock.SkippedSessions == 0)
            {
                Clock.SkipNextSession();
                SkipFromClock = true;
            }
            else if (skip == false && Clock.SkippedSessions > 0)
            {
                // 이미 그 장의 시간을 써 버렸으면 무를 수 없다. 시간을 빚질 수는 없다.
                if (Clock.UndoSkippedSession() == true)
                {
                    SkipFromClock = false;
                }
            }

            RefreshClockRange();
        }

        private void ShowClockPick(int minute)
        {
            if (Clock == null)
            {
                return;
            }

            var spend = minute - Clock.BreakMinuteOfDay;
            SetText("village-clock-pick", string.Format(StringConstants.TEXT_CLOCK_PICK,
                minute / 60 % 24, minute % 60, spend / 60, spend % 60));
        }

        private void ConfirmClock()
        {
            if (Clock != null && Dial != null)
            {
                Clock.SpendBreakMinutes(Dial.SelectedMinute - Clock.BreakMinuteOfDay);
            }

            CloseClock();
        }

        private void CancelClock()
        {
            // 시계 안에서 켠 건너뛰기는 「그만둔다」와 함께 되돌린다.
            if (SkipFromClock == true && Clock != null)
            {
                Clock.UndoSkippedSession();
            }

            CloseClock();
        }

        private void CloseClock()
        {
            SkipFromClock = false;
            var clock = Root.Q<VisualElement>("village-clock");

            if (clock != null)
            {
                clock.AddToClassList("hidden");
            }
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
            SetText("village-visits",
                $"{hour:00}:{minute:00} · 다음 장까지 {left / 60}시간 {left % 60}분" + InspectionTag());

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
            Say(spot, body, PortraitOf(pick.StockId), note != null, HeldNote(pick.StockId));
        }

        /// <summary>
        /// 그 종목을 지금 가지고 있으면 "내가 가진 종목 · N주"를 돌려준다. 아니면 null.
        /// 영지에서 캔 소식이 내 돈에 닿는지 그 자리에서 알게 한다(2026-10-17 사용자 요청).
        /// </summary>
        private static string HeldNote(string stockId)
        {
            var sim = BreakClock.Simulation;

            if (sim == null)
            {
                return null;
            }

            var quantity = sim.Wallet.QuantityOf(stockId);
            return quantity > 0 ? string.Format(StringConstants.TEXT_HELD_NOTE, quantity) : null;
        }

        /// <summary>
        /// 자리의 주인이 말한다. 묻는 쪽은 플레이어이므로 말하는 이는 늘 그 자리의 사람이다.
        /// </summary>
        private void Say(VillageSpot spot, string text, Sprite portrait, bool secret, string heldNote = null)
        {
            var line = new DialogueLine(SpeakerOf(spot.InfoSource), spot.Name, text);
            line.Portrait = portrait;
            line.IsSecret = secret;
            line.Note = heldNote == null ? BuildNote(spot, secret) : heldNote + " · " + BuildNote(spot, secret);

            Dialogue.Play(new List<DialogueLine> { line }, Back(spot));
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
