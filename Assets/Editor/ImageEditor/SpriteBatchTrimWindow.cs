using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Underwrite.Editor.ImageTools
{
    /// <summary>
    /// <see cref="SpriteRectTrimmer"/>의 UI. 판단과 계산은 전부 Trimmer에 있고 이 창은 입력만 받는다.
    /// 같은 로직을 자동화(Unity Pipeline)에서도 호출하므로 여기에 규칙을 넣지 않는다.
    /// </summary>
    public sealed class SpriteBatchTrimWindow : EditorWindow
    {
        private const int PADDING_MAX = 8;
        private const int SEARCH_MARGIN_MAX = 256;

        private static readonly string[] SCOPE_LABELS =
        {
            "기존 영역 안에서만 (축소만 가능)",
            "기존 영역 밖으로 확장",
            "이미지 전체"
        };

        private static readonly string[] PIVOT_LABELS =
        {
            "기존 기준점 유지 (절대 위치 환산)",
            "일괄 피봇 지정",
            "캔버스 기준점으로 환산"
        };

        private Texture2D Texture;
        private readonly TrimOptions Options = new TrimOptions();
        private string Result = "";
        private Vector2 ResultScroll;
        private Dictionary<string, string> LastBackups;

        [MenuItem("Tools/Underwrite/Sprites/Batch Trim")]
        public static void Open()
        {
            var window = GetWindow<SpriteBatchTrimWindow>("일괄 Sprite Trim");
            window.minSize = new Vector2(460f, 560f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "먼저 Automatic Slice하세요. PNG와 이름·ID는 유지하고 .meta의 Rect와 피봇만 바꿉니다.",
                MessageType.Info);

            Texture = (Texture2D)EditorGUILayout.ObjectField("이미지", Texture, typeof(Texture2D), false);
            Options.AlphaThreshold = EditorGUILayout.IntSlider("알파 기준", Options.AlphaThreshold, 1, 255);
            Options.Padding = EditorGUILayout.IntSlider("여백 (픽셀)", Options.Padding, 0, PADDING_MAX);

            EditorGUILayout.Space();
            DrawScopeSection();

            EditorGUILayout.Space();
            DrawPivotSection();

            EditorGUILayout.Space();
            DrawActionSection();

            if (string.IsNullOrEmpty(Result) == false)
            {
                EditorGUILayout.Space();
                ResultScroll = EditorGUILayout.BeginScrollView(ResultScroll, GUILayout.MinHeight(160f));
                EditorGUILayout.TextArea(Result, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawScopeSection()
        {
            EditorGUILayout.LabelField("탐색 범위", EditorStyles.boldLabel);

            Options.PivotOnly = EditorGUILayout.Toggle("Rect는 그대로 두고 피봇만 바꾸기", Options.PivotOnly);

            if (Options.PivotOnly == true)
            {
                EditorGUILayout.HelpBox("Rect를 다시 계산하지 않고 피봇만 바꿉니다. 탐색 범위는 쓰이지 않습니다.",
                    MessageType.None);
                return;
            }

            Options.Scope = (TrimSearchScope)EditorGUILayout.Popup("범위", (int)Options.Scope, SCOPE_LABELS);

            switch (Options.Scope)
            {
                case TrimSearchScope.Scope_ExistingRect:
                {
                    EditorGUILayout.HelpBox(
                        "기존 Rect 안쪽만 훑습니다. 영역은 줄어들 수만 있습니다. Rect 밖으로 나간 머리카락은 포함되지 않습니다.",
                        MessageType.Warning);
                    break;
                }
                case TrimSearchScope.Scope_ExpandedRect:
                {
                    Options.SearchMargin = EditorGUILayout.IntSlider("여유 (픽셀)", Options.SearchMargin, 1, SEARCH_MARGIN_MAX);
                    EditorGUILayout.HelpBox(
                        "기존 Rect를 지정한 픽셀만큼 넓힌 범위를 훑습니다. 한 이미지에 스프라이트가 여러 개일 때 안전합니다.",
                        MessageType.None);
                    break;
                }
                default:
                {
                    EditorGUILayout.HelpBox(
                        "이미지 전체를 훑습니다. 한 이미지에 스프라이트가 하나일 때만 쓸 수 있습니다. 여러 개면 중단합니다.",
                        MessageType.None);
                    break;
                }
            }
        }

        private void DrawPivotSection()
        {
            EditorGUILayout.LabelField("피봇", EditorStyles.boldLabel);
            Options.PivotMode = (TrimPivotMode)EditorGUILayout.Popup("방식", (int)Options.PivotMode, PIVOT_LABELS);

            switch (Options.PivotMode)
            {
                case TrimPivotMode.Pivot_KeepAbsolute:
                {
                    EditorGUILayout.HelpBox(
                        "기존 피봇의 캔버스 절대 위치를 계산해 새 Rect에 다시 정규화합니다. 기준점이 움직이지 않습니다. "
                        + "alignment가 Custom이 아니면 그 열거값의 피봇을 기준으로 삼습니다.",
                        MessageType.None);
                    break;
                }
                case TrimPivotMode.Pivot_Uniform:
                {
                    Options.UniformPivot = EditorGUILayout.Vector2Field("일괄 피봇 (0~1)", Options.UniformPivot);
                    Options.UniformPivot = new Vector2(
                        Mathf.Clamp01(Options.UniformPivot.x), Mathf.Clamp01(Options.UniformPivot.y));

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Center") == true)
                    {
                        Options.UniformPivot = new Vector2(0.5f, 0.5f);
                    }

                    if (GUILayout.Button("Bottom Center") == true)
                    {
                        Options.UniformPivot = new Vector2(0.5f, 0f);
                    }

                    if (GUILayout.Button("Bottom Left") == true)
                    {
                        Options.UniformPivot = Vector2.zero;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.HelpBox(
                        "값은 각 Rect 기준입니다. Rect가 프레임마다 다르면 같은 0.5라도 절대 위치가 달라져 흔들릴 수 있습니다. "
                        + "미리 계산 결과의 기준점 절대 좌표 편차를 확인하세요.",
                        MessageType.Warning);
                    break;
                }
                default:
                {
                    Options.CanvasPoint = EditorGUILayout.Vector2Field("캔버스 기준점 (픽셀, 좌하단 원점)", Options.CanvasPoint);
                    EditorGUILayout.HelpBox(
                        "캔버스의 한 점을 모든 프레임의 기준점으로 씁니다. Rect가 달라도 절대 위치가 같아 흔들림이 없습니다.",
                        MessageType.None);
                    break;
                }
            }
        }

        private void DrawActionSection()
        {
            using (new EditorGUI.DisabledScope(
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                using (new EditorGUI.DisabledScope(Texture == null))
                {
                    if (GUILayout.Button("이 이미지 미리 계산") == true)
                    {
                        Run(new[] { Texture }, false);
                    }

                    if (GUILayout.Button("이 이미지 적용") == true)
                    {
                        Run(new[] { Texture }, true);
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Project에서 선택한 이미지 미리 계산") == true)
                {
                    Run(Selection.objects.OfType<Texture2D>().ToArray(), false);
                }

                if (GUILayout.Button("Project에서 선택한 이미지 적용") == true)
                {
                    Run(Selection.objects.OfType<Texture2D>().ToArray(), true);
                }

                using (new EditorGUI.DisabledScope(LastBackups == null || LastBackups.Count == 0))
                {
                    if (GUILayout.Button("마지막 작업 복원") == true)
                    {
                        Restore();
                    }
                }
            }
        }

        private void Run(Texture2D[] textures, bool apply)
        {
            try
            {
                if (textures == null || textures.Length == 0)
                {
                    throw new InvalidOperationException("PNG 이미지를 하나 이상 선택하세요.");
                }

                var plans = new List<TrimPlan>();
                var seen = new HashSet<string>();

                for (var i = 0; i < textures.Length; i++)
                {
                    var path = AssetDatabase.GetAssetPath(textures[i]);

                    if (seen.Add(path) == false)
                    {
                        continue;
                    }

                    plans.Add(SpriteRectTrimmer.BuildPlan(path, Options));
                }

                var backupDirectory = "";

                if (apply == true)
                {
                    LastBackups = SpriteRectTrimmer.Apply(plans, out backupDirectory);
                }

                Result = SpriteRectTrimmer.BuildReport(plans, Options, apply, backupDirectory);
            }
            catch (Exception ex)
            {
                Result = ex.Message;
                UnityEngine.Debug.LogError("일괄 Sprite Trim: " + ex);
            }
        }

        private void Restore()
        {
            var confirmed = EditorUtility.DisplayDialog(
                "Trim 복원",
                "이후 변경한 이미지 Import 설정도 마지막 작업 이전으로 되돌아갑니다. 복원할까요?",
                "복원",
                "취소");

            if (confirmed == false)
            {
                return;
            }

            SpriteRectTrimmer.Restore(LastBackups);
            LastBackups = null;
            Result = "마지막 작업 전 Rect와 Import 설정을 복원했습니다.";
        }
    }
}
