using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
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
        private bool LeaveBound;

        /// <summary>영지에서도 시각이 흐른다. 거래 화면의 시계와 같은 것을 쓴다.</summary>
        private MarketClock Clock
        {
            get
            {
                return BreakClock.Current;
            }
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

            // 영지의 시간은 실시간 1:1로 흐른다. 기획서 §11.
            if (Clock != null)
            {
                Clock.AdvanceBreak(Time.unscaledDeltaTime);
            }

            RefreshPrompt();

            if (Here == null || Dialogue == null || Dialogue.IsOpen == true)
            {
                return;
            }

            // 상호작용은 한 키로 끝낸다. 자리에 서서 누르면 그만이다.
            var keyboard = Keyboard.current;

            if (keyboard == null || keyboard.eKey.wasPressedThisFrame == false)
            {
                return;
            }

            Gather(Here);
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
                skip.clicked += () =>
                {
                    if (Clock != null)
                    {
                        Clock.SkipBreak();
                    }
                };
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
            prompt.EnableInClassList("hidden", Here == null || talking);
            var left = Clock == null ? 0 : Clock.BreakMinutesLeft;
            var hour = Clock == null ? 0 : Clock.BreakHour;
            var minute = Clock == null ? 0 : Clock.BreakMinute;
            SetText("village-visits",
                $"{hour:00}:{minute:00} · 다음 장까지 {left / 60}시간 {left % 60}분");

            if (Here == null)
            {
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
