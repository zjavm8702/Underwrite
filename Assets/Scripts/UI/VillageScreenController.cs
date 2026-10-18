using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Save;
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
    [DefaultExecutionOrder(10000)]
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
        private UI_Tutorial Tutorial;

        // 지금 선택지를 띄운 자리. 용사 테이블처럼 자리를 넘겨받지 않는 곳에서 돌아갈 곳이다.
        private VillageSpot CurrentSpot;

        // 서로 돌아보거나 몸짓하는 중이다. 그동안 E를 다시 받지 않는다.
        private bool Approaching;

        // 하루가 넘어갔는지 마지막으로 본 날. 0이면 아직 보지 않았다.
        private int LastDayChecked;

        // 대화가 닫힌 뒤 보일 주인공의 반응.
        private bool HasPending;
        private ActorMotion PendingMotion;
        private ActorEmote PendingEmote;

        // 자동 저장. 마지막 저장 뒤 흐른 실제 초와, 지난 프레임에 대화가 떠 있었는가.
        private float SaveTimer;
        private bool WasTalking;

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

        // 이름표·말풍선은 카메라가 따라간 뒤에 자리를 잡는다. Update에서 잡으면 카메라보다 한발 앞서
        // 움직일 때마다 튀었다(2026-10-17 사용자 보고). 이 컴포넌트는 늦게 돌도록 실행 순서를 뒤로 뺐다.
        private void LateUpdate()
        {
            var talking = Dialogue != null && Dialogue.IsOpen == true;
            WorldBubbles.Tick(talking);
            WorldSigns.Tick(talking || ClockOpen == true || NotebookOpen == true || LedgerOpen == true || MiniGameOpen == true,
                Constants.SIGN_HEIGHT_UNITS);
        }

        // 폰은 꺼지기 전에 알려 주지 않는다. 앱이 뒤로 가는 순간이 마지막 기회다.
        private void OnApplicationPause(bool paused)
        {
            if (paused == true)
            {
                SaveSystem.Save();
            }
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save();
        }

        /// <summary>
        /// 영지에 들어온 첫 프레임, 대화가 끝날 때, 그리고 일정 간격으로 저장한다.
        /// 대화 중에는 저장하지 않는다 — 고르는 도중의 상태가 반쯤 바뀌어 있을 수 있다.
        /// </summary>
        private void AutoSave()
        {
            var talking = Dialogue != null && Dialogue.IsOpen == true;
            SaveTimer -= Time.unscaledDeltaTime;

            if (talking == false && (WasTalking == true || SaveTimer <= 0f))
            {
                SaveSystem.Save();
                SaveTimer = Constants.SAVE_INTERVAL_SECONDS;
            }

            WasTalking = talking;
        }

        /// <summary>지하거래소로 내려간다. 장중은 저장하지 않으므로 내려가기 직전을 남긴다.</summary>
        private void GoToMarket()
        {
            SaveSystem.Save();
            SceneManager.LoadScene(SceneRoutes.UNDERWORLD);
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

                GameAudio.AttachClicks(Root);
            }

            if (Dialogue == null && DialogueTree != null)
            {
                WorldSigns.Attach(Root);
                WorldBubbles.Attach(Root);
                Dialogue = new UI_Dialogue(Root, DialogueTree);
                AddSigns();
            }

            BindLeave();
            EnsureClock();

            // 영지의 시간은 실시간 1:1로 흐른다. 기획서 §11.
            // 첫 안내를 읽는 동안에는 시간이 흐르지 않는다.
            if (Clock != null && (Tutorial == null || Tutorial.IsOpen == false))
            {
                Clock.AdvanceBreak(Time.unscaledDeltaTime);
            }

            AutoSave();

            NearNpc = FindNearNpc();
            RefreshPrompt();

            StartVillageTutorialOnce();
            RefreshObjective();

            NpcAmbient.Player = Player;

            PlayPendingReaction();
            PlayDepartureIfDue();
            PlayQuestIntroIfDue();

            // 실제 시간으로 날이 넘어가도(드물다) 기한은 따진다.
            if (Approaching == false)
            {
                var turned = CheckDayTurn();

                if (turned.Length > 0 && PlayerActor != null)
                {
                    PlayerActor.Say(turned);
                }
            }

            // 대화·시계·메뉴가 떠 있으면 휠이 카메라로 새지 않게 한다.
            VillageCameraZoom.Blocked = (Dialogue != null && Dialogue.IsOpen == true) || ClockOpen == true
                || MenuOpen == true || NotebookOpen == true || LedgerOpen == true || MiniGameOpen == true
                || Approaching == true;

            // 연출 중(돌아보기·건네기·시간이 흐르는 동안)과 대화·시계·수첩이 떠 있는 동안에는 주인공이 걷지 않는다
            // (2026-10-17 사용자 보고).
            LockPlayer(Approaching == true || (Dialogue != null && Dialogue.IsOpen == true) || ClockOpen == true
                || NotebookOpen == true || LedgerOpen == true || MiniGameOpen == true);

            var keyboard = Keyboard.current;

            if (Dialogue != null && Dialogue.IsChoosing == true)
            {
                PickByNumber(keyboard);
                return;
            }

            if ((Here == null && NearNpc == null) || Dialogue == null || Dialogue.IsOpen == true
                || ClockOpen == true || Approaching == true || NotebookOpen == true || LedgerOpen == true
                || MiniGameOpen == true || (Tutorial != null && Tutorial.IsOpen == true))
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
                StartCoroutine(ApproachSpot(Here));
                return;
            }

            StartCoroutine(ApproachNpc(NearNpc));
        }

        // ── 필드 연기 (2026-10-17) ────────────────
        // 대화창은 화면을 덮는다. 그래서 상호작용은 대화 앞뒤로 필드에서 보인다.

        private CharacterActor PlayerActor
        {
            get
            {
                return Player == null ? null : Player.GetComponent<CharacterActor>();
            }
        }

        /// <summary>문 앞에서 E. 주인공이 문 쪽을 돌아본 뒤 선택지가 열린다.</summary>
        private System.Collections.IEnumerator ApproachSpot(VillageSpot spot)
        {
            Approaching = true;
            var actor = PlayerActor;

            if (actor != null)
            {
                // 문은 문 앞 자리의 위(화면 안쪽)에 있다.
                actor.Face(Vector2.up);
            }

            yield return new WaitForSeconds(Constants.ACTOR_APPROACH_SECONDS);
            Approaching = false;
            Ask(spot);
        }

        /// <summary>
        /// 영지 사람에게 말을 건다. 대화창을 열지 않고 필드에서 끝낸다 — 서로 돌아보고,
        /// 상대가 머리 위 말풍선으로 대꾸하며 몸짓한다. 한마디 인사에 화면을 덮을 이유가 없다.
        /// 모험단과는 관계를 맺지 않는다(설계안 §1-2). 이름과 얼굴만 아는 사이다.
        /// </summary>
        private System.Collections.IEnumerator ApproachNpc(VillageNpc npc)
        {
            Approaching = true;
            var me = PlayerActor;
            var them = npc.GetComponent<CharacterActor>();

            if (me != null)
            {
                me.FaceTowards(npc.transform.position);
            }

            if (them != null && Player != null)
            {
                them.FaceTowards(Player.position);
                them.Emote(ActorEmote.Emote_Question);
            }

            yield return new WaitForSeconds(Constants.ACTOR_APPROACH_SECONDS);

            if (them != null)
            {
                them.Say(npc.PickLine());
                them.Play(MotionOf(npc.Kind));
            }

            yield return new WaitForSeconds(Constants.ACTOR_APPROACH_SECONDS);

            // 모험단에게 직접 말을 걸면 그 단에 대해 하나 알게 된다. 수첩(도감)에 적는다.
            var learned = npc.Party != null ? BandCodex.Learn(npc.Party.Stock, CodexSource.Codex_Talk) : null;

            if (learned != null && me != null)
            {
                me.Say(string.Format(StringConstants.TEXT_CODEX_LEARNED, npc.Group));
                me.Emote(ActorEmote.Emote_Exclaim);
            }

            Approaching = false;
        }

        private static ActorMotion MotionOf(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Role_Vendor:
                {
                    return ActorMotion.Motion_Hop;
                }
                case NpcRole.Role_Adventurer:
                {
                    return ActorMotion.Motion_Shake;
                }
                default:
                {
                    return ActorMotion.Motion_Nod;
                }
            }
        }

        /// <summary>
        /// 주인공의 반응을 적어 두고 다음 일로 바로 넘어간다. 반응은 대화가 완전히 닫힌 뒤 필드에서 한다.
        ///
        /// 처음에는 대화를 닫고 반응한 뒤 선택지를 다시 열었는데, 필드가 잠깐 비쳤다 다시 덮여 어색했다
        /// (2026-10-17 사용자 보고). 그래서 같은 자리의 볼일은 끊김 없이 잇고, 자리를 뜰 때 마지막 반응을 보인다.
        /// 더 센 반응(환호 > 깡충 > 놀람 > 끄덕)이 약한 것을 덮는다.
        /// </summary>
        private System.Action React(ActorMotion motion, ActorEmote emote, System.Action then)
        {
            return () =>
            {
                if (HasPending == false || Strength(motion) >= Strength(PendingMotion))
                {
                    HasPending = true;
                    PendingMotion = motion;
                    PendingEmote = emote;
                }

                if (then != null)
                {
                    then();
                }
            };
        }

        private static int Strength(ActorMotion motion)
        {
            switch (motion)
            {
                case ActorMotion.Motion_Cheer:
                {
                    return 3;
                }
                case ActorMotion.Motion_Hop:
                {
                    return 2;
                }
                case ActorMotion.Motion_Shake:
                {
                    return 1;
                }
                default:
                {
                    return 0;
                }
            }
        }

        /// <summary>대화가 닫혔으면 적어 둔 반응을 필드에서 한다. 매 프레임 부른다.</summary>
        private void PlayPendingReaction()
        {
            if (HasPending == false || Dialogue == null || Dialogue.IsOpen == true)
            {
                return;
            }

            HasPending = false;
            var actor = PlayerActor;

            if (actor == null)
            {
                return;
            }

            actor.Face(Vector2.down);
            actor.Emote(PendingEmote);
            actor.Play(PendingMotion);
        }

        private PlayerCharacterInput PlayerInput;

        private void LockPlayer(bool locked)
        {
            if (PlayerInput == null)
            {
                PlayerInput = FindAnyObjectByType<PlayerCharacterInput>();
            }

            if (PlayerInput != null && PlayerInput.enabled == locked)
            {
                PlayerInput.enabled = locked == false;
            }
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

            var tag = string.IsNullOrEmpty(npc.Group) == true ? role : $"{role} · {npc.Group}";

            // 모험단이면 시장에서 어디에 있는지, 내가 가졌는지를 붙인다. 숫자로만 보던 종목을 영지에서 만난다.
            var party = npc.Party;
            var sim = BreakClock.Simulation;

            if (party == null || sim == null || string.IsNullOrEmpty(party.Stock) == true)
            {
                return tag;
            }

            var stage = sim.StageOf(party.Stock);

            if (stage == BandStage.Band_Arrived)
            {
                return tag + " · " + StringConstants.TEXT_BAND_ARRIVED;
            }

            var held = sim.Wallet.QuantityOf(party.Stock);
            return held > 0
                ? tag + " · " + string.Format(StringConstants.TEXT_BAND_HELD, held)
                : tag + " · " + StringConstants.TEXT_BAND_LISTED;
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
                    Ask(spot, true);
                }
            };
        }

        private void Ask(VillageSpot spot)
        {
            Ask(spot, false);
        }

        /// <summary>
        /// 자리의 선택지. <paramref name="again"/>이면 볼일을 하나 마치고 돌아온 것이다 — 처음 인사를 되풀이하지 않는다
        /// (2026-10-17 사용자 보고: "들은 게 있긴 한데…"가 자꾸 나온다).
        /// </summary>
        private void Ask(VillageSpot spot, bool again)
        {
            CurrentSpot = spot;

            if (again == false)
            {
                GameAudio.Play(SoundEffect.Sound_Door);
            }

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

            if (spot.InfoSource == InfoSource.Source_GuildRecord)
            {
                choices.Add(LedgerChoice(spot));
            }

            if (spot.HasRequestDesk == true)
            {
                choices.Add(PostChoice(spot));
            }

            if (spot.HasCellar == true)
            {
                if (StoryProgress.CellarFound == true)
                {
                    choices.Add(EavesChoice());
                }

                choices.Add(HeroTableChoice(spot));
                choices.Add(SleepChoice());
            }

            // 창고 안쪽을 아직 모른다. 웹툰 5~6화의 사건을 겪어야 길이 열린다.
            if (spot.HasCellar == true && StoryProgress.CellarFound == true)
            {
                // 웹툰 6화. 술통을 옮기러 내려간 창고 안쪽에 지하거래소가 있다.
                var cellar = new DialogueChoice(StringConstants.TEXT_CELLAR_GO,
                    GoToMarket);
                cellar.IsEnabled = MarketOpenNow();
                cellar.Note = cellar.IsEnabled == true ? StringConstants.TEXT_CELLAR_NOTE : MarketClosedNote();
                choices.Add(cellar);
            }

            choices.Add(new DialogueChoice(StringConstants.TEXT_GATHER_LEAVE, null));

            var line = OwnerLine(spot,
                again == true ? StringConstants.TEXT_GATHER_ASK_AGAIN : StringConstants.TEXT_GATHER_ASK);
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

            var owner = InfoPeople.Speaker(spot.InfoSource);
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
            Dialogue.Play(lines, React(ActorMotion.Motion_Shake, ActorEmote.Emote_Exclaim, () => Ask(spot)));
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

                // 떠난 사람의 자리도 테이블에 남겨 둔다. 비어 있는 것이 보여야 한다.
                if (hero.Condition == HeroCondition.Condition_Dead)
                {
                    choice.IsEnabled = false;
                    choice.Note = StringConstants.TEXT_HERO_GONE;
                    choices.Add(choice);
                    continue;
                }

                choice.IsEnabled = hero.LastTalkDay != day;
                choice.Note = choice.IsEnabled == true ? hero.Role : StringConstants.TEXT_HERO_TALKED_TODAY;
                choices.Add(choice);
            }

            choices.Add(new DialogueChoice(StringConstants.TEXT_REQUEST_CANCEL, () => Ask(spot, true)));

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

            var topic = HeroTopics.Next(hero);

            if (topic != null)
            {
                TalkAbout(hero, topic);
                return;
            }

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
        /// 용사가 이야기를 꺼낸다. 주인공은 대답을 고른다. 반응부터는 평소 대화와 같다(호감·몸짓·현장 이야기).
        /// </summary>
        private void TalkAbout(HeroProfile hero, HeroTopic topic)
        {
            hero.TopicsSeen.Add(topic.Id);

            var opening = HeroLine(hero, topic.Opening, topic.OpeningEmotion);
            opening.Choices = new List<DialogueChoice>();

            for (var i = 0; i < topic.Replies.Length; i++)
            {
                var reply = topic.Replies[i];
                opening.Choices.Add(new DialogueChoice(reply.Text,
                    () => ReactToHero(hero, reply.Affinity, reply.Answer, reply.AnswerEmotion)));
            }

            Dialogue.Play(new List<DialogueLine> { opening }, null);
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
                    var heldTag = HeldNote(pick.StockId);

                    field.Badge = heldTag;
                    lines.Add(field);
                    GatherSession.Record(0f, 0f, new GatheredNote(pick.StockId, pick.Headline,
                        InfoSource.Source_TavernTalk));
                }
            }

            // 이야기를 마치면 테이블로 돌아간다. 다른 사람에게도 말을 걸 수 있다.
            // 따뜻하게 대답했으면 주인공도 살짝 들뜬다. 숫자 대신 몸짓으로 관계가 오른 것을 보인다.
            System.Action back = () =>
            {
                if (CurrentSpot != null)
                {
                    OpenHeroTable(CurrentSpot);
                }
            };

            Dialogue.Play(lines, affinity >= 2
                ? React(ActorMotion.Motion_Hop, ActorEmote.Emote_Heart, back)
                : React(ActorMotion.Motion_Nod, ActorEmote.Emote_Dots, back));
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
            var sword = HeroRoster.All[1].Condition == HeroCondition.Condition_Dead ? favorite : HeroRoster.All[1];
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

            // 데모의 마지막 귀환이면 귀환 장면 뒤에 마무리를 붙인다.
            if (StoryProgress.DemoEndSeen == false && MarketClock.SeasonOf(day) >= Constants.DEMO_END_SEASON)
            {
                var after = onDone;
                onDone = () => PlayDemoEnd(after);
            }

            HeroRoster.MarkReturn(day);
            var heroes = HeroRoster.All;
            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_EXPEDITION_TAG, StringConstants.TEXT_RETURN_OPEN)
            };

            // 지난 계절에 놓친 사정의 대가. 몸 상태가 바뀐 날이 지난 계절 안이면 이번 원정에서 일어난 일이다.
            var since = day - Constants.SEASON_DAYS;
            var struck = false;

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];

                if (hero.ConditionDay < since || hero.ConditionDay >= day)
                {
                    continue;
                }

                if (hero.Condition == HeroCondition.Condition_Dead)
                {
                    if (struck == false)
                    {
                        lines[0].Text = StringConstants.TEXT_RETURN_DEATH_OPEN;
                    }

                    lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_EXPEDITION_TAG,
                        string.Format(StringConstants.TEXT_RETURN_DEATH_NAME, hero.Name)));
                    var mourner = HeroRoster.Favorite();

                    if (mourner != null)
                    {
                        lines.Add(HeroLine(mourner, StringConstants.TEXT_RETURN_DEATH_LINE, PortraitEmotion.Emotion_Sad));
                    }

                    struck = true;
                }
                else if (hero.Condition == HeroCondition.Condition_Wounded)
                {
                    lines.Add(HeroLine(hero, StringConstants.HERO_RETURN_WOUNDED[i], PortraitEmotion.Emotion_Sad));
                    struck = true;
                }
            }

            if (struck == false)
            {
                var warrior = heroes[Constants.HERO_INDEX_WARRIOR];
                var speaker = warrior.Condition == HeroCondition.Condition_Dead ? HeroRoster.Favorite() : warrior;

                if (speaker != null)
                {
                    lines.Add(HeroLine(speaker, StringConstants.TEXT_RETURN_GOOD, PortraitEmotion.Emotion_Happy));
                }
            }

            if (heroes[0].Condition != HeroCondition.Condition_Dead
                && PatronageLedger.Clues >= Constants.HERO_SUSPECT_CLUES)
            {
                lines.Add(HeroLine(heroes[0], StringConstants.TEXT_RETURN_SUSPECT, PortraitEmotion.Emotion_Shocked));
            }

            Dialogue.Play(lines, onDone);
        }

        /// <summary>
        /// 데모 마무리(2026-10-18). 두 계절 동안 무엇을 지켰고 무엇을 잃었는지 숫자로 한 번 보여 준다.
        /// 「조금 더 둘러본다」면 판은 그대로 이어진다 — 저장도 지우지 않는다.
        /// </summary>
        private void PlayDemoEnd(System.Action onStay)
        {
            StoryProgress.DemoEndSeen = true;

            var healthy = 0;
            var wounded = 0;
            var dead = 0;
            var heroes = HeroRoster.All;

            for (var i = 0; i < heroes.Count; i++)
            {
                switch (heroes[i].Condition)
                {
                    case HeroCondition.Condition_Dead:
                    {
                        dead++;
                        break;
                    }
                    case HeroCondition.Condition_Wounded:
                    {
                        wounded++;
                        break;
                    }
                    default:
                    {
                        healthy++;
                        break;
                    }
                }
            }

            var sim = BreakClock.Simulation;
            var worth = (sim == null ? 0f : sim.Wallet.TotalValue(sim.StockLookup)) + GatherSession.NetChange;
            var last = new DialogueLine(string.Empty, StringConstants.TEXT_DEMO_TAG, StringConstants.TEXT_DEMO_END_2);
            last.Choices = new List<DialogueChoice>
            {
                new DialogueChoice(StringConstants.TEXT_DEMO_TO_TITLE, () =>
                {
                    SaveSystem.Save();
                    SceneManager.LoadScene(SceneRoutes.INTRO);
                }),
                new DialogueChoice(StringConstants.TEXT_DEMO_STAY, onStay)
            };

            Dialogue.SetBackground(BackgroundOf(InfoSource.Source_TavernTalk));
            Dialogue.Play(new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_DEMO_TAG, StringConstants.TEXT_DEMO_END_1),
                new DialogueLine(string.Empty, StringConstants.TEXT_DEMO_TAG, string.Format(
                    StringConstants.TEXT_DEMO_END_SUMMARY, worth, healthy, wounded, dead, SeasonPlan.OmenClues)),
                last
            }, null);
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
                new DialogueChoice(StringConstants.TEXT_REQUEST_CANCEL, () => Ask(spot, true))
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
            Dialogue.Play(new List<DialogueLine> { line },
                React(ActorMotion.Motion_Nod, ActorEmote.Emote_Dots, Back(CurrentSpot)));
        }

        /// <summary>
        /// 영지의 시계가 자정을 넘었으면, 지나간 날의 후원 기한을 따지고 알릴 말을 모은다.
        ///
        /// 잠자리(하루 마감)는 2026-10-17에 뺐다 — 시간 넘기기가 있고, 이세계에 떨어진 주인공이 어디서 자는지도
        /// 맞지 않으며, 정보는 수첩과 정보 기록에 이미 남는다. 그래서 하루를 닫는 일은 시간이 흐르는 순간에 한다.
        /// 넘어간 날이 없으면 빈 문자열.
        /// </summary>
        private string CheckDayTurn()
        {
            var today = Today();

            if (LastDayChecked <= 0)
            {
                LastDayChecked = today;
                PartyCasting.Cast(today, false);
                return string.Empty;
            }

            if (today <= LastDayChecked)
            {
                return string.Empty;
            }

            var failed = 0;

            for (var day = LastDayChecked; day < today; day++)
            {
                failed += PatronageLedger.EndOfDay(day);
            }

            LastDayChecked = today;

            // 날이 바뀌면 영지를 걷는 단도 바뀐다.
            PartyCasting.Cast(today, false);

            if (failed > 0)
            {
                return StringConstants.TEXT_DAY_END_FAILED;
            }

            var notes = new List<string>();
            var deadline = PatronageLedger.NearestDeadline(today);

            if (deadline > 0)
            {
                notes.Add(string.Format(StringConstants.TEXT_DAY_END_DEADLINE, deadline));
            }

            if (PatronageLedger.HasUnheardUrgent(today) == true)
            {
                notes.Add(StringConstants.TEXT_DAY_END_WARNING);
            }

            if (MarketClock.IsInspectionDay(today) == true)
            {
                notes.Add(StringConstants.TEXT_DAY_TODAY_INSPECTION);
            }

            return string.Join("\n", notes);
        }

        /// <summary>
        /// 지하거래소로 내려갈 수 있는가. 장이 열려 있을 때(곧 열릴 때)만 된다.
        /// 장이 끝나면 지하세계는 닫힌다(2026-10-17 사용자 결정).
        /// </summary>
        private bool MarketOpenNow()
        {
            return Clock != null && Clock.BreakMinutesLeft <= Constants.BREAK_SKIP_MARGIN_MINUTES;
        }

        private string MarketClosedNote()
        {
            return Clock == null
                ? StringConstants.TEXT_MARKET_CLOSED_NOTE
                : string.Format(StringConstants.TEXT_MARKET_OPENS_AT, ClockText.FromMinutes(Clock.NextSessionMinute));
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

            StartWorkOrders(spot);
        }

        // ── 주문 나르기 (2026-10-17 사장님 결정 — 실제로 해야 끝나는 일) ───────

        private UI_WorkOrders Work;
        private VillageSpot WorkSpot;

        private bool WorkOpen
        {
            get
            {
                return Work != null && Work.IsOpen == true;
            }
        }

        // 손놀림·엿듣기 화면이 떠 있는가. 떠 있는 동안 주인공은 걷지 않고 휠은 카메라로 새지 않는다.
        private bool MiniGameOpen
        {
            get
            {
                return WorkOpen == true || (Eaves != null && Eaves.IsOpen == true);
            }
        }

        // ── 여관 엿듣기 (시간구조 설계안 §7) ───────
        // 술을 나르며 테이블 사이를 돈다. 누구 곁에 머물지 고르는 판단형. 하루에 한 번.

        private UI_Eavesdrop Eaves;
        private EavesdropSession EavesSession;
        private int EavesDay;

        private DialogueChoice EavesChoice()
        {
            var choice = new DialogueChoice(StringConstants.TEXT_EAVES_CHOICE, StartEaves);
            var left = Clock == null ? 0 : Clock.BreakMinutesLeft;

            if (EavesDay == Today())
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_EAVES_DONE_TODAY;
            }
            else if (left < Constants.EAVES_TURNS * Constants.EAVES_MINUTES)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_GATHER_NO_TIME;
            }
            else
            {
                choice.Note = string.Format(StringConstants.TEXT_EAVES_CHOICE_NOTE, Constants.EAVES_TURNS,
                    Constants.EAVES_MINUTES);
            }

            return choice;
        }

        private void StartEaves()
        {
            var heroesTalk = HeroRoster.Favorite() != null && PatronageLedger.HasOverhear(Today()) == true;
            EavesSession = EavesdropSession.Create(BreakClock.Simulation, heroesTalk,
                new System.Random(Random.Range(int.MinValue, int.MaxValue)));

            if (EavesSession == null)
            {
                Dialogue.Play(new List<DialogueLine>
                {
                    new DialogueLine(string.Empty, StringConstants.TEXT_EAVES_TITLE, StringConstants.TEXT_EAVES_NONE)
                }, null);
                return;
            }

            Dialogue.Close();
            EavesDay = Today();

            if (Eaves == null)
            {
                Eaves = new UI_Eavesdrop(Root);
                Eaves.Listened += ListenAt;
                Eaves.Left += () => Eaves.Close();
            }

            Eaves.Open(EavesSession);
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        private void ListenAt(EavesdropSession.Table table)
        {
            var heard = EavesSession.Listen(table);
            Clock.SpendBreakMinutes(Constants.EAVES_MINUTES);

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
                Eaves.AddLine(PatronageLedger.Overhear(Today()) ?? StringConstants.TEXT_EAVES_HEROES_NOTHING);
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

        private void StartWorkOrders(VillageSpot spot)
        {
            Dialogue.Close();
            WorkSpot = spot;

            if (Work == null)
            {
                Work = new UI_WorkOrders(Root);
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
            Work.Open(task, title, BackgroundOf(spot.InfoSource));
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        /// <summary>일을 마쳤다. 품삯은 손놀림만큼(<paramref name="pay"/>는 정해진 품삯에 곱하는 비율).</summary>
        private void FinishWork(VillageSpot spot, float pay)
        {
            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            var quest = spot.Work;
            var reward = Mathf.Round(quest.Reward * pay);
            GameAudio.Play(SoundEffect.Sound_Coins);
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

            GatherSession.Record(0f, reward, note);

            var minutes = Clock == null ? 0 : Clock.BreakMinutesLeft;
            var line = OwnerLine(spot, text);
            line.IsSecret = note != null;
            line.Note = string.Format(StringConstants.TEXT_WORK_RESULT, reward, minutes / 60, minutes % 60);
            var held = rumorStock == null ? null : HeldNote(rumorStock);

            line.Badge = held;

            Dialogue.Play(new List<DialogueLine> { line },
                React(ActorMotion.Motion_Cheer, ActorEmote.Emote_Coin, Back(spot)));
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
            if ((Dialogue != null && Dialogue.IsOpen == true) || Approaching == true)
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
            MenuItem(list, StringConstants.TEXT_NOTEBOOK, true, string.Empty, () => OpenNotebook(false));
            MenuItem(list, string.Format(StringConstants.TEXT_MENU_VOLUME, StringConstants.TEXT_VOLUME_STEPS[GameAudio.VolumeStep]),
                true, string.Empty, GameAudio.CycleVolume);

            if (CanSleep() == true)
            {
                MenuItem(list, StringConstants.TEXT_SLEEP_CHOICE, true, string.Empty, () =>
                {
                    CloseMenu();
                    Sleep();
                });
            }
            MenuItem(list, StringConstants.TEXT_MENU_MARKET, StoryProgress.CellarFound && MarketOpenNow(),
                StoryProgress.CellarFound == false ? StringConstants.TEXT_MENU_MARKET_LOCKED : MarketClosedNote(),
                GoToMarket);

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

        // ── 첫 안내와 할 일 ───────────────────────

        /// <summary>
        /// 처음 영지에 서면 어디로 가서 무엇을 누르면 어떻게 되는지까지 알려 준다(2026-10-17 사용자 요청).
        /// 웹툰이 끝나고 처음 서는 곳이다. 그동안 시간을 멈춘다.
        /// </summary>
        private void StartVillageTutorialOnce()
        {
            if (Tutorial == null)
            {
                Tutorial = new UI_Tutorial(Root, StringConstants.TEXT_TUTORIAL_NEXT, StringConstants.TEXT_TUTORIAL_DONE,
                    StringConstants.TEXT_TUTORIAL_SKIP);
            }

            Tutorial.Tick();

            if (StoryProgress.VillageTutorialDone == true || Tutorial.IsOpen == true || StoryProgress.CellarFound == true)
            {
                return;
            }

            StoryProgress.VillageTutorialDone = true;
            Tutorial.Play(new List<TutorialStep>
            {
                new TutorialStep(null, StringConstants.TEXT_TUT_VILLAGE_1_TITLE, StringConstants.TEXT_TUT_VILLAGE_1_BODY),
                new TutorialStep("village-visits", StringConstants.TEXT_TUT_VILLAGE_2_TITLE,
                    StringConstants.TEXT_TUT_VILLAGE_2_BODY),
                new TutorialStep("village-objective", StringConstants.TEXT_TUT_VILLAGE_3_TITLE,
                    StringConstants.TEXT_TUT_VILLAGE_3_BODY),
                new TutorialStep(null, StringConstants.TEXT_TUT_VILLAGE_4_TITLE, StringConstants.TEXT_TUT_VILLAGE_4_BODY),
                new TutorialStep("village-menu", StringConstants.TEXT_TUT_VILLAGE_5_TITLE,
                    StringConstants.TEXT_TUT_VILLAGE_5_BODY)
            }, null);
        }

        /// <summary>
        /// 화면 왼쪽 위의 「할 일」(QuestLog). 지금 할 일과 어디서 무엇을 누르면 되는지.
        /// 마치면 주인공이 기뻐하고 머리 위에 알린다. 보상이 있으면 영지에서 번 돈으로 들어간다.
        /// </summary>
        private void RefreshObjective()
        {
            var label = Root.Q<Label>("village-objective");

            if (label == null)
            {
                label = new Label();
                label.name = "village-objective";
                label.AddToClassList("village-objective");
                label.pickingMode = PickingMode.Ignore;
                Root.Add(label);
            }

            var done = QuestLog.Tick();

            if (done != null)
            {
                if (done.Reward > 0f)
                {
                    GatherSession.Record(0f, done.Reward, null);
                }

                var actor = PlayerActor;

                if (actor != null)
                {
                    actor.Say(done.Reward > 0f
                        ? string.Format(StringConstants.TEXT_QUEST_DONE_REWARD, done.Title, done.Reward)
                        : string.Format(StringConstants.TEXT_QUEST_DONE, done.Title));
                }

                HasPending = true;
                PendingMotion = ActorMotion.Motion_Cheer;
                PendingEmote = ActorEmote.Emote_Music;
            }

            // 받은 것과 진행도만 보인다(2026-10-17 사장님 결정). 아직 받지 않은 일은 보이지 않는다.
            var active = QuestLog.HasPendingIntro == true ? null : QuestLog.Active;
            var text = string.Empty;

            if (active != null)
            {
                text = string.Format(StringConstants.TEXT_QUEST_LABEL, active.Title);

                if (active.Goal > 0 && active.Progress != null)
                {
                    text += "\n<size=15>" + string.Format(StringConstants.TEXT_QUEST_PROGRESS,
                        System.Math.Min(active.Progress(), active.Goal), active.Goal) + "</size>";
                }
            }

            label.text = text;
            label.EnableInClassList("hidden", active == null);
        }

        /// <summary>
        /// 새 할 일을 받는 장면. 맡기는 사람이 직접 부탁한다 — 그 말이 어디로 가야 하는지 알려 주는 유일한 길잡이다.
        /// 대화·연출·튜토리얼이 없을 때 띄운다.
        /// </summary>
        private void PlayQuestIntroIfDue()
        {
            if (QuestLog.HasPendingIntro == false || Dialogue == null || Dialogue.IsOpen == true
                || Approaching == true || ClockOpen == true || LedgerOpen == true || MiniGameOpen == true
                || DepartureRunning == true
                || (Tutorial != null && Tutorial.IsOpen == true))
            {
                return;
            }

            var quest = QuestLog.Active;
            QuestLog.MarkIntroduced();
            DialogueLine line;

            // 부탁은 맡기는 사람의 자리에서 받는다. 혼잣말과 용사단의 부탁은 여관이다.
            var place = InfoSource.Source_TavernTalk;

            switch (quest.Giver)
            {
                case QuestGiver.Giver_Innkeeper:
                {
                    line = GiverLine(InfoSource.Source_TavernTalk, quest.Ask);
                    break;
                }
                case QuestGiver.Giver_Archivist:
                {
                    line = GiverLine(InfoSource.Source_GuildRecord, quest.Ask);
                    place = InfoSource.Source_GuildRecord;
                    break;
                }
                case QuestGiver.Giver_Broker:
                {
                    line = GiverLine(InfoSource.Source_MarketBoard, quest.Ask);
                    place = InfoSource.Source_MarketBoard;
                    break;
                }
                case QuestGiver.Giver_Swordswoman:
                {
                    line = HeroLine(HeroRoster.All[1], quest.Ask, PortraitEmotion.Emotion_Happy);
                    break;
                }
                case QuestGiver.Giver_Scholar:
                {
                    line = HeroLine(HeroRoster.All[0], quest.Ask, PortraitEmotion.Emotion_Neutral);
                    break;
                }
                default:
                {
                    line = new DialogueLine(string.Empty, StringConstants.TEXT_QUEST_SELF_TAG, quest.Ask);
                    break;
                }
            }

            line.Note = string.Format(StringConstants.TEXT_QUEST_LABEL, quest.Title);
            Dialogue.SetBackground(BackgroundOf(place));
            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private DialogueLine GiverLine(InfoSource source, string text)
        {
            var line = new DialogueLine(InfoPeople.Speaker(source), StringConstants.TEXT_QUEST_ASK_TAG, text);
            line.Portrait = InfoPeople.Face(Data, source, PortraitEmotion.Emotion_Neutral);
            return line;
        }

        // ── 수첩 (2026-10-17 사용자 요청) ─────────
        // 영지에서 알아낸 것을 모아 본다. 들은 소식과 모험단 도감 두 갈래.

        private bool NotebookOpen
        {
            get
            {
                var book = Root.Q<VisualElement>("notebook");
                return book != null && book.style.display != DisplayStyle.None;
            }
        }

        private void OpenNotebook(bool bands)
        {
            var book = Root.Q<VisualElement>("notebook");

            if (book == null)
            {
                book = new VisualElement();
                book.name = "notebook";
                book.AddToClassList("notebook");

                var panel = new VisualElement();
                panel.AddToClassList("popup");
                panel.AddToClassList("notebook-panel");

                var title = new Label(StringConstants.TEXT_NOTEBOOK);
                title.AddToClassList("popup-title");

                var tabs = new VisualElement();
                tabs.AddToClassList("notebook-tabs");
                var news = new Button(() => OpenNotebook(false));
                news.name = "notebook-news";
                news.text = StringConstants.TEXT_NOTEBOOK_NEWS;
                news.AddToClassList("btn");
                var codex = new Button(() => OpenNotebook(true));
                codex.name = "notebook-bands";
                codex.text = StringConstants.TEXT_CODEX_TAB;
                codex.AddToClassList("btn");
                tabs.Add(news);
                tabs.Add(codex);

                var list = new ScrollView();
                list.name = "notebook-list";
                list.AddToClassList("notebook-list");

                var close = new Button(() => book.style.display = DisplayStyle.None);
                close.text = StringConstants.TEXT_NOTEBOOK_CLOSE;
                close.AddToClassList("popup-button");
                close.AddToClassList("popup-button--main");

                panel.Add(title);
                panel.Add(tabs);
                panel.Add(list);
                panel.Add(close);
                book.Add(panel);
                Root.Add(book);
            }

            book.style.display = DisplayStyle.Flex;
            book.BringToFront();
            Root.Q<Button>("notebook-news").EnableInClassList("btn--selected", bands == false);
            Root.Q<Button>("notebook-bands").EnableInClassList("btn--selected", bands == true);

            var scroll = Root.Q<ScrollView>("notebook-list");
            scroll.Clear();
            var sim = BreakClock.Simulation;

            if (bands == true)
            {
                var subjects = BandCodex.Subjects();

                for (var i = 0; i < subjects.Count; i++)
                {
                    var id = subjects[i];
                    scroll.Add(CodexCards.Build(sim, id, NameFor(sim, id)));
                }

                return;
            }

            // 이번에 영지에서 캔 것(가장 최근부터), 그 앞의 나만 아는 소식들.
            var shown = 0;
            var gathered = GatherSession.Gathered;

            for (var i = gathered.Count - 1; i >= 0; i--)
            {
                scroll.Add(NewsCard(sim, gathered[i].StockId, gathered[i].Headline));
                shown++;
            }

            if (sim != null)
            {
                var log = sim.InfoLog;

                for (var i = log.Count - 1; i >= 0 && shown < Constants.NOTEBOOK_MAX_ROWS; i--)
                {
                    if (log[i].IsPublic == true)
                    {
                        continue;
                    }

                    scroll.Add(NewsCard(sim, log[i].StockId, log[i].Headline));
                    shown++;
                }
            }

            if (shown == 0)
            {
                var empty = new Label(StringConstants.TEXT_NOTEBOOK_EMPTY);
                empty.AddToClassList("t-note");
                scroll.Add(empty);
            }
        }

        private VisualElement NewsCard(MarketSimulation sim, string stockId, string headline)
        {
            var card = new VisualElement();
            card.AddToClassList("info-card");
            card.AddToClassList("info-card--private");

            var held = sim == null ? 0 : sim.Wallet.QuantityOf(stockId);
            card.EnableInClassList("info-card--held", held > 0);

            var line = new VisualElement();
            line.AddToClassList("info-line");
            var name = new Label(NameFor(sim, stockId));
            name.AddToClassList("info-stock");
            line.Add(name);

            if (held > 0)
            {
                var badge = new Label(string.Format(StringConstants.TEXT_INFO_HELD, held));
                badge.AddToClassList("info-held");
                line.Add(badge);
            }

            var text = new Label(headline);
            text.AddToClassList("info-headline");
            card.Add(line);
            card.Add(text);
            return card;
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
            SetText("village-clock-limit", string.Format(StringConstants.TEXT_CLOCK_LIMIT, DialTimeText(next)));

            var toggle = Root.Q<Toggle>("village-clock-skip");

            if (toggle == null)
            {
                return;
            }

            // 건너뛸 장의 이름. 이미 하나 건너뛰었으면 그 장을 가리킨다.
            var skipped = Clock.SkippedSessions > 0;
            var morning = Clock.SessionAt(skipped == true ? Clock.SkippedSessions - 1 : 0) == MarketSession.Session_Morning;
            var session = morning == true ? StringConstants.TEXT_SESSION_MORNING : StringConstants.TEXT_SESSION_EVENING;
            toggle.text = string.Format(StringConstants.TEXT_CLOCK_SKIP_SESSION, session);
            toggle.SetValueWithoutNotify(skipped);

            // 계절 첫날의 재개장은 건너뛸 수 없다(설계안 §2). 막힌 까닭을 토글 글자로 알린다.
            var locked = skipped == false && Clock.IsReopeningAt(0) == true;
            toggle.SetEnabled(locked == false);

            if (locked == true)
            {
                toggle.text = StringConstants.TEXT_CLOCK_SKIP_REOPENING;
            }
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
                DialTimeText(minute), spend / 60, spend % 60));
        }

        /// <summary>
        /// 시계 창에 적는 시각. 지금보다 다음 날이면 「내일」을 붙인다 — 문자판은 12시간이라
        /// 바늘만 보고는 오늘 밤 11시인지 내일 오후 1시인지 알 수 없다(띠 색과 함께 읽힌다).
        /// </summary>
        private string DialTimeText(int minute)
        {
            var text = ClockText.FromMinutes(minute);

            if (minute / (24 * 60) > Clock.BreakMinuteOfDay / (24 * 60))
            {
                return string.Format(StringConstants.TEXT_CLOCK_TOMORROW, text);
            }

            return text;
        }

        private void ConfirmClock()
        {
            var minutes = Clock != null && Dial != null ? Dial.SelectedMinute - Clock.BreakMinuteOfDay : 0;
            CloseClock();

            if (minutes > 0)
            {
                StartCoroutine(PassTime(minutes));
            }
        }

        /// <summary>
        /// 시간이 흐르는 연출(2026-10-17 사용자 요청). 화면이 어두워지고 시각이 넘어간 뒤 다시 밝아진다.
        /// 어두운 동안 사람들이 자리를 옮긴다 — 다시 밝아지면 영지가 다른 시간의 모습이다.
        /// </summary>
        private System.Collections.IEnumerator PassTime(int minutes)
        {
            return PassTime(minutes, null);
        }

        /// <param name="title">어두운 화면에 시각 대신 띄울 말. 비우면 「몇 시 → 몇 시」.</param>
        private System.Collections.IEnumerator PassTime(int minutes, string title)
        {
            Approaching = true;
            var fade = EnsureFade();
            var fromHour = Clock.BreakHour;
            var fromMinute = Clock.BreakMinute;

            yield return FadeTo(fade, 1f);

            Clock.SpendBreakMinutes(minutes);
            SetText("village-fade-time", title ?? string.Format(StringConstants.TEXT_TIME_PASS,
                ClockText.FromMinutes(fromHour * 60 + fromMinute), ClockText.FromMinutes(Clock.BreakMinuteOfDay)));
            var dayNote = CheckDayTurn();
            var passNote = string.Format(StringConstants.TEXT_TIME_PASS_NOTE, minutes / 60, minutes % 60);
            SetText("village-fade-note", dayNote.Length > 0 ? passNote + "\n\n" + dayNote : passNote);

            foreach (var party in FindObjectsByType<NpcParty>(FindObjectsInactive.Exclude))
            {
                party.Scatter();
            }

            foreach (var ambient in FindObjectsByType<NpcAmbient>(FindObjectsInactive.Exclude))
            {
                ambient.Shuffle();
            }

            yield return new WaitForSeconds(Constants.TIME_PASS_HOLD);
            yield return FadeTo(fade, 0f);

            SetText("village-fade-time", string.Empty);
            SetText("village-fade-note", string.Empty);
            Approaching = false;
        }

        private VisualElement EnsureFade()
        {
            var fade = Root.Q<VisualElement>("village-fade");

            if (fade != null)
            {
                return fade;
            }

            fade = new VisualElement();
            fade.name = "village-fade";
            fade.AddToClassList("village-fade");
            fade.pickingMode = PickingMode.Position;
            fade.style.opacity = 0f;
            fade.style.display = DisplayStyle.None;

            var time = new Label();
            time.name = "village-fade-time";
            time.AddToClassList("village-fade-time");
            var note = new Label();
            note.name = "village-fade-note";
            note.AddToClassList("village-fade-note");
            fade.Add(time);
            fade.Add(note);
            Root.Add(fade);
            return fade;
        }

        private static System.Collections.IEnumerator FadeTo(VisualElement fade, float target)
        {
            fade.style.display = DisplayStyle.Flex;
            var from = fade.resolvedStyle.opacity;

            for (var t = 0f; t < Constants.TIME_PASS_FADE; t += Time.deltaTime)
            {
                fade.style.opacity = Mathf.Lerp(from, target, t / Constants.TIME_PASS_FADE);
                yield return null;
            }

            fade.style.opacity = target;

            if (target <= 0f)
            {
                fade.style.display = DisplayStyle.None;
            }
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
            // 며칠째인지 맨 앞에 둔다. 기한·시찰·원정이 모두 날짜로 움직인다(2026-10-17 사용자 요청).
            SetText("village-visits",
                ClockText.SeasonDay(Today()) + " · "
                + ClockText.FromSeconds((hour * 60 + minute) * 60 + (Clock == null ? 0 : Clock.BreakSecond), true)
                + " · " + string.Format(StringConstants.TEXT_VILLAGE_NEXT_SESSION, left / 60, left % 60)
                + InspectionTag());

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
                GoToMarket();
                return;
            }

            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            if (HasTime() == false)
            {
                Say(spot, "다음 장이 코앞입니다. 지금 가면 개장을 놓칩니다.", false);
                return;
            }

            var rows = NextSlotRows();

            if (rows.Count == 0)
            {
                SpendTime(spot);
                GatherSession.Record(spot.Price, 0f, null);
                Say(spot, "아직 돌고 있는 이야기가 없습니다.", false, null, ScoutBand(spot));
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
            Say(spot, body, note != null, HeldNote(pick.StockId), ScoutBand(spot));
        }

        /// <summary>
        /// 그 자리에서 모험단 하나에 대해 알게 된다(도감). 갓 도착한 단이 먼저다 — 상장 전 조사.
        /// 새로 알았으면 대화에 붙일 한 줄, 없으면 null.
        /// </summary>
        private static string ScoutBand(VillageSpot spot)
        {
            CodexSource source;

            switch (spot.InfoSource)
            {
                case InfoSource.Source_GuildRecord:
                {
                    source = CodexSource.Codex_Guild;
                    break;
                }
                case InfoSource.Source_SmithOrder:
                {
                    source = CodexSource.Codex_Smith;
                    break;
                }
                case InfoSource.Source_TavernTalk:
                {
                    source = CodexSource.Codex_Tavern;
                    break;
                }
                default:
                {
                    return null;
                }
            }

            var sim = BreakClock.Simulation;
            var id = BandCodex.PickUnknown(sim, source);
            var fact = id == null ? null : BandCodex.Learn(id, source);

            if (fact == null)
            {
                return null;
            }

            return string.Format(StringConstants.TEXT_CODEX_LEARNED,
                string.Format(StringConstants.TEXT_CODEX_LEARNED_LINE, sim.NameOf(id), fact));
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
        private void Say(VillageSpot spot, string text, bool secret, string heldNote = null, string scouted = null)
        {
            var line = OwnerLine(spot, text);
            line.IsSecret = secret;
            // 가진 종목의 소문이면 이름 옆에 붉은 배지를 붙인다. 작은 글씨 안내로는 놓친다(2026-10-17 사용자 요청).
            line.Badge = heldNote;
            line.Note = BuildNote(spot, secret);

            var lines = new List<DialogueLine> { line };

            if (scouted != null)
            {
                lines.Add(OwnerLine(spot, scouted));
            }

            Dialogue.Play(lines, secret == true
                ? React(ActorMotion.Motion_Hop, ActorEmote.Emote_Exclaim, () => StartCoroutine(HandOver(spot)))
                : React(ActorMotion.Motion_Nod, ActorEmote.Emote_Dots, Back(spot)));
        }

        /// <summary>
        /// 정보를 산 뒤 대화창을 잠깐 닫고 필드에서 건네는 장면을 보인다. 대화창이 화면을 덮어
        /// 건네기 동작이 보이지 않았다(2026-10-17 사용자 보고). 자리의 주인이 건네고 주인공이 반응한 뒤 선택지로 돌아간다.
        /// </summary>
        private System.Collections.IEnumerator HandOver(VillageSpot spot)
        {
            Approaching = true;
            Dialogue.Close();
            StationNpc.GiveAt(spot.Name);

            if (PlayerActor != null)
            {
                PlayerActor.Face(Vector2.up);
            }

            yield return new WaitForSeconds(Constants.HAND_OVER_SECONDS);
            Approaching = false;
            Back(spot)();
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

        // 장소마다 이름표를 세운다. 무엇을 하는 곳인지 다가가기 전에 알 수 있게.
        private void AddSigns()
        {
            foreach (var spot in FindObjectsByType<VillageSpot>(FindObjectsSortMode.None))
            {
                WorldSigns.Add(spot.transform, spot.Name, SignOf(spot.InfoSource));
            }
        }

        private static string SignOf(InfoSource source)
        {
            switch (source)
            {
                case InfoSource.Source_GuildRecord:
                {
                    return StringConstants.TEXT_SIGN_GUILD;
                }
                case InfoSource.Source_SmithOrder:
                {
                    return StringConstants.TEXT_SIGN_SMITH;
                }
                case InfoSource.Source_PrivateLetter:
                {
                    return StringConstants.TEXT_SIGN_MANOR;
                }
                case InfoSource.Source_MarketBoard:
                {
                    return StringConstants.TEXT_SIGN_BOARD;
                }
                default:
                {
                    return StringConstants.TEXT_SIGN_TAVERN;
                }
            }
        }

        // ── 장부 맞추기 (시간구조 설계안 §7 — 첫 추리형 정보 모으기) ───────
        // 의뢰 기록과 지급 기록을 맞춰 어긋난 단을 찾고, 그것이 무슨 뜻인지 짐작한다. 맞히면 그 단의 대장간 사정을 안다.
        // 돈 대신 시간과 기록 담당의 의심을 건다. 하루에 한 번.

        private UI_LedgerPuzzle Ledger;
        private LedgerPuzzle Puzzle;
        private VillageSpot LedgerSpot;
        private int LedgerDay;

        private bool LedgerOpen
        {
            get
            {
                return Ledger != null && Ledger.IsOpen == true;
            }
        }

        private DialogueChoice LedgerChoice(VillageSpot spot)
        {
            var choice = new DialogueChoice(StringConstants.TEXT_LEDGER_CHOICE, () => StartLedger(spot));
            var left = Clock == null ? 0 : Clock.BreakMinutesLeft;

            if (BreakClock.Simulation == null)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_LEDGER_NONE;
            }
            else if (LedgerDay == Today())
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_LEDGER_DONE_TODAY;
            }
            else if (left < Constants.LEDGER_MINUTES)
            {
                choice.IsEnabled = false;
                choice.Note = StringConstants.TEXT_GATHER_NO_TIME;
            }
            else
            {
                choice.Note = string.Format(StringConstants.TEXT_LEDGER_CHOICE_NOTE, Constants.LEDGER_MINUTES);
            }

            return choice;
        }

        private void StartLedger(VillageSpot spot)
        {
            Puzzle = LedgerPuzzle.Create(BreakClock.Simulation, new System.Random(Random.Range(int.MinValue, int.MaxValue)),
                Today());

            if (Puzzle == null)
            {
                Dialogue.Play(new List<DialogueLine> { OwnerLine(spot, StringConstants.TEXT_LEDGER_NONE) }, null);
                return;
            }

            Dialogue.Close();
            LedgerSpot = spot;
            LedgerDay = Today();
            Clock.SpendBreakMinutes(Constants.LEDGER_MINUTES);

            if (Ledger == null)
            {
                Ledger = new UI_LedgerPuzzle(Root);
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

            Clock.SpendBreakMinutes(Constants.LEDGER_WRONG_MINUTES);
            Ledger.MarkWrong(index, Puzzle.Suspicion);
            GameAudio.Play(SoundEffect.Sound_Close);

            if (Puzzle.IsKickedOut == true)
            {
                Ledger.Close();
                Dialogue.SetBackground(BackgroundOf(LedgerSpot.InfoSource));
                Dialogue.Play(new List<DialogueLine> { OwnerLine(LedgerSpot, StringConstants.TEXT_LEDGER_KICKED) },
                    React(ActorMotion.Motion_Shake, ActorEmote.Emote_Exclaim, null));
            }
        }

        // 찾았다. 무슨 뜻인지 고른다. 맞는 답은 섞어서 보인다.
        private void DeduceLedger()
        {
            var name = StockName(Puzzle.TargetId);
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

            Dialogue.SetBackground(BackgroundOf(LedgerSpot.InfoSource));
            Dialogue.Play(new List<DialogueLine>
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
                Dialogue.Play(new List<DialogueLine> { miss }, React(ActorMotion.Motion_Shake, ActorEmote.Emote_Dots, null));
                return;
            }

            var id = Puzzle.TargetId;
            var fact = BandCodex.Learn(id, CodexSource.Codex_Smith);

            if (fact != null)
            {
                GatherSession.Record(0f, 0f, new GatheredNote(id, fact, InfoSource.Source_SmithOrder));
            }

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_LEDGER_TAG,
                string.Format(StringConstants.TEXT_LEDGER_RIGHT, StockName(id), fact ?? string.Empty));
            Dialogue.Play(new List<DialogueLine> { line }, React(ActorMotion.Motion_Nod, ActorEmote.Emote_Exclaim, null));
        }

        // ── 출발과 계절 계획 (시간구조 설계안 §2·§3) ───────
        // 계절의 마지막 밤이 지나면 용사단이 떠나고, 주인공은 휴장하는 석 달에 할 일을 고른다.
        // 고른 대로 몽타주가 흐르고, 친한 용사가 있으면 원정지에서 편지가 온다. 끝나면 새 계절 첫날 아침이다.

        private bool DepartureRunning;
        private readonly List<SeasonTask> PlanMonths = new List<SeasonTask>();

        /// <summary>
        /// 새 계절 첫날이 됐는데 지난 계절의 출발을 아직 치르지 않았으면 띄운다.
        /// 4일째 오후장을 거래소에서 마쳤든 영지에서 건너뛰었든, 자정이 지나면 같은 자리에서 시작한다.
        /// </summary>
        private void PlayDepartureIfDue()
        {
            if (DepartureRunning == true || Clock == null || Clock.IsBreak == false || Approaching == true
                || Dialogue == null || Dialogue.IsOpen == true || ClockOpen == true
                || (Tutorial != null && Tutorial.IsOpen == true))
            {
                return;
            }

            var today = Today();

            if (today <= 1 || MarketClock.DayInSeason(today) != 1
                || SeasonPlan.HasDeparted(MarketClock.SeasonOf(today) - 1) == true)
            {
                return;
            }

            DepartureRunning = true;
            PlanMonths.Clear();
            Dialogue.SetBackground(BackgroundOf(InfoSource.Source_TavernTalk));

            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_DEPART_TAG, StringConstants.TEXT_DEPART_OPEN),
                HeroLine(HeroRoster.Favorite(), StringConstants.TEXT_DEPART_FAREWELL, PortraitEmotion.Emotion_Happy),
                new DialogueLine(string.Empty, StringConstants.TEXT_DEPART_TAG, StringConstants.TEXT_DEPART_PLAN)
            };

            Dialogue.Play(lines, AskPlanMonth);
        }

        // 한 달씩 묻는다. 세 달을 다 고르면 몽타주로.
        private void AskPlanMonth()
        {
            var month = PlanMonths.Count;

            if (month >= StringConstants.TEXT_PLAN_MONTH_ASK.Length)
            {
                PlayPlanMontage();
                return;
            }

            var choices = new List<DialogueChoice>();

            for (var i = 0; i < StringConstants.TEXT_PLAN_TASK.Length; i++)
            {
                var task = (SeasonTask)i;
                var choice = new DialogueChoice(StringConstants.TEXT_PLAN_TASK[i], () =>
                {
                    PlanMonths.Add(task);
                    AskPlanMonth();
                });
                choice.Note = string.Format(StringConstants.TEXT_PLAN_TASK_NOTE[i], Constants.PLAN_INN_EARN);
                choices.Add(choice);
            }

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_PLAN_TAG,
                StringConstants.TEXT_PLAN_MONTH_ASK[month]);
            line.Choices = choices;
            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        private void PlayPlanMontage()
        {
            var texts = SeasonPlan.Apply(PlanMonths, HeroRoster.Favorite());
            var lines = new List<DialogueLine>();

            for (var i = 0; i < texts.Count; i++)
            {
                lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_PLAN_TAG, texts[i]));
            }

            Dialogue.Play(lines, PlayExpeditionLetter);
        }

        /// <summary>
        /// 원정지에서 온 편지(설계안 §3). 가장 친한 용사가 충분히 가까우면 온다.
        /// 다음 계절에 드러날 방패 사정을 미리 흘리고, 원격 익명 의뢰로 먼저 막을 수 있게 한다 — 대신 더 비싸다.
        /// </summary>
        private void PlayExpeditionLetter()
        {
            var favorite = HeroRoster.Favorite();

            if (favorite == null || favorite.Affinity < Constants.HERO_LETTER_AFFINITY)
            {
                FinishDeparture();
                return;
            }

            var lines = new List<DialogueLine>
            {
                new DialogueLine(string.Empty, StringConstants.TEXT_LETTER_TAG,
                    string.Format(StringConstants.TEXT_LETTER_BODY, favorite.Name))
            };

            if (PatronageLedger.IsHidden(StringConstants.NEED_SHIELD_ID) == false)
            {
                Dialogue.Play(lines, FinishDeparture);
                return;
            }

            var cost = Constants.REQUEST_COST_GEAR * Constants.REMOTE_REQUEST_MARKUP;
            var money = GatherSession.Available;
            var send = new DialogueChoice(StringConstants.TEXT_LETTER_REMOTE, () => SendRemoteShield(cost));
            send.IsEnabled = money >= cost;
            send.Note = string.Format(StringConstants.TEXT_LETTER_REMOTE_NOTE, cost, money);

            var shield = new DialogueLine(string.Empty, StringConstants.TEXT_LETTER_TAG, StringConstants.TEXT_LETTER_SHIELD);
            shield.Choices = new List<DialogueChoice>
            {
                send,
                new DialogueChoice(StringConstants.TEXT_LETTER_KEEP, FinishDeparture)
            };
            lines.Add(shield);
            Dialogue.Play(lines, null);
        }

        private void SendRemoteShield(float cost)
        {
            GatherSession.Record(cost, 0f, null);
            PatronageLedger.RevealEarly(StringConstants.NEED_SHIELD_ID);
            PatronageLedger.Post(RequestPurpose.Purpose_Gear, Today());

            var line = new DialogueLine(string.Empty, StringConstants.TEXT_LETTER_TAG, StringConstants.TEXT_LETTER_SENT);
            Dialogue.Play(new List<DialogueLine> { line }, FinishDeparture);
        }

        // 석 달이 흐르고 새 계절 첫날 아침이 된다. 귀환은 여관에서 맞는다.
        private void FinishDeparture()
        {
            SeasonPlan.MarkDeparted(MarketClock.SeasonOf(Today()) - 1);
            DepartureRunning = false;

            var minutes = WakeMinute() - Clock.BreakMinuteOfDay;

            if (minutes > 0)
            {
                StartCoroutine(PassTime(minutes, StringConstants.TEXT_DEPART_FADE));
            }
        }

        // ── 잠자리 (2026-10-17 다시 넣음) ───────
        // 오후장이 끝난 밤에만 잔다. 그날을 정리하고 주인공이 혼잣말을 한 뒤 아침 장 직전으로 넘어간다.
        // 날이 바뀌는 처리(기한·붕괴)는 시간 넘기기와 같은 CheckDayTurn이 맡는다 — 두 번 세지 않는다.

        /// <summary>
        /// 잘 수 있는 밤인가. 자정이 지났고 아직 깰 시각 전이면 된다.
        /// 오후장을 거래소에서 마쳤든 영지에서 건너뛰었든 같다(2026-10-17 — 건너뛰면 잠자리가 안 떴다는 사용자 보고).
        /// </summary>
        private bool CanSleep()
        {
            if (Clock == null || Clock.IsBreak == false)
            {
                return false;
            }

            var minute = Clock.BreakMinuteOfDay;
            return minute >= Constants.SLEEP_FROM_HOUR * 60 && WakeMinute() > minute;
        }

        /// <summary>
        /// 잠에서 깨는 시각. 다음 장 직전이되 늦어도 그날 아침 장 시각 직전이다.
        /// 계절 첫날은 장이 저녁에야 열리지만 낮에 귀환을 맞아야 한다 — 다음 장까지 자면 그 낮을 통째로 건너뛴다.
        /// </summary>
        private int WakeMinute()
        {
            var dayStart = Clock.BreakMinuteOfDay / (24 * 60) * (24 * 60);
            var dawn = dayStart + Constants.MARKET_MORNING_START_HOUR * 60 - Constants.BREAK_SKIP_MARGIN_MINUTES;
            return Mathf.Min(Clock.LatestBreakMinute, dawn);
        }

        private DialogueChoice SleepChoice()
        {
            var choice = new DialogueChoice(StringConstants.TEXT_SLEEP_CHOICE, Sleep);
            choice.IsEnabled = CanSleep();
            choice.Note = choice.IsEnabled == true
                ? string.Format(StringConstants.TEXT_SLEEP_CHOICE_NOTE, ClockText.FromMinutes(WakeMinute()))
                : StringConstants.TEXT_SLEEP_LOCKED;
            return choice;
        }

        private void Sleep()
        {
            if (CanSleep() == false)
            {
                return;
            }

            Dialogue.SetBackground(BackgroundOf(InfoSource.Source_TavernTalk));
            // 오후장은 자정에 끝나므로 영지 날짜는 이미 다음 날이다. 「며칠째 밤」은 장의 날짜로 적는다.
            var today = Today();
            var tag = string.Format(StringConstants.TEXT_SLEEP_TAG, ClockText.SeasonDay(Clock.Day));
            var lines = new List<DialogueLine>
            {
                new DialogueLine(StringConstants.TEXT_SLEEP_SPEAKER, tag, SleepRecap(today)),
                new DialogueLine(string.Empty, tag, PickMonologue(today))
            };

            Dialogue.Play(lines, () =>
            {
                var minutes = WakeMinute() - Clock.BreakMinuteOfDay;
                StartCoroutine(PassTime(minutes, StringConstants.TEXT_SLEEP_FADE));
            });
        }

        // 그날의 정리. 기한은 보이고 금액은 숨긴다(상장흐름 설계안 §1-5 완화책).
        private string SleepRecap(int today)
        {
            var sim = BreakClock.Simulation;
            var cash = sim == null ? GatherSession.Available : sim.Wallet.Cash + GatherSession.NetChange;
            var held = sim == null ? 0f : sim.Wallet.HoldingsValue(sim.StockLookup);
            var text = string.Format(StringConstants.TEXT_SLEEP_RECAP, cash, held, GatherSession.Gathered.Count);
            var deadline = PatronageLedger.NearestDeadline(today);

            if (deadline > 0)
            {
                text = text + "\n" + string.Format(StringConstants.TEXT_SLEEP_DEADLINE, deadline);
            }

            if (MarketClock.IsInspectionDay(today) == true)
            {
                text = text + "\n" + StringConstants.TEXT_SLEEP_INSPECTION;
            }

            return text;
        }

        /// <summary>
        /// 이야기의 고비를 지난 뒤 한 번씩 나오는 혼잣말을 먼저, 없으면 평소의 밤 혼잣말을 고른다.
        /// 열쇠는 상수 이름이다(저장에 남는다).
        /// </summary>
        private static string PickMonologue(int today)
        {
            var sim = BreakClock.Simulation;
            var traded = sim != null && sim.Wallet.AllHoldings.Count > 0;
            var heardNeed = false;

            foreach (var need in PatronageLedger.All)
            {
                if (need.Heard == true && need.State == NeedState.State_Open)
                {
                    heardNeed = true;
                }
            }

            var healed = PatronageLedger.IsMet(StringConstants.NEED_HEALER_ID);
            var candidates = new List<KeyValuePair<string, bool>>
            {
                Monologue(nameof(StringConstants.TEXT_SLEEP_RETURN_SAFE), HeroRoster.ReturnDone == true && healed == true),
                Monologue(nameof(StringConstants.TEXT_SLEEP_RETURN_HURT), HeroRoster.ReturnDone == true && healed == false),
                Monologue(nameof(StringConstants.TEXT_SLEEP_SUSPECT),
                    HeroRoster.ReturnDone == true && PatronageLedger.Clues >= Constants.HERO_SUSPECT_CLUES),
                Monologue(nameof(StringConstants.TEXT_SLEEP_DAY_OFF), HeroRoster.DayOffDone),
                Monologue(nameof(StringConstants.TEXT_SLEEP_FIRST_REQUEST), PatronageLedger.RequestsPosted > 0),
                Monologue(nameof(StringConstants.TEXT_SLEEP_HEARD_NEED), heardNeed),
                Monologue(nameof(StringConstants.TEXT_SLEEP_FIRST_TRADE), traded),
                Monologue(nameof(StringConstants.TEXT_SLEEP_CELLAR), StoryProgress.CellarFound)
            };

            for (var i = 0; i < candidates.Count; i++)
            {
                var key = candidates[i].Key;

                if (candidates[i].Value == true && StoryProgress.SleepSeen.Contains(key) == false)
                {
                    StoryProgress.SleepSeen.Add(key);
                    return MonologueText(key);
                }
            }

            switch (today % 3)
            {
                case 0:
                {
                    return StringConstants.TEXT_SLEEP_PLAIN_1;
                }
                case 1:
                {
                    return StringConstants.TEXT_SLEEP_PLAIN_2;
                }
                default:
                {
                    return StringConstants.TEXT_SLEEP_PLAIN_3;
                }
            }
        }

        private static KeyValuePair<string, bool> Monologue(string key, bool ready)
        {
            return new KeyValuePair<string, bool>(key, ready);
        }

        // 열쇠(상수 이름)로 지금 문구를 읽는다. 문구 표가 덮어쓴 값을 써야 한다.
        private static string MonologueText(string key)
        {
            var field = typeof(StringConstants).GetField(key);
            return field == null ? string.Empty : (string)field.GetValue(null);
        }

        /// <summary>
        /// 자리의 주인이 하는 말. 이름과 얼굴은 그 자리의 소식을 전하는 주민이다 — 지하 정보 타임에도 같은 사람이 나온다.
        /// </summary>
        private DialogueLine OwnerLine(VillageSpot spot, string text)
        {
            var line = new DialogueLine(InfoPeople.Speaker(spot.InfoSource), spot.Name, text);
            line.Portrait = InfoPeople.Face(Data, spot.InfoSource, PortraitEmotion.Emotion_Neutral);
            return line;
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

        /// <summary>
        /// 단 이름. 시장이 아직 없거나(지하를 찾기 전) 시장이 모르는 아이디면 종목 표에서 찾는다.
        /// 수첩에 STOCK_ROOKIE 같은 열쇠가 그대로 떴다(2026-10-17 사용자 보고).
        /// </summary>
        private string NameFor(MarketSimulation sim, string stockId)
        {
            var name = sim == null ? stockId : sim.NameOf(stockId);
            return name == stockId ? StockName(stockId) : name;
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
