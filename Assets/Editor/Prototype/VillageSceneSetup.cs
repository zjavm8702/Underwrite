using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using Underwrite.Characters;
using Underwrite.Enumeration;
using Underwrite.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UIElements = UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Underwrite.Editor
{
    /// <summary>
    /// 영지 시험 씬을 만든다.
    ///
    /// 기획서 §11의 "자금 및 정보 모으기"는 메뉴가 아니라 걸어가서 만나는 것이다.
    /// 그 전제를 먼저 세우기 위한 임시 씬이다. 바닥과 건물은 단색 사각형이고,
    /// 아트가 나오면 그대로 갈아 끼운다.
    /// </summary>
    public static class VillageSceneSetup
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs";
        private const string PREFAB_PATH = "Assets/Prefabs/Character.prefab";
        private const string LIGHT_PREFAB_PATH = "Assets/Prefabs/GlobalLight2D.prefab";
        private const string SAMPLE_SCENE = "Assets/Scenes/SampleScene.unity";
        private const string VILLAGE_SCENE = "Assets/Scenes/VillageTest.unity";

        private const string VILLAGE_ART = "Assets/UI/Images/village_ground.png";
        private const string VILLAGE_MASK = "Assets/UI/Images/village_block_mask.png";
        private const float CHARACTER_SCALE = 0.27f;
        private const float GROUND_SIZE = 44f;

        // 문 앞 한 칸의 크기. 사람 키가 1.9유닛쯤이므로 두 걸음 안에 들어오는 넓이다.
        private const float DOOR_WIDTH = 4.5f;
        private const float DOOR_HEIGHT = 2.6f;

        // 발자국의 크기(월드 유닛). 사람 키 1.9유닛에 견주어 발밑 한 뼘이다.
        private const float FOOT_WIDTH = 0.55f;
        private const float FOOT_HEIGHT = 0.30f;
        private const float FOOT_DROP = 0.75f;

        // 그림이 들어오면 그 크기를 따른다. 없으면 단색 바닥의 크기를 쓴다.
        private static float GroundWidth = GROUND_SIZE;
        private static float GroundHeight = GROUND_SIZE;
        private const float WALL_THICK = 1.5f;

        [MenuItem("Underwrite/영지 시험 씬 만들기")]
        public static void Build()
        {
            var prefab = EnsureCharacterPrefab();

            if (prefab == null)
            {
                UnderwriteLog.Error("캐릭터 프리팹을 만들지 못했습니다.");
                return;
            }

            // EnsureCharacterPrefab이 SampleScene을 열어 두었다. 조명도 여기서 굳힌다.
            var lightPrefab = EnsureLightPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGround();
            BuildBlockers();

            // 기획서 §5가 정한 여섯 곳. 값이 오를수록 이야기가 또렷해진다.
            // 좌표는 배경 그림의 문 앞을 걸을 수 있는 마스크로 찾아 잰 값이다.
            // 그림을 다시 그리면 이 값도 같이 다시 재야 한다.
            BuildSpot("시장 게시판", new Vector2(15.18f, -6.28f), InfoSource.Source_MarketBoard, 0f);
            BuildSpot("여관", new Vector2(-15.90f, 5.22f), InfoSource.Source_TavernTalk, 20f);
            BuildSpot("대장간", new Vector2(-18.57f, -6.50f), InfoSource.Source_SmithOrder, 40f);
            BuildSpot("길드 기록실", new Vector2(17.07f, 3.57f), InfoSource.Source_GuildRecord, 80f);
            BuildSpot("영주관", new Vector2(0.40f, 6.15f), InfoSource.Source_PrivateLetter, 120f);

            // 거래소로 가는 길은 화면 구석의 단추가 아니라 걸어가서 여는 문이다.
            BuildEntrance("지하시장 입구", new Vector2(-22.50f, -10.35f));

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Character";
            // 우물 앞 광장에서 시작한다. 여섯 곳 어디로도 걸어갈 수 있는 자리다.
            player.transform.position = new Vector3(0f, -2f, 0f);

            // 캐릭터 원화는 640×724로 크다. 그대로 두면 사람이 건물만 해진다.
            // 화면 높이(카메라 15유닛)의 1/8쯤 되도록 줄인다.
            player.transform.localScale = Vector3.one * CHARACTER_SCALE;
            ShrinkToFootprint(player);

            BuildLight(lightPrefab);
            BuildCamera(player);
            BuildScreen();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(VILLAGE_SCENE));
            EditorSceneManager.SaveScene(scene, VILLAGE_SCENE);
            AssetDatabase.Refresh();

            UnderwriteLog.Info($"영지 시험 씬을 만들었습니다: {VILLAGE_SCENE}");
        }

        /// <summary>
        /// SampleScene의 Character를 프리팹으로 굳힌다.
        /// 씬마다 손으로 다시 꾸미면 설정이 조금씩 어긋난다.
        /// </summary>
        private static GameObject EnsureCharacterPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);

            if (existing != null)
            {
                return existing;
            }

            if (AssetDatabase.IsValidFolder(PREFAB_FOLDER) == false)
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            var opened = EditorSceneManager.OpenScene(SAMPLE_SCENE, OpenSceneMode.Single);
            GameObject source = null;

            foreach (var root in opened.GetRootGameObjects())
            {
                if (root.name == "Character")
                {
                    source = root;
                    break;
                }
            }

            if (source == null)
            {
                UnderwriteLog.Error("SampleScene에서 Character를 찾지 못했습니다.");
                return null;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(source, PREFAB_PATH);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        /// <summary>
        /// 영지 바닥. 타일을 깔지 않고 **그려진 배경 한 장**을 둔다.
        ///
        /// 도트 타일셋을 쓰면 한 화면에 두 해상도가 섞인다. 타일은 한 픽셀이 화면 서너 픽셀인데
        /// 캐릭터는 1:1이라 캐릭터만 떠 보이거나 배경이 뭉갠다.
        /// 기획서가 비도트로 못 박혀 있으므로 배경도 같은 손길로 그린다.
        /// </summary>
        private static void BuildGround()
        {
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(VILLAGE_ART);

            if (art == null)
            {
                UnderwriteLog.Warning($"영지 배경이 없어 단색으로 깝니다: {VILLAGE_ART}");
                var fallback = CreateQuad("Ground", Vector2.zero,
                    new Vector2(GROUND_SIZE, GROUND_SIZE), new Color(0.16f, 0.14f, 0.11f), -10);
                fallback.transform.SetAsFirstSibling();
                return;
            }

            var go = new GameObject("Ground");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.sortingOrder = -10;

            // 그림이 걸어 다닐 범위를 정한다. 벽과 카메라 경계가 이 크기를 따른다.
            var size = renderer.bounds.size;
            GroundWidth = size.x;
            GroundHeight = size.y;

            go.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// 걸을 수 없는 자리를 막는다.
        ///
        /// 네모난 벽 넷으로는 건물과 담을 흉내 낼 수 없다. 건물 모서리를 뚫고 지나가거나,
        /// 반대로 빈 바닥이 막혀 보인다. 그림과 충돌이 어긋나면 플레이어는 자기가 뭘 밟고
        /// 있는지 알 수 없게 된다.
        ///
        /// 그래서 **막힌 자리를 칠한 그림 한 장**을 따로 두고, Unity가 그 알파에서 뽑은
        /// 윤곽을 그대로 쓴다. 배경을 다시 그리면 마스크만 다시 뽑으면 된다.
        /// </summary>
        private static void BuildBlockers()
        {
            var mask = AssetDatabase.LoadAssetAtPath<Sprite>(VILLAGE_MASK);

            if (mask == null)
            {
                UnderwriteLog.Warning($"막힘 마스크가 없어 바깥 벽만 세웁니다: {VILLAGE_MASK}");
                BuildEdgeWalls();
                return;
            }

            var go = new GameObject("Blockers");
            var collider = go.AddComponent<PolygonCollider2D>();
            var count = mask.GetPhysicsShapeCount();
            collider.pathCount = count;

            var points = new List<Vector2>();

            for (var i = 0; i < count; i++)
            {
                points.Clear();
                mask.GetPhysicsShape(i, points);
                collider.SetPath(i, points.ToArray());
            }

            UnderwriteLog.Info($"막힘 윤곽 {count}개를 세웠습니다.");
        }

        /// <summary>마스크가 없을 때의 최소한. 바깥으로만 나가지 못하게 한다.</summary>
        private static void BuildEdgeWalls()
        {
            var halfX = GroundWidth * 0.5f;
            var halfY = GroundHeight * 0.5f;
            var walls = new GameObject("Walls");

            MakeWall(walls, "North", new Vector2(0f, halfY), new Vector2(GroundWidth, WALL_THICK));
            MakeWall(walls, "South", new Vector2(0f, -halfY), new Vector2(GroundWidth, WALL_THICK));
            MakeWall(walls, "West", new Vector2(-halfX, 0f), new Vector2(WALL_THICK, GroundHeight));
            MakeWall(walls, "East", new Vector2(halfX, 0f), new Vector2(WALL_THICK, GroundHeight));
        }

        private static void MakeWall(GameObject parent, string name, Vector2 at, Vector2 size)
        {
            // 벽은 보이지 않는다. 그림에 이미 담이 그려져 있다.
            var wall = new GameObject(name);
            wall.transform.position = new Vector3(at.x, at.y, 0f);
            wall.transform.SetParent(parent.transform);
            var box = wall.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        /// <summary>
        /// 부딪히는 자리를 발밑으로 내린다.
        ///
        /// 사람 전체를 캡슐로 감싸면 머리가 벽에 먼저 닿는다. 내려다보는 화면에서는
        /// 상체가 건물 앞을 지나가는 것이 자연스럽고, 실제로 막히는 것은 발이다.
        /// 전신 캡슐을 그대로 두면 문에서 한 몸 길이 떨어진 곳에서 멈춘다.
        /// </summary>
        private static void ShrinkToFootprint(GameObject player)
        {
            var cap = player.GetComponent<CapsuleCollider2D>();

            if (cap == null)
            {
                UnderwriteLog.Warning("캐릭터에 CapsuleCollider2D가 없습니다.");
                return;
            }

            var scale = player.transform.localScale.x;

            if (scale <= 0f)
            {
                return;
            }

            // 월드 기준으로 정해 두고 캐릭터 축척으로 되돌린다. 축척을 바꿔도 발자국 크기는 같다.
            cap.size = new Vector2(FOOT_WIDTH / scale, FOOT_HEIGHT / scale);
            cap.offset = new Vector2(0f, -FOOT_DROP / scale);
            cap.direction = CapsuleDirection2D.Horizontal;
        }

        /// <summary>
        /// 말을 걸 수 있는 문 앞 한 칸.
        ///
        /// 건물은 그림에 그려져 있고 그 몸통은 마스크가 막는다. 여기서 둘 것은
        /// **문 앞에 섰는지 알아보는 칸** 하나뿐이다. <paramref name="at"/>은
        /// 건물 가운데가 아니라 플레이어가 실제로 서는 자리다.
        /// </summary>
        private static void BuildSpot(string name, Vector2 at, InfoSource source, float cost)
        {
            var door = new GameObject(name);
            door.transform.position = new Vector3(at.x, at.y, 0f);

            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(DOOR_WIDTH, DOOR_HEIGHT);
            trigger.isTrigger = true;

            var place = door.AddComponent<VillageSpot>();
            var so = new SerializedObject(place);
            so.FindProperty("Activity").enumValueIndex = (int)VillageActivity.Activity_Gather;
            so.FindProperty("Source").enumValueIndex = (int)source;
            so.FindProperty("DisplayName").stringValue = name;
            so.FindProperty("Cost").floatValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 다른 곳으로 들어가는 문. 지하시장 입구가 여기에 해당한다.
        ///
        /// 정보를 캐는 자리와 생김새는 같지만 하는 일이 다르다.
        /// 값을 받지 않고, 시간을 쓰지 않고, 씬을 넘긴다.
        /// </summary>
        private static void BuildEntrance(string name, Vector2 at)
        {
            var door = new GameObject(name);
            door.transform.position = new Vector3(at.x, at.y, 0f);

            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(DOOR_WIDTH, DOOR_HEIGHT);
            trigger.isTrigger = true;

            var place = door.AddComponent<VillageSpot>();
            var so = new SerializedObject(place);
            so.FindProperty("Activity").enumValueIndex = (int)VillageActivity.Activity_Enter;
            so.FindProperty("DisplayName").stringValue = name;
            so.FindProperty("Cost").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateQuad(string name, Vector2 at, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(at.x, at.y, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SolidSprite();
            renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.sortingOrder = order;
            return go;
        }

        /// <summary>
        /// 단색 사각형 스프라이트. 임시 바닥과 건물에 쓴다.
        /// 아트가 나오면 이 자리를 그림으로 갈아 끼운다.
        /// </summary>
        private static Sprite SolidSprite()
        {
            var texture = new Texture2D(4, 4);
            var pixels = new Color[16];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f),
                4f, 0, SpriteMeshType.FullRect, new Vector4(1f, 1f, 1f, 1f));
        }

        /// <summary>
        /// 전역 2D 조명. Light2D 타입을 코드에서 직접 쓰면 URP 어셈블리를 끌어와야 하므로,
        /// SampleScene에 이미 있는 것을 프리팹으로 굳혀 두고 그것을 놓는다.
        /// </summary>
        /// <summary>영지 화면을 붙인다. 자리 안내와 조사 결과가 여기에 뜬다.</summary>
        private static void BuildScreen()
        {
            var tree = AssetDatabase.LoadAssetAtPath<UIElements.VisualTreeAsset>(
                "Assets/UI/VillageScreen.uxml");
            var dialogue = AssetDatabase.LoadAssetAtPath<UIElements.VisualTreeAsset>(
                "Assets/UI/CommonDialogue.uxml");
            var panel = AssetDatabase.LoadAssetAtPath<UIElements.PanelSettings>(
                "Assets/UI/MarketPanelSettings.asset");
            var data = AssetDatabase.LoadAssetAtPath<Data.GameDataSet>("Assets/Data/GameDataSet.asset");

            var go = new GameObject("VillageScreen");
            var document = go.AddComponent<UIElements.UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = tree;

            var controller = go.AddComponent<UI.VillageScreenController>();
            var so = new SerializedObject(controller);
            so.FindProperty("DialogueTree").objectReferenceValue = dialogue;
            so.FindProperty("Data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildLight(GameObject lightPrefab)
        {
            if (lightPrefab == null)
            {
                UnderwriteLog.Warning("전역 조명 프리팹이 없어 조명 없이 만듭니다. 화면이 어두울 수 있습니다.");
                return;
            }

            var light = (GameObject)PrefabUtility.InstantiatePrefab(lightPrefab);
            light.name = "Global Light 2D";
        }

        private static GameObject EnsureLightPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(LIGHT_PREFAB_PATH);

            if (existing != null)
            {
                return existing;
            }

            var scene = EditorSceneManager.GetActiveScene();

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Global Light 2D")
                {
                    continue;
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(root, LIGHT_PREFAB_PATH);
                AssetDatabase.SaveAssets();
                return saved;
            }

            return null;
        }

        /// <summary>
        /// 카메라를 세운다. 따라가는 일은 Cinemachine이 맡는다.
        ///
        /// 직접 Lerp로 쫓아가게 만들면 데드존도 경계 제한도 없어서,
        /// 걸음마다 화면이 미세하게 흔들리고 맵 끝에서 바깥이 비친다.
        /// </summary>
        private static void BuildCamera(GameObject player)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.backgroundColor = new Color(0.07f, 0.06f, 0.05f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            go.AddComponent<AudioListener>();
            go.AddComponent<CinemachineBrain>();

            // 물리가 루트에 있으므로 루트가 곧 플레이어가 선 자리다. 카메라도 그것을 따른다.
            var body = player.transform;

            var rig = new GameObject("Village Camera");
            rig.transform.position = new Vector3(body.position.x, body.position.y, -10f);

            var vcam = rig.AddComponent<CinemachineCamera>();
            vcam.Follow = body;
            // Orthographic 여부는 Main Camera를 따라간다. 여기서는 크기만 맞춘다.
            vcam.Lens.OrthographicSize = 7.5f;

            // 화면 가운데 작은 네모 안에서는 카메라가 가만히 있는다.
            // 그래야 제자리걸음이나 미세한 흔들림에 화면이 따라 움직이지 않는다.
            var follow = rig.AddComponent<CinemachinePositionComposer>();
            follow.Damping = new Vector3(0.6f, 0.6f, 0f);
            follow.Composition.DeadZone.Enabled = true;
            follow.Composition.DeadZone.Size = new Vector2(0.18f, 0.18f);
            follow.CameraDistance = 10f;

            // 영지 바깥이 비치지 않게 가둔다.
            var bounds = new GameObject("Camera Bounds");
            var shape = bounds.AddComponent<PolygonCollider2D>();
            shape.isTrigger = true;
            var halfX = GroundWidth * 0.5f;
            var halfY = GroundHeight * 0.5f;
            shape.points = new[]
            {
                new Vector2(-halfX, -halfY),
                new Vector2(halfX, -halfY),
                new Vector2(halfX, halfY),
                new Vector2(-halfX, halfY)
            };

            var confiner = rig.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = shape;
        }
    }
}
