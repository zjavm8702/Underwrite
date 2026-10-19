using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.UI
{
    /// <summary>
    /// 소식을 전하는 사람(2026-10-17 사용자 결정). 대화창에 서는 얼굴은 소식의 주인공(용병단)이 아니라 전하는 사람이다.
    ///
    /// - 영지: 장소의 주인(여관 주인·기록 담당 …)이 자기 얼굴로 말한다
    /// - 지하 정보 타임: 거래소를 운영하는 「정보상」 한 사람이 전한다. 누구인지는 플레이어가 알 필요 없다 — 연출이다
    /// </summary>
    public static class InfoPeople
    {
        /// <summary>
        /// 장소 주인마다 말투가 다른 문구에서 그 사람 것을 고른다.
        /// <paramref name="byPerson"/> 순서: 여관 주인 · 기록 담당 · 대장장이 · 중개인 · 집사.
        /// </summary>
        public static string Pick(InfoSource source, string[] byPerson)
        {
            int index;

            switch (source)
            {
                case InfoSource.Source_GuildRecord:
                {
                    index = 1;
                    break;
                }
                case InfoSource.Source_SmithOrder:
                {
                    index = 2;
                    break;
                }
                case InfoSource.Source_MarketBoard:
                case InfoSource.Source_BrokerLeak:
                {
                    index = 3;
                    break;
                }
                case InfoSource.Source_PrivateLetter:
                {
                    index = 4;
                    break;
                }
                default:
                {
                    index = 0;
                    break;
                }
            }

            return index < byPerson.Length ? byPerson[index] : byPerson[0];
        }

        public static string Speaker(InfoSource source)
        {
            switch (source)
            {
                case InfoSource.Source_GuildRecord:
                {
                    return StringConstants.TEXT_FOLK_ARCHIVIST;
                }
                case InfoSource.Source_PrivateLetter:
                {
                    return StringConstants.TEXT_FOLK_STEWARD;
                }
                case InfoSource.Source_MarketBoard:
                case InfoSource.Source_BrokerLeak:
                {
                    return StringConstants.TEXT_FOLK_BROKER;
                }
                case InfoSource.Source_SmithOrder:
                {
                    return StringConstants.TEXT_FOLK_SMITH;
                }
                default:
                {
                    return StringConstants.TEXT_FOLK_INNKEEPER;
                }
            }
        }

        /// <summary>그 사람의 얼굴. 데이터 묶음에 얼굴이 없으면 null — 대화창이 인물 자리를 비운다.</summary>
        public static Sprite Face(GameDataSet data, InfoSource source, PortraitEmotion emotion)
        {
            if (data == null)
            {
                return null;
            }

            var set = data.FolkFaceOf(source);
            return set == null ? null : set.Get(emotion);
        }
    }
}
