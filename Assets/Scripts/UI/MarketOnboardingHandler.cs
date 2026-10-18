using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Story;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 첫 지하 방문 튜토리얼(2026-10-18 사장님). 거래 화면(<see cref="MarketScreenController"/>)이 조립한다.
    ///
    /// 단계(<see cref="MarketOnboardingStage"/> · <see cref="StoryProgress.MarketOnboarding"/>, 저장 포함):
    /// 0 도착 — 시간을 멈추고 정보상이 지하와 시장 게시판을 소개한다
    /// 1 게시판으로 — 거래판을 처음 열면 종목·시세·호가·매수매도·시장가지정가·수량·주문·자산을 짚는다
    /// 2 첫 매수 — 1주를 산다(종목만 고르고 매수·시장가·1주는 고정)
    /// 3 첫 매도 — 그 1주를 판다. 팔고 나면 지갑을 도착했을 때 금액으로 되돌린다(연습이라 손익 없음, 사장님 10/18)
    /// 4 첫 정보 타임 — 여기서부터 시간이 흐른다. 정보상이 자기와 정보를 짧게 소개하고 소식을 전한다
    /// 5 다시 거래 — 정보 기록·매도·지정가 매도·장 끝내기를 짚는다
    /// 6 끝
    /// </summary>
    public sealed class MarketOnboardingHandler
    {

        private readonly MarketScreenController Host;
        private readonly Label Coach;
        private bool Busy;
        private float CoachTimer;

        private string PracticeStock;

        public MarketOnboardingHandler(MarketScreenController host, VisualElement root)
        {
            Host = host;
            Coach = new Label();
            Coach.AddToClassList("market-coach");
            Coach.pickingMode = PickingMode.Ignore;
            root.Add(Coach);
            SetCoach(null);
        }

        private static MarketOnboardingStage Stage
        {
            get
            {
                return StoryProgress.MarketOnboarding;
            }
            set
            {
                StoryProgress.MarketOnboarding = value;
            }
        }

        public bool IsActive
        {
            get
            {
                return Stage < MarketOnboardingStage.Onboarding_Done;
            }
        }

        /// <summary>첫 거래를 마치기 전에는 장이 흐르지 않는다.</summary>
        public bool HoldsTime
        {
            get
            {
                return Stage < MarketOnboardingStage.Onboarding_FirstBriefing;
            }
        }

        /// <summary>첫 정보 타임에 소식 앞에 붙일 정보상 소개. 이 단계가 아니면 null.</summary>
        public List<DialogueLine> BriefingIntro()
        {
            if (Stage != MarketOnboardingStage.Onboarding_FirstBriefing)
            {
                return null;
            }

            var lines = new List<DialogueLine>();

            foreach (var text in StringConstants.TEXT_MTUT_INFORMANT)
            {
                lines.Add(Host.InformantLine(StringConstants.TEXT_MTUT_TAG, text, PortraitEmotion.Emotion_Neutral));
            }

            return lines;
        }

        /// <summary>정보 타임 대화가 끝났다.</summary>
        public void OnBriefingClosed()
        {
            if (Stage == MarketOnboardingStage.Onboarding_FirstBriefing)
            {
                Stage = MarketOnboardingStage.Onboarding_TradeAgain;
                SetCoach(StringConstants.TEXT_MTUT_COACH_AFTER);
            }
        }

        /// <summary>매 프레임.</summary>
        public void Tick(float delta)
        {
            if (CoachTimer > 0f)
            {
                CoachTimer -= delta;

                if (CoachTimer <= 0f)
                {
                    SetCoach(null);
                }
            }

            if (IsActive == false || Busy == true || Host.CanDirect() == false)
            {
                return;
            }

            if (HoldsTime == true)
            {
                Host.HoldTime();
            }

            // 연습 매수 직전 현금을 한 번만 적는다(저장된다 — 나갔다 와도 같은 값).
            if (StoryProgress.PracticeCash < 0f && Stage <= MarketOnboardingStage.Onboarding_FirstBuy)
            {
                StoryProgress.PracticeCash = Host.CashNow();
            }

            switch (Stage)
            {
                case MarketOnboardingStage.Onboarding_Arrive:
                {
                    PlayArrive();
                    break;
                }
                case MarketOnboardingStage.Onboarding_ToBoard:
                {
                    if (Host.IsPanelOpen == true)
                    {
                        PlayPanelTour();
                    }

                    break;
                }
                case MarketOnboardingStage.Onboarding_FirstBuy:
                {
                    // 종목만 고른다. 매수·시장가·1주는 고정.
                    Host.ForceOrder(null, OrderSide.Side_Buy);
                    PracticeStock = Host.HeldStock();

                    if (PracticeStock != null)
                    {
                        Stage = MarketOnboardingStage.Onboarding_FirstSell;
                        SetCoach(StringConstants.TEXT_MTUT_COACH_SELL);
                    }

                    break;
                }
                case MarketOnboardingStage.Onboarding_FirstSell:
                {
                    Host.ForceOrder(PracticeStock, OrderSide.Side_Sell);

                    if (Host.HeldStock() == null)
                    {
                        // 연습이었다 — 오르내린 차이는 정보상이 메워 준다.
                        Host.ResetPracticeCash(StoryProgress.PracticeCash);
                        Stage = MarketOnboardingStage.Onboarding_FirstBriefing;
                        Host.ReleaseTime();
                        SetCoach(StringConstants.TEXT_MTUT_COACH_FLOW);
                        CoachTimer = Constants.MTUT_FLOW_COACH_SECONDS;
                    }

                    break;
                }
                case MarketOnboardingStage.Onboarding_TradeAgain:
                {
                    if (Host.IsPanelOpen == true)
                    {
                        PlayTradeAgain();
                    }

                    break;
                }
            }
        }

        private void PlayArrive()
        {
            Busy = true;
            var lines = new List<DialogueLine>();

            foreach (var text in StringConstants.TEXT_MTUT_ARRIVE)
            {
                lines.Add(Host.InformantLine(StringConstants.TEXT_MTUT_TAG, text, PortraitEmotion.Emotion_Neutral));
            }

            Host.PlayRoomLines(lines, () =>
            {
                Busy = false;
                Stage = MarketOnboardingStage.Onboarding_ToBoard;
                SetCoach(StringConstants.TEXT_MTUT_COACH_BOARD);
            });
        }

        private void PlayPanelTour()
        {
            Busy = true;
            SetCoach(null);
            Host.PlayTutorial(new List<TutorialStep>
            {
                new TutorialStep("stock-list", StringConstants.TEXT_MTUT_1_TITLE, StringConstants.TEXT_MTUT_1_BODY),
                new TutorialStep("chart", StringConstants.TEXT_MTUT_2_TITLE, StringConstants.TEXT_MTUT_2_BODY),
                new TutorialStep("book-list", StringConstants.TEXT_MTUT_3_TITLE, StringConstants.TEXT_MTUT_3_BODY),
                new TutorialStep("side-buy", StringConstants.TEXT_MTUT_4_TITLE, StringConstants.TEXT_MTUT_4_BODY),
                new TutorialStep("type-limit", StringConstants.TEXT_MTUT_5_TITLE, StringConstants.TEXT_MTUT_5_BODY),
                new TutorialStep("qty-box", StringConstants.TEXT_MTUT_6_TITLE, StringConstants.TEXT_MTUT_6_BODY),
                new TutorialStep("submit-button", StringConstants.TEXT_MTUT_7_TITLE, StringConstants.TEXT_MTUT_7_BODY),
                new TutorialStep("asset-row", StringConstants.TEXT_MTUT_8_TITLE, StringConstants.TEXT_MTUT_8_BODY)
            }, () =>
            {
                Busy = false;
                StoryProgress.MarketTutorialDone = true;
                Stage = MarketOnboardingStage.Onboarding_FirstBuy;
                SetCoach(StringConstants.TEXT_MTUT_COACH_BUY);
            });
        }

        private void PlayTradeAgain()
        {
            Busy = true;
            SetCoach(null);
            Host.PlayTutorial(new List<TutorialStep>
            {
                new TutorialStep("info-popup", StringConstants.TEXT_MTUT_9_TITLE, StringConstants.TEXT_MTUT_9_BODY),
                new TutorialStep("side-sell", StringConstants.TEXT_MTUT_10_TITLE, StringConstants.TEXT_MTUT_10_BODY),
                new TutorialStep("book-list", StringConstants.TEXT_MTUT_11_TITLE, StringConstants.TEXT_MTUT_11_BODY),
                new TutorialStep("skip-button", StringConstants.TEXT_MTUT_12_TITLE, StringConstants.TEXT_MTUT_12_BODY)
            }, () =>
            {
                Busy = false;
                Stage = MarketOnboardingStage.Onboarding_Done;
            });
        }

        private void SetCoach(string text)
        {
            Coach.text = text ?? string.Empty;
            Coach.style.display = string.IsNullOrEmpty(text) == true ? DisplayStyle.None : DisplayStyle.Flex;
            Coach.BringToFront();
            CoachTimer = 0f;
        }
    }
}
