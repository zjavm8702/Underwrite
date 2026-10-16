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

        private const float GROUND_SIZE = 44f;
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
            BuildWalls();

            // 세 자리는 기획서 §8의 정보 출처와 그대로 맞물린다.
            BuildSpot("여관", new Vector2(-13f, 8f), new Color(0.55f, 0.36f, 0.20f),
                InfoSource.Source_TavernTalk, 20f);
            BuildSpot("길드 기록실", new Vector2(13f, 8f), new Color(0.27f, 0.33f, 0.45f),
                InfoSource.Source_GuildRecord, 80f);
            BuildSpot("시장 게시판", new Vector2(0f, -12f), new Color(0.45f, 0.42f, 0.30f),
                InfoSource.Source_MarketBoard, 0f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Character";
            player.transform.position = Vector3.zero;

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

        private static void BuildGround()
        {
            var ground = CreateQuad("Ground", Vector2.zero,
                new Vector2(GROUND_SIZE, GROUND_SIZE), new Color(0.16f, 0.14f, 0.11f), -10);
            ground.transform.SetAsFirstSibling();
        }

        /// <summary>바깥으로 걸어 나가지 못하게 막는다. 임시 씬이라 네모난 벽이면 충분하다.</summary>
        private static void BuildWalls()
        {
            var half = GROUND_SIZE * 0.5f;
            var walls = new GameObject("Walls");

            MakeWall(walls, "North", new Vector2(0f, half), new Vector2(GROUND_SIZE, WALL_THICK));
            MakeWall(walls, "South", new Vector2(0f, -half), new Vector2(GROUND_SIZE, WALL_THICK));
            MakeWall(walls, "West", new Vector2(-half, 0f), new Vector2(WALL_THICK, GROUND_SIZE));
            MakeWall(walls, "East", new Vector2(half, 0f), new Vector2(WALL_THICK, GROUND_SIZE));
        }

        private static void MakeWall(GameObject parent, string name, Vector2 at, Vector2 size)
        {
            var wall = CreateQuad(name, at, size, new Color(0.10f, 0.09f, 0.07f), -9);
            wall.transform.SetParent(parent.transform);
            var box = wall.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        private static void BuildSpot(string name, Vector2 at, Color color, InfoSource source, float cost)
        {
            var spot = CreateQuad(name, at, new Vector2(7f, 5f), color, 0);

            // 건물 자체는 막고, 그 앞에 선 자리에서 말을 건다.
            var body = spot.AddComponent<BoxCollider2D>();
            body.size = new Vector2(7f, 5f);

            var door = new GameObject("Door");
            door.transform.SetParent(spot.transform);
            door.transform.localPosition = new Vector3(0f, -3.6f, 0f);

            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(7f, 3f);
            trigger.isTrigger = true;

            var place = door.AddComponent<VillageSpot>();
            var so = new SerializedObject(place);
            so.FindProperty("Source").enumValueIndex = (int)source;
            so.FindProperty("DisplayName").stringValue = name;
            so.FindProperty("Cost").floatValue = cost;
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
            camera.orthographicSize = 12f;
            camera.backgroundColor = new Color(0.07f, 0.06f, 0.05f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            go.AddComponent<AudioListener>();
            go.AddComponent<CinemachineBrain>();

            var body = player.transform.childCount > 0 ? player.transform.GetChild(0) : player.transform;

            var rig = new GameObject("Village Camera");
            rig.transform.position = new Vector3(body.position.x, body.position.y, -10f);

            var vcam = rig.AddComponent<CinemachineCamera>();
            vcam.Follow = body;
            // Orthographic 여부는 Main Camera를 따라간다. 여기서는 크기만 맞춘다.
            vcam.Lens.OrthographicSize = 12f;

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
            var half = GROUND_SIZE * 0.5f;
            shape.points = new[]
            {
                new Vector2(-half, -half),
                new Vector2(half, -half),
                new Vector2(half, half),
                new Vector2(-half, half)
            };

            var confiner = rig.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = shape;
        }
    }
}
