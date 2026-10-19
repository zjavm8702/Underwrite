using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Underwrite.Editor.Utility
{
    /// <summary>
    /// 웹 데모 빌드(2026-10-19 사장님 — itch.io에 올려 본다).
    ///
    /// 압축은 Gzip + 해제 대체(decompression fallback)를 켠다. 올리는 곳이 압축 헤더를 안 붙여도 열리게 하려는 것이다.
    /// 그림은 PC에서 선명하게 보이려고 압축하지 않는다(빌드 데이터 1.1GB). 웹은 itch.io 파일 한도(200MB)와 브라우저 메모리 때문에
    /// 웹에만 압축(DXT + crunch) 덮어쓰기를 건다 — 「웹 그림 압축 걸기」. PC 빌드와 에디터 그림은 그대로다.
    /// 올릴 zip은 `Tools/web/zip_web_demo.py`로 묶는다 — 윈도 압축은 경로에 역슬래시를 넣어 웹 호스팅에서 깨질 수 있다.
    /// </summary>
    public static class WebDemoBuild
    {
        private const string OUTPUT = "Builds/WebDemo";
        private const int WIDTH = 1280;
        private const int HEIGHT = 720;

        private const string WEB_PLATFORM = "WebGL";
        private const string WEB_TEMPLATE = "PROJECT:Underwrite";
        private const int WEB_CRUNCH_QUALITY = 75;

        /// <summary>마지막 빌드에 들어간 그림에 웹 전용 압축 덮어쓰기를 건다. 한 번 빌드한 뒤 부른다.</summary>
        [MenuItem("Tools/Underwrite/Build/웹 그림 압축 걸기")]
        public static void CompressTexturesForWeb()
        {
            var report = BuildReport.GetLatestReport();

            if (report == null)
            {
                Debug.LogError("빌드 기록이 없습니다. 웹 데모 빌드를 먼저 한 번 하세요.");
                return;
            }

            var paths = new HashSet<string>();

            foreach (var pack in report.packedAssets)
            {
                foreach (var content in pack.contents)
                {
                    if (content.type == typeof(Texture2D) && content.sourceAssetPath.StartsWith("Assets/") == true)
                    {
                        paths.Add(content.sourceAssetPath);
                    }
                }
            }

            var changed = 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var path in paths)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer == null)
                    {
                        continue;
                    }

                    var web = importer.GetPlatformTextureSettings(WEB_PLATFORM);

                    if (web.overridden == true && web.crunchedCompression == true)
                    {
                        continue;
                    }

                    web.overridden = true;
                    web.maxTextureSize = importer.maxTextureSize;
                    web.format = TextureImporterFormat.Automatic;
                    web.textureCompression = TextureImporterCompression.Compressed;
                    web.crunchedCompression = true;
                    web.compressionQuality = WEB_CRUNCH_QUALITY;
                    importer.SetPlatformTextureSettings(web);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"웹 그림 압축을 걸었습니다: {changed}개 (빌드에 든 그림 {paths.Count}개)");
        }

        [MenuItem("Tools/Underwrite/Build/웹 데모 빌드")]
        public static void Build()
        {
            var scenes = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled == true)
                {
                    scenes.Add(scene.path);
                }
            }

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            // 창을 꽉 채우는 틀(Assets/WebGLTemplates/Underwrite). 기본 틀은 아래 띠 때문에 스크롤이 생겼다.
            PlayerSettings.WebGL.template = WEB_TEMPLATE;
            PlayerSettings.defaultWebScreenWidth = WIDTH;
            PlayerSettings.defaultWebScreenHeight = HEIGHT;
            PlayerSettings.runInBackground = true;

            if (Directory.Exists(OUTPUT) == true)
            {
                Directory.Delete(OUTPUT, true);
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = OUTPUT,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"웹 데모 빌드 실패: {report.summary.result}, 오류 {report.summary.totalErrors}개");
                return;
            }

            Debug.Log($"웹 데모 빌드 완료: {OUTPUT} ({report.summary.totalSize / (1024 * 1024)}MB)");
        }
    }
}
