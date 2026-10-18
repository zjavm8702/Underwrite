using System;
using System.Collections.Generic;
using System.IO;
using Underwrite.Editor.Utility;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Underwrite.Editor
{
    /// <summary>
    /// 파츠로 나눈 영지 블록을 씬으로 세운다.
    ///
    /// 한 장짜리 배경은 캐릭터가 무엇의 뒤로도 갈 수 없어 사람이 그림 위에 붙은 것처럼 보였다.
    /// 그래서 바닥판 한 장과 세워진 사물을 따로 받아, 사물마다 발밑 높이로 앞뒤를 정한다.
    /// 충돌은 사물이 바닥에 닿는 면(footprint)만 막는다. 지붕·잎 밑은 걸어 들어가 가려진다.
    ///
    /// 입력은 Codex 납품 폴더 하나다. ground.png, 사물 PNG들, layout.json.
    /// 좌표는 전부 바닥판 기준 왼쪽 위 원점 픽셀이다.
    /// </summary>
    public static class VillagePartsSetup
    {
        // 납품은 VillagePartsPilot_v1, _v2 … 로 쌓인다. 배치표가 있는 가장 높은 판을 쓴다.
        private const string SOURCE_ROOT = "Outputs";
        private const string SOURCE_PREFIX = "VillagePartsPilot_v";
        private const string PILOT_ASSETS = "Assets/Images/Village/PartsPilot";
        private const string PILOT_SCENE = "Assets/Scenes/VillagePartsPilot.unity";

        // 영지 전체 지도(2026-10-17 납품). 장소 여섯·종족 집·소품 151개. 이 씬이 실제 영지가 된다.
        private const string FULL_PREFIX = "VillagePartsFull_v";
        private const string FULL_ASSETS = "Assets/Images/Village/Full";
        private const string FULL_SCENE = "Assets/Scenes/Village.unity";

        // 광장 우물 앞, 조금 아래에서 시작한다. 여섯 곳 어디로도 걸어갈 수 있는 자리다.
        private static readonly Vector2 START_BELOW_WELL = new Vector2(0f, -1.6f);

        // 충돌을 그림보다 작게 잡는다(2026-10-17 사용자 요청 — 너무 부딪힌다).
        // 발 닿는 면은 가운데로 줄이고, 건물 뒤편 막기는 좌우만 줄여 옆으로 비켜 지나가기 쉽게 한다.
        private const float FOOTPRINT_SHRINK_X = 0.75f;
        private const float FOOTPRINT_SHRINK_Y = 0.8f;
        private const float BLOCK_SHRINK_X = 0.85f;
        private const float TREE_SHRINK = 0.4f;

        // 대장간 화로 입구(대장간 PNG 안의 픽셀, 2배 그림 기준). 배치표에 불 자리가 없어 그림을 보고 쟀다.
        private const string SMITH_FILE = "smith.png";
        private static readonly Vector2 SMITH_FORGE_IN_PNG = new Vector2(650f, 740f);

        private const float FORGE_LIGHT_INTENSITY = 0.8f;
        private const float FORGE_LIGHT_RADIUS = 1.8f;
        private static readonly Color FORGE_COLOR = new Color(1f, 0.55f, 0.2f);
        private const string SPRITE_UNLIT_MATERIAL =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        // 이번에 세우는 판의 그림 폴더. 메뉴마다 바뀐다.
        private static string AssetFolder = PILOT_ASSETS;
        private const string LAYOUT_FILE = "layout.json";
        private const string GROUND_FILE = "ground.png";

        // 바닥 그림 72px가 1유닛이다. 카메라 높이 15유닛이 화면 1080px와 맞는다. v2 배경과 같다.
        public const float PIXELS_PER_UNIT = 72f;
        private const float CHARACTER_SCALE = 0.27f;

        // 밤에 완전히 켜졌을 때의 가로등. 낮에는 하루의 흐름(DayNightCycle)이 꺼 둔다.
        private const float LAMP_INTENSITY = 1.35f;
        private const float LAMP_RADIUS = 2.4f;
        private static readonly Color LAMP_COLOR = new Color(1f, 0.82f, 0.55f);

        private const string SMOKE_SPRITE = "Assets/Images/Village/Fx/smoke_puff.png";
        private const string SPRITE_LIT_MATERIAL =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        [MenuItem("Underwrite/영지 파츠 시범 씬 만들기")]
        public static void Build()
        {
            BuildScene(SOURCE_PREFIX, PILOT_ASSETS, PILOT_SCENE, false);
        }

        [MenuItem("Underwrite/영지 파츠 시범 씬 만들기 (그림 덮어쓰기)")]
        public static void BuildWithOverwrite()
        {
            BuildScene(SOURCE_PREFIX, PILOT_ASSETS, PILOT_SCENE, true);
        }

        [MenuItem("Underwrite/영지 전체 씬 만들기")]
        public static void BuildFull()
        {
            BuildScene(FULL_PREFIX, FULL_ASSETS, FULL_SCENE, false);
        }

        [MenuItem("Underwrite/영지 전체 씬 만들기 (그림 덮어쓰기)")]
        public static void BuildFullWithOverwrite()
        {
            BuildScene(FULL_PREFIX, FULL_ASSETS, FULL_SCENE, true);
        }

        private static void BuildScene(string prefix, string assetFolder, string scenePath, bool allowOverwrite)
        {
            AssetFolder = assetFolder;
            var source = FindLatestSource(prefix);

            if (source == null)
            {
                UnderwriteLog.Error($"{SOURCE_ROOT}/{prefix}N 아래에 layout.json이 있는 납품이 없습니다. "
                    + "Codex 납품이 끝났는지 확인하세요.");
                return;
            }

            UnderwriteLog.Info($"납품 폴더: {source}");
            var layoutPath = Path.Combine(source, LAYOUT_FILE);

            List<PartEntry> parts;
            float partScale;

            try
            {
                parts = ReadLayout(File.ReadAllText(layoutPath), out partScale);
            }
            catch (Exception e)
            {
                UnderwriteLog.Error($"배치표를 읽지 못했습니다: {e.Message}. layout.json의 형식을 요청서와 대조하세요.");
                return;
            }

            if (CopyArt(source, parts, partScale, allowOverwrite) == false)
            {
                return;
            }

            // 실제 영지는 사장님이 배치 툴로 옮긴 결과를 얹는다. 시범판에는 쓰지 않는다.
            if (prefix == FULL_PREFIX)
            {
                parts = ApplyOverride(parts, Path.GetFileName(source));
            }

            var prefab = VillageSceneSetup.EnsureCharacterPrefab();

            if (prefab == null)
            {
                UnderwriteLog.Error("캐릭터 프리팹을 만들지 못했습니다.");
                return;
            }

            var lightPrefab = VillageSceneSetup.EnsureLightPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var groundSize = BuildGround();

            if (groundSize == Vector2.zero)
            {
                return;
            }

            VillageSceneSetup.SetGroundSize(groundSize / PIXELS_PER_UNIT);
            VillageSceneSetup.BuildEdgeWalls();

            var partsRoot = new GameObject("Parts");
            var effectsRoot = new GameObject("Effects");
            Vector2? door = null;
            Vector2? well = null;
            var stalls = new List<Vector2>();
            var placed = 0;

            foreach (var part in parts)
            {
                BuildPart(part, groundSize, partsRoot.transform);

                foreach (var light in part.Lights)
                {
                    BuildLamp(ToWorld(light, groundSize), effectsRoot.transform);
                }

                foreach (var smoke in part.Smokes)
                {
                    BuildSmoke(ToWorld(smoke, groundSize), ToWorld(part.Foot, groundSize).y, effectsRoot.transform);
                }

                if (part.Door.HasValue == false)
                {
                    continue;
                }

                var at = ToWorld(part.Door.Value, groundSize);

                if (part.Place == PLACE_WELL)
                {
                    well = at;
                }
                else if (part.Place == PLACE_STALL)
                {
                    stalls.Add(at);
                }
                else if (BuildPlace(part.Place, at) == true)
                {
                    placed++;
                }
                else if (string.IsNullOrEmpty(part.Place) == true && door.HasValue == false)
                {
                    // 시범판은 장소 이름 없이 문 하나만 있다. 그것이 여관이다.
                    door = at;
                }
            }

            if (placed == 0 && door.HasValue == true)
            {
                VillageSceneSetup.BuildSpot("여관", door.Value, InfoSource.Source_TavernTalk, 20f,
                    VillageSceneSetup.QuestFor(InfoSource.Source_TavernTalk));
            }
            else if (placed == 0)
            {
                UnderwriteLog.Warning("배치표에 door 자리가 없어 장소 칸을 세우지 않았습니다.");
            }

            // 일하는 사람 그림이 있으면 장소마다 세우고, 없으면 옛 임시 주민을 쓴다.
            var doorsByName = new Dictionary<string, Vector2>();

            foreach (var spot in UnityEngine.Object.FindObjectsByType<Characters.VillageSpot>(FindObjectsInactive.Exclude))
            {
                doorsByName[spot.Name] = spot.transform.position;
            }

            var stationCount = StationNpcSetup.Build(doorsByName, stalls, pixel => ToWorld(pixel, groundSize));
            VillageNpcSetup.Build(prefab, groundSize / PIXELS_PER_UNIT, stalls.Count > 0 ? stalls : null, stationCount > 0);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Character";
            // 우물 광장이 있으면 그 앞에서, 없으면 문 앞에서 조금 아래에 세운다.
            // 바로 문 칸 안이면 시작하자마자 안내가 뜬다.
            Vector2 start;

            if (well.HasValue == true)
            {
                start = well.Value + START_BELOW_WELL;
            }
            else
            {
                start = door.HasValue == true ? door.Value + new Vector2(0f, -2.5f) : Vector2.zero;
            }
            player.transform.position = new Vector3(start.x, start.y, 0f);
            player.transform.localScale = Vector3.one * CHARACTER_SCALE;
            VillageSceneSetup.ShrinkToFootprint(player);

            // 그림자와 몸을 한 덩어리로 정렬해야 가로등 뒤에서 그림자만 앞에 남는 일이 없다.
            player.AddComponent<SortingGroup>();
            player.AddComponent<YSortRenderer>().Configure(Constants.PROTAGONIST_FOOT_OFFSET, false);
            player.AddComponent<CharacterActor>();

            VillageSceneSetup.BuildLight(lightPrefab);
            VillageSceneSetup.BuildCamera(player);
            VillageSceneSetup.BuildScreen();

            // 영지 시계에 맞춰 빛이 바뀌고 가로등이 켜진다.
            new GameObject("Day Night").AddComponent<DayNightCycle>();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuild(scenePath);
            AssetDatabase.Refresh();

            UnderwriteLog.Info($"영지 씬을 만들었습니다: {scenePath} (사물 {parts.Count}개, 장소 {placed}곳)");
        }

        // ── 장소 ────────────────────────────────

        private const string PLACE_WELL = "well";
        private const string PLACE_STALL = "stall";

        /// <summary>
        /// 배치표의 장소 이름으로 문 앞 칸을 세운다. 값·출처·일감은 기획서 §5의 여섯 곳 그대로다
        /// (지금 영지 VillageSceneSetup과 같다). 모르는 이름이면 거짓.
        /// </summary>
        private static bool BuildPlace(string place, Vector2 at)
        {
            switch (place)
            {
                case "inn":
                {
                    VillageSceneSetup.BuildSpot("여관", at, InfoSource.Source_TavernTalk, 20f,
                        VillageSceneSetup.QuestFor(InfoSource.Source_TavernTalk));
                    return true;
                }
                case "smith":
                {
                    VillageSceneSetup.BuildSpot("대장간", at, InfoSource.Source_SmithOrder, 40f,
                        VillageSceneSetup.QuestFor(InfoSource.Source_SmithOrder));
                    return true;
                }
                case "guild":
                {
                    VillageSceneSetup.BuildSpot("길드 기록실", at, InfoSource.Source_GuildRecord, 80f,
                        VillageSceneSetup.QuestFor(InfoSource.Source_GuildRecord));
                    return true;
                }
                case "manor":
                {
                    VillageSceneSetup.BuildSpot("영주관", at, InfoSource.Source_PrivateLetter, 120f);
                    return true;
                }
                case "board":
                {
                    VillageSceneSetup.BuildSpot("시장 게시판", at, InfoSource.Source_MarketBoard, 0f);
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        private static void AddToBuild(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ── 배치표 ──────────────────────────────

        private sealed class PartEntry
        {
            /// <summary>원본 배치표 parts의 순번. 배치 툴이 원본을 찾는 열쇠다.</summary>
            public int Source;

            public string File;
            public Vector2 Size;
            public Vector2 Foot;
            public Vector2 Pivot;
            public List<Vector2> Footprint = new List<Vector2>();
            public List<Vector2> Lights = new List<Vector2>();
            public List<Vector2> Smokes = new List<Vector2>();
            public Vector2? Door;
            public bool Sways;

            /// <summary>장소 이름(inn·manor·guild·smith·board·well·stall). 집과 소품은 비어 있다.</summary>
            public string Place;

            /// <summary>걸을 수 없게 막는 다각형들 — 건물 뒤편. 지붕 위로 사람 머리가 튀어나오지 않게.</summary>
            public List<List<Vector2>> Blocks = new List<List<Vector2>>();
        }

        /// <summary>
        /// 배치표를 읽는다. <paramref name="partScale"/>은 사물 PNG가 지도보다 몇 배로 그려졌는가
        /// (전체 지도는 1.7배 확대에 대비해 2배). 좌표는 늘 지도 1배 기준이다.
        /// </summary>
        private static List<PartEntry> ReadLayout(string json, out float partScale)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>;

            if (root == null || root.TryGetValue("parts", out var partsValue) == false)
            {
                throw new FormatException("layout.json에 parts 배열이 없습니다.");
            }

            partScale = root.TryGetValue("part_scale", out var scaleValue) == true && scaleValue != null
                ? Convert.ToSingle(scaleValue)
                : 1f;

            var list = partsValue as List<object>;

            if (list == null)
            {
                throw new FormatException("parts가 배열이 아닙니다.");
            }

            var result = new List<PartEntry>();

            foreach (var item in list)
            {
                var source = result.Count;
                var entry = item as Dictionary<string, object>;

                if (entry == null)
                {
                    throw new FormatException("parts 안에 객체가 아닌 항목이 있습니다.");
                }

                var part = new PartEntry
                {
                    Source = source,
                    File = (string)Require(entry, "file"),
                    Size = ToVector(Require(entry, "size")),
                    Foot = ToVector(Require(entry, "foot")),
                    Pivot = ToVector(Require(entry, "pivot_in_png"))
                };

                if (entry.TryGetValue("footprint", out var footprint) == true && footprint != null)
                {
                    part.Footprint = ToVectors(footprint);
                }

                if (entry.TryGetValue("place", out var place) == true && place != null)
                {
                    part.Place = (string)place;
                }

                if (entry.TryGetValue("block", out var blocks) == true && blocks is List<object> blockList)
                {
                    foreach (var polygon in blockList)
                    {
                        part.Blocks.Add(ToVectors(polygon));
                    }
                }

                if (entry.TryGetValue("anchors", out var anchorsValue) == true)
                {
                    var anchors = anchorsValue as Dictionary<string, object>;

                    if (anchors != null)
                    {
                        part.Lights = ReadAnchor(anchors, "light");
                        part.Smokes = ReadAnchor(anchors, "smoke");

                        var doors = ReadAnchor(anchors, "door");

                        if (doors.Count > 0)
                        {
                            part.Door = doors[0];
                        }

                        part.Sways = ReadAnchor(anchors, "sway").Count > 0;
                    }
                }

                result.Add(part);
            }

            return result;
        }

        /// <summary>
        /// 배치 툴의 덮어쓰기를 얹는다(<see cref="VillageLayoutOverride"/>). 덮어쓰기에 적힌 사물만, 적힌 발밑으로 다시 만든다.
        /// 없거나 다른 납품 판에서 만든 것이면 원본 그대로.
        /// </summary>
        private static List<PartEntry> ApplyOverride(List<PartEntry> parts, string sourceName)
        {
            var layout = VillageLayoutOverride.Load();

            if (layout == null)
            {
                return parts;
            }

            if (layout.Base != sourceName)
            {
                UnderwriteLog.Warning($"배치 덮어쓰기는 {layout.Base}에서 만든 것이라 {sourceName}에 쓰지 않습니다. "
                    + "배치 툴에서 다시 저장하세요.");
                return parts;
            }

            var result = new List<PartEntry>();

            foreach (var entry in layout.Parts)
            {
                if (entry.Source < 0 || entry.Source >= parts.Count || parts[entry.Source].File != entry.File)
                {
                    UnderwriteLog.Warning($"배치 덮어쓰기의 {entry.Source}번({entry.File})이 원본과 맞지 않아 뺍니다.");
                    continue;
                }

                result.Add(Moved(parts[entry.Source], new Vector2(entry.X, entry.Y)));
            }

            UnderwriteLog.Info($"배치 덮어쓰기를 얹었습니다: 원본 {parts.Count}개 → {result.Count}개");
            return result;
        }

        // 발밑을 옮기고, 딸린 좌표(문·불빛·연기·발 닿는 면·뒤편 막기)를 같은 만큼 옮긴 사본.
        private static PartEntry Moved(PartEntry original, Vector2 foot)
        {
            var delta = foot - original.Foot;
            var part = new PartEntry
            {
                Source = original.Source,
                File = original.File,
                Size = original.Size,
                Foot = foot,
                Pivot = original.Pivot,
                Sways = original.Sways,
                Place = original.Place,
                Footprint = Shifted(original.Footprint, delta),
                Lights = Shifted(original.Lights, delta),
                Smokes = Shifted(original.Smokes, delta)
            };

            if (original.Door.HasValue == true)
            {
                part.Door = original.Door.Value + delta;
            }

            foreach (var block in original.Blocks)
            {
                part.Blocks.Add(Shifted(block, delta));
            }

            return part;
        }

        private static List<Vector2> Shifted(List<Vector2> points, Vector2 delta)
        {
            var result = new List<Vector2>(points.Count);

            foreach (var point in points)
            {
                result.Add(point + delta);
            }

            return result;
        }

        /// <summary>월드 좌표를 지도 1배 픽셀로. <see cref="ToWorld"/>의 반대다. 배치 툴이 쓴다.</summary>
        public static Vector2 ToPixel(Vector2 world, Vector2 groundSize)
        {
            return new Vector2(
                world.x * PIXELS_PER_UNIT + groundSize.x * 0.5f,
                groundSize.y * 0.5f - world.y * PIXELS_PER_UNIT);
        }

        /// <summary>실제 영지가 쓰는 원본 납품 폴더 이름(예: VillagePartsFull_v2). 없으면 null.</summary>
        public static string FullSourceName()
        {
            var source = FindLatestSource(FULL_PREFIX);
            return source == null ? null : Path.GetFileName(source);
        }

        private static object Require(Dictionary<string, object> entry, string key)
        {
            if (entry.TryGetValue(key, out var value) == false || value == null)
            {
                throw new FormatException($"사물 항목에 '{key}'가 없습니다.");
            }

            return value;
        }

        /// <summary>앵커는 [x, y] 하나로 올 수도, [[x, y], ...] 목록으로 올 수도 있다. 둘 다 받는다.</summary>
        private static List<Vector2> ReadAnchor(Dictionary<string, object> anchors, string key)
        {
            if (anchors.TryGetValue(key, out var value) == false || value == null)
            {
                return new List<Vector2>();
            }

            var list = value as List<object>;

            if (list != null && list.Count > 0 && list[0] is List<object> == false)
            {
                return new List<Vector2> { ToVector(value) };
            }

            return ToVectors(value);
        }

        private static Vector2 ToVector(object value)
        {
            var list = value as List<object>;

            if (list == null || list.Count < 2)
            {
                throw new FormatException("좌표는 [x, y] 두 숫자여야 합니다.");
            }

            return new Vector2(Convert.ToSingle(list[0]), Convert.ToSingle(list[1]));
        }

        private static List<Vector2> ToVectors(object value)
        {
            var result = new List<Vector2>();
            var list = value as List<object>;

            if (list == null)
            {
                throw new FormatException("좌표 목록은 [[x, y], ...] 형태여야 합니다.");
            }

            foreach (var item in list)
            {
                result.Add(ToVector(item));
            }

            return result;
        }

        /// <summary>바닥판 픽셀(왼쪽 위 원점, 아래로 +)을 월드 좌표(가운데 원점, 위로 +)로 바꾼다.</summary>
        private static Vector2 ToWorld(Vector2 pixel, Vector2 groundSize)
        {
            return new Vector2(
                (pixel.x - groundSize.x * 0.5f) / PIXELS_PER_UNIT,
                (groundSize.y * 0.5f - pixel.y) / PIXELS_PER_UNIT);
        }

        // ── 그림 들이기 ─────────────────────────

        /// <summary>
        /// 납품 폴더의 PNG를 Assets로 옮긴다. 이미 있으면 덮어쓰기 메뉴로만 바꾼다.
        /// 다른 그림으로 바뀐 것을 모르고 덮으면 이미 맞춰 둔 피벗·씬 배치가 조용히 어긋난다.
        /// </summary>
        /// <summary>판 번호가 가장 높고 layout.json이 있는 납품 폴더. 없으면 null.</summary>
        private static string FindLatestSource(string prefix)
        {
            if (Directory.Exists(SOURCE_ROOT) == false)
            {
                return null;
            }

            string best = null;
            var bestVersion = -1;

            foreach (var folder in Directory.GetDirectories(SOURCE_ROOT, prefix + "*"))
            {
                var suffix = Path.GetFileName(folder).Substring(prefix.Length);

                if (int.TryParse(suffix, out var version) == false)
                {
                    continue;
                }

                if (version > bestVersion && File.Exists(Path.Combine(folder, LAYOUT_FILE)) == true)
                {
                    best = folder;
                    bestVersion = version;
                }
            }

            return best;
        }

        private static bool CopyArt(string source, List<PartEntry> parts, float partScale, bool allowOverwrite)
        {
            // 같은 그림(나무·화분 …)을 여러 자리에 쓴다. 파일은 한 번만 옮긴다.
            var files = new List<string> { GROUND_FILE };

            foreach (var part in parts)
            {
                if (files.Contains(part.File) == false)
                {
                    files.Add(part.File);
                }
            }

            var blocked = new List<string>();

            foreach (var file in files)
            {
                var from = Path.Combine(source, file);

                if (File.Exists(from) == false)
                {
                    UnderwriteLog.Error($"납품 폴더에 그림이 없습니다: {from}. layout.json의 file 이름과 실제 파일을 대조하세요.");
                    return false;
                }

                var to = Path.Combine(AssetFolder, file);

                if (File.Exists(to) == true && allowOverwrite == false && SameBytes(from, to) == false)
                {
                    blocked.Add(file);
                }
            }

            if (blocked.Count > 0)
            {
                UnderwriteLog.Error($"이미 다른 그림이 있어 멈췄습니다: {string.Join(", ", blocked)}. "
                    + "새 납품으로 바꾸려면 「영지 파츠 시범 씬 만들기 (그림 덮어쓰기)」를 쓰세요.");
                return false;
            }

            Directory.CreateDirectory(AssetFolder);

            foreach (var file in files)
            {
                File.Copy(Path.Combine(source, file), Path.Combine(AssetFolder, file), true);
            }

            AssetDatabase.Refresh();

            ConfigureSprite(AssetPath(GROUND_FILE), new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);

            var configured = new HashSet<string>();

            foreach (var part in parts)
            {
                if (configured.Add(part.File) == false)
                {
                    continue;
                }

                // PNG 안의 발밑 점을 피벗으로. PNG는 위가 0이고 Unity 피벗은 아래가 0이다.
                // 2배로 그린 사물은 PPU도 2배 — 화면에서는 같은 크기, 확대하면 더 선명하다.
                var pivot = new Vector2(part.Pivot.x / part.Size.x, 1f - part.Pivot.y / part.Size.y);
                ConfigureSprite(AssetPath(part.File), pivot, PIXELS_PER_UNIT * partScale);
            }

            return true;
        }

        private static bool SameBytes(string a, string b)
        {
            var left = File.ReadAllBytes(a);
            var right = File.ReadAllBytes(b);

            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string AssetPath(string file)
        {
            return AssetFolder + "/" + file;
        }

        private static void ConfigureSprite(string path, Vector2 pivot, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                UnderwriteLog.Error($"텍스처로 들어오지 않았습니다: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            // Multiple이면 Unity가 알파 덩어리마다 멋대로 잘라 낸다. 사물 하나는 스프라이트 하나다.
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        // ── 씬 구성 ─────────────────────────────

        private static Vector2 BuildGround()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(GROUND_FILE));

            if (sprite == null)
            {
                UnderwriteLog.Error($"바닥판을 불러오지 못했습니다: {AssetPath(GROUND_FILE)}");
                return Vector2.zero;
            }

            var go = new GameObject("Ground");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = Constants.VILLAGE_SORT_GROUND;
            go.transform.SetAsFirstSibling();

            return new Vector2(sprite.texture.width, sprite.texture.height);
        }

        private static void BuildPart(PartEntry part, Vector2 groundSize, Transform parent)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(part.File));

            if (sprite == null)
            {
                UnderwriteLog.Error($"사물 그림을 불러오지 못했습니다: {AssetPath(part.File)}");
                return;
            }

            var foot = ToWorld(part.Foot, groundSize);
            var go = new GameObject(Path.GetFileNameWithoutExtension(part.File));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(foot.x, foot.y, 0f);

            go.AddComponent<VillageLayoutTag>().Configure(part.Source, part.File, part.Place);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // 씬을 열자마자 맞는 순서로 보이게 미리 넣는다. 실행 중에는 YSortRenderer가 같은 값을 쓴다.
            renderer.sortingOrder = YSortRenderer.OrderFor(foot.y);
            go.AddComponent<YSortRenderer>().Configure(0f, true);

            if (part.Footprint.Count >= 3)
            {
                // 나무는 줄기만 막는다. 잎 밑은 걸어 들어간다(노점과 건물 사이가 좁다는 사용자 보고).
                var tree = part.Sways == true;
                var shrunk = Shrink(part.Footprint, tree == true ? TREE_SHRINK : FOOTPRINT_SHRINK_X,
                    tree == true ? TREE_SHRINK : FOOTPRINT_SHRINK_Y);
                var points = new Vector2[shrunk.Count];

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = ToWorld(shrunk[i], groundSize) - foot;
                }

                go.AddComponent<PolygonCollider2D>().points = points;
            }

            // 건물 뒤편은 따로 막는다. 발 닿는 면만 막으면 뒤로 돌아가 지붕 위에 선 것처럼 보인다.
            foreach (var block in part.Blocks)
            {
                if (block.Count < 3)
                {
                    continue;
                }

                var shrunk = Shrink(block, BLOCK_SHRINK_X, 1f);
                var points = new Vector2[shrunk.Count];

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = ToWorld(shrunk[i], groundSize) - foot;
                }

                go.AddComponent<PolygonCollider2D>().points = points;
            }

            if (part.Sways == true)
            {
                go.AddComponent<PartSway>();
                BuildFireflies(go.transform, renderer.sortingOrder);
            }

            BuildNightWindows(part, go.transform, sprite);

            if (part.File == SMITH_FILE)
            {
                // PNG 안 자리를 지도 1배로: 발밑 + (PNG 점 - 피벗) / 배율. PNG는 아래로 +.
                var scale = sprite.pixelsPerUnit / PIXELS_PER_UNIT;
                var mapPoint = part.Foot + (SMITH_FORGE_IN_PNG - part.Pivot) / scale;
                BuildForge(ToWorld(mapPoint, groundSize), foot.y, go.transform);
            }
        }

        /// <summary>
        /// 밤의 창문 불빛. Tools/village_window_glow.py가 만든 Night/{이름}_night.png가 있으면 건물 위에 같은 피벗으로 겹친다.
        /// </summary>
        private static void BuildNightWindows(PartEntry part, Transform parent, Sprite owner)
        {
            var path = AssetFolder + "/Night/" + Path.GetFileNameWithoutExtension(part.File) + "_night.png";

            if (File.Exists(path) == false)
            {
                return;
            }

            var pivot = new Vector2(part.Pivot.x / part.Size.x, 1f - part.Pivot.y / part.Size.y);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null || importer.textureType != TextureImporterType.Sprite
                || Mathf.Approximately(importer.spritePixelsPerUnit, owner.pixelsPerUnit) == false)
            {
                ConfigureSprite(path, pivot, owner.pixelsPerUnit);
            }

            var go = new GameObject("Night Windows");
            go.transform.SetParent(parent, false);
            var glow = go.AddComponent<SpriteRenderer>();
            glow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            // 불빛은 스스로 빛난다. 밤의 어두운 전역광에 먹히지 않게 Unlit.
            glow.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SPRITE_UNLIT_MATERIAL);
            glow.color = new Color(1f, 1f, 1f, 0f);
            go.AddComponent<NightGlow>();
        }

        /// <summary>나무 둘레의 반딧불이. 밤에만 천천히 떠다니며 깜빡인다.</summary>
        private static void BuildFireflies(Transform parent, int order)
        {
            var go = new GameObject("Fireflies");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 1f, 0.5f, 1f), new Color(1f, 0.95f, 0.55f, 1f));
            main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3.5f, 2.5f, 0.1f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.4f;

            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.2f, 0.45f),
                    new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            go.AddComponent<NightFireflies>();
            Finish(go, system, order + 2, 0f);
        }

        /// <summary>다각형을 가로·세로로 줄인다. 기준은 둘러싼 상자의 가운데.</summary>
        private static List<Vector2> Shrink(List<Vector2> polygon, float scaleX, float scaleY)
        {
            var min = polygon[0];
            var max = polygon[0];

            for (var i = 1; i < polygon.Count; i++)
            {
                min = Vector2.Min(min, polygon[i]);
                max = Vector2.Max(max, polygon[i]);
            }

            var center = (min + max) * 0.5f;
            var result = new List<Vector2>(polygon.Count);

            for (var i = 0; i < polygon.Count; i++)
            {
                var offset = polygon[i] - center;
                result.Add(center + new Vector2(offset.x * scaleX, offset.y * scaleY));
            }

            return result;
        }

        /// <summary>
        /// 대장간 화로의 불(2026-10-17 사용자 요청). 처음 판은 큰 원이 피어올라 "효과처럼" 보였다.
        /// 그래서 진짜 불처럼 둘로 나눈다 — 화로 안에서 낮게 일렁이는 작은 불꽃, 그리고 가끔 튀어 올라
        /// 중력에 휘며 꺼지는 불씨. 불빛은 2D 라이트로 은은하게 일렁인다.
        /// 그림에 불을 그리지 않은 것은 아트 규칙(불빛 번짐은 코드가 붙인다) 그대로다.
        /// </summary>
        private static void BuildForge(Vector2 at, float ownerFootY, Transform parent)
        {
            var go = new GameObject("Forge Fire");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = FORGE_COLOR;
            light.intensity = FORGE_LIGHT_INTENSITY;
            light.pointLightOuterRadius = FORGE_LIGHT_RADIUS;
            light.pointLightInnerRadius = 0f;
            go.AddComponent<LampFlicker>();

            var order = YSortRenderer.OrderFor(ownerFootY) + 1;
            BuildFlames(go.transform, order);
            BuildSparks(go.transform, order + 1);
        }

        // 화로 안의 낮은 불꽃. 작고 짧게, 거의 오르지 않는다 — 숯 위에서 혀처럼 날름거린다.
        private static void BuildFlames(Transform parent, int order)
        {
            var go = new GameObject("Flames");
            go.transform.SetParent(parent, false);

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.3f, 0.85f),
                new Color(1f, 0.45f, 0.12f, 0.85f));
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var emission = system.emission;
            emission.rateOverTime = 45f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.8f, 0.06f, 0.05f);
            // 상자는 기본으로 +Z를 향한다. 화면 위(+Y)로 피어오르게 눕힌다.
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.92f, 0.6f), 0f), new GradientColorKey(new Color(0.9f, 0.25f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            Finish(go, system, order, 0f);
        }

        // 튀는 불씨. 아주 작고 밝다. 위로 튀었다가 중력에 휘며 꺼지고, 움직이는 쪽으로 살짝 늘어진다.
        // 고르게 나오지 않고 가끔 몇 개씩 터진다 — 숯이 탁 튀는 느낌.
        private static void BuildSparks(Transform parent, int order)
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(parent, false);

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f, 1f),
                new Color(1f, 0.7f, 0.25f, 1f));
            main.gravityModifier = 0.35f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.rateOverTime = 4f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.4f, 3, 6, 1, 1.3f),
                new ParticleSystem.Burst(1.1f, 2, 4, 1, 2.1f)
            });

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 1.5f;

            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.97f, 0.8f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.5f),
                    new GradientColorKey(new Color(0.8f, 0.2f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            // 움직이는 쪽으로 늘린다 — 점이 아니라 짧은 선으로 보여야 불똥이다.
            Finish(go, system, order, 0.06f);
        }

        private static void Finish(GameObject go, ParticleSystem system, int order, float stretch)
        {
            var sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.SetSprite(0, EnsureSmokeSprite());

            // 불은 스스로 빛난다. 조명에 먹히지 않게 Unlit을 쓴다.
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SPRITE_UNLIT_MATERIAL);
            renderer.sortingOrder = order;

            if (stretch > 0f)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = stretch;
                renderer.lengthScale = 1f;
            }
        }

        private static void BuildLamp(Vector2 at, Transform parent)
        {
            var go = new GameObject("Lamp Light");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = LAMP_COLOR;
            light.intensity = LAMP_INTENSITY;
            light.pointLightOuterRadius = LAMP_RADIUS;
            light.pointLightInnerRadius = 0f;

            go.AddComponent<LampFlicker>();
        }

        /// <summary>
        /// 굴뚝 연기. 그림이 아니라 파티클이라 프레임마다 다시 그린 자글거림이 없다.
        /// 지붕 위에 보여야 하므로 그 건물 바로 앞 순서에 둔다.
        /// </summary>
        private static void BuildSmoke(Vector2 at, float ownerFootY, Transform parent)
        {
            var go = new GameObject("Chimney Smoke");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = 5f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startColor = new Color(0.93f, 0.92f, 0.9f, 0.55f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var emission = system.emission;
            emission.rateOverTime = 2.5f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.05f;
            // 원뿔은 기본으로 +Z를 향한다. 화면 위(+Y)로 피어오르게 눕힌다.
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // 빛이 왼쪽 위에서 오는 장면이므로 바람은 오른쪽으로 조금 분다.
            velocity.x = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));

            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.SetSprite(0, EnsureSmokeSprite());

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SPRITE_LIT_MATERIAL);
            renderer.sortingOrder = YSortRenderer.OrderFor(ownerFootY) + 1;
        }

        /// <summary>
        /// 연기 한 덩이. 가장자리가 부드러운 원을 코드로 만든다.
        /// 단순한 그라데이션이라 Codex에 맡길 일이 아니다.
        /// </summary>
        private static Sprite EnsureSmokeSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SMOKE_SPRITE);

            if (existing != null)
            {
                return existing;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    var alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(SMOKE_SPRITE));
            File.WriteAllBytes(SMOKE_SPRITE, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SMOKE_SPRITE);

            ConfigureSprite(SMOKE_SPRITE, new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
            return AssetDatabase.LoadAssetAtPath<Sprite>(SMOKE_SPRITE);
        }
    }
}
