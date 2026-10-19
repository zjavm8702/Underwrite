using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Patronage;
using Underwrite.Save;
using Underwrite.Story;

namespace Underwrite.UI
{
    /// <summary>
    /// 첫 후원 안내(2026-10-19 사장님: 「용사단에게 어떻게 후원할지 설명이 부족하다. 이것도 강제로」).
    ///
    /// 용사단을 알아본 뒤 할 일 셋을 프롤로그처럼 데려가서 시킨다 —
    /// 단골 테이블(인사) → 여관 일(술을 나르며 사정을 듣는다) → 길드 창구(서기의 설명 뒤 사정에 맞는 익명 의뢰).
    /// 할 일을 받는 장면이 끝나면 그 자리로 데려간다. 서기의 설명을 본 뒤로는 데려가지 않는다.
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립한다.
    /// </summary>
    public sealed class VillageSponsorGuideHandler
    {
        private readonly VillageScreenController Host;
        private readonly VillageHeroHandler Heroes;

        // 이번 판에서 이미 데려간 할 일 순번. 같은 할 일로 두 번 데려가지 않는다(도중에 그만둬도).
        private int GuidedIndex = -1;

        public VillageSponsorGuideHandler(VillageScreenController host, VillageHeroHandler heroes)
        {
            Host = host;
            Heroes = heroes;
        }

        /// <summary>
        /// 매 프레임. 영지가 한가할 때(대화·연출이 없고 할 일을 받는 장면도 끝났을 때) 다음 자리로 데려간다.
        /// 부르는 쪽이 프롤로그·출발 장면이 아닌지 먼저 본다.
        /// </summary>
        public void Tick()
        {
            if (StoryProgress.SponsorGuideSeen == true || StoryProgress.HeroesRecognized == false
                || QuestLog.HasPendingIntro == true || Host.IsGuiding == true || Host.CanDirect() == false)
            {
                return;
            }

            var index = QuestLog.CurrentIndex;

            if (index == GuidedIndex)
            {
                return;
            }

            if (index == Constants.QUEST_HEROES_MET_INDEX)
            {
                GuidedIndex = index;
                GuideToInn(spot => Heroes.OpenTable(spot));
                return;
            }

            // 일하려면 행동력이 든다. 행동력 안내(2일째 아침)를 본 뒤, 남아 있을 때만 데려간다.
            if (index == Constants.QUEST_HEAR_NEED_INDEX)
            {
                if (StoryProgress.ActionTutorialDone == false || ActionPoints.Has(Constants.AP_WORK) == false)
                {
                    return;
                }

                GuidedIndex = index;
                GuideToInn(PlayWorkCall);
                return;
            }

            if (index == Constants.QUEST_FIRST_REQUEST_INDEX && HasHeardNeed() == true)
            {
                GuidedIndex = index;
                PlayThink();
            }
        }

        private void GuideToInn(System.Action<VillageSpot> onArrive)
        {
            var inn = Host.FindSpot(InfoSource.Source_TavernTalk);

            if (inn == null)
            {
                return;
            }

            Host.GuideTo(inn, () =>
            {
                Host.GreetAt(inn);
                Host.CurrentSpot = inn;
                onArrive(inn);
            });
        }

        // 여관 주인이 구석 테이블 쪽 주문을 맡긴다. 일을 마치면 그 테이블의 사정이 들린다(VillageMiniGameHandler.FinishWork).
        private void PlayWorkCall(VillageSpot inn)
        {
            Host.PlayLines(inn.InfoSource, new List<DialogueLine> { Host.OwnerLineFor(inn, StringConstants.TEXT_SPONSOR_WORK_CALL) },
                () => Host.DoWork(inn));
        }

        // 왜 이름을 숨기고 길드로 가는지 주인공이 스스로 짚는다. 거리의 혼잣말이라 배경 그림을 깔지 않는다.
        private void PlayThink()
        {
            var lines = new List<DialogueLine>();

            for (var i = 0; i < StringConstants.TEXT_SPONSOR_THINK.Length; i++)
            {
                lines.Add(new DialogueLine(string.Empty, StringConstants.TEXT_QUEST_SELF_TAG, StringConstants.TEXT_SPONSOR_THINK[i]));
            }

            Host.PlayLines(null, lines, () =>
            {
                var guild = Host.FindSpot(InfoSource.Source_GuildRecord);

                if (guild == null)
                {
                    return;
                }

                Host.GuideTo(guild, () =>
                {
                    Host.GreetAt(guild);
                    // 설명을 봤으면 다시 데려가지 않는다 — 돈이 모자라 못 냈어도 방법은 안다.
                    StoryProgress.SponsorGuideSeen = true;
                    SaveSystem.Save();
                    Heroes.PlayRequestGuide(guild);
                });
            });
        }

        private static bool HasHeardNeed()
        {
            return VillageHeroHandler.HeardOpenNeed() != null;
        }
    }
}
