using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.UI;
using Underwrite.Underworld;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Underwrite.Editor
{
    /// <summary>
    /// 걸어 다니는 지하 방 시험 씬(2026-10-17 사용자 스케치: 왼쪽·가운데 둥근 테이블에 앉은 투자꾼,
    /// 오른쪽 위 주식판, 오른쪽 아래 긴 카운터와 그 뒤의 정보상).
    ///
    /// 거래소 씬(MarketPrototype)을 복사해 거래 화면은 그대로 두고, 그 아래에 방을 깐다.
    /// 그림이 아직 없어 바닥·가구는 단색 상자, 사람은 주인공 프리팹에 색을 입힌 임시본이다.
    /// 투자꾼은 서 있는 임시본이지만 앉은 그림이 오면 그 자리에 앉은 사물로 바꾼다(걷지 않는다).
    /// </summary>
    public static class UnderworldSceneSetup
    {
        private const string SOURCE_SCENE = "Assets/Scenes/MarketPrototype.unity";
        private const string ROOM_SCENE = "Assets/Scenes/Underworld.unity";
        private const string ROOM_TREE = "Assets/UI/UnderworldRoom.uxml";
        private const string PANEL_SETTINGS = "Assets/UI/MarketPanelSettings.asset";
        private const float CHARACTER_SCALE = 0.27f;

        // 방 크기(유닛). 화면(약 26.7 × 15)보다 조금 넓어 걸을 맛이 난다.
        private static readonly Vector2 ROOM_SIZE = new Vector2(30f, 17f);

        // 둥근 테이블 다섯. 세로가 가로의 절반 — 캐릭터 15도 시점의 원형 상판 비율.
        private static readonly Vector2[] TABLES =
        {
            new Vector2(-9f, 3f), new Vector2(-8f, -2.5f), new Vector2(-1.5f, 4.5f),
            new Vector2(-2f, 0f), new Vector2(-1.5f, -5f)
        };

        private static readonly Vector2 TABLE_SIZE = new Vector2(2.8f, 1.4f);

        // 앉은 투자꾼 자리. 스케치대로 왼쪽 테이블에 몰려 있다.
        private static readonly Vector2[] INVESTORS =
        {
            new Vector2(-10.6f, 4.3f), new Vector2(-11f, 2.4f), new Vector2(-9.4f, 0.8f),
            new Vector2(-10f, -3f), new Vector2(-7.2f, -4.3f), new Vector2(-3.4f, 5.6f), new Vector2(-3.6f, 1f)
        };

        private static readonly Color[] INVESTOR_TINTS =
        {
            new Color(0.85f, 0.75f, 0.65f), new Color(0.7f, 0.78f, 0.9f), new Color(0.9f, 0.85f, 0.7f),
            new Color(0.75f, 0.85f, 0.75f)
        };

        private static readonly Vector2 BOARD_AT = new Vector2(9f, 4.6f);
        private static readonly Vector2 BOARD_SIZE = new Vector2(5f, 2.6f);
        private static readonly Vector2 COUNTER_AT = new Vector2(8.6f, -3f);
        private static readonly Vector2 COUNTER_SIZE = new Vector2(1.2f, 6f);
        private static readonly Vector2 INFORMANT_AT = new Vector2(9.8f, -3f);
        private static readonly Vector2 PLAYER_AT = new Vector2(-12.5f, -5.5f);

        [MenuItem("Underwrite/지하 방 시험 씬 만들기")]
        public static void Build()
        {
            var prefab = VillageSceneSetup.EnsureCharacterPrefab();
            var lightPrefab = VillageSceneSetup.EnsureLightPrefab();

            if (prefab == null)
            {
                UnderwriteLog.Error("캐릭터 프리팹을 만들지 못했습니다.");
                return;
            }

            // 거래 화면 설정을 그대로 가져오려고 거래소 씬을 복사한다.
            AssetDatabase.DeleteAsset(ROOM_SCENE);

            if (AssetDatabase.CopyAsset(SOURCE_SCENE, ROOM_SCENE) == false)
            {
                UnderwriteLog.Error($"거래소 씬을 복사하지 못했습니다: {SOURCE_SCENE}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ROOM_SCENE, OpenSceneMode.Single);
            var market = Object.FindFirstObjectByType<MarketScreenController>();

            if (market == null)
            {
                UnderwriteLog.Error("복사한 씬에 거래소 화면이 없습니다.");
                return;
            }

            var marketSo = new SerializedObject(market);
            marketSo.FindProperty("RoomMode").boolValue = true;
            marketSo.ApplyModifiedPropertiesWithoutUndo();

            // 방은 영지 카메라가 비춘다. 거래소 씬의 카메라는 걷어 낸다.
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(camera.gameObject);
            }

            // Codex 납품(지하 방 파츠)이 있으면 그림으로, 없으면 임시 상자로 깐다.
            var delivery = UnderworldRoomParts.FindDelivery();
            var points = delivery == null ? null : UnderworldRoomParts.Build(delivery);
            var boardFront = new GameObject("Board Front");
            var boardText = new GameObject("Board Text Area");
            var boardTextSize = new Vector2(BOARD_SIZE.x * 0.9f, BOARD_SIZE.y * 0.8f);
            var investorSpots = new List<Vector2>(INVESTORS);
            var informantAt = INFORMANT_AT;
            var playerAt = PLAYER_AT;

            if (points != null)
            {
                VillageSceneSetup.SetGroundSize(points.Size);
                boardFront.transform.position = new Vector3(points.BoardFront.x, points.BoardFront.y, 0f);
                boardText.transform.position = new Vector3(points.BoardTextCenter.x, points.BoardTextCenter.y, 0f);
                boardTextSize = points.BoardTextSize;
                informantAt = points.Informant;
                playerAt = points.Spawn;

                // 앉은 그림이 오기 전까지는 의자 자리 옆에 선 임시본. 테이블마다 한두 명.
                investorSpots.Clear();

                for (var i = 0; i < points.Seats.Count && investorSpots.Count < INVESTORS.Length; i += 2)
                {
                    investorSpots.Add(points.Seats[i]);
                }
            }
            else
            {
                VillageSceneSetup.CreateQuad("Room Floor", Vector2.zero, ROOM_SIZE, new Color(0.18f, 0.15f, 0.12f), -10);
                VillageSceneSetup.SetGroundSize(ROOM_SIZE);
                VillageSceneSetup.BuildEdgeWalls();

                for (var i = 0; i < TABLES.Length; i++)
                {
                    Solid("Table", TABLES[i], TABLE_SIZE, new Color(0.38f, 0.26f, 0.16f));
                }

                var board = Solid("Stock Board", BOARD_AT, BOARD_SIZE, new Color(0.12f, 0.2f, 0.16f));
                Solid("Counter", COUNTER_AT, COUNTER_SIZE, new Color(0.32f, 0.2f, 0.12f));

                // 주식판 앞 자리. 판 아래쪽 가운데에 선다.
                boardFront.transform.SetParent(board.transform);
                boardFront.transform.position = new Vector3(BOARD_AT.x, BOARD_AT.y - BOARD_SIZE.y * 0.5f - 0.6f, 0f);
                boardText.transform.position = new Vector3(BOARD_AT.x, BOARD_AT.y, 0f);
            }

            var investors = new List<CharacterActor>();

            for (var i = 0; i < investorSpots.Count; i++)
            {
                investors.Add(Person("Investor", investorSpots[i], INVESTOR_TINTS[i % INVESTOR_TINTS.Length], prefab));
            }

            var informant = Person("Informant", informantAt, new Color(0.38f, 0.36f, 0.45f), prefab);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Character";
            player.transform.position = new Vector3(playerAt.x, playerAt.y, 0f);
            player.transform.localScale = Vector3.one * CHARACTER_SCALE;
            VillageSceneSetup.ShrinkToFootprint(player);
            player.AddComponent<SortingGroup>();
            player.AddComponent<YSortRenderer>().Configure(Constants.PROTAGONIST_FOOT_OFFSET, false);
            player.AddComponent<CharacterActor>();

            VillageSceneSetup.BuildLight(lightPrefab);

            if (points != null)
            {
                UnderworldRoomParts.DimGlobalLight();
            }

            VillageSceneSetup.BuildCamera(player);
            BuildRoomScreen(market, boardFront.transform, informant, investors, boardText.transform, boardTextSize);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ROOM_SCENE);
            AddToBuild();
            AssetDatabase.Refresh();

            UnderwriteLog.Info($"지하 방 시험 씬을 만들었습니다: {ROOM_SCENE}");
        }

        // 막히는 가구. 바닥에 닿는 면 전체를 막는다 — 그림이 오면 발 닿는 면만 막는다.
        private static GameObject Solid(string name, Vector2 at, Vector2 size, Color color)
        {
            var go = VillageSceneSetup.CreateQuad(name, at, size, color, 0);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            return go;
        }

        // 서 있는 임시 사람. 걷지 않는다.
        private static CharacterActor Person(string name, Vector2 at, Color tint, GameObject prefab)
        {
            var body = VillageNpcSetup.CreateBody(prefab);
            body.name = name;
            body.transform.position = new Vector3(at.x, at.y, 0f);
            body.transform.localScale = Vector3.one * CHARACTER_SCALE;
            body.AddComponent<SortingGroup>();
            body.AddComponent<YSortRenderer>().Configure(Constants.PROTAGONIST_FOOT_OFFSET, false);

            foreach (var renderer in body.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.gameObject.name == "Body")
                {
                    renderer.color = tint;
                }
            }

            return body.AddComponent<CharacterActor>();
        }

        // 방 화면. 거래소 화면보다 아래에 깐다 — 판과 대화가 그 위를 덮는다.
        private static void BuildRoomScreen(MarketScreenController market, Transform board, CharacterActor informant,
            List<CharacterActor> investors, Transform boardText, Vector2 boardTextSize)
        {
            var go = new GameObject("RoomScreen");
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PANEL_SETTINGS);
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ROOM_TREE);
            document.sortingOrder = -1f;

            var room = go.AddComponent<UnderworldRoom>();
            var so = new SerializedObject(room);
            so.FindProperty("Market").objectReferenceValue = market;
            so.FindProperty("Board").objectReferenceValue = board;
            so.FindProperty("Informant").objectReferenceValue = informant;
            so.FindProperty("BoardTextArea").objectReferenceValue = boardText;
            so.FindProperty("BoardTextSize").vector2Value = boardTextSize;

            var list = so.FindProperty("Investors");
            list.arraySize = investors.Count;

            for (var i = 0; i < investors.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = investors[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddToBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == ROOM_SCENE)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(ROOM_SCENE, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
