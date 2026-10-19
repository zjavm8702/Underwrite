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
    /// 올릴 zip은 `Tools/web/zip_web_demo.py`로 묶는다 — 윈도 압축은 경로에 역슬래시를 넣어 웹 호스팅에서 깨질 수 있다.
    /// </summary>
    public static class WebDemoBuild
    {
        private const string OUTPUT = "Builds/WebDemo";
        private const int WIDTH = 1280;
        private const int HEIGHT = 720;

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
