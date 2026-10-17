using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Underwrite.Editor
{
    /// <summary>
    /// 영지에 사람을 세운다. 사용자 기준(2026-10-17):
    /// 건물마다 주민 1~2명, 노점마다 장수 1명, 모험단 단위로 돌아다니는 파티 3개 이상.
    /// **최대한 많은 사람이 보이게** 하는 것이 목적이다.
    ///
    /// NPC 그림이 아직 없어 주인공 프리팹에 색을 입혀 임시로 쓴다.
    /// Codex NPC가 들어오면 <see cref="CreateBody"/>만 그 프리팹으로 바꾸면 된다.
    /// </summary>
    public static class VillageNpcSetup
    {
        private const float CHARACTER_SCALE = 0.27f;

        // 주민은 문 앞 자리에서 옆으로 비켜 선다. 문 앞을 막으면 플레이어가 들어갈 수 없다.
        private const float RESIDENT_SIDE_OFFSET = 1.6f;

        private static readonly Color RESIDENT_TINT = new Color(0.95f, 0.88f, 0.8f);
        private static readonly Color VENDOR_TINT = new Color(1f, 0.93f, 0.7f);

        private sealed class PlaceCast
        {
            public string Place;
            public string[] Names;
            public string[][] Lines;
        }

        private sealed class PartyCast
        {
            public string Band;
            public string StockId;
            public Color Tint;
            public string[] Names;
            public string[] Lines;
        }

        // 장소별 주민. 이름 수만큼 세운다(1~2명).
        private static readonly PlaceCast[] RESIDENTS =
        {
            new PlaceCast
            {
                Place = "여관",
                Names = new[] { "여관 손님", "술꾼" },
                Lines = new[]
                {
                    new[] { "여기 스튜는 영지에서 제일이오.", "요즘 외지 용병들이 부쩍 늘었어." },
                    new[] { "한 잔 사 주면 재밌는 얘기 해 주지.", "어젯밤엔 누가 금화를 뿌리고 갔다더군." }
                }
            },
            new PlaceCast
            {
                Place = "대장간",
                Names = new[] { "견습 대장장이" },
                Lines = new[]
                {
                    new[] { "망치질 소리 시끄럽죠? 요즘 주문이 밀려서요.", "칼날 세우는 건 사부님 몫이에요." }
                }
            },
            new PlaceCast
            {
                Place = "길드 기록실",
                Names = new[] { "길드 서기", "의뢰를 고르는 모험가" },
                Lines = new[]
                {
                    new[] { "의뢰 접수는 안쪽 창구에서 받습니다.", "기록은 정직합니다. 소문과 달리요." },
                    new[] { "북쪽 동굴 건은 보수가 짜.", "이번엔 쉬운 걸로 고를 거야." }
                }
            },
            new PlaceCast
            {
                Place = "영주관",
                Names = new[] { "영주관 경비", "심부름꾼" },
                Lines = new[]
                {
                    new[] { "볼일 없으면 지나가시오.", "영주님 시찰이 머지않았소." },
                    new[] { "서신 나르느라 다리가 남아나질 않네요." }
                }
            },
            new PlaceCast
            {
                Place = "시장 게시판",
                Names = new[] { "게시판 앞 구경꾼", "벽보 붙이는 사람" },
                Lines = new[]
                {
                    new[] { "새로 붙은 건 없나…", "저 단은 또 의뢰를 맡았네." },
                    new[] { "풀 마르기 전에 붙여야 해서요." }
                }
            }
        };

        private static readonly string[] VENDOR_NAMES = { "약초 장수", "빵 장수", "잡화 장수" };

        private static readonly string[][] VENDOR_LINES =
        {
            new[] { "상처엔 이 잎을 짓이겨 붙이시오.", "모험단이 오면 금방 동나요." },
            new[] { "갓 구운 빵이오! 원정 가는 길에 하나씩.", "보리빵은 오래 가요." },
            new[] { "밧줄, 횃불, 부싯돌. 다 있소.", "깎아 드릴 수는 없소." }
        };

        // v2 그림에서 노점 셋은 시장 게시판 오른쪽에 나란히 있다. 게시판 문 앞 자리에서의 상대 위치로,
        // 지도 전체 화면에서 좌판 앞 발 자리를 재어 옮긴 값이다. 그림이 바뀌면 다시 잰다.
        // 좌판 안쪽에 걸리면 NpcWalker가 가장 가까운 걸을 수 있는 칸으로 옮겨 세운다.
        private static readonly Vector2[] VENDOR_OFFSETS =
        {
            new Vector2(4.2f, -0.2f),
            new Vector2(6.7f, -0.3f),
            new Vector2(9.1f, -0.5f)
        };

        // 모험단. 지하시장이 거래하는 단이다. 검은 늪 용병단은 시제품 종목과 같은 단이다.
        private static readonly PartyCast[] PARTIES =
        {
            new PartyCast
            {
                Band = "검은 늪 용병단",
                StockId = "STOCK_HIGH_RISK",
                Tint = new Color(0.72f, 0.8f, 1f),
                Names = new[] { "검은 늪 단장", "검은 늪 궁수", "검은 늪 방패병", "검은 늪 짐꾼" },
                Lines = new[] { "길 막지 마쇼.", "이번 의뢰만 끝나면 한몫 잡는다.", "…뭘 그렇게 봐?" }
            },
            new PartyCast
            {
                Band = "회색 파수대",
                StockId = "STOCK_WARDEN",
                Tint = new Color(1f, 0.78f, 0.7f),
                Names = new[] { "회색 파수대 대장", "회색 파수대 척후", "회색 파수대 주술사" },
                Lines = new[] { "이 영지는 처음이야. 여관이 어디지?", "의뢰만 잡히면 바로 나간다.", "이 영지 몬스터는 소문보다 질기더군." }
            },
            new PartyCast
            {
                Band = "등불 기사단",
                StockId = "STOCK_LANTERN",
                Tint = new Color(0.78f, 1f, 0.78f),
                Names = new[] { "등불 기사단장", "등불 기사단 창병", "등불 기사단 치유사" },
                Lines = new[] { "우리는 북쪽 영지에서 왔소.", "새벽에 출발한다. 늘 그랬지.", "여관 침대가 그리울 거야." }
            }
        };

        /// <summary>
        /// 씬의 장소(VillageSpot)를 읽어 그 둘레에 사람을 세운다. 장소를 먼저 세운 뒤에 부른다.
        /// </summary>
        /// <param name="vendorFeet">노점 앞 자리(월드). 없으면 게시판 문 앞에서의 옛 상대 위치를 쓴다.</param>
        /// <param name="skipTownsfolk">일하는 사람 그림(StationNpcSetup)이 들어왔으면 임시 주민·장수를 세우지 않는다.</param>
        public static void Build(GameObject prefab, Vector2 groundSize, List<Vector2> vendorFeet = null,
            bool skipTownsfolk = false)
        {
            var root = new GameObject("NPCs");

            var grid = new GameObject("Nav Grid");
            grid.AddComponent<VillageNavGrid>().Configure(groundSize);

            var spots = UnityEngine.Object.FindObjectsByType<VillageSpot>(FindObjectsInactive.Exclude);
            var doors = new Dictionary<string, Vector2>();
            var destinations = new List<Vector2>();

            foreach (var spot in spots)
            {
                doors[spot.Name] = spot.transform.position;
                destinations.Add(spot.transform.position);
            }

            var count = 0;

            foreach (var cast in skipTownsfolk == true ? new PlaceCast[0] : RESIDENTS)
            {
                if (doors.TryGetValue(cast.Place, out var door) == false)
                {
                    continue;
                }

                for (var i = 0; i < cast.Names.Length; i++)
                {
                    // 첫 사람은 문 왼쪽, 둘째는 오른쪽.
                    var side = i == 0 ? -RESIDENT_SIDE_OFFSET : RESIDENT_SIDE_OFFSET;
                    var npc = CreateNpc(prefab, root.transform, door + new Vector2(side, 0.3f), RESIDENT_TINT, 1f);
                    npc.AddComponent<VillageNpc>().Configure(NpcRole.Role_Resident, cast.Names[i], cast.Place,
                        new List<string>(cast.Lines[i]));
                    npc.AddComponent<NpcAmbient>();
                    count++;
                }
            }

            if (skipTownsfolk == false && (doors.TryGetValue("시장 게시판", out var board) == true || vendorFeet != null))
            {
                for (var i = 0; i < VENDOR_NAMES.Length; i++)
                {
                    var foot = vendorFeet != null && i < vendorFeet.Count ? vendorFeet[i] : board + VENDOR_OFFSETS[i];
                    var npc = CreateNpc(prefab, root.transform, foot, VENDOR_TINT, 1f);
                    npc.AddComponent<VillageNpc>().Configure(NpcRole.Role_Vendor, VENDOR_NAMES[i], string.Empty,
                        new List<string>(VENDOR_LINES[i]));
                    npc.AddComponent<NpcAmbient>();
                    count++;
                }
            }

            var bandArt = BandWalkerSetup.Import();

            for (var p = 0; p < PARTIES.Length; p++)
            {
                var cast = PARTIES[p];
                var party = new GameObject(cast.Band);
                party.transform.SetParent(root.transform);

                // 단마다 다른 장소에서 출발한다.
                var start = destinations.Count > 0 ? destinations[p % destinations.Count] : Vector2.zero;
                var members = new List<NpcWalker>();

                for (var m = 0; m < cast.Names.Length; m++)
                {
                    var foot = start + new Vector2(m * 0.6f, -0.4f * m);
                    // 걸음 빠르기를 조금씩 달리한다. 똑같으면 행진처럼 보인다.
                    // 단원 그림(NpcField_v2)이 들어왔으면 단장·궁수·방패병·짐꾼을 쓰고, 없으면 임시본.
                    var speed = 0.95f + 0.05f * m;
                    var npc = bandArt == true
                        ? BandWalkerSetup.Create(BandWalkerSetup.TYPES[m % BandWalkerSetup.TYPES.Length], party.transform, foot, speed)
                        : CreateNpc(prefab, party.transform, foot, cast.Tint, speed);
                    npc.AddComponent<VillageNpc>().Configure(NpcRole.Role_Adventurer, cast.Names[m], cast.Band,
                        new List<string>(cast.Lines));
                    npc.AddComponent<NpcAmbient>();
                    members.Add(npc.GetComponent<NpcWalker>());
                    count++;
                }

                var npcParty = party.AddComponent<NpcParty>();
                npcParty.Configure(members, new List<Vector2>(destinations), cast.StockId);
                npcParty.SetDefaultTint(Color.HSVToRGB((p * 0.33f + 0.05f) % 1f, 0.6f, 1f));
            }

            UnderwriteLog.Info($"영지에 사람 {count}명을 세웠습니다. 모험단 {PARTIES.Length}개.");
        }

        private static GameObject CreateNpc(GameObject prefab, Transform parent, Vector2 foot, Color tint,
            float speedScale)
        {
            var npc = CreateBody(prefab);
            npc.transform.SetParent(parent);
            npc.transform.localScale = Vector3.one * CHARACTER_SCALE;

            var walker = npc.AddComponent<NpcWalker>();
            walker.Configure(Constants.PROTAGONIST_FOOT_OFFSET, speedScale);
            walker.PlaceFoot(foot);

            npc.AddComponent<CharacterActor>();

            // 그림자와 몸을 한 덩어리로 정렬한다.
            npc.AddComponent<SortingGroup>();
            npc.AddComponent<YSortRenderer>().Configure(Constants.PROTAGONIST_FOOT_OFFSET, false);

            foreach (var renderer in npc.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.gameObject.name == "Body")
                {
                    renderer.color = tint;
                }
            }

            return npc;
        }

        /// <summary>
        /// 임시 몸. 주인공 프리팹에서 조작·물리를 떼어 낸다.
        /// NPC는 물리 몸 없이 걷는다(NpcWalker). 수십 명이 서로 밀치면 문간에 끼어 서 버린다.
        /// </summary>
        internal static GameObject CreateBody(GameObject prefab)
        {
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(npc, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            npc.name = "NPC";

            // 조작 스크립트가 PlayerInput을 요구하므로 조작 스크립트를 먼저 뗀다.
            RemoveAll<PlayerCharacterInput>(npc);
            RemoveAll<PlayerInput>(npc);
            RemoveAll<Collider2D>(npc);
            RemoveAll<Rigidbody2D>(npc);

            return npc;
        }

        private static void RemoveAll<T>(GameObject go) where T : Component
        {
            foreach (var component in go.GetComponentsInChildren<T>(true))
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }
    }
}
