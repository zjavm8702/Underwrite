using System;
using System.Collections.Generic;
using System.IO;
using Underwrite.Editor.Utility;
using Underwrite.Global;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Underwrite.Editor
{
    /// <summary>
    /// 지하 방 파츠(Codex 납품 `Outputs/UnderworldRoom_vN`)를 씬에 깐다. 영지 파츠와 같은 규격이다 —
    /// 바닥판 한 장 + 2배로 그린 사물 + layout.json(좌표는 지도 1배 px, 왼쪽 위 원점).
    /// 주식판 앞·정보상 자리·의자 자리·들어오는 자리를 돌려주면 <see cref="UnderworldSceneSetup"/>이 사람과 방 화면을 붙인다.
    /// </summary>
    public static class UnderworldRoomParts
    {
        private const string SOURCE_ROOT = "Outputs";
        private const string SOURCE_PREFIX = "UnderworldRoom_v";
        private const string ASSET_FOLDER = "Assets/Images/Underworld/Room";
        private const string LAYOUT_FILE = "layout.json";

        // 영지와 같다 — 지도 72px가 1유닛, 캐릭터 0.27배가 사람 135px.
        private const float PIXELS_PER_UNIT = 72f;

        // 지하는 어둡다. 전역 빛을 낮추고 테이블마다 작은 불을 둔다.
        private const float ROOM_GLOBAL_INTENSITY = 0.55f;
        private const float CANDLE_INTENSITY = 0.9f;
        private const float CANDLE_RADIUS = 2.6f;
        private static readonly Color CANDLE_COLOR = new Color(1f, 0.78f, 0.5f);

        // 주식판을 보는 자리. 판의 발밑(벽)에서 이만큼 앞(유닛).
        private const float BOARD_FRONT_OFFSET = 1.2f;

        // 납품에는 있지만 깔지 않는 사물(배치표 id 앞부분). 검수에서 반려됐고 빼도 방이 성립하는 것.
        // 비워 두면 모두 깐다.
        // 계단은 바닥 위에 얹힌 모형처럼 보여 뺀다(2026-10-18 사장님). 위층으로는 벽의 문(출구)에서 바로 올라간다.
        private static readonly string[] SKIP_PART_PREFIXES = { "stairs" };

        // 벽의 문(배치표 room_block의 틈 x 140~380, 벽 바닥 y 245) 앞 — 출구와 들어온 자리(지도 1배 픽셀).
        private static readonly Vector2 EXIT_DOOR_PX = new Vector2(260f, 280f);
        private static readonly Vector2 ENTER_PX = new Vector2(260f, 420f);
        // 벽 바닥(지도 픽셀 y). 이보다 아래에서 시작해 뺀 사물 안에 든 막이는 그 사물의 것이다(계단 옆벽).
        private const float WALL_BOTTOM_PX = 245f;

        /// <summary>방에 사람과 화면을 붙일 자리들(월드 좌표).</summary>
        public sealed class RoomPoints
        {
            public Vector2 Size;
            public Vector2 Spawn;
            public Vector2 BoardFront;
            public Vector2 Informant;
            public Vector2 Exit;
            public Vector2 BoardTextCenter;
            public Vector2 BoardTextSize;
            public readonly List<Vector2> Seats = new List<Vector2>();
        }

        /// <summary>판 번호가 가장 높고 layout.json이 있는 납품 폴더. 없으면 null — 그때는 임시 상자로 만든다.</summary>
        public static string FindDelivery()
        {
            if (Directory.Exists(SOURCE_ROOT) == false)
            {
                return null;
            }

            string best = null;
            var bestVersion = -1;

            foreach (var dir in Directory.GetDirectories(SOURCE_ROOT, SOURCE_PREFIX + "*"))
            {
                int version;

                if (int.TryParse(Path.GetFileName(dir).Substring(SOURCE_PREFIX.Length), out version) == false
                    || File.Exists(Path.Combine(dir, LAYOUT_FILE)) == false)
                {
                    continue;
                }

                if (version > bestVersion)
                {
                    bestVersion = version;
                    best = dir;
                }
            }

            return best;
        }

        /// <summary>바닥판·사물·벽 충돌·불을 깔고 자리들을 돌려준다. 실패하면 null.</summary>
        public static RoomPoints Build(string folder)
        {
            var root = MiniJson.Parse(File.ReadAllText(Path.Combine(folder, LAYOUT_FILE))) as Dictionary<string, object>;

            if (root == null)
            {
                UnderwriteLog.Error("지하 방 배치표를 읽지 못했습니다. layout.json 형식을 요청서와 대조하세요.");
                return null;
            }

            var partScale = root.TryGetValue("part_scale", out var scaleValue) == true ? Convert.ToSingle(scaleValue) : 1f;
            var plate = (Dictionary<string, object>)root["room_plate"];
            var plateFile = (string)plate["file"];
            var size = ToVector(plate["size"]);

            Directory.CreateDirectory(ASSET_FOLDER);
            Import(folder, plateFile);
            Configure(plateFile, new Vector2(0f, 1f), PIXELS_PER_UNIT);

            var parts = (List<object>)root["parts"];
            var byId = new Dictionary<string, Dictionary<string, object>>();

            foreach (var item in parts)
            {
                var part = (Dictionary<string, object>)item;
                byId[(string)part["id"]] = part;

                var file = (string)part["file"];

                // 같은 그림을 여러 사물이 함께 쓴다 — 한 번만 들인다. 단 새 판에서 그림이 바뀌었으면 덮는다
                // (v4 계단이 v3와 같은 파일 이름이라 옛 그림이 남았다, 2026-10-18).
                if (IsSameFile(Path.Combine(folder, file), Path.Combine(ASSET_FOLDER, file)) == false)
                {
                    Import(folder, file);
                }

                var partSize = ToVector(part["size"]);
                var pivot = ToVector(part["pivot_in_png"]);
                Configure(file, new Vector2(pivot.x / partSize.x, 1f - pivot.y / partSize.y), PIXELS_PER_UNIT * partScale);
            }

            AssetDatabase.Refresh();

            var holder = new GameObject("Room Parts");
            BuildPlate(plateFile, size);
            BuildWalls(root, size, holder.transform, SkippedRects(parts));

            var points = new RoomPoints { Size = size / PIXELS_PER_UNIT };

            foreach (var item in parts)
            {
                var part = (Dictionary<string, object>)item;

                if (IsSkipped((string)part["id"]) == true)
                {
                    continue;
                }

                BuildPart(part, byId, size, holder.transform, points);
            }

            if (root.TryGetValue("anchors", out var anchorsValue) == true && anchorsValue is Dictionary<string, object> anchors)
            {
                if (anchors.TryGetValue("spawn", out var spawn) == true)
                {
                    points.Spawn = ToWorld(FirstPoint(spawn), size);
                }

                // 계단을 빼서 문 앞에서 들어오고 나간다.
                points.Exit = ToWorld(EXIT_DOOR_PX, size);
                points.Spawn = ToWorld(ENTER_PX, size);

                if (anchors.TryGetValue("informant", out var informant) == true)
                {
                    points.Informant = ToWorld(FirstPoint(informant), size);
                }
            }

            return points;
        }

        private static bool IsSkipped(string id)
        {
            for (var i = 0; i < SKIP_PART_PREFIXES.Length; i++)
            {
                if (id.StartsWith(SKIP_PART_PREFIXES[i], StringComparison.Ordinal) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>지하의 어둠. 방을 다 깐 뒤 전역 빛을 낮춘다.</summary>
        public static void DimGlobalLight()
        {
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    light.intensity = ROOM_GLOBAL_INTENSITY;
                }
            }
        }

        private static void BuildPlate(string file, Vector2 size)
        {
            var go = new GameObject("Room Plate");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(file));
            renderer.sortingOrder = Constants.VILLAGE_SORT_GROUND;
            // 피벗이 왼쪽 위다. 지도 가운데가 월드 원점이 되게 놓는다.
            go.transform.position = new Vector3(-size.x * 0.5f / PIXELS_PER_UNIT, size.y * 0.5f / PIXELS_PER_UNIT, 0f);
            go.transform.SetAsFirstSibling();
        }

        // 벽(위·좌우 테두리). 배치표의 room_block 다각형을 막는다.
        // 뺀 사물이 차지하던 지도 영역(픽셀, 위 원점).
        private static List<Rect> SkippedRects(List<object> parts)
        {
            var rects = new List<Rect>();

            foreach (var item in parts)
            {
                var part = (Dictionary<string, object>)item;

                if (IsSkipped((string)part["id"]) == false || part.ContainsKey("foot") == false)
                {
                    continue;
                }

                var foot = ToVector(part["foot"]);
                var pivot = ToVector(part["pivot_in_png"]);
                var partSize = ToVector(part["size"]);
                rects.Add(new Rect(foot - pivot, partSize));
            }

            return rects;
        }

        private static bool IsInsideSkipped(List<Vector2> polygon, List<Rect> skipped)
        {
            var min = polygon[0];
            var max = polygon[0];

            for (var i = 1; i < polygon.Count; i++)
            {
                min = Vector2.Min(min, polygon[i]);
                max = Vector2.Max(max, polygon[i]);
            }

            if (min.y < WALL_BOTTOM_PX)
            {
                return false;
            }

            for (var i = 0; i < skipped.Count; i++)
            {
                if (skipped[i].Contains(min) == true && skipped[i].Contains(max) == true)
                {
                    return true;
                }
            }

            return false;
        }

        private static void BuildWalls(Dictionary<string, object> root, Vector2 size, Transform parent, List<Rect> skipped)
        {
            if (root.TryGetValue("room_block", out var blocks) == false || blocks is List<object> list == false)
            {
                return;
            }

            var walls = new GameObject("Room Walls");
            walls.transform.SetParent(parent);

            foreach (var polygon in list)
            {
                var points = ToVectors(polygon);

                // 계단을 뺐으면 그 옆벽 막이도 뺀다 — 보이지 않는 벽이 남는다(2026-10-19 사장님).
                if (IsInsideSkipped(points, skipped) == true)
                {
                    continue;
                }

                var world = new Vector2[points.Count];

                for (var i = 0; i < world.Length; i++)
                {
                    world[i] = ToWorld(points[i], size);
                }

                walls.AddComponent<PolygonCollider2D>().points = world;
            }
        }

        private static void BuildPart(Dictionary<string, object> part, Dictionary<string, Dictionary<string, object>> byId,
            Vector2 size, Transform parent, RoomPoints points)
        {
            var file = (string)part["file"];
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(file));

            if (sprite == null)
            {
                UnderwriteLog.Error($"지하 방 사물 그림을 불러오지 못했습니다: {file}");
                return;
            }

            var id = (string)part["id"];
            var foot = ToWorld(ToVector(part["foot"]), size);
            var go = new GameObject(id);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(foot.x, foot.y, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            // 테이블 위 촛대는 테이블의 발밑으로 줄을 선다 — 상판 위에 있으니 테이블보다 한 칸 앞.
            var sortY = foot.y;

            if (part.TryGetValue("parent", out var parentId) == true && parentId is string owner
                && byId.TryGetValue(owner, out var ownerPart) == true)
            {
                sortY = ToWorld(ToVector(ownerPart["foot"]), size).y;
            }

            if (id == "stock_board")
            {
                // 벽에 걸린 판은 늘 사람 뒤다.
                renderer.sortingOrder = Constants.VILLAGE_SORT_GROUND + 1;
            }
            else
            {
                renderer.sortingOrder = YSortRenderer.OrderFor(sortY) + (sortY != foot.y ? 1 : 0);
            }

            AddPolygon(go, part, "footprint", foot, size);

            if (part.TryGetValue("block", out var blocks) == true && blocks is List<object> blockList)
            {
                foreach (var polygon in blockList)
                {
                    AddPolygon(go, ToVectors(polygon), foot, size);
                }
            }

            if (part.TryGetValue("anchors", out var anchorsValue) == false || anchorsValue is Dictionary<string, object> anchors == false)
            {
                return;
            }

            if (anchors.TryGetValue("light", out var light) == true)
            {
                AddCandle(go.transform, ToWorld(FirstPoint(light), size));
            }

            if (anchors.TryGetValue("seat_ground", out var seats) == true)
            {
                foreach (var seat in ToVectors(seats))
                {
                    points.Seats.Add(ToWorld(seat, size));
                }
            }

            if (id == "stock_board")
            {
                points.BoardFront = new Vector2(foot.x, foot.y - BOARD_FRONT_OFFSET);

                // 글자 칸: [x, y, w, h] (지도 1배, 왼쪽 위). 칠판 격자의 칸마다 하나씩 온다 — 모두를 감싸는 사각형을 쓴다.
                if (anchors.TryGetValue("board_text", out var textValue) == true && textValue is List<object> rects
                    && rects.Count > 0)
                {
                    var cells = rects[0] is List<object> ? rects : new List<object> { rects };
                    var min = new Vector2(float.MaxValue, float.MaxValue);
                    var max = new Vector2(float.MinValue, float.MinValue);

                    foreach (var cell in cells)
                    {
                        var rect = (List<object>)cell;
                        var x = Convert.ToSingle(rect[0]);
                        var y = Convert.ToSingle(rect[1]);
                        min = Vector2.Min(min, new Vector2(x, y));
                        max = Vector2.Max(max, new Vector2(x + Convert.ToSingle(rect[2]), y + Convert.ToSingle(rect[3])));
                    }

                    points.BoardTextCenter = ToWorld((min + max) * 0.5f, size);
                    points.BoardTextSize = (max - min) / PIXELS_PER_UNIT;
                }
            }
        }

        private static void AddPolygon(GameObject go, Dictionary<string, object> part, string key, Vector2 foot, Vector2 size)
        {
            if (part.TryGetValue(key, out var value) == true && value != null)
            {
                AddPolygon(go, ToVectors(value), foot, size);
            }
        }

        private static void AddPolygon(GameObject go, List<Vector2> polygon, Vector2 foot, Vector2 size)
        {
            if (polygon.Count < 3)
            {
                return;
            }

            var points = new Vector2[polygon.Count];

            for (var i = 0; i < points.Length; i++)
            {
                points[i] = ToWorld(polygon[i], size) - foot;
            }

            go.AddComponent<PolygonCollider2D>().points = points;
        }

        private static void AddCandle(Transform parent, Vector2 at)
        {
            var go = new GameObject("Candle Light");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = CANDLE_COLOR;
            light.intensity = CANDLE_INTENSITY;
            light.pointLightOuterRadius = CANDLE_RADIUS;
            light.pointLightInnerRadius = 0f;
            go.AddComponent<LampFlicker>();
        }

        private static bool IsSameFile(string source, string target)
        {
            if (File.Exists(target) == false)
            {
                return false;
            }

            var a = File.ReadAllBytes(source);
            var b = File.ReadAllBytes(target);

            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Import(string folder, string file)
        {
            File.Copy(Path.Combine(folder, file), Path.Combine(ASSET_FOLDER, file), true);
        }

        private static void Configure(string file, Vector2 pivot, float pixelsPerUnit)
        {
            var path = AssetPath(file);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                UnderwriteLog.Error($"텍스처로 들어오지 않았습니다: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
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

        private static string AssetPath(string file)
        {
            return ASSET_FOLDER + "/" + file;
        }

        private static Vector2 ToWorld(Vector2 pixel, Vector2 size)
        {
            return new Vector2((pixel.x - size.x * 0.5f) / PIXELS_PER_UNIT, (size.y * 0.5f - pixel.y) / PIXELS_PER_UNIT);
        }

        // 앵커는 [x, y] 하나로도, [[x, y], ...] 목록으로도 온다. 첫 점을 쓴다.
        private static Vector2 FirstPoint(object value)
        {
            var list = (List<object>)value;
            return list.Count > 0 && list[0] is List<object> ? ToVector(list[0]) : ToVector(value);
        }

        private static Vector2 ToVector(object value)
        {
            var list = (List<object>)value;
            return new Vector2(Convert.ToSingle(list[0]), Convert.ToSingle(list[1]));
        }

        private static List<Vector2> ToVectors(object value)
        {
            var result = new List<Vector2>();

            foreach (var item in (List<object>)value)
            {
                result.Add(ToVector(item));
            }

            return result;
        }
    }
}
