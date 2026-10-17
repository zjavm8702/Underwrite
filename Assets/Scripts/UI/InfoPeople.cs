using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.UI
{
    /// <summary>
    /// 소식을 전하는 사람. 출처마다 한 사람이 정해져 있다(2026-10-17 사용자 결정).
    ///
    /// 지하거래소는 한 조직(임시 이름 「검은 장부」)이 운영한다. 영지에서 돈을 받고 정보를 파는 이들이
    /// 그 조직의 정보원이다. 영지에서는 직함으로 나오고, 지하 정보 타임에서는 정보원으로 나온다.
    /// 대화창에 서는 얼굴은 소식의 주인공(용병단)이 아니라 전하는 사람이다.
    /// </summary>
    public static class InfoPeople
    {
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

        /// <summary>지하 정보 타임에서의 이름. 영지의 직함 앞에 조직이 붙는다.</summary>
        public static string Informant(InfoSource source)
        {
            return string.Format(StringConstants.TEXT_LEDGER_INFORMANT, StringConstants.TEXT_LEDGER_NAME, Speaker(source));
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
