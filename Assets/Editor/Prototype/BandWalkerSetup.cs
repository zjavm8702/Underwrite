using System.Collections.Generic;
using System.IO;
using Underwrite.Editor.Utility;
using Underwrite.Global;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Underwrite.Editor
{
    /// <summary>
    /// Codex NPC 2차(Outputs/NpcField_v2) — 걷는 용병단 단원 네 유형을 들인다(2026-10-17).
    /// 다섯 방향 × 대기·걷기, 프레임마다 단 색 마스크. 왼쪽은 코드가 뒤집는다(DirectionalSprites).
    /// </summary>
    public static class BandWalkerSetup
    {
        private const string SOURCE = "Outputs/NpcField_v2";
        private const string ASSET_FOLDER = "Assets/Images/Characters/Band";
        private const string MULTIPLY_SHADER = "Assets/Shaders/SpriteMultiply.shader";
        private const string MULTIPLY_MATERIAL = "Assets/Images/Characters/Band/BandCloth.mat";
        private const float PIXELS_PER_UNIT = 100f;
        private const float CHARACTER_SCALE = 0.27f;

        // 단원 자리 순서: 앞사람부터 단장·궁수·방패병·짐꾼(NpcParty의 이름과 같다).
        public static readonly string[] TYPES = { "leader", "archer", "shield", "porter" };

        private static Dictionary<string, object> Characters;
        private static Material ClothMaterial;

        public static bool Available
        {
            get
            {
                return File.Exists(Path.Combine(SOURCE, "timing.json"));
            }
        }

        /// <summary>그림을 들인다. 영지 씬을 만들 때 한 번 부른다. 납품이 없으면 거짓.</summary>
        public static bool Import()
        {
            if (Available == false)
            {
                return false;
            }

            var timing = MiniJson.Parse(File.ReadAllText(Path.Combine(SOURCE, "timing.json"))) as Dictionary<string, object>;
            Characters = timing["characters"] as Dictionary<string, object>;
            var paths = new List<KeyValuePair<string, Vector2>>();

            foreach (var character in Characters.Values)
            {
                foreach (var pair in (Dictionary<string, object>)character)
                {
                    var pivot = pair.Key == "Walking" ? new Vector2(320f / 640f, 13f / 724f) : new Vector2(181f / 362f, 13f / 724f);
                    var directions = (Dictionary<string, object>)((Dictionary<string, object>)pair.Value)["directions"];

                    foreach (var direction in directions.Values)
                    {
                        foreach (var item in (List<object>)((Dictionary<string, object>)direction)["frames"])
                        {
                            var frame = (Dictionary<string, object>)item;

                            foreach (var key in new[] { "file", "mask" })
                            {
                                var file = (string)frame[key];
                                var to = ASSET_FOLDER + "/" + file;
                                Directory.CreateDirectory(Path.GetDirectoryName(to));
                                File.Copy(Path.Combine(SOURCE, file), to, true);
                                paths.Add(new KeyValuePair<string, Vector2>(to, pivot));
                            }
                        }
                    }
                }
            }

            AssetDatabase.Refresh();

            foreach (var pair in paths)
            {
                Configure(pair.Key, pair.Value);
            }

            ClothMaterial = AssetDatabase.LoadAssetAtPath<Material>(MULTIPLY_MATERIAL);

            if (ClothMaterial == null)
            {
                ClothMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>(MULTIPLY_SHADER));
                AssetDatabase.CreateAsset(ClothMaterial, MULTIPLY_MATERIAL);
            }

            UnderwriteLog.Info($"용병단 단원 그림 {paths.Count}장을 들였습니다(NpcField_v2).");
            return true;
        }

        /// <summary>단원 하나. NpcWalker·CharacterActor가 붙은 걷는 사람을 만든다.</summary>
        public static GameObject Create(string type, Transform parent, Vector2 foot, float speedScale)
        {
            var go = new GameObject("NPC");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(foot.x, foot.y, 0f);
            go.transform.localScale = Vector3.one * CHARACTER_SCALE;

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            var bodyRenderer = body.AddComponent<SpriteRenderer>();

            var cloth = new GameObject("Cloth");
            cloth.transform.SetParent(body.transform, false);
            var clothRenderer = cloth.AddComponent<SpriteRenderer>();
            clothRenderer.sharedMaterial = ClothMaterial;
            // 몸(외곽선이 붙으면 1) 위에 곱한다.
            clothRenderer.sortingOrder = 2;

            var actions = (Dictionary<string, object>)Characters[type];
            var frames = go.AddComponent<DirectionalSprites>();
            frames.Configure(bodyRenderer, clothRenderer, Clips(actions["Idle"]), Clips(actions["Walking"]));

            var walker = go.AddComponent<NpcWalker>();
            walker.Configure(0f, speedScale);
            walker.PlaceFoot(foot);

            go.AddComponent<CharacterActor>();
            go.AddComponent<SortingGroup>();
            go.AddComponent<YSortRenderer>().Configure(0f, false);
            return go;
        }

        private static List<DirectionalClip> Clips(object action)
        {
            var list = new List<DirectionalClip>();
            var directions = (Dictionary<string, object>)((Dictionary<string, object>)action)["directions"];

            foreach (var pair in directions)
            {
                var clip = new DirectionalClip { Direction = pair.Key };

                foreach (var item in (List<object>)((Dictionary<string, object>)pair.Value)["frames"])
                {
                    var frame = (Dictionary<string, object>)item;
                    clip.Frames.Add(AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_FOLDER + "/" + (string)frame["file"]));
                    clip.Masks.Add(AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_FOLDER + "/" + (string)frame["mask"]));
                    clip.Milliseconds.Add(System.Convert.ToInt32(frame["ms"]));
                }

                list.Add(clip);
            }

            return list;
        }

        private static void Configure(string path, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
