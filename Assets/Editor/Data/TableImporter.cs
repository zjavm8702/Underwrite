using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Utility;
using UnityEditor;
using UnityEngine;

namespace Underwrite.Editor
{
    /// <summary>
    /// `Tables/*.csv`를 읽어 `Assets/Data`의 ScriptableObject를 채운다.
    ///
    /// 방향은 한쪽이다. 표가 자산을 덮어쓴다.
    /// 원본은 자산이고 CSV는 외부에서 한 번에 넣을 때 쓰는 통로다. 되돌리는 내보내기는
    /// 실제로 필요해질 때 만든다. 양방향을 미리 만들면 어느 쪽이 맞는지 알 수 없게 된다.
    /// </summary>
    public static class TableImporter
    {
        private const string TABLE_FOLDER = "Tables";
        private const string ASSET_FOLDER = "Assets/Data";

        [MenuItem("Underwrite/테이블 가져오기")]
        public static void Import()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var tables = Path.Combine(root, TABLE_FOLDER);

            if (Directory.Exists(tables) == false)
            {
                UnderwriteLog.Error($"표 폴더가 없습니다: {tables}");
                return;
            }

            if (AssetDatabase.IsValidFolder(ASSET_FOLDER) == false)
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            var tuning = Load<MarketTuningData>("MarketTuningData");
            var personalities = Load<PersonalityTable>("PersonalityTable");
            var sentiments = Load<SentimentTable>("SentimentTable");
            var stocks = Load<StockTable>("StockTable");
            var events = Load<EventTable>("EventTable");
            var texts = Load<UITextTable>("UITextTable");
            var set = Load<GameDataSet>("GameDataSet");

            ImportTuning(tables, tuning);
            ImportPersonalities(tables, personalities);
            ImportSentiments(tables, sentiments);
            ImportStocks(tables, stocks);
            ImportEvents(tables, events);
            ImportTexts(tables, texts);

            set.Assign(tuning, personalities, sentiments, stocks, events, texts);

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnderwriteLog.Info("표를 자산으로 옮겼습니다.");
        }

        private static T Load<T>(string name) where T : ScriptableObject
        {
            var path = $"{ASSET_FOLDER}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ── 표별 읽기 ─────────────────────────────

        private static void ImportTuning(string folder, MarketTuningData asset)
        {
            var rows = ReadCsv(Path.Combine(folder, "MarketTuning.csv"));

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                if (Field(row, "Owner") != "튜닝")
                {
                    continue;
                }

                asset.SetByKey(Field(row, "Key"), ParseFloat(Field(row, "Value")));
            }

            EditorUtility.SetDirty(asset);
        }

        private static void ImportPersonalities(string folder, PersonalityTable asset)
        {
            var rows = ReadCsv(Path.Combine(folder, "Personality.csv"));
            var list = new List<PersonalityEntry>();

            for (var i = 0; i < rows.Count; i++)
            {
                var entry = new PersonalityEntry();
                entry.Personality = ParsePersonality(Field(rows[i], "Personality"));
                entry.VolatilityMultiplier = ParseFloat(Field(rows[i], "VolatilityMultiplier"));
                list.Add(entry);
            }

            asset.Replace(list);
            EditorUtility.SetDirty(asset);
        }

        private static void ImportSentiments(string folder, SentimentTable asset)
        {
            var rows = ReadCsv(Path.Combine(folder, "Sentiment.csv"));
            var list = new List<SentimentEntry>();

            for (var i = 0; i < rows.Count; i++)
            {
                var entry = new SentimentEntry();
                entry.Sentiment = ParseEnum<MarketSentiment>("Sentiment_", Field(rows[i], "Sentiment"));
                entry.DriftPerTick = ParseFloat(Field(rows[i], "DriftPerTick"));
                entry.VolatilityMultiplier = ParseFloat(Field(rows[i], "VolatilityMultiplier"));
                list.Add(entry);
            }

            asset.Replace(list);
            EditorUtility.SetDirty(asset);
        }

        private static void ImportStocks(string folder, StockTable asset)
        {
            var rows = ReadCsv(Path.Combine(folder, "Stocks.csv"));
            var list = new List<StockEntry>();

            for (var i = 0; i < rows.Count; i++)
            {
                var entry = new StockEntry();
                entry.Id = Field(rows[i], "Id");
                entry.DisplayName = Field(rows[i], "DisplayName");
                entry.Personality = ParsePersonality(Field(rows[i], "Personality"));
                entry.TrueValue = ParseFloat(Field(rows[i], "TrueValue"));
                entry.Liquidity = ParseFloat(Field(rows[i], "Liquidity"));
                list.Add(entry);
            }

            asset.Replace(list);
            EditorUtility.SetDirty(asset);
        }

        private static void ImportEvents(string folder, EventTable asset)
        {
            var rows = ReadCsv(Path.Combine(folder, "Events.csv"));
            var list = new List<EventEntry>();

            for (var i = 0; i < rows.Count; i++)
            {
                var entry = new EventEntry();
                entry.Slot = ParseInt(Field(rows[i], "Slot"));
                entry.StockId = Field(rows[i], "StockId");
                entry.Stage = ParseEnum<EventStage>("Stage_", Field(rows[i], "Stage"));
                entry.Headline = Field(rows[i], "Headline");
                entry.Source = ParseEnum<InfoSource>("Source_", Field(rows[i], "Source"));
                entry.Visibility = ParseEnum<InfoVisibility>("Visibility_", Field(rows[i], "Visibility"));
                entry.ExpectationDelta = ParseFloat(Field(rows[i], "ExpectationDelta"));
                entry.ValueDelta = ParseFloat(Field(rows[i], "ValueDelta"));
                list.Add(entry);
            }

            asset.Replace(list);
            EditorUtility.SetDirty(asset);
        }

        private static void ImportTexts(string folder, UITextTable asset)
        {
            var rows = ReadCsv(Path.Combine(folder, "UIText.csv"));
            var list = new List<TextEntry>();

            for (var i = 0; i < rows.Count; i++)
            {
                // 로그와 인스펙터 라벨은 화면에 나가지 않는다. 표로 옮길 대상이 아니다.
                if (Field(rows[i], "Kind") != "UI")
                {
                    continue;
                }

                var entry = new TextEntry();
                entry.Key = Field(rows[i], "Key");
                entry.Korean = Field(rows[i], "Korean");
                entry.HasFormat = Field(rows[i], "HasFormat") == "O";
                list.Add(entry);
            }

            asset.Replace(list);
            EditorUtility.SetDirty(asset);
        }

        // ── CSV ───────────────────────────────────

        private static string Field(Dictionary<string, string> row, string column)
        {
            string value;

            if (row.TryGetValue(column, out value) == true)
            {
                return value;
            }

            return string.Empty;
        }

        private static float ParseFloat(string text)
        {
            float value;

            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) == true)
            {
                return value;
            }

            return 0f;
        }

        private static int ParseInt(string text)
        {
            int value;

            if (int.TryParse(text, out value) == true)
            {
                return value;
            }

            return 0;
        }

        private static StockPersonality ParsePersonality(string text)
        {
            // 표는 HIGH_RISK처럼 적고 enum은 Personality_HighRisk다. 밑줄을 지우고 맞춘다.
            var cleaned = text.Replace("_", string.Empty);

            foreach (StockPersonality value in Enum.GetValues(typeof(StockPersonality)))
            {
                var name = value.ToString().Replace("Personality_", string.Empty);

                if (string.Equals(name, cleaned, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return value;
                }
            }

            return StockPersonality.Personality_Famous;
        }

        private static T ParseEnum<T>(string prefix, string text) where T : struct
        {
            T parsed;

            if (Enum.TryParse(prefix + text, true, out parsed) == true)
            {
                return parsed;
            }

            if (Enum.TryParse(text, true, out parsed) == true)
            {
                return parsed;
            }

            UnderwriteLog.Warning($"표의 값을 읽지 못했습니다: {prefix}{text}");
            return default(T);
        }

        /// <summary>
        /// 쉼표와 줄바꿈이 큰따옴표 안에 들어 있는 CSV를 읽는다.
        /// 설명 칸에 쉼표가 들어 있어 단순 Split으로는 열이 밀린다.
        /// </summary>
        private static List<Dictionary<string, string>> ReadCsv(string path)
        {
            var rows = new List<Dictionary<string, string>>();

            if (File.Exists(path) == false)
            {
                UnderwriteLog.Error($"표 파일이 없습니다: {path}");
                return rows;
            }

            var cells = ParseCells(File.ReadAllText(path, Encoding.UTF8));

            if (cells.Count < 2)
            {
                return rows;
            }

            var header = cells[0];

            for (var i = 1; i < cells.Count; i++)
            {
                var line = cells[i];

                if (line.Count == 0)
                {
                    continue;
                }

                var row = new Dictionary<string, string>();

                for (var c = 0; c < header.Count; c++)
                {
                    row[header[c]] = c < line.Count ? line[c] : string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<List<string>> ParseCells(string text)
        {
            var lines = new List<List<string>>();
            var line = new List<string>();
            var cell = new StringBuilder();
            var quoted = false;

            // 엑셀과 구글 시트가 붙이는 BOM을 떼어 낸다. 첫 열 이름이 어긋난다.
            if (text.Length > 0 && text[0] == '﻿')
            {
                text = text.Substring(1);
            }

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (quoted == true)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                            continue;
                        }

                        quoted = false;
                        continue;
                    }

                    cell.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    quoted = true;
                    continue;
                }

                if (c == ',')
                {
                    line.Add(cell.ToString());
                    cell.Length = 0;
                    continue;
                }

                if (c == '\n')
                {
                    line.Add(cell.ToString().TrimEnd('\r'));
                    cell.Length = 0;
                    lines.Add(line);
                    line = new List<string>();
                    continue;
                }

                cell.Append(c);
            }

            if (cell.Length > 0 || line.Count > 0)
            {
                line.Add(cell.ToString().TrimEnd('\r'));
                lines.Add(line);
            }

            return lines;
        }
    }
}
