using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Save;
using Underwrite.Story;
using Underwrite.Utility;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 영지 메뉴(드롭업)와 턴 넘기기 — 장으로 간다 · 이번 장은 쉰다 · 수첩 · 소리 · 잠.
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립한다(2026-10-19 정리 — 화면에서 떼어 냄).
    /// </summary>
    public sealed class VillageMenuHandler
    {
        private const string MENU_BUTTON = "village-menu";
        private const string MENU_LIST = "village-menu-list";
        private const string BUTTON_ON = "village-button--on";

        private readonly VillageScreenController Host;
        private readonly VillagePrologueHandler Prologue;
        private readonly VillageNightHandler Night;
        private bool IsBound;

        public VillageMenuHandler(VillageScreenController host, VillagePrologueHandler prologue, VillageNightHandler night)
        {
            Host = host;
            Prologue = prologue;
            Night = night;
        }

        /// <summary>메뉴가 펼쳐져 있는가.</summary>
        public bool IsOpen
        {
            get
            {
                var list = Host.Cached<VisualElement>(MENU_LIST);
                return list != null && list.ClassListContains("hidden") == false;
            }
        }

        /// <summary>메뉴 단추를 묶는다. 화면이 준비된 뒤에 한 번만.</summary>
        public void Bind()
        {
            if (IsBound == true)
            {
                return;
            }

            var menu = Host.Cached<Button>(MENU_BUTTON);

            if (menu == null)
            {
                return;
            }

            IsBound = true;
            menu.text = StringConstants.TEXT_MENU;
            menu.clicked += Toggle;
        }

        // ── 턴 넘기기 (2026-10-19 행동력) ─────────

        /// <summary>
        /// 지하거래소로 내려갈 수 있는가. 장이 열려 있을 때(곧 열릴 때)만 된다.
        /// 장이 끝나면 지하세계는 닫힌다(2026-10-17 사용자 결정).
        /// </summary>
        public bool MarketOpenNow()
        {
            var clock = Host.Clock;
            return clock != null && clock.IsBreak == true && Host.Phase != VillagePhase.Phase_Night;
        }

        public string MarketClosedNote()
        {
            return StringConstants.TEXT_MENU_NIGHT_NO_SESSION;
        }

        public string SessionNote()
        {
            return ActionPoints.Current > 0
                ? string.Format(StringConstants.TEXT_MENU_TO_SESSION_NOTE, ActionPoints.Current)
                : StringConstants.TEXT_CELLAR_NOTE;
        }

        /// <summary>장으로 간다 — 남은 휴장을 넘기고 지하로 내려간다(턴 넘기기).</summary>
        public void GoToSession()
        {
            if (MarketOpenNow() == false)
            {
                return;
            }

            SaveSystem.Save();
            Host.Clock.SkipBreak();
            SceneManager.LoadScene(SceneRoutes.UNDERWORLD);
        }

        /// <summary>이번 장은 쉰다 — 장 하나를 거르고 그 뒤 휴장으로 넘어간다(다음 휴장의 행동력을 받는다).</summary>
        private void SkipSessionTurn()
        {
            if (MarketOpenNow() == false)
            {
                return;
            }

            var clock = Host.Clock;
            var start = clock.NextSessionMinute;

            if (clock.SkipNextSession() == false)
            {
                return;
            }

            Host.Staging.PassTime(start + Constants.MARKET_SESSION_MINUTES - clock.BreakMinuteOfDay,
                StringConstants.TEXT_SKIP_SESSION_FADE);
        }

        // ── 메뉴 (드롭업) ─────────

        private void Toggle()
        {
            if ((Host.Dialogue != null && Host.Dialogue.IsOpen == true) || Host.IsApproaching == true || Host.IsStaging == true)
            {
                return;
            }

            if (IsOpen == true)
            {
                Close();
                return;
            }

            Build();
        }

        /// <summary>
        /// 메뉴 항목을 연다. 열 때마다 새로 채운다 — 잠자리처럼 때에 따라 잠기는 항목이 있다.
        /// 항목을 늘릴 때는 여기에 Item 한 줄을 더한다.
        /// </summary>
        private void Build()
        {
            var list = Host.Cached<VisualElement>(MENU_LIST);
            var clock = Host.Clock;

            if (list == null || clock == null)
            {
                return;
            }

            list.Clear();

            // 프롤로그 동안은 시간 넘기기·수첩·지하거래소를 보이지 않는다. 시장을 아직 모른다.
            if (Prologue.IsActive == true)
            {
                Item(list, VolumeText(), true, string.Empty, GameAudio.CycleVolume);
                Show(list);
                return;
            }

            Item(list, StringConstants.TEXT_MENU_TO_SESSION, MarketOpenNow(), MarketClosedNote(), GoToSession);

            var nextSession = clock.SessionAt(clock.SkippedSessions) == MarketSession.Session_Morning
                ? StringConstants.TEXT_SESSION_MORNING
                : StringConstants.TEXT_SESSION_EVENING;
            Item(list, string.Format(StringConstants.TEXT_MENU_SKIP_SESSION, nextSession),
                MarketOpenNow() == true && clock.IsReopeningAt(clock.SkippedSessions) == false,
                MarketOpenNow() == true ? StringConstants.TEXT_MENU_SKIP_LOCKED : MarketClosedNote(), SkipSessionTurn);
            Item(list, StringConstants.TEXT_NOTEBOOK, true, string.Empty, () => Host.OpenNotebook(false));
            Item(list, VolumeText(), true, string.Empty, GameAudio.CycleVolume);

            if (Night.CanSleep() == true)
            {
                Item(list, StringConstants.TEXT_SLEEP_CHOICE, true, string.Empty, Night.Sleep);
            }

            Show(list);
        }

        private static string VolumeText()
        {
            return string.Format(StringConstants.TEXT_MENU_VOLUME, StringConstants.TEXT_VOLUME_STEPS[GameAudio.VolumeStep]);
        }

        private void Show(VisualElement list)
        {
            list.RemoveFromClassList("hidden");
            Host.Cached<Button>(MENU_BUTTON).AddToClassList(BUTTON_ON);
        }

        private void Item(VisualElement list, string text, bool enabled, string lockedNote, System.Action action)
        {
            var button = new Button(() =>
            {
                Close();
                action();
            });

            button.AddToClassList("village-button");
            button.AddToClassList("village-menu-item");
            // 잠긴 항목도 보여 준다. 언제 열리는지를 작은 글씨로 아랫줄에 붙인다.
            button.text = enabled == true ? text : $"{text}\n<size=13>{lockedNote}</size>";
            button.SetEnabled(enabled);

            list.Add(button);
        }

        public void Close()
        {
            var list = Host.Cached<VisualElement>(MENU_LIST);

            if (list != null)
            {
                list.AddToClassList("hidden");
            }

            var menu = Host.Cached<Button>(MENU_BUTTON);

            if (menu != null)
            {
                menu.RemoveFromClassList(BUTTON_ON);
            }
        }
    }
}
