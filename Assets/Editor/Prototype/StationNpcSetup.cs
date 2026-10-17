using System.Collections.Generic;
using System.IO;
using Underwrite.Editor.Utility;
using Underwrite.Enumeration;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Underwrite.Editor
{
    /// <summary>
    /// Codex NPC 1차(Outputs/NpcField_v1)를 들여 영지 자리에 세운다(2026-10-17).
    /// 서서 일하는 열 명 — 정면 한 방향, 대기·일·건네기. 프레임과 시간은 timing.json에서 읽는다.
    /// </summary>
    public static class StationNpcSetup
    {
        private const string SOURCE = "Outputs/NpcField_v1";
        private const string ASSET_FOLDER = "Assets/Images/Characters/Npc";
        private const float PIXELS_PER_UNIT = 100f;
        private const float CHARACTER_SCALE = 0.27f;
        private static readonly Vector2 PIVOT = new Vector2(181f / 362f, 13f / 724f);
        private static readonly string[] ACTIONS = { "Idle", "Work", "Give" };

        private sealed class Cast
        {
            public string Id;
            public string Name;
            public string Place;
            public NpcRole Role;

            // 그 자리 문 앞에서의 거리(지도 1배 px). 노점 장수는 노점 앞(stall 순번)에 선다.
            public Vector2 FromDoor;
            public int Stall = -1;
            public string[] Lines;
        }

        private static readonly Cast[] CASTS =
        {
            new Cast { Id = "smith", Name = "대장장이", Place = "대장간", Role = NpcRole.Role_Resident, FromDoor = new Vector2(-19f, 1f),
                Lines = new[] { "쇠는 뜨거울 때 쳐야지.", "주문은 저쪽에 말하게." } },
            new Cast { Id = "apprentice", Name = "견습 대장장이", Place = "대장간", Role = NpcRole.Role_Resident, FromDoor = new Vector2(147f, 3f),
                Lines = new[] { "날 세우는 건 아직 사부님 몫이에요.", "숫돌 소리 시끄럽죠?" } },
            new Cast { Id = "innkeeper", Name = "여관 주인", Place = "여관", Role = NpcRole.Role_Resident, FromDoor = new Vector2(-125f, 10f),
                Lines = new[] { "자리 비었으면 앉아.", "오늘 스튜는 괜찮아." } },
            new Cast { Id = "archivist", Name = "기록 담당", Place = "길드 기록실", Role = NpcRole.Role_Resident, FromDoor = new Vector2(-125f, 10f),
                Lines = new[] { "기록은 정직하오.", "의뢰 접수는 안에서 하시오." } },
            new Cast { Id = "steward", Name = "영주관 집사", Place = "영주관", Role = NpcRole.Role_Resident, FromDoor = new Vector2(-135f, 10f),
                Lines = new[] { "영주님은 바쁘십니다.", "시찰 준비로 정신이 없군요." } },
            new Cast { Id = "guard", Name = "영주관 경비", Place = "영주관", Role = NpcRole.Role_Resident, FromDoor = new Vector2(135f, 10f),
                Lines = new[] { "볼일 없으면 지나가시오.", "…하암." } },
            new Cast { Id = "broker", Name = "게시판 앞 중개인", Place = "시장 게시판", Role = NpcRole.Role_Resident, FromDoor = new Vector2(125f, 10f),
                Lines = new[] { "새 벽보가 붙었어.", "저 단, 또 의뢰를 맡았대." } },
            new Cast { Id = "vendor_fruit", Name = "과일 장수", Place = "노점", Role = NpcRole.Role_Vendor, Stall = 0,
                Lines = new[] { "사과 사시오, 사과!", "원정길엔 말린 과일이 좋소." } },
            new Cast { Id = "vendor_cloth", Name = "천 장수", Place = "노점", Role = NpcRole.Role_Vendor, Stall = 1,
                Lines = new[] { "북쪽에서 온 천이오.", "붕대 감으로도 좋소." } },
            new Cast { Id = "vendor_goods", Name = "잡화 장수", Place = "노점", Role = NpcRole.Role_Vendor, Stall = 2,
                Lines = new[] { "밧줄, 횃불, 부싯돌. 다 있소.", "깎아 드릴 수는 없소." } }
        };

        /// <summary>그림이 있으면 들여 세운 사람 수, 납품이 없으면 0(그러면 옛 임시 주민을 쓴다).</summary>
        public static int Build(Dictionary<string, Vector2> doors, List<Vector2> stalls, System.Func<Vector2, Vector2> mapToWorld)
        {
            var timingPath = Path.Combine(SOURCE, "timing.json");

            if (File.Exists(timingPath) == false)
            {
                return 0;
            }

            var timing = MiniJson.Parse(File.ReadAllText(timingPath)) as Dictionary<string, object>;
            var characters = timing["characters"] as Dictionary<string, object>;
            ImportFrames(characters);

            var root = new GameObject("Station NPCs");
            var count = 0;

            foreach (var cast in CASTS)
            {
                Vector2 at;

                if (cast.Stall >= 0)
                {
                    if (stalls == null || cast.Stall >= stalls.Count)
                    {
                        continue;
                    }

                    at = stalls[cast.Stall];
                }
                else if (doors.TryGetValue(cast.Place, out var door) == true)
                {
                    // 문 앞 칸(월드) + 지도 px 거리. 지도는 아래로 +, 월드는 위로 +.
                    at = door + mapToWorld(cast.FromDoor) - mapToWorld(Vector2.zero);
                }
                else
                {
                    continue;
                }

                BuildOne(cast, characters[cast.Id] as Dictionary<string, object>, at, root.transform);
                count++;
            }

            UnderwriteLog.Info($"영지에 일하는 사람 {count}명을 세웠습니다(NpcField_v1).");
            return count;
        }

        private static void BuildOne(Cast cast, Dictionary<string, object> actions, Vector2 at, Transform parent)
        {
            var go = new GameObject(cast.Name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.transform.localScale = Vector3.one * CHARACTER_SCALE;

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();

            var clips = new StationClip[ACTIONS.Length];

            for (var a = 0; a < ACTIONS.Length; a++)
            {
                clips[a] = LoadClip(cast.Id, actions[ACTIONS[a]] as Dictionary<string, object>);
            }

            renderer.sprite = clips[1].Frames.Count > 0 ? clips[1].Frames[0] : null;

            FootShadow.Add(go.transform);
            go.AddComponent<SortingGroup>();
            go.AddComponent<YSortRenderer>().Configure(0f, true);
            go.AddComponent<StationNpc>().Configure(cast.Place, renderer, clips[0], clips[1], clips[2]);
            go.AddComponent<VillageNpc>().Configure(cast.Role, cast.Name, cast.Place, new List<string>(cast.Lines));
            go.AddComponent<CharacterActor>();

            // 발밑만 막는다. 일하는 사람을 뚫고 지나가지 않게.
            var foot = go.AddComponent<BoxCollider2D>();
            foot.size = new Vector2(0.5f, 0.25f) / CHARACTER_SCALE;
            foot.offset = new Vector2(0f, 0.1f) / CHARACTER_SCALE;
        }

        private static StationClip LoadClip(string id, Dictionary<string, object> action)
        {
            var clip = new StationClip();

            foreach (var item in action["frames"] as List<object>)
            {
                var frame = item as Dictionary<string, object>;
                var file = (string)frame["file"];
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_FOLDER + "/" + file);

                if (sprite == null)
                {
                    UnderwriteLog.Warning($"NPC 프레임이 없습니다: {file}");
                    continue;
                }

                clip.Frames.Add(sprite);
                clip.Milliseconds.Add(System.Convert.ToInt32(frame["ms"]));
            }

            return clip;
        }

        // 납품 프레임을 Assets로 옮기고 주인공과 같은 규격(PPU 100, 발밑 피벗)으로 들인다.
        private static void ImportFrames(Dictionary<string, object> characters)
        {
            var paths = new List<string>();

            foreach (var character in characters.Values)
            {
                foreach (var action in (character as Dictionary<string, object>).Values)
                {
                    foreach (var item in (action as Dictionary<string, object>)["frames"] as List<object>)
                    {
                        var file = (string)(item as Dictionary<string, object>)["file"];
                        var to = ASSET_FOLDER + "/" + file;
                        Directory.CreateDirectory(Path.GetDirectoryName(to));
                        File.Copy(Path.Combine(SOURCE, file), to, true);
                        paths.Add(to);
                    }
                }
            }

            AssetDatabase.Refresh();

            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = PIVOT;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}
