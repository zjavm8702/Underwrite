using System;
using System.IO;
using Underwrite.Data;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using Underwrite.Utility;
using UnityEngine;

namespace Underwrite.Save
{
    /// <summary>
    /// 한 칸짜리 자동 저장. 영지에 있을 때(장 사이)만 저장하고, 이어하기는 늘 영지에서 시작한다.
    ///
    /// 장중을 저장하지 않는 이유: 대기 주문·정보 타임·틱 사이 흔들림까지 되살리려면 저장할 것이 배로 는다.
    /// 장은 길어야 실제 18분이라 잃는 것이 적다. 장 사이에 저장하면 시장·지갑·관계가 모두 한 순간에 맞는다.
    /// </summary>
    public static class SaveSystem
    {
        private static string FilePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, StringConstants.SAVE_FILE_NAME);
            }
        }

        public static bool HasSave
        {
            get
            {
                return Peek() != null;
            }
        }

        /// <summary>지금 저장할 수 있는가. 장이 열려 있거나 영지 시간이 아니면 저장하지 않는다.</summary>
        public static bool CanSave
        {
            get
            {
                var clock = BreakClock.Current;
                return clock != null && clock.IsBreak == true && clock.Phase == Enumeration.MarketPhase.Phase_Closed;
            }
        }

        /// <summary>지금 상태를 저장한다. 저장할 수 없는 때면 아무것도 하지 않고 거짓.</summary>
        public static bool Save()
        {
            if (CanSave == false)
            {
                return false;
            }

            var data = new SaveData
            {
                Version = Constants.SAVE_VERSION,
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                Clock = BreakClock.Current.Capture(),
                HasMarket = BreakClock.Simulation != null,
                Gather = GatherSession.Capture(),
                Patronage = PatronageLedger.Capture(),
                Heroes = HeroRoster.Capture(),
                Codex = BandCodex.Capture(),
                QuestIndex = QuestLog.CurrentIndex,
                Story = new StoryState
                {
                    CellarFound = StoryProgress.CellarFound,
                    MarketTutorialDone = StoryProgress.MarketTutorialDone,
                    VillageTutorialDone = StoryProgress.VillageTutorialDone
                }
            };

            data.Story.SleepSeen.AddRange(StoryProgress.SleepSeen);

            if (data.HasMarket == true)
            {
                data.Market = BreakClock.Simulation.Capture();
            }

            // 쓰다가 꺼지면 저장이 반쪽이 된다. 옆에 다 쓴 뒤 바꿔 끼운다.
            var path = FilePath;
            var temp = path + StringConstants.SAVE_TEMP_SUFFIX;

            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(data));

                if (File.Exists(path) == true)
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception exception)
            {
                UnderwriteLog.Warning($"저장하지 못했습니다: {exception.Message}");
                return false;
            }

            return true;
        }

        /// <summary>저장 파일을 읽기만 한다. 없거나, 깨졌거나, 판이 다르면 null.</summary>
        public static SaveData Peek()
        {
            var path = FilePath;

            if (File.Exists(path) == false)
            {
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                return data != null && data.Version == Constants.SAVE_VERSION ? data : null;
            }
            catch (Exception exception)
            {
                UnderwriteLog.Warning($"저장을 읽지 못했습니다: {exception.Message}");
                return null;
            }
        }

        /// <summary>저장된 시각. 이어하기 단추에 며칠째 몇 시인지 적는 데 쓴다. 저장이 없으면 null.</summary>
        public static MarketClock PeekClock()
        {
            var data = Peek();
            return data == null ? null : MarketClock.Restore(data.Clock);
        }

        /// <summary>
        /// 저장을 불러와 정적 상태를 모두 되살린다. 시장은 표로 새로 짠 뒤 덮어쓴다.
        /// 부른 쪽이 영지 씬을 연다. 실패하면 아무것도 바꾸지 않고 거짓.
        /// </summary>
        public static bool Load(GameDataSet tables)
        {
            var data = Peek();

            if (data == null)
            {
                return false;
            }

            // 표의 수치와 문구를 먼저 덮어쓴다. 시장을 짜기 전에 해야 시작 자금·문구가 맞는다.
            if (tables != null)
            {
                tables.Apply();
            }

            var clock = MarketClock.Restore(data.Clock);

            if (data.HasMarket == true)
            {
                var sim = MarketFactory.Create(tables, data.Market.Seed, data.Market.Sentiment);
                sim.Restore(data.Market, clock);
                BreakClock.Simulation = sim;
            }
            else
            {
                BreakClock.Simulation = null;
            }

            BreakClock.Current = clock;
            GatherSession.Restore(data.Gather);
            PatronageLedger.Restore(data.Patronage);
            HeroRoster.Restore(data.Heroes);
            BandCodex.Restore(data.Codex);
            QuestLog.Restore(data.QuestIndex);
            StoryProgress.CellarFound = data.Story.CellarFound;
            StoryProgress.MarketTutorialDone = data.Story.MarketTutorialDone;
            StoryProgress.VillageTutorialDone = data.Story.VillageTutorialDone;
            StoryProgress.SleepSeen.Clear();
            StoryProgress.SleepSeen.UnionWith(data.Story.SleepSeen);
            return true;
        }

        /// <summary>판이 끝났다(시나리오 붕괴). 이어할 것이 없다.</summary>
        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath) == true)
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception exception)
            {
                UnderwriteLog.Warning($"저장을 지우지 못했습니다: {exception.Message}");
            }
        }
    }
}
