using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Underwrite.Editor.ImageTools
{
    /// <summary>
    /// 알파 기준으로 Sprite Rect를 다시 계산하고 피봇을 일괄 정리합니다.
    /// 기존 도구는 기존 Rect 안쪽만 스캔해 영역이 줄어들기만 했습니다. 이 버전은 탐색 범위를 선택할 수 있어
    /// Rect 밖으로 나간 머리카락·무기도 포함할 수 있습니다.
    /// PNG 자체는 바꾸지 않고 `.meta`의 Rect와 피봇만 수정합니다.
    /// </summary>
    public sealed class SpriteBatchTrimWindow : EditorWindow
    {
        private const int ALPHA_THRESHOLD_DEFAULT = 32;
        private const int PADDING_DEFAULT = 1;
        private const int PADDING_MAX = 8;
        private const int SEARCH_MARGIN_MAX = 256;
        private const int REPORT_SAMPLE_MAX = 8;
        private const string BACKUP_ROOT = "Outputs/SpriteBatchTrim";

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

        /// <summary>알파를 어디까지 훑어 Rect를 다시 계산할지 정합니다.</summary>
        private enum SearchScope
        {
            Scope_ExistingRect,
            Scope_ExpandedRect,
            Scope_WholeTexture
        }

        /// <summary>Rect를 바꾼 뒤 피봇을 어떻게 정할지 정합니다.</summary>
        private enum PivotMode
        {
            Pivot_KeepAbsolute,
            Pivot_Uniform,
            Pivot_CanvasPoint
        }

        private Texture2D Texture;
        private int AlphaThreshold = ALPHA_THRESHOLD_DEFAULT;
        private int Padding = PADDING_DEFAULT;
        private SearchScope Scope = SearchScope.Scope_WholeTexture;
        private int SearchMargin = 64;
        private PivotMode Pivot = PivotMode.Pivot_Uniform;
        private bool PivotOnly;
        private Vector2 UniformPivot = new Vector2(0.5f, 0.5f);
        private Vector2 CanvasPoint = new Vector2(256f, 13f);
        private string Result = "";
        private Vector2 ResultScroll;
        private readonly Dictionary<string, string> Backups = new Dictionary<string, string>();

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
            AlphaThreshold = EditorGUILayout.IntSlider("알파 기준", AlphaThreshold, 1, 255);
            Padding = EditorGUILayout.IntSlider("여백 (픽셀)", Padding, 0, PADDING_MAX);

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

            PivotOnly = EditorGUILayout.Toggle("Rect는 그대로 두고 피봇만 바꾸기", PivotOnly);

            if (PivotOnly == true)
            {
                EditorGUILayout.HelpBox("Rect를 다시 계산하지 않고 피봇만 바꿉니다. 탐색 범위는 쓰이지 않습니다.", MessageType.None);
                return;
            }

            Scope = (SearchScope)EditorGUILayout.Popup("범위", (int)Scope, SCOPE_LABELS);

            switch (Scope)
            {
                case SearchScope.Scope_ExistingRect:
                {
                    EditorGUILayout.HelpBox(
                        "기존 Rect 안쪽만 훑습니다. 영역은 줄어들 수만 있습니다. Rect 밖으로 나간 머리카락은 포함되지 않습니다.",
                        MessageType.Warning);
                    break;
                }
                case SearchScope.Scope_ExpandedRect:
                {
                    SearchMargin = EditorGUILayout.IntSlider("여유 (픽셀)", SearchMargin, 1, SEARCH_MARGIN_MAX);
                    EditorGUILayout.HelpBox(
                        "기존 Rect를 지정한 픽셀만큼 넓힌 범위를 훑습니다. 한 이미지에 스프라이트가 여러 개일 때 안전한 선택입니다.",
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
            Pivot = (PivotMode)EditorGUILayout.Popup("방식", (int)Pivot, PIVOT_LABELS);

            switch (Pivot)
            {
                case PivotMode.Pivot_KeepAbsolute:
                {
                    EditorGUILayout.HelpBox(
                        "기존 피봇의 캔버스 절대 위치를 계산해 새 Rect에 다시 정규화합니다. 기준점이 움직이지 않습니다. "
                        + "alignment가 Custom이 아니면 그 열거값의 피봇을 기준으로 삼습니다.",
                        MessageType.None);
                    break;
                }
                case PivotMode.Pivot_Uniform:
                {
                    UniformPivot = EditorGUILayout.Vector2Field("일괄 피봇 (0~1)", UniformPivot);
                    UniformPivot = new Vector2(Mathf.Clamp01(UniformPivot.x), Mathf.Clamp01(UniformPivot.y));

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Center"))
                    {
                        UniformPivot = new Vector2(0.5f, 0.5f);
                    }

                    if (GUILayout.Button("Bottom Center"))
                    {
                        UniformPivot = new Vector2(0.5f, 0f);
                    }

                    if (GUILayout.Button("Bottom Left"))
                    {
                        UniformPivot = Vector2.zero;
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
                    CanvasPoint = EditorGUILayout.Vector2Field("캔버스 기준점 (픽셀, 좌하단 원점)", CanvasPoint);
                    EditorGUILayout.HelpBox(
                        "캔버스의 한 점을 모든 프레임의 기준점으로 씁니다. Rect가 달라도 절대 위치가 같아 흔들림이 없습니다. "
                        + "발밑 기준점을 쓰려면 이 방식을 고르세요.",
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
                    if (GUILayout.Button("이 이미지 미리 계산"))
                    {
                        Run(() => Process(new[] { Texture }, false));
                    }

                    if (GUILayout.Button("이 이미지 적용"))
                    {
                        Run(() => Process(new[] { Texture }, true));
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Project에서 선택한 이미지 미리 계산"))
                {
                    Run(() => Process(Selection.objects.OfType<Texture2D>().ToArray(), false));
                }

                if (GUILayout.Button("Project에서 선택한 이미지 적용"))
                {
                    Run(() => Process(Selection.objects.OfType<Texture2D>().ToArray(), true));
                }

                using (new EditorGUI.DisabledScope(Backups.Count == 0))
                {
                    if (GUILayout.Button("마지막 작업 복원"))
                    {
                        Run(Restore);
                    }
                }
            }
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Result = ex.Message;
                UnityEngine.Debug.LogError("일괄 Sprite Trim: " + ex);
            }
        }

        private sealed class Change
        {
            public string SpriteName;
            public Rect OldRect;
            public Rect NewRect;
            public Vector2 OldPivot;
            public Vector2 NewPivot;
            public Vector2 OldAnchor;
            public Vector2 NewAnchor;
        }

        private sealed class Plan
        {
            public string Path;
            public ISpriteEditorDataProvider Provider;
            public SpriteRect[] Rects;
            public readonly HashSet<SpriteRect> Touched = new HashSet<SpriteRect>();
            public readonly List<Change> Changes = new List<Change>();
            public int Empty;
        }

        private void Process(Texture2D[] textures, bool apply)
        {
            if (textures.Length == 0)
            {
                throw new InvalidOperationException("PNG 이미지를 하나 이상 선택하세요.");
            }

            var plans = textures.Distinct().Select(BuildPlan).ToArray();

            if (apply == true && plans.Any(p => p.Touched.Count > 0) == true)
            {
                ApplyPlans(plans);
            }

            Result = BuildReport(plans, apply);
        }

        private void ApplyPlans(Plan[] plans)
        {
            Backups.Clear();

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupDir = Path.GetFullPath(BACKUP_ROOT + "/" + stamp);
            Directory.CreateDirectory(backupDir);

            foreach (var plan in plans.Where(p => p.Touched.Count > 0))
            {
                var backup = Path.Combine(backupDir, Guid.NewGuid().ToString("N") + ".meta");
                File.Copy(plan.Path + ".meta", backup);
                Backups.Add(plan.Path, backup);
            }

            File.WriteAllText(
                Path.Combine(backupDir, "paths.txt"),
                string.Join(Environment.NewLine, Backups.Select(p => p.Value + " <- " + p.Key)),
                new UTF8Encoding(false));

            try
            {
                foreach (var plan in plans.Where(p => p.Touched.Count > 0))
                {
                    plan.Provider.SetSpriteRects(plan.Rects);
                    plan.Provider.Apply();
                    AssetImporter.GetAtPath(plan.Path).SaveAndReimport();
                }
            }
            catch
            {
                RestoreFiles();
                throw;
            }
        }

        private Plan BuildPlan(Texture2D selected)
        {
            var path = AssetDatabase.GetAssetPath(selected);

            if (path.StartsWith("Assets/", StringComparison.Ordinal) == false
                || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new InvalidOperationException("Assets 폴더의 PNG만 지원합니다: " + path);
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null
                || importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                throw new InvalidOperationException(path + ": Sprite / Multiple로 설정하고 먼저 Slice하세요.");
            }

            var factories = new SpriteDataProviderFactories();
            factories.Init();

            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);

            if (provider == null)
            {
                throw new InvalidOperationException("Sprite 데이터 공급자가 없습니다: " + path);
            }

            provider.InitSpriteEditorDataProvider();

            var rects = provider.GetSpriteRects();

            if (rects.Length == 0)
            {
                throw new InvalidOperationException(path + ": 자른 영역이 없습니다. 먼저 Automatic Slice하세요.");
            }

            if (PivotOnly == false && Scope == SearchScope.Scope_WholeTexture && rects.Length > 1)
            {
                throw new InvalidOperationException(
                    path + ": 스프라이트가 " + rects.Length + "개입니다. 전체 텍스처 범위는 스프라이트가 하나일 때만 쓸 수 있습니다. "
                    + "'기존 영역 밖으로 확장'을 쓰세요.");
            }

            var plan = new Plan { Path = path, Provider = provider, Rects = rects };

            if (PivotOnly == true)
            {
                foreach (var sprite in rects)
                {
                    BuildPivotOnlyChange(plan, sprite);
                }

                return plan;
            }

            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                if (ImageConversion.LoadImage(raw, File.ReadAllBytes(path)) == false)
                {
                    throw new InvalidOperationException("PNG를 읽을 수 없습니다: " + path);
                }

                var pixels = raw.GetPixels32();

                foreach (var sprite in rects)
                {
                    BuildSpriteChange(plan, sprite, pixels, raw.width, raw.height);
                }
            }
            finally
            {
                DestroyImmediate(raw);
            }

            return plan;
        }

        private void BuildSpriteChange(Plan plan, SpriteRect sprite, Color32[] pixels, int width, int height)
        {
            var old = sprite.rect;
            var search = SearchBounds(old, width, height);

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            for (var y = (int)search.yMin; y < (int)search.yMax; y++)
            {
                var row = y * width;

                for (var x = (int)search.xMin; x < (int)search.xMax; x++)
                {
                    if (pixels[row + x].a < AlphaThreshold)
                    {
                        continue;
                    }

                    if (x < minX)
                    {
                        minX = x;
                    }

                    if (x > maxX)
                    {
                        maxX = x;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }

            if (maxX < minX)
            {
                plan.Empty++;
                return;
            }

            minX = Mathf.Max(0, minX - Padding);
            minY = Mathf.Max(0, minY - Padding);
            maxX = Mathf.Min(width - 1, maxX + Padding);
            maxY = Mathf.Min(height - 1, maxY + Padding);

            var next = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            var oldPivot = EffectivePivot(sprite);
            var oldAnchor = old.position + Vector2.Scale(old.size, oldPivot);
            var nextPivot = ResolvePivot(next, oldAnchor);
            var nextAnchor = next.position + Vector2.Scale(next.size, nextPivot);

            if (next == old && nextPivot == oldPivot && sprite.alignment == SpriteAlignment.Custom)
            {
                return;
            }

            plan.Changes.Add(new Change
            {
                SpriteName = sprite.name,
                OldRect = old,
                NewRect = next,
                OldPivot = oldPivot,
                NewPivot = nextPivot,
                OldAnchor = oldAnchor,
                NewAnchor = nextAnchor
            });

            sprite.rect = next;
            sprite.alignment = SpriteAlignment.Custom;
            sprite.pivot = nextPivot;
            plan.Touched.Add(sprite);
        }

        /// <summary>Rect를 유지한 채 피봇만 다시 계산합니다.</summary>
        private void BuildPivotOnlyChange(Plan plan, SpriteRect sprite)
        {
            var rect = sprite.rect;
            var oldPivot = EffectivePivot(sprite);
            var oldAnchor = rect.position + Vector2.Scale(rect.size, oldPivot);
            var nextPivot = ResolvePivot(rect, oldAnchor);

            if (nextPivot == oldPivot && sprite.alignment == SpriteAlignment.Custom)
            {
                return;
            }

            plan.Changes.Add(new Change
            {
                SpriteName = sprite.name,
                OldRect = rect,
                NewRect = rect,
                OldPivot = oldPivot,
                NewPivot = nextPivot,
                OldAnchor = oldAnchor,
                NewAnchor = rect.position + Vector2.Scale(rect.size, nextPivot)
            });

            sprite.alignment = SpriteAlignment.Custom;
            sprite.pivot = nextPivot;
            plan.Touched.Add(sprite);
        }

        private Rect SearchBounds(Rect old, int width, int height)
        {
            switch (Scope)
            {
                case SearchScope.Scope_ExistingRect:
                {
                    var x0 = Mathf.Clamp(Mathf.FloorToInt(old.xMin), 0, width);
                    var y0 = Mathf.Clamp(Mathf.FloorToInt(old.yMin), 0, height);
                    var x1 = Mathf.Clamp(Mathf.CeilToInt(old.xMax), 0, width);
                    var y1 = Mathf.Clamp(Mathf.CeilToInt(old.yMax), 0, height);
                    return Rect.MinMaxRect(x0, y0, x1, y1);
                }
                case SearchScope.Scope_ExpandedRect:
                {
                    var x0 = Mathf.Clamp(Mathf.FloorToInt(old.xMin) - SearchMargin, 0, width);
                    var y0 = Mathf.Clamp(Mathf.FloorToInt(old.yMin) - SearchMargin, 0, height);
                    var x1 = Mathf.Clamp(Mathf.CeilToInt(old.xMax) + SearchMargin, 0, width);
                    var y1 = Mathf.Clamp(Mathf.CeilToInt(old.yMax) + SearchMargin, 0, height);
                    return Rect.MinMaxRect(x0, y0, x1, y1);
                }
                default:
                {
                    return Rect.MinMaxRect(0f, 0f, width, height);
                }
            }
        }

        private Vector2 ResolvePivot(Rect next, Vector2 oldAnchor)
        {
            switch (Pivot)
            {
                case PivotMode.Pivot_Uniform:
                {
                    return UniformPivot;
                }
                case PivotMode.Pivot_CanvasPoint:
                {
                    return new Vector2(
                        (CanvasPoint.x - next.x) / next.width,
                        (CanvasPoint.y - next.y) / next.height);
                }
                default:
                {
                    return new Vector2(
                        (oldAnchor.x - next.x) / next.width,
                        (oldAnchor.y - next.y) / next.height);
                }
            }
        }

        /// <summary>
        /// alignment가 Custom이 아니면 저장된 pivot 값이 쓰이지 않습니다. 열거값에 해당하는 피봇을 돌려줍니다.
        /// </summary>
        private static Vector2 EffectivePivot(SpriteRect sprite)
        {
            switch (sprite.alignment)
            {
                case SpriteAlignment.Center:
                {
                    return new Vector2(0.5f, 0.5f);
                }
                case SpriteAlignment.TopLeft:
                {
                    return new Vector2(0f, 1f);
                }
                case SpriteAlignment.TopCenter:
                {
                    return new Vector2(0.5f, 1f);
                }
                case SpriteAlignment.TopRight:
                {
                    return new Vector2(1f, 1f);
                }
                case SpriteAlignment.LeftCenter:
                {
                    return new Vector2(0f, 0.5f);
                }
                case SpriteAlignment.RightCenter:
                {
                    return new Vector2(1f, 0.5f);
                }
                case SpriteAlignment.BottomLeft:
                {
                    return Vector2.zero;
                }
                case SpriteAlignment.BottomCenter:
                {
                    return new Vector2(0.5f, 0f);
                }
                case SpriteAlignment.BottomRight:
                {
                    return new Vector2(1f, 0f);
                }
                default:
                {
                    return sprite.pivot;
                }
            }
        }

        private string BuildReport(Plan[] plans, bool apply)
        {
            var text = new StringBuilder();
            text.AppendLine(apply == true ? "적용 완료" : "미리 계산 — 아직 변경하지 않았습니다");
            var scopeText = PivotOnly == true ? "피봇만 변경" : SCOPE_LABELS[(int)Scope];
            text.AppendLine($"탐색 범위: {scopeText}  피봇 방식: {PIVOT_LABELS[(int)Pivot]}  알파 기준: {AlphaThreshold}  여백: {Padding}");
            text.AppendLine();

            var all = plans.SelectMany(p => p.Changes).ToArray();

            if (all.Length > 0)
            {
                var ax = all.Select(c => c.NewAnchor.x).ToArray();
                var ay = all.Select(c => c.NewAnchor.y).ToArray();
                text.AppendLine($"기준점 절대 좌표 X: {ax.Min():0.#} ~ {ax.Max():0.#} (편차 {ax.Max() - ax.Min():0.#}px)");
                text.AppendLine($"기준점 절대 좌표 Y: {ay.Min():0.#} ~ {ay.Max():0.#} (편차 {ay.Max() - ay.Min():0.#}px)");
                text.AppendLine("편차가 0이 아니면 재생 중 그만큼 흔들립니다.");
                text.AppendLine();
            }

            foreach (var plan in plans)
            {
                text.AppendLine($"{Path.GetFileName(plan.Path)}: 전체 {plan.Rects.Length}, 변경 {plan.Touched.Count}, 기준 이상 픽셀 없음 {plan.Empty}");

                foreach (var c in plan.Changes.Take(REPORT_SAMPLE_MAX))
                {
                    text.AppendLine($"  {c.SpriteName}");
                    text.AppendLine($"    Rect  {RectText(c.OldRect)} -> {RectText(c.NewRect)}");
                    text.AppendLine($"    피봇  ({c.OldPivot.x:0.####}, {c.OldPivot.y:0.####}) -> ({c.NewPivot.x:0.####}, {c.NewPivot.y:0.####})");
                    text.AppendLine($"    기준점 ({c.OldAnchor.x:0.#}, {c.OldAnchor.y:0.#}) -> ({c.NewAnchor.x:0.#}, {c.NewAnchor.y:0.#})");
                }

                if (plan.Changes.Count > REPORT_SAMPLE_MAX)
                {
                    text.AppendLine($"  ... 외 {plan.Changes.Count - REPORT_SAMPLE_MAX}개");
                }
            }

            if (apply == true && Backups.Count > 0)
            {
                text.AppendLine();
                text.AppendLine($"백업 {Backups.Count}개: {BACKUP_ROOT}");
            }

            return text.ToString();
        }

        private static string RectText(Rect r)
        {
            return $"x{r.x:0} y{r.y:0} w{r.width:0} h{r.height:0}";
        }

        private void RestoreFiles()
        {
            foreach (var pair in Backups)
            {
                File.Copy(pair.Value, pair.Key + ".meta", true);
                AssetDatabase.ImportAsset(pair.Key, ImportAssetOptions.ForceUpdate);
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

            RestoreFiles();
            Backups.Clear();
            Result = "마지막 작업 전 Rect와 Import 설정을 복원했습니다.";
        }
    }
}
