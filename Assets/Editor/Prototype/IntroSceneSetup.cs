using System.Collections.Generic;
using System.IO;
using System.Linq;
using Underwrite.Data;
using Underwrite.UI;
using Underwrite.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.Editor
{
    /// <summary>
    /// 인트로 씬을 만든다. 웹툰 1~6화를 넘기고 거래소로 들어간다.
    /// 빌드 목록의 맨 앞에 올려 게임이 여기서 시작하게 한다.
    /// </summary>
    public static class IntroSceneSetup
    {
        private const string SCENE_PATH = "Assets/Scenes/Intro.unity";
        private const string WEBTOON_FOLDER = "Assets/Images/Webtoon";
        private const string PANEL_SETTINGS = "Assets/UI/MarketPanelSettings.asset";
        private const string GAME_DATA = "Assets/Data/GameDataSet.asset";

        [MenuItem("Underwrite/인트로 씬 만들기")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.05f, 0.04f);
            camera.orthographic = true;

            var go = new GameObject("Intro");
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PANEL_SETTINGS);

            var intro = go.AddComponent<IntroController>();
            var so = new SerializedObject(intro);
            var pages = so.FindProperty("Pages");

            // 파일 이름이 01_, 02_ … 로 시작하므로 이름순이 곧 화의 순서다. 에디터 도구라 LINQ를 쓴다.
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { WEBTOON_FOLDER })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => Path.GetFileName(path))
                .ToList();

            pages.arraySize = paths.Count;

            for (var i = 0; i < paths.Count; i++)
            {
                pages.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);
            }

            // 이어하기는 시장을 표에서 다시 짠다.
            so.FindProperty("Data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameDataSet>(GAME_DATA);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            PutFirstInBuild();
            AssetDatabase.Refresh();

            UnderwriteLog.Info($"인트로 씬을 만들었습니다: {SCENE_PATH} (웹툰 {paths.Count}장)");
        }

        // 빌드 목록 맨 앞에 둔다. 이미 있으면 맨 앞으로 옮긴다.
        private static void PutFirstInBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(entry => entry.path == SCENE_PATH);
            scenes.Insert(0, new EditorBuildSettingsScene(SCENE_PATH, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
