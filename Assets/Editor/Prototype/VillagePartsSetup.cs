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
        private const string ASSET_FOLDER = "Assets/Images/Village/PartsPilot";
        private const string SCENE_PATH = "Assets/Scenes/VillagePartsPilot.unity";
        private const string LAYOUT_FILE = "layout.json";
        private const string GROUND_FILE = "ground.png";

        // 바닥 그림 72px가 1유닛이다. 카메라 높이 15유닛이 화면 1080px와 맞는다. v2 배경과 같다.
        private const float PIXELS_PER_UNIT = 72f;
        private const float CHARACTER_SCALE = 0.27f;

        // 낮이라 등불은 은은하게 둔다. 전역광 1.0 위에 더해지므로 세면 그림이 하얗게 뜬다.
        private const float LAMP_INTENSITY = 0.35f;
        private const float LAMP_RADIUS = 1.6f;
        private static readonly Color LAMP_COLOR = new Color(1f, 0.82f, 0.55f);

        private const string SMOKE_SPRITE = "Assets/Images/Village/Fx/smoke_puff.png";
        private const string SPRITE_LIT_MATERIAL =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        [MenuItem("Underwrite/영지 파츠 시범 씬 만들기")]
        public static void Build()
        {
            BuildScene(false);
        }

        [MenuItem("Underwrite/영지 파츠 시범 씬 만들기 (그림 덮어쓰기)")]
        public static void BuildWithOverwrite()
        {
            BuildScene(true);
        }

        private static void BuildScene(bool allowOverwrite)
        {
            var source = FindLatestSource();

            if (source == null)
            {
                UnderwriteLog.Error($"{SOURCE_ROOT}/{SOURCE_PREFIX}N 아래에 layout.json이 있는 납품이 없습니다. "
                    + "Codex 납품이 끝났는지 확인하세요.");
                return;
            }

            UnderwriteLog.Info($"납품 폴더: {source}");
            var layoutPath = Path.Combine(source, LAYOUT_FILE);

            List<PartEntry> parts;

            try
            {
                parts = ReadLayout(File.ReadAllText(layoutPath));
            }
            catch (Exception e)
            {
                UnderwriteLog.Error($"배치표를 읽지 못했습니다: {e.Message}. layout.json의 형식을 요청서와 대조하세요.");
                return;
            }

            if (CopyArt(source, parts, allowOverwrite) == false)
            {
                return;
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

                if (part.Door.HasValue == true && door.HasValue == false)
                {
                    door = ToWorld(part.Door.Value, groundSize);
                }
            }

            if (door.HasValue == true)
            {
                VillageSceneSetup.BuildSpot("여관", door.Value, InfoSource.Source_TavernTalk, 20f,
                    VillageSceneSetup.QuestFor(InfoSource.Source_TavernTalk));
            }
            else
            {
                UnderwriteLog.Warning("배치표에 door 자리가 없어 여관 칸을 세우지 않았습니다.");
            }

            VillageNpcSetup.Build(prefab, groundSize / PIXELS_PER_UNIT);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Character";
            // 문 앞에서 조금 아래에 세운다. 바로 문 칸 안이면 시작하자마자 안내가 뜬다.
            var start = door.HasValue == true ? door.Value + new Vector2(0f, -2.5f) : Vector2.zero;
            player.transform.position = new Vector3(start.x, start.y, 0f);
            player.transform.localScale = Vector3.one * CHARACTER_SCALE;
            VillageSceneSetup.ShrinkToFootprint(player);

            // 그림자와 몸을 한 덩어리로 정렬해야 가로등 뒤에서 그림자만 앞에 남는 일이 없다.
            player.AddComponent<SortingGroup>();
            player.AddComponent<YSortRenderer>().Configure(Constants.PROTAGONIST_FOOT_OFFSET, false);

            VillageSceneSetup.BuildLight(lightPrefab);
            VillageSceneSetup.BuildCamera(player);
            VillageSceneSetup.BuildScreen();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            UnderwriteLog.Info($"파츠 시범 씬을 만들었습니다: {SCENE_PATH} (사물 {parts.Count}개)");
        }

        // ── 배치표 ──────────────────────────────

        private sealed class PartEntry
        {
            public string File;
            public Vector2 Size;
            public Vector2 Foot;
            public Vector2 Pivot;
            public List<Vector2> Footprint = new List<Vector2>();
            public List<Vector2> Lights = new List<Vector2>();
            public List<Vector2> Smokes = new List<Vector2>();
            public Vector2? Door;
            public bool Sways;
        }

        private static List<PartEntry> ReadLayout(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>;

            if (root == null || root.TryGetValue("parts", out var partsValue) == false)
            {
                throw new FormatException("layout.json에 parts 배열이 없습니다.");
            }

            var list = partsValue as List<object>;

            if (list == null)
            {
                throw new FormatException("parts가 배열이 아닙니다.");
            }

            var result = new List<PartEntry>();

            foreach (var item in list)
            {
                var entry = item as Dictionary<string, object>;

                if (entry == null)
                {
                    throw new FormatException("parts 안에 객체가 아닌 항목이 있습니다.");
                }

                var part = new PartEntry
                {
                    File = (string)Require(entry, "file"),
                    Size = ToVector(Require(entry, "size")),
                    Foot = ToVector(Require(entry, "foot")),
                    Pivot = ToVector(Require(entry, "pivot_in_png"))
                };

                if (entry.TryGetValue("footprint", out var footprint) == true && footprint != null)
                {
                    part.Footprint = ToVectors(footprint);
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
        private static string FindLatestSource()
        {
            if (Directory.Exists(SOURCE_ROOT) == false)
            {
                return null;
            }

            string best = null;
            var bestVersion = -1;

            foreach (var folder in Directory.GetDirectories(SOURCE_ROOT, SOURCE_PREFIX + "*"))
            {
                var suffix = Path.GetFileName(folder).Substring(SOURCE_PREFIX.Length);

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

        private static bool CopyArt(string source, List<PartEntry> parts, bool allowOverwrite)
        {
            var files = new List<string> { GROUND_FILE };

            foreach (var part in parts)
            {
                files.Add(part.File);
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

                var to = Path.Combine(ASSET_FOLDER, file);

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

            Directory.CreateDirectory(ASSET_FOLDER);

            foreach (var file in files)
            {
                File.Copy(Path.Combine(source, file), Path.Combine(ASSET_FOLDER, file), true);
            }

            AssetDatabase.Refresh();

            ConfigureSprite(AssetPath(GROUND_FILE), new Vector2(0.5f, 0.5f));

            foreach (var part in parts)
            {
                // PNG 안의 발밑 점을 피벗으로. PNG는 위가 0이고 Unity 피벗은 아래가 0이다.
                var pivot = new Vector2(part.Pivot.x / part.Size.x, 1f - part.Pivot.y / part.Size.y);
                ConfigureSprite(AssetPath(part.File), pivot);
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
            return ASSET_FOLDER + "/" + file;
        }

        private static void ConfigureSprite(string path, Vector2 pivot)
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
            importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
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

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // 씬을 열자마자 맞는 순서로 보이게 미리 넣는다. 실행 중에는 YSortRenderer가 같은 값을 쓴다.
            renderer.sortingOrder = YSortRenderer.OrderFor(foot.y);
            go.AddComponent<YSortRenderer>().Configure(0f, true);

            if (part.Footprint.Count >= 3)
            {
                var points = new Vector2[part.Footprint.Count];

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = ToWorld(part.Footprint[i], groundSize) - foot;
                }

                go.AddComponent<PolygonCollider2D>().points = points;
            }

            if (part.Sways == true)
            {
                go.AddComponent<PartSway>();
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

            ConfigureSprite(SMOKE_SPRITE, new Vector2(0.5f, 0.5f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(SMOKE_SPRITE);
        }
    }
}
