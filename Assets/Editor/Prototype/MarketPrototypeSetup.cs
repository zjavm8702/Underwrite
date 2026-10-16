using System.IO;
using Underwrite.Market;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Underwrite.Editor.Prototype
{
    /// <summary>
    /// 시장 코어 시제품용 테스트 씬을 만들고 관리한다.
    /// 씬 파일을 텍스트로 편집하지 않고 Editor API로만 생성·저장한다. 코드 규칙 §11.
    /// </summary>
    public static class MarketPrototypeSetup
    {
        private const string OBJECT_NAME = "MarketPrototype";
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = "Assets/Scenes/MarketPrototype.unity";
        private const float CAMERA_ORTHOGRAPHIC_SIZE = 5f;

        [MenuItem("Tools/Underwrite/Prototype/시장 테스트 씬 만들기", priority = 0)]
        public static void CreateTestScene()
        {
            if (File.Exists(SCENE_PATH) == true)
            {
                var overwrite = EditorUtility.DisplayDialog(
                    "시장 테스트 씬",
                    $"{SCENE_PATH}\n\n이미 있습니다. 새로 만들면 기존 씬의 내용이 사라집니다.",
                    "새로 만들기",
                    "기존 씬 열기");

                if (overwrite == false)
                {
                    OpenTestScene();
                    return;
                }
            }

            // 현재 씬의 미저장 변경을 먼저 처리한다. 사용자가 취소하면 아무것도 하지 않는다.
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                return;
            }

            if (AssetDatabase.IsValidFolder(SCENE_FOLDER) == false)
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateDriver();

            var saved = EditorSceneManager.SaveScene(scene, SCENE_PATH);

            if (saved == false)
            {
                EditorUtility.DisplayDialog("시장 테스트 씬", "씬을 저장하지 못했습니다. 콘솔을 확인하세요.", "확인");
                return;
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("시장 테스트 씬",
                $"{SCENE_PATH}\n\n만들었습니다. 재생을 누르면 장이 열립니다.\n\n"
                + "MarketPrototype 오브젝트의 인스펙터에서 조정할 수 있습니다.\n"
                + "· Seed — 같은 값이면 같은 장세가 재현됩니다\n"
                + "· Is Inspection Day — 켜면 휴장 상태로 열립니다\n"
                + "· Starting Sentiment — 시장 분위기\n"
                + "· Auto Pause On Outcome — 결과 확정 시 자동 정지\n"
                + "· Order Quantity / Patronage Amount — 주문·후원 단위",
                "확인");
        }

        [MenuItem("Tools/Underwrite/Prototype/시장 테스트 씬 열기", priority = 1)]
        public static void OpenTestScene()
        {
            if (File.Exists(SCENE_PATH) == false)
            {
                EditorUtility.DisplayDialog("시장 테스트 씬",
                    "씬이 아직 없습니다. '시장 테스트 씬 만들기'를 먼저 실행하세요.", "확인");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                return;
            }

            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Underwrite/Prototype/시장 테스트 씬 열기", validate = true)]
        private static bool ValidateOpenTestScene()
        {
            return File.Exists(SCENE_PATH);
        }

        /// <summary>현재 열린 씬에 시제품 오브젝트만 추가한다. 다른 씬에서 함께 보고 싶을 때 쓴다.</summary>
        [MenuItem("Tools/Underwrite/Prototype/현재 씬에 시제품 추가", priority = 20)]
        public static void AddToCurrentScene()
        {
            var existing = GameObject.Find(OBJECT_NAME);

            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorUtility.DisplayDialog("시장 시제품",
                    $"'{OBJECT_NAME}'이 이미 이 씬에 있습니다. 선택해 두었습니다.", "확인");
                return;
            }

            var host = CreateDriver();
            Undo.RegisterCreatedObjectUndo(host, "시장 시제품 추가");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Underwrite/Prototype/현재 씬에서 시제품 제거", priority = 21)]
        public static void RemoveFromCurrentScene()
        {
            var existing = GameObject.Find(OBJECT_NAME);

            if (existing == null)
            {
                EditorUtility.DisplayDialog("시장 시제품", "이 씬에 없습니다.", "확인");
                return;
            }

            Undo.DestroyObjectImmediate(existing);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static GameObject CreateDriver()
        {
            var host = new GameObject(OBJECT_NAME);
            host.AddComponent<MarketPrototypeDriver>();
            Selection.activeGameObject = host;
            return host;
        }

        /// <summary>
        /// IMGUI는 카메라 없이도 그려지지만, 카메라가 없으면 Game 뷰에 경고가 남고
        /// 2단계에서 영지를 붙일 때 다시 만들어야 한다. 직교 카메라를 미리 둔다.
        /// </summary>
        private static void CreateCamera()
        {
            var host = new GameObject("Main Camera");
            host.tag = "MainCamera";

            var camera = host.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CAMERA_ORTHOGRAPHIC_SIZE;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.07f, 0.10f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            host.AddComponent<AudioListener>();
        }
    }
}
