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

        private VisualElement root;
        private UI_Dialogue dialogue;

        // 정보 모으기 놀이(2026-10-19 정리 — 화면에서 떼어 냄).
        private VillageMiniGameHandler MiniGames;

        // 용사단과 길드 창구(2026-10-19 정리 — 화면에서 떼어 냄).
        private VillageHeroHandler Heroes;

        // 잠과 계절 출발(2026-10-19 정리 — 화면에서 떼어 냄).
        private VillageNightHandler Night;

        // 돈 내고 묻기(2026-10-19 정리 — 화면에서 떼어 냄).
        private VillageGatherHandler Gathering;

        // 첫 후원 안내 — 용사단 테이블 → 여관 일 → 길드 첫 의뢰를 데려가서 시킨다(2026-10-19 사장님).
        private VillageSponsorGuideHandler Sponsor;

        // 메뉴와 턴 넘기기, 할 일(2026-10-19 정리 — 화면에서 떼어 냄).
        private VillageMenuHandler Menu;

        // 제목 카드·암전·데려가기(2026-10-19 정리 — 화면에서 떼어 냄). 프롤로그·후원 안내·밤·메뉴가 함께 쓴다.
        internal VillageStagingHandler Staging { get; private set; }
        private VillageQuestHandler Quests;

        internal GameDataSet DataSet
        {
            get
            {
                return Data;
            }
        }

        internal VisualElement Root
        {
            get
            {
                return root;
            }
        }

        internal UI_Dialogue Dialogue
        {
            get
            {
                return dialogue;
            }
        }
        private VillageSpot Here;
        private VillageNpc NearNpc;
        private UI_Tutorial Tutorial;

        // 지하를 찾기 전의 마을 생활(2026-10-18 온보딩). 순찰·잠·여덟째 날을 맡는다.
        private VillagePrologueHandler Prologue;

        // 영지 미니맵(2026-10-18). 장소·일하는 사람이 다 서고 나서 한 번 만든다.
        private UI_VillageMinimap Minimap;

        // 지금 선택지를 띄운 자리. 용사 테이블처럼 자리를 넘겨받지 않는 곳에서 돌아갈 곳이다.
        private VillageSpot currentSpot;

        internal VillageSpot CurrentSpot
        {
            get
            {
                return currentSpot;
            }
            set
            {
                currentSpot = value;
            }
        }

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

        private Transform Player;

        /// <summary>영지에서도 시각이 흐른다. 거래 화면의 시계와 같은 것을 쓴다.</summary>
        internal MarketClock Clock
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
            SaveSystem.Saved += OnSaved;
            VillageSpotEvents.Entered += OnEnter;
            VillageSpotEvents.Exited += OnExit;
        }

        private void OnDestroy()
        {
            if (Minimap != null)
            {
                Minimap.Dispose();
            }
        }

        private void OnDisable()
        {
            SaveSystem.Saved -= OnSaved;
            VillageSpotEvents.Entered -= OnEnter;
            VillageSpotEvents.Exited -= OnExit;
        }

        // 이름표·말풍선은 카메라가 따라간 뒤에 자리를 잡는다. Update에서 잡으면 카메라보다 한발 앞서
        // 움직일 때마다 튀었다(2026-10-17 사용자 보고). 이 컴포넌트는 늦게 돌도록 실행 순서를 뒤로 뺐다.
        private void LateUpdate()
        {
            var talking = Dialogue != null && Dialogue.IsOpen == true;
            WorldBubbles.Tick(talking);
            WorldSigns.Tick(talking || NotebookOpen == true || PuzzleOpen == true || MiniGameOpen == true,
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

        // ── 저장 표시 ─────────

        private Label SaveBadge;
        private float SaveBadgeShownAt = -1000f;

        private void OnSaved()
        {
            if (Root == null || Time.unscaledTime - SaveBadgeShownAt < Constants.SAVE_BADGE_COOLDOWN)
            {
                return;
            }

            if (SaveBadge == null)
            {
                SaveBadge = new Label(StringConstants.TEXT_SAVED);
                SaveBadge.AddToClassList("village-visits");
                SaveBadge.pickingMode = PickingMode.Ignore;
                SaveBadge.style.top = StyleKeyword.Auto;
                SaveBadge.style.right = StyleKeyword.Auto;
                SaveBadge.style.left = 32f;
                SaveBadge.style.bottom = 28f;
                SaveBadge.style.fontSize = 15f;
                Root.Add(SaveBadge);
            }

            SaveBadgeShownAt = Time.unscaledTime;
            SaveBadge.BringToFront();
            SaveBadge.style.opacity = 1f;
            SaveBadge.style.display = DisplayStyle.Flex;
        }

        private void TickSaveBadge()
        {
            if (SaveBadge == null || SaveBadge.style.display == DisplayStyle.None)
            {
                return;
            }

            var age = Time.unscaledTime - SaveBadgeShownAt;
            var fade = Constants.SAVE_BADGE_SECONDS * 0.4f;

            if (age >= Constants.SAVE_BADGE_SECONDS)
            {
                SaveBadge.style.display = DisplayStyle.None;
                return;
            }

            SaveBadge.style.opacity = Mathf.Clamp01((Constants.SAVE_BADGE_SECONDS - age) / fade);
        }

        /// <summary>지하거래소로 내려간다. 장중은 저장하지 않으므로 내려가기 직전을 남긴다.</summary>
        internal void GoToMarket()
        {
            SaveSystem.Save();
            SceneManager.LoadScene(SceneRoutes.UNDERWORLD);
        }

        private void Update()
        {
            if (Root == null)
            {
                var document = GetComponent<UIDocument>();
                root = document == null ? null : document.rootVisualElement;

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
                dialogue = new UI_Dialogue(Root, DialogueTree);
                AddSigns();
            }

            if (Staging == null)
            {
                Staging = new VillageStagingHandler(this);
            }

            if (Prologue == null)
            {
                Prologue = new VillagePrologueHandler(this);
            }

            if (MiniGames == null)
            {
                MiniGames = new VillageMiniGameHandler(this);
            }

            if (Heroes == null)
            {
                Heroes = new VillageHeroHandler(this);
            }

            if (Night == null)
            {
                Night = new VillageNightHandler(this);
            }

            if (Gathering == null)
            {
                Gathering = new VillageGatherHandler(this);
            }

            if (Sponsor == null)
            {
                Sponsor = new VillageSponsorGuideHandler(this, Heroes);
            }

            if (Menu == null)
            {
                Menu = new VillageMenuHandler(this, Prologue, Night);
            }

            if (Quests == null)
            {
                Quests = new VillageQuestHandler(this, Prologue);
            }

            Menu.Bind();
            EnsureClock();

            // 영지는 시간 대신 행동력(2026-10-19 사장님). 시계는 휴장 동안 흐르지 않고, 휴장이 바뀌면 행동력을 채운다.
            // 행동력 안내(2일째 아침)를 보기 전에는 0 — 첫날 새벽은 용사단 장면을 보고 자는 밤이다.
            if (Clock != null && Clock.IsBreak == true && StoryProgress.CellarFound == true)
            {
                var normalized = ActionPoints.Normalize(Clock.BreakMinuteOfDay);

                if (normalized != Clock.BreakMinuteOfDay)
                {
                    Clock.SetBreakMinute(normalized);
                }

                ActionPoints.FillIfNew(Clock.Day, Clock.BreakMinuteOfDay, StoryProgress.ActionTutorialDone == false);
            }

            AutoSave();
            TickSaveBadge();

            NearNpc = FindNearNpc();
            RefreshPrompt();
            TickMinimap(Keyboard.current);

            // 지하에서 올라왔으면 여관 문 앞에 선다(창고는 여관 지하다).
            if (SceneRoutes.ArrivedFromCellar == true && Player != null)
            {
                SceneRoutes.ArrivedFromCellar = false;
                var inn = FindSpot(InfoSource.Source_TavernTalk);

                if (inn != null)
                {
                    Staging.WarpTo(inn);
                }
            }

            StartVillageTutorialOnce();

            // 게임 안 웹툰은 스페이스로도 넘긴다.
            if (Webtoon != null && Webtoon.IsOpen == true && Keyboard.current != null
                && Keyboard.current.spaceKey.wasPressedThisFrame == true)
            {
                Webtoon.Next();
            }

            Staging.Tick(Time.unscaledDeltaTime);

            Prologue.Tick();
            MiniGames.TickHelp(Rules != null && Rules.IsOpen == true);
            Quests.RefreshObjective();

            NpcAmbient.Player = Player;

            PlayPendingReaction();
            Night.PlayDepartureIfDue();
            PlayQuestIntroIfDue();

            // 귀환일 아침의 재회, 첫 후원 안내 — 영지가 한가할 때 그 자리로 데려간다.
            if (Prologue.IsActive == false && Prologue.IsDirecting == false && HasPending == false
                && Night.IsDepartureRunning == false)
            {
                Heroes.GuideToReturnIfDue();
                Sponsor.Tick();
            }

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
            VillageCameraZoom.Blocked = (Dialogue != null && Dialogue.IsOpen == true)
                || Menu.IsOpen == true || Menu.IsSettingsOpen == true || NotebookOpen == true || PuzzleOpen == true || MiniGameOpen == true
                || Approaching == true;

            // 연출 중(돌아보기·건네기·시간이 흐르는 동안)과 대화·시계·수첩이 떠 있는 동안에는 주인공이 걷지 않는다
            // (2026-10-17 사용자 보고).
            LockPlayer(Approaching == true || (Dialogue != null && Dialogue.IsOpen == true)
                || NotebookOpen == true || PuzzleOpen == true || MiniGameOpen == true || Menu.IsSettingsOpen == true
                || Prologue.LocksPlayer == true || IsStaging == true);

            var keyboard = Keyboard.current;

            // Esc로 메뉴(처음 화면으로 · 게임 종료가 들어 있다). 대화·연출 중이면 메뉴 쪽이 막는다.
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame == true)
            {
                Menu.OnEscape();
            }

            if (Dialogue != null && Dialogue.IsChoosing == true)
            {
                PickByNumber(keyboard);
                return;
            }

            if ((Here == null && NearNpc == null) || Dialogue == null || Dialogue.IsOpen == true
                || Approaching == true || NotebookOpen == true || PuzzleOpen == true
                || MiniGameOpen == true || IsStaging == true)
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

        private CharacterActor playerActor;

        // 매 프레임 쓰인다 — 한 번 찾아 둔다.
        internal CharacterActor PlayerActor
        {
            get
            {
                if (Player == null)
                {
                    return null;
                }

                if (playerActor == null)
                {
                    playerActor = Player.GetComponent<CharacterActor>();
                }

                return playerActor;
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

            // 프롤로그가 맡는 동안(순찰 순서·잠·여덟째 날)은 평소 선택지를 띄우지 않는다.
            if (Prologue.HandleSpot(spot) == true)
            {
                yield break;
            }

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

            // 모험단에게 직접 말을 걸면 그 단에 대해 하나 알게 된다 — 행동력 1(2026-10-19). 수첩(도감)에 적는다.
            // 지하를 찾기 전에는 그냥 오가는 손님이다 — 적지 않는다(온보딩 설계안 §5).
            // 반응은 필드 몸짓으로 보인다(대화창이 없는 곳이라 몸짓이 유일한 감정 표현이다).
            if (npc.Party != null && StoryProgress.CellarFound == true && me != null)
            {
                ReactToAdventurer(npc, me, them);
            }

            Approaching = false;
        }

        private void ReactToAdventurer(VillageNpc npc, CharacterActor me, CharacterActor them)
        {
            var stock = npc.Party.Stock;

            if (BandCodex.IsKnown(stock, CodexSource.Codex_Talk) == true)
            {
                me.Say(StringConstants.TEXT_NPC_ALREADY_KNOWN);
                return;
            }

            if (ActionPoints.Has(Constants.AP_TALK) == false)
            {
                me.Say(StringConstants.TEXT_NPC_NO_AP);
                me.Emote(ActorEmote.Emote_Dots);
                return;
            }

            var learned = BandCodex.Learn(stock, CodexSource.Codex_Talk);

            if (learned == null)
            {
                // 도감에 오르지 않는 단 — 알려 줄 게 없다.
                if (them != null)
                {
                    them.Play(ActorMotion.Motion_Shake);
                    them.Emote(ActorEmote.Emote_Dots);
                }

                me.Say(StringConstants.TEXT_NPC_SHRUG);
                return;
            }

            ActionPoints.Spend(Constants.AP_TALK);

            if (them != null)
            {
                them.Play(ActorMotion.Motion_Nod);
            }

            me.Say(string.Format(StringConstants.TEXT_CODEX_LEARNED, npc.Group));
            me.Emote(ActorEmote.Emote_Exclaim);
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

        /// <summary>대화가 닫혔으면 적어 둔 반응을 필드에서 한다. 매 프레임 부른다.</summary>
        /// <summary>대화가 닫힌 뒤 주인공이 할 몸짓을 예약한다.</summary>
        internal void QueueReaction(ActorMotion motion, ActorEmote emote)
        {
            HasPending = true;
            PendingMotion = motion;
            PendingEmote = emote;
        }

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
        /// 그 자리에서 고를 것이 있는가.
        /// 들어가는 문, 값도 일도 창고도 창구도 없는 곳, 시간이 없는데 창고도 없는 곳은 고를 것이 없다.
        /// 창고가 있는 여관은 시간이 없어도 내려갈 수 있어야 한다. 다음 장이 거기서 열린다.
        /// </summary>
        private bool OffersChoices(VillageSpot spot)
        {
            return spot.Kind != VillageActivity.Activity_Enter
                && (spot.Price > 0f || spot.HasWork == true || spot.HasCellar == true || spot.HasRequestDesk == true);
        }

        /// <summary>
        /// 한 가지 일을 마친 뒤 그 자리의 선택지로 돌아간다(2026-10-17 사용자 요청).
        /// 대화가 끝날 때마다 마을로 튕겨 나가면 같은 자리에서 여러 일을 할 때 E를 다시 눌러야 한다.
        /// 고를 것이 없어진 자리(시간이 다 됨 등)면 그냥 닫는다.
        /// </summary>
        internal System.Action Back(VillageSpot spot)
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
        internal void Ask(VillageSpot spot, bool again)
        {
            currentSpot = spot;

            if (again == false)
            {
                GameAudio.Play(SoundEffect.Sound_Door);
            }

            if (OffersChoices(spot) == false)
            {
                Gathering.Gather(spot);
                return;
            }

            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            // 용사단이 원정에서 돌아온 날, 여관 문을 열면 재회 장면이 먼저다.
            if (spot.HasCellar == true && HeroRoster.HasReturn(Today()) == true)
            {
                Heroes.PlayReturn(() => Ask(spot));
                return;
            }

            var choices = new List<DialogueChoice>();
            var money = GatherSession.Available;

            // 돈 내고 묻기는 지하를 안 뒤에 연다 — 그 전에는 물을 이유가 없다(온보딩 설계안 §5).
            if (spot.Price > 0f && StoryProgress.CellarFound == true)
            {
                var pay = new DialogueChoice(string.Format(StringConstants.TEXT_GATHER_PAY, spot.Price),
                    () => Gathering.Gather(spot));
                pay.IsEnabled = money >= spot.Price && Gathering.HasTime() == true;

                if (Gathering.HasTime() == false)
                {
                    pay.Note = StringConstants.TEXT_GATHER_NO_TIME;
                }
                else if (money < spot.Price)
                {
                    pay.Note = string.Format(StringConstants.TEXT_GATHER_NO_MONEY, money);
                }
                else
                {
                    pay.Note = string.Format(StringConstants.TEXT_GATHER_PAY_NOTE, Constants.AP_GATHER, money);
                }
                choices.Add(pay);
            }

            if (spot.HasWork == true)
            {
                var quest = spot.Work;
                var work = new DialogueChoice(quest.Title, () => DoWork(spot));
                work.IsEnabled = ActionPoints.Has(Constants.AP_WORK);
                work.Note = work.IsEnabled == true
                    ? string.Format(StringConstants.TEXT_WORK_NOTE, Constants.AP_WORK, quest.Reward)
                    : string.Format(StringConstants.TEXT_WORK_NO_TIME, ActionPoints.Current);
                choices.Add(work);
            }

            if (spot.InfoSource == InfoSource.Source_GuildRecord)
            {
                choices.Add(MiniGames.LedgerChoice(spot));
                choices.Add(MiniGames.DeduceChoice(spot));
            }

            if (spot.InfoSource == InfoSource.Source_SmithOrder)
            {
                choices.Add(MiniGames.SmithOrderChoice(spot));
            }

            if (spot.HasRequestDesk == true)
            {
                choices.Add(Heroes.PostChoice(spot));
            }

            if (spot.HasCellar == true)
            {
                if (StoryProgress.CellarFound == true)
                {
                    choices.Add(MiniGames.EavesChoice());
                    choices.Add(MiniGames.DiceChoice(spot));
                }

                // 용사단 테이블은 그들을 알아본 뒤에 연다(온보딩 — 동기 전환 장면).
                if (StoryProgress.HeroesRecognized == true)
                {
                    choices.Add(Heroes.HeroTableChoice(spot));
                }

                choices.Add(Night.SleepChoice());
            }

            // 창고 안쪽을 아직 모른다. 웹툰 5~6화의 사건을 겪어야 길이 열린다.
            if (spot.HasCellar == true && StoryProgress.CellarFound == true)
            {
                // 웹툰 6화. 술통을 옮기러 내려간 창고 안쪽에 지하거래소가 있다.
                var cellar = new DialogueChoice(StringConstants.TEXT_CELLAR_GO, Menu.GoToSession);
                cellar.IsEnabled = Menu.MarketOpenNow();
                cellar.Note = cellar.IsEnabled == true ? Menu.SessionNote() : Menu.MarketClosedNote();
                choices.Add(cellar);
            }

            choices.Add(new DialogueChoice(StringConstants.TEXT_GATHER_LEAVE, null));

            var line = OwnerLine(spot,
                again == true ? StringConstants.TEXT_GATHER_ASK_AGAIN : StringConstants.TEXT_GATHER_ASK);
            line.Choices = choices;

            Dialogue.Play(new List<DialogueLine> { line }, null);
        }

        /// <summary>
        /// 지하거래소를 찾는 장면. 웹툰 5~6화를 플레이어가 직접 겪는다(2026-10-17 사용자 결정).
        /// 인트로 다음에 곧바로 주식 창이 열리면 뜬금없다. 마을에서 일하다 우연히 찾아야 한다.
        /// </summary>
        private void PlayDiscovery(VillageSpot spot)
        {
            StoryProgress.CellarFound = true;

            // 프롤로그 끝. 주방 일을 마치고 술통을 나르러 갈 무렵은 해 질 녘이다 — 첫 장(재개장) 직전으로 넘긴다.
            Prologue.FinishWithDiscovery(Clock);

            var owner = InfoPeople.Speaker(spot.InfoSource);
            var lines = new List<DialogueLine>
            {
                new DialogueLine(owner, StringConstants.TEXT_DISCOVER_TAG, StringConstants.TEXT_DISCOVER_OWNER),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_OVERHEAR),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_THINK),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_CELLAR),
                new DialogueLine(string.Empty, spot.Name, StringConstants.TEXT_DISCOVER_END)
            };

            // 다 보면 곧장 창고 안쪽으로 내려간다. 다른 볼일을 늘어놓으면 이 순간이 묻힌다(2026-10-18 온보딩).
            var last = lines[lines.Count - 1];
            last.Note = StringConstants.TEXT_DISCOVER_NOTE;
            last.Choices = new List<DialogueChoice> { new DialogueChoice(StringConstants.TEXT_CELLAR_GO, GoToMarket) };

            // 하루 일을 마칠 무렵으로 넘어간다는 것을 제목 카드로 보인다.
            Staging.ShowTitle(StringConstants.TEXT_PROLOGUE_CARD_DUSK, StringConstants.TEXT_PROLOGUE_CARD_DUSK_SUB, null,
                () => Dialogue.Play(lines, null));
        }

        internal DialogueLine HeroLine(HeroProfile hero, string text, PortraitEmotion emotion)
        {
            var line = new DialogueLine(hero.Name, hero.Role, text);
            line.Emotion = emotion;

            if (hero.FaceIndex < HeroFaces.Count && HeroFaces[hero.FaceIndex] != null)
            {
                line.Portrait = HeroFaces[hero.FaceIndex].Get(emotion);
            }

            return line;
        }

        internal int Today()
        {
            return Clock == null ? 1 : Clock.CalendarDay;
        }

        /// <summary>
        /// 영지의 시계가 자정을 넘었으면, 지나간 날의 후원 기한을 따지고 알릴 말을 모은다.
        ///
        /// 잠자리(하루 마감)는 2026-10-17에 뺐다 — 시간 넘기기가 있고, 이세계에 떨어진 주인공이 어디서 자는지도
        /// 맞지 않으며, 정보는 수첩과 정보 기록에 이미 남는다. 그래서 하루를 닫는 일은 시간이 흐르는 순간에 한다.
        /// 넘어간 날이 없으면 빈 문자열.
        /// </summary>
        internal string CheckDayTurn()
        {
            var today = Today();

            if (LastDayChecked <= 0)
            {
                LastDayChecked = today;
                PartyCasting.Cast(today, false);
                // 씬이 새로 떴다 — 장중(오후장 끝 자정)이나 다른 씬에서 넘어간 날을 놓치지 않게 어제를 따진다.
                // 이미 따진 사정은 실패로 바뀌어 있어 다시 세지 않는다.
                if (today > 1 && PatronageLedger.EndOfDay(today - 1) > 0)
                {
                    return StringConstants.TEXT_DAY_END_FAILED;
                }

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

            return string.Join("\n", notes);
        }

        /// <summary>지금 휴장(오전 · 오후 · 새벽). 영지 시계가 휴장 동안 흐르지 않으므로 시각 하나로 정해진다.</summary>
        internal VillagePhase Phase
        {
            get
            {
                return Clock == null ? VillagePhase.Phase_Morning : ActionPoints.PhaseOf(Clock.BreakMinuteOfDay);
            }
        }

        /// <summary>
        /// 자리의 일을 맡는다. 기획서 §10 — 시간을 쓰고 돈을 번다.
        ///
        /// 사람을 많이 만나는 일이면 소문 하나를 덤으로 주워듣는다. 다만 여관에 돈을 내고
        /// 묻는 것만큼 또렷하지는 않다. 누구 이야기인지까지만 안다.
        /// </summary>
        internal void DoWork(VillageSpot spot)
        {
            Dialogue.SetBackground(BackgroundOf(spot.InfoSource));

            var quest = spot.Work;

            // 프롤로그의 주방 일은 공짜다(행동력은 지하를 안 뒤부터).
            if (StoryProgress.CellarFound == true)
            {
                ActionPoints.Spend(Constants.AP_WORK);
            }

            // 처음으로 여관 일을 거들면 웹툰 5~6화가 벌어진다. 창고 안쪽에서 지하거래소를 찾는다.
            if (spot.HasCellar == true && StoryProgress.CellarFound == false)
            {
                GatherSession.Record(0f, quest.Reward, null);
                PlayDiscovery(spot);
                return;
            }

            MiniGames.StartWorkOrders(spot);
        }

        // ── 놀이 화면 ─────────────────────────

        // 추리형 놀이 화면이나 규칙서가 떠 있는가.
        private bool PuzzleOpen
        {
            get
            {
                return (MiniGames != null && MiniGames.IsPuzzleOpen == true) || (Rules != null && Rules.IsOpen == true);
            }
        }

        // 손놀림·엿듣기 화면이 떠 있는가.
        private bool MiniGameOpen
        {
            get
            {
                return MiniGames != null && MiniGames.IsHandOpen == true;
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

            // 프롤로그가 있는 새 판은 프롤로그가 첫 안내를 맡는다.
            if (StoryProgress.VillageTutorialDone == true || Tutorial.IsOpen == true || StoryProgress.CellarFound == true
                || Prologue == null || Prologue.IsActive == true)
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

        private VillageSpot CostSpot;
        private string CostText;
        private VillageNpc TagNpc;
        private int TagHeld = -1;
        private int TagStage = -1;
        private string TagGroup;
        private string TagStock;
        private string TagText;

        // 장소 값 칸. 서 있는 동안 매 프레임 불리니 장소가 바뀔 때만 만든다.
        private string CostOf(VillageSpot spot)
        {
            if (spot != CostSpot || CostText == null)
            {
                CostSpot = spot;
                CostText = spot.Price > 0f ? string.Format(StringConstants.TEXT_GATHER_COST, spot.Price) : StringConstants.TEXT_GATHER_FREE;
            }

            return CostText;
        }

        // 가까운 사람의 꼬리표. 사람·보유 수량·상장 단계가 바뀔 때만 다시 만든다.
        private string NearTag(VillageNpc npc)
        {
            var held = -1;
            var stage = -1;
            var party = npc.Party;
            var sim = BreakClock.Simulation;

            if (party != null && sim != null && string.IsNullOrEmpty(party.Stock) == false)
            {
                held = sim.Wallet.QuantityOf(party.Stock);
                stage = (int)sim.StageOf(party.Stock);
            }

            // 같은 사람에게 다른 단이 배정될 수 있다(PartyCasting) — 소속 이름과 종목도 본다(Codex 리뷰 10/19).
            var stock = party == null ? null : party.Stock;

            if (npc != TagNpc || held != TagHeld || stage != TagStage || TagText == null
                || ReferenceEquals(npc.Group, TagGroup) == false || ReferenceEquals(stock, TagStock) == false)
            {
                TagNpc = npc;
                TagGroup = npc.Group;
                TagStock = stock;
                TagHeld = held;
                TagStage = stage;
                TagText = TagOf(npc);
            }

            return TagText;
        }

        /// <summary>새 할 일을 받는 장면 — 대화·연출·튜토리얼이 없을 때만(장면은 VillageQuestHandler).</summary>
        private void PlayQuestIntroIfDue()
        {
            if (QuestLog.HasPendingIntro == false || Prologue == null || Prologue.AllowsQuests == false
                || Prologue.AwaitsRecognition == true
                || Prologue.IsDirecting == true || Staging.IsGuiding == true || Staging.IsTitleOpen == true
                || Dialogue == null || Dialogue.IsOpen == true
                || Approaching == true || PuzzleOpen == true || MiniGameOpen == true
                || Night.IsDepartureRunning == true
                || (Tutorial != null && Tutorial.IsOpen == true))
            {
                return;
            }

            Quests.PlayIntro();
        }

        // ── 수첩 (2026-10-17 사용자 요청) ─────────

        private UI_VillageNotebook Notebook;

        private bool NotebookOpen
        {
            get
            {
                return Notebook != null && Notebook.IsOpen == true;
            }
        }

        internal void OpenNotebook(bool bands)
        {
            if (Notebook == null)
            {
                Notebook = new UI_VillageNotebook(Root, NameFor);
            }

            Notebook.Open(bands);
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
            var prompt = Cached<VisualElement>("village-prompt");

            if (prompt == null)
            {
                return;
            }

            // 대화 중에는 안내를 감춘다. 두 개가 같이 떠 있으면 무엇을 누르라는지 모른다.
            var talking = Dialogue != null && Dialogue.IsOpen == true;
            prompt.EnableInClassList("hidden", (Here == null && NearNpc == null) || talking);
            // 프롤로그 동안은 시장 달력과 다음 장 시각을 숨긴다.
            if (Prologue != null && Prologue.IsActive == true)
            {
                var hour = Clock == null ? 0 : Clock.BreakHour;
                var minute = Clock == null ? 0 : Clock.BreakMinute;
                SetText("village-visits", Prologue.ClockLabel((hour * 60 + minute) * 60 + (Clock == null ? 0 : Clock.BreakSecond)));
            }
            else
            {
                SetText("village-visits", StatusText());
            }

            if (Here == null)
            {
                if (NearNpc != null)
                {
                    SetText("village-place", NearNpc.Name);
                    SetText("village-cost", NearTag(NearNpc));
                    SetText("village-key", StringConstants.TEXT_NPC_TALK_KEY);
                }

                return;
            }

            SetText("village-place", Here.Name);
            // E는 언제나 선택지를 연다(잠자리·용사단 탁자는 행동력이 없어도 된다). 값 칸은 돈 내고 묻기만 말한다 —
            // 창고를 찾기 전(프롤로그)에는 그 선택지가 없으니 비운다.
            var paid = StoryProgress.CellarFound == true && Here.Kind == VillageActivity.Activity_Gather;
            var cost = string.Empty;

            if (paid == true)
            {
                cost = Gathering.HasTime() == true ? CostOf(Here) : StringConstants.TEXT_GATHER_KEY_LATE;
            }

            SetText("village-cost", cost);
            SetText("village-key", StringConstants.TEXT_GATHER_KEY);
        }

        /// <summary>
        /// 그 종목을 지금 가지고 있으면 "내가 가진 종목 · N주"를 돌려준다. 아니면 null.
        /// 영지에서 캔 소식이 내 돈에 닿는지 그 자리에서 알게 한다(2026-10-17 사용자 요청).
        /// </summary>
        internal static string HeldNote(string stockId)
        {
            var sim = BreakClock.Simulation;

            if (sim == null)
            {
                return null;
            }

            var quantity = sim.Wallet.QuantityOf(stockId);
            return quantity > 0 ? string.Format(StringConstants.TEXT_HELD_NOTE, quantity) : null;
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

        // 오른쪽 위 상태 줄. 날짜·휴장·행동력이 바뀔 때만 다시 조립한다(규칙 §10 — 매 프레임 문자열 조립 금지).
        private string StatusCache = string.Empty;
        private int StatusKey = -1;
        private float StatusMoney = -1f;

        private string StatusText()
        {
            var key = ActionPoints.KeyOf(Today(), Clock == null ? 0 : Clock.BreakMinuteOfDay) * 10 + ActionPoints.Current;
            var money = Mathf.Round(GatherSession.Available);

            if (key == StatusKey && money == StatusMoney)
            {
                return StatusCache;
            }

            StatusKey = key;
            StatusMoney = money;
            StatusCache = string.Format(StringConstants.TEXT_VILLAGE_STATUS, ClockText.SeasonDay(Today()), PhaseName(Phase),
                ActionDots(), money);
            return StatusCache;
        }

        private static string PhaseName(VillagePhase phase)
        {
            switch (phase)
            {
                case VillagePhase.Phase_Morning:
                {
                    return StringConstants.TEXT_PHASE_MORNING;
                }
                case VillagePhase.Phase_Afternoon:
                {
                    return StringConstants.TEXT_PHASE_AFTERNOON;
                }
                default:
                {
                    return StringConstants.TEXT_PHASE_NIGHT;
                }
            }
        }

        // 행동력 점(●●○). 휴장이 바뀔 때만 다시 만든다 — 매 프레임 문자열을 조립하지 않게.
        private string DotsCache = string.Empty;
        private int DotsCurrent = -1;
        private int DotsMax = -1;

        private string ActionDots()
        {
            var max = ActionPoints.MaxOf(Phase);

            if (DotsCurrent == ActionPoints.Current && DotsMax == max)
            {
                return DotsCache;
            }

            DotsCurrent = ActionPoints.Current;
            DotsMax = max;
            var builder = new System.Text.StringBuilder();

            for (var i = 0; i < max; i++)
            {
                builder.Append(i < DotsCurrent ? StringConstants.TEXT_AP_FULL : StringConstants.TEXT_AP_EMPTY);
            }

            DotsCache = builder.ToString();
            return DotsCache;
        }

        private UI_WebtoonPanels Webtoon;

        /// <summary>2일째 아침 — 웹툰 7장 「개미의 결심」을 칸 단위로 보여 준다. 그림이 없으면 바로 넘어간다.</summary>
        internal void PlayResolveWebtoon(System.Action then)
        {
            var texture = Resources.Load<Texture2D>(StringConstants.WEBTOON_CH7_RESOURCE);

            if (texture == null)
            {
                then();
                return;
            }

            if (Webtoon == null)
            {
                Webtoon = new UI_WebtoonPanels(Root);
            }

            Webtoon.Open(texture, Constants.WEBTOON_CH7_PANELS, () =>
            {
                Resources.UnloadAsset(texture);
                then();
            });
        }

        /// <summary>2일째 아침 — 행동력 안내. 끝나면 이번 휴장의 몫을 채운다.</summary>
        internal void PlayActionTutorial(System.Action then)
        {
            Tutorial.Play(new List<TutorialStep>
            {
                new TutorialStep("village-visits", StringConstants.TEXT_AP_TUT_1_TITLE, StringConstants.TEXT_AP_TUT_1_BODY),
                new TutorialStep(null, StringConstants.TEXT_AP_TUT_2_TITLE, StringConstants.TEXT_AP_TUT_2_BODY),
                new TutorialStep("village-menu", StringConstants.TEXT_AP_TUT_3_TITLE, StringConstants.TEXT_AP_TUT_3_BODY),
                new TutorialStep(null, StringConstants.TEXT_AP_TUT_4_TITLE, StringConstants.TEXT_AP_TUT_4_BODY)
            }, () =>
            {
                StoryProgress.ActionTutorialDone = true;
                ActionPoints.Refill(Clock.BreakMinuteOfDay);

                if (then != null)
                {
                    then();
                }
            });
        }

        internal bool IsMorningBreak
        {
            get
            {
                return Clock != null && Clock.IsBreak == true && Phase == VillagePhase.Phase_Morning;
            }
        }

        private void TickMinimap(Keyboard keyboard)
        {
            if (Minimap == null && Player != null)
            {
                var ground = GameObject.Find(Constants.VILLAGE_GROUND_NAME);
                Minimap = new UI_VillageMinimap(Root, ground == null ? null : ground.GetComponent<SpriteRenderer>(),
                    FindObjectsByType<VillageSpot>(FindObjectsInactive.Exclude),
                    FindObjectsByType<VillageNpc>(FindObjectsInactive.Exclude), Player.gameObject);
            }

            if (Minimap == null)
            {
                return;
            }

            var covered = (Dialogue != null && Dialogue.IsOpen == true) || NotebookOpen == true || PuzzleOpen == true
                || MiniGameOpen == true || Staging.IsTitleOpen == true;

            if (covered == false && keyboard != null && keyboard.mKey.wasPressedThisFrame == true)
            {
                Minimap.Toggle();
            }

            Minimap.Tick(Player.position, covered, Prologue == null ? null : Prologue.CurrentTarget);
        }

        // ── 핸들러가 쓰는 창구 ───────

        /// <summary>돌아보기·건네기·시간이 흐르는 연출 중인가.</summary>
        /// <summary>주인공의 자리(없으면 null).</summary>
        internal Transform PlayerTransform
        {
            get
            {
                return Player;
            }
        }

        /// <summary>연출이 주인공을 붙잡는다(시간이 흐르는 암전 동안).</summary>
        internal void SetApproaching(bool value)
        {
            Approaching = value;
        }

        internal bool IsApproaching
        {
            get
            {
                return Approaching;
            }
        }

        internal bool IsTutorialOpen
        {
            get
            {
                return Tutorial != null && Tutorial.IsOpen == true;
            }
        }

        // ── 프롤로그가 쓰는 연출 창구 (VillagePrologueHandler) ───────

        /// <summary>대화·연출·안내가 모두 닫혀 있어 새 장면을 띄워도 되는가.</summary>
        internal bool CanDirect()
        {
            return Dialogue != null && Dialogue.IsOpen == false && Approaching == false
                && Staging.IsTitleOpen == false && (Webtoon == null || Webtoon.IsOpen == false)
                && PuzzleOpen == false && MiniGameOpen == false && (Tutorial == null || Tutorial.IsOpen == false);
        }

        /// <summary>주인공 머리 위 혼잣말.</summary>
        internal void SayToSelf(string text)
        {
            var actor = PlayerActor;

            if (actor != null)
            {
                actor.Say(text);
            }
        }

        // 장소는 씬이 뜰 때 정해져 바뀌지 않는다. 처음 한 번만 찾는다(코드 규칙 §10 — 매 프레임 씬 검색 금지).
        private Dictionary<InfoSource, VillageSpot> SpotsBySource;

        internal VillageSpot FindSpot(InfoSource source)
        {
            if (SpotsBySource == null)
            {
                SpotsBySource = new Dictionary<InfoSource, VillageSpot>();

                foreach (var spot in FindObjectsByType<VillageSpot>(FindObjectsInactive.Exclude))
                {
                    if (spot.Kind != VillageActivity.Activity_Enter && SpotsBySource.ContainsKey(spot.InfoSource) == false)
                    {
                        SpotsBySource.Add(spot.InfoSource, spot);
                    }
                }
            }

            VillageSpot found;
            return SpotsBySource.TryGetValue(source, out found) == true ? found : null;
        }

        internal DialogueLine OwnerLineFor(VillageSpot spot, string text)
        {
            return OwnerLine(spot, text);
        }

        /// <summary>장면 일러스트를 깔고 대화를 띄운다(온보딩 — 다락방 아침, 용사단을 알아보는 순간).</summary>
        internal void PlayScene(string sceneClass, List<DialogueLine> lines, System.Action then)
        {
            Dialogue.SetBackground(sceneClass);
            Dialogue.Play(lines, then);
        }

        /// <summary>대화를 띄운다. <paramref name="place"/>가 없으면 배경 그림 없이 — 영지가 비쳐 보이는 혼잣말.</summary>
        internal void PlayLines(InfoSource? place, List<DialogueLine> lines, System.Action then)
        {
            Dialogue.SetBackground(place.HasValue == true ? BackgroundOf(place.Value) : "dialogue-root--street");
            Dialogue.Play(lines, then);
        }

        /// <summary>
        /// 순찰 정거장의 연습 판을 연다. 그 장소에 연습 판이 있으면 참 — 끝나면 <paramref name="onDone"/>.
        /// </summary>
        internal bool StartStopPractice(InfoSource place, System.Action onDone)
        {
            // 연습 판 전에 규칙서를 넘겨 본다 - 규칙을 다 보고 나서 손으로 해 본다(2026-10-18 사장님).
            if (place == InfoSource.Source_TavernTalk)
            {
                OpenRules(StringConstants.TEXT_RULE_DICE_HEADING, MiniGameRules.Dice(), place, () => MiniGames.StartDicePractice(onDone));
                return true;
            }

            if (place == InfoSource.Source_SmithOrder)
            {
                OpenRules(StringConstants.TEXT_RULE_SMITH_HEADING, MiniGameRules.Smith(), place, () => MiniGames.StartSmithPractice(onDone));
                return true;
            }

            if (place == InfoSource.Source_GuildRecord)
            {
                OpenRules(StringConstants.TEXT_RULE_DEDUCE_HEADING, MiniGameRules.Deduce(), place, () => MiniGames.StartDeducePractice(onDone));
                return true;
            }

            return false;
        }

        private UI_RuleBook Rules;

        internal void OpenRules(string heading, List<RulePage> pages, InfoSource place, System.Action then)
        {
            Dialogue.Close();

            if (Rules == null)
            {
                Rules = new UI_RuleBook(Root);
            }

            Rules.Open(heading, pages, BackgroundOf(place), then);
            GameAudio.Play(SoundEffect.Sound_Page);
        }

        /// <summary>프롤로그 첫 안내: 왼쪽 위 목표. 걷기는 순찰 동안 데려가므로 지금은 말하지 않는다.</summary>
        internal void PlayPrologueTutorial()
        {
            Tutorial.Play(new List<TutorialStep>
            {
                new TutorialStep("village-objective", StringConstants.TEXT_PROLOGUE_TUT_GOAL_TITLE,
                    StringConstants.TEXT_PROLOGUE_TUT_GOAL_BODY)
            }, null);
        }

        /// <summary>
        /// 연출이 화면을 잡고 있는가 — 암전·제목 카드·웹툰·안내·프롤로그 장면. 이동·상호작용·메뉴가 함께 본다(코드 리뷰 10/19).
        /// </summary>
        internal bool IsStaging
        {
            get
            {
                return Staging.IsGuiding == true || (Prologue != null && Prologue.IsDirecting == true)
                    || Staging.IsTitleOpen == true || (Webtoon != null && Webtoon.IsOpen == true)
                    || (Tutorial != null && Tutorial.IsOpen == true);
            }
        }

        /// <summary>여덟째 날 여관에 닿았다 — 주방 일을 거든다(그 도중에 창고를 찾는다).</summary>
        internal void StartKitchenWork(VillageSpot spot)
        {
            DoWork(spot);
        }

        private DialogueLine OwnerLine(VillageSpot spot, string text)
        {
            var line = new DialogueLine(InfoPeople.Speaker(spot.InfoSource), spot.Name, text);
            line.Portrait = InfoPeople.Face(Data, spot.InfoSource, PortraitEmotion.Emotion_Neutral);
            return line;
        }

        internal static string BackgroundOf(InfoSource source)
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
        internal List<EventEntry> NextSlotRows()
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

        internal string StockName(string stockId)
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

        private readonly Dictionary<string, Label> Labels = new Dictionary<string, Label>();
        private readonly Dictionary<string, VisualElement> Elements = new Dictionary<string, VisualElement>();

        // UXML에 늘 있는 요소를 한 번 찾아 기억한다. 매 프레임 트리를 뒤지지 않게.
        internal T Cached<T>(string elementName) where T : VisualElement
        {
            VisualElement element;

            if (Elements.TryGetValue(elementName, out element) == false)
            {
                element = Root.Q<T>(elementName);

                if (element == null)
                {
                    return null;
                }

                Elements.Add(elementName, element);
            }

            return element as T;
        }

        // 매 프레임 불린다. 찾은 이름표를 기억하고, 글이 같으면 다시 넣지 않는다.
        internal void SetText(string elementName, string value)
        {
            Label label;

            if (Labels.TryGetValue(elementName, out label) == false)
            {
                label = Root.Q<Label>(elementName);

                if (label == null)
                {
                    return;
                }

                Labels.Add(elementName, label);
            }

            if (label.text != value)
            {
                label.text = value;
            }
        }
    }
}
