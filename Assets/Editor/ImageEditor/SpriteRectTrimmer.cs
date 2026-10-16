using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Underwrite.Editor.ImageTools
{
    /// <summary>알파를 어디까지 훑어 Rect를 다시 계산할지 정한다.</summary>
    public enum TrimSearchScope
    {
        Scope_ExistingRect,
        Scope_ExpandedRect,
        Scope_WholeTexture
    }

    /// <summary>Rect를 바꾼 뒤 피봇을 어떻게 정할지 정한다.</summary>
    public enum TrimPivotMode
    {
        Pivot_KeepAbsolute,
        Pivot_Uniform,
        Pivot_CanvasPoint
    }

    /// <summary>Trim 설정 한 벌. 창과 자동화가 같은 값을 쓴다.</summary>
    public sealed class TrimOptions
    {
        public int AlphaThreshold = 32;
        public int Padding = 1;
        public TrimSearchScope Scope = TrimSearchScope.Scope_WholeTexture;
        public int SearchMargin = 64;
        public TrimPivotMode PivotMode = TrimPivotMode.Pivot_Uniform;
        public Vector2 UniformPivot = new Vector2(0.5f, 0.5f);
        public Vector2 CanvasPoint = new Vector2(256f, 13f);

        /// <summary>참이면 Rect를 다시 계산하지 않고 피봇만 바꾼다.</summary>
        public bool PivotOnly;
    }

    /// <summary>스프라이트 하나의 변경 내역.</summary>
    public sealed class TrimChange
    {
        public string SpriteName;
        public Rect OldRect;
        public Rect NewRect;
        public Vector2 OldPivot;
        public Vector2 NewPivot;
        public Vector2 OldAnchor;
        public Vector2 NewAnchor;
    }

    /// <summary>텍스처 한 장의 계획. 적용 전까지 에셋을 바꾸지 않는다.</summary>
    public sealed class TrimPlan
    {
        public string Path;
        public ISpriteEditorDataProvider Provider;
        public SpriteRect[] Rects;
        public readonly HashSet<SpriteRect> Touched = new HashSet<SpriteRect>();
        public readonly List<TrimChange> Changes = new List<TrimChange>();
        public int Empty;
    }

    /// <summary>
    /// Sprite Rect를 알파 기준으로 다시 계산하고 피봇을 정리하는 실제 로직.
    ///
    /// UI와 분리해 두었으므로 에디터 창에서도, 자동화(Unity Pipeline 등)에서도 같은 코드를 쓴다.
    /// 창에만 로직이 있으면 자동 실행이 불가능하고, 복제하면 두 경로가 어긋난다.
    ///
    /// PNG는 바꾸지 않는다. `.meta`의 Rect와 피봇만 수정한다.
    /// </summary>
    public static class SpriteRectTrimmer
    {
        public const string BACKUP_ROOT = "Outputs/SpriteBatchTrim";
        private const int REPORT_SAMPLE_MAX = 8;

        /// <summary>
        /// 텍스처 하나의 변경 계획을 만든다. 에셋은 바꾸지 않는다.
        /// </summary>
        public static TrimPlan BuildPlan(Texture2D texture, TrimOptions options)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            return BuildPlan(AssetDatabase.GetAssetPath(texture), options);
        }

        public static TrimPlan BuildPlan(string path, TrimOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

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

            if (options.PivotOnly == false
                && options.Scope == TrimSearchScope.Scope_WholeTexture
                && rects.Length > 1)
            {
                throw new InvalidOperationException(
                    path + ": 스프라이트가 " + rects.Length + "개입니다. 전체 텍스처 범위는 스프라이트가 하나일 때만 "
                    + "쓸 수 있습니다. '기존 영역 밖으로 확장'을 쓰세요.");
            }

            var plan = new TrimPlan { Path = path, Provider = provider, Rects = rects };

            if (options.PivotOnly == true)
            {
                foreach (var sprite in rects)
                {
                    BuildPivotOnlyChange(plan, sprite, options);
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
                    BuildSpriteChange(plan, sprite, pixels, raw.width, raw.height, options);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raw);
            }

            return plan;
        }

        /// <summary>
        /// 계획을 실제로 적용한다. 적용 전에 `.meta` 원본을 백업하고, 실패하면 전부 되돌린다.
        /// </summary>
        public static Dictionary<string, string> Apply(IList<TrimPlan> plans, out string backupDirectory)
        {
            if (plans == null)
            {
                throw new ArgumentNullException(nameof(plans));
            }

            var backups = new Dictionary<string, string>();
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            backupDirectory = Path.GetFullPath(BACKUP_ROOT + "/" + stamp);
            Directory.CreateDirectory(backupDirectory);

            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];

                if (plan.Touched.Count == 0)
                {
                    continue;
                }

                var backup = Path.Combine(backupDirectory, Guid.NewGuid().ToString("N") + ".meta");
                File.Copy(plan.Path + ".meta", backup);
                backups.Add(plan.Path, backup);
            }

            var lines = new List<string>();

            foreach (var pair in backups)
            {
                lines.Add(pair.Value + " <- " + pair.Key);
            }

            File.WriteAllText(Path.Combine(backupDirectory, "paths.txt"),
                string.Join(Environment.NewLine, lines), new UTF8Encoding(false));

            try
            {
                for (var i = 0; i < plans.Count; i++)
                {
                    var plan = plans[i];

                    if (plan.Touched.Count == 0)
                    {
                        continue;
                    }

                    plan.Provider.SetSpriteRects(plan.Rects);
                    plan.Provider.Apply();
                    AssetImporter.GetAtPath(plan.Path).SaveAndReimport();
                }
            }
            catch
            {
                Restore(backups);
                throw;
            }

            return backups;
        }

        /// <summary>백업한 `.meta`를 되돌린다.</summary>
        public static void Restore(Dictionary<string, string> backups)
        {
            if (backups == null)
            {
                return;
            }

            foreach (var pair in backups)
            {
                File.Copy(pair.Value, pair.Key + ".meta", true);
                AssetDatabase.ImportAsset(pair.Key, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void BuildPivotOnlyChange(TrimPlan plan, SpriteRect sprite, TrimOptions options)
        {
            var rect = sprite.rect;
            var oldPivot = EffectivePivot(sprite);
            var oldAnchor = rect.position + Vector2.Scale(rect.size, oldPivot);
            var nextPivot = ResolvePivot(rect, oldAnchor, options);

            if (nextPivot == oldPivot && sprite.alignment == SpriteAlignment.Custom)
            {
                return;
            }

            plan.Changes.Add(new TrimChange
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

        private static void BuildSpriteChange(TrimPlan plan, SpriteRect sprite, Color32[] pixels,
            int width, int height, TrimOptions options)
        {
            var old = sprite.rect;
            var search = SearchBounds(old, width, height, options);

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            for (var y = (int)search.yMin; y < (int)search.yMax; y++)
            {
                var row = y * width;

                for (var x = (int)search.xMin; x < (int)search.xMax; x++)
                {
                    if (pixels[row + x].a < options.AlphaThreshold)
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

            minX = Mathf.Max(0, minX - options.Padding);
            minY = Mathf.Max(0, minY - options.Padding);
            maxX = Mathf.Min(width - 1, maxX + options.Padding);
            maxY = Mathf.Min(height - 1, maxY + options.Padding);

            var next = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            var oldPivot = EffectivePivot(sprite);
            var oldAnchor = old.position + Vector2.Scale(old.size, oldPivot);
            var nextPivot = ResolvePivot(next, oldAnchor, options);
            var nextAnchor = next.position + Vector2.Scale(next.size, nextPivot);

            if (next == old && nextPivot == oldPivot && sprite.alignment == SpriteAlignment.Custom)
            {
                return;
            }

            plan.Changes.Add(new TrimChange
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

        private static Rect SearchBounds(Rect old, int width, int height, TrimOptions options)
        {
            switch (options.Scope)
            {
                case TrimSearchScope.Scope_ExistingRect:
                {
                    var x0 = Mathf.Clamp(Mathf.FloorToInt(old.xMin), 0, width);
                    var y0 = Mathf.Clamp(Mathf.FloorToInt(old.yMin), 0, height);
                    var x1 = Mathf.Clamp(Mathf.CeilToInt(old.xMax), 0, width);
                    var y1 = Mathf.Clamp(Mathf.CeilToInt(old.yMax), 0, height);
                    return Rect.MinMaxRect(x0, y0, x1, y1);
                }
                case TrimSearchScope.Scope_ExpandedRect:
                {
                    var x0 = Mathf.Clamp(Mathf.FloorToInt(old.xMin) - options.SearchMargin, 0, width);
                    var y0 = Mathf.Clamp(Mathf.FloorToInt(old.yMin) - options.SearchMargin, 0, height);
                    var x1 = Mathf.Clamp(Mathf.CeilToInt(old.xMax) + options.SearchMargin, 0, width);
                    var y1 = Mathf.Clamp(Mathf.CeilToInt(old.yMax) + options.SearchMargin, 0, height);
                    return Rect.MinMaxRect(x0, y0, x1, y1);
                }
                default:
                {
                    return Rect.MinMaxRect(0f, 0f, width, height);
                }
            }
        }

        private static Vector2 ResolvePivot(Rect next, Vector2 oldAnchor, TrimOptions options)
        {
            switch (options.PivotMode)
            {
                case TrimPivotMode.Pivot_Uniform:
                {
                    return options.UniformPivot;
                }
                case TrimPivotMode.Pivot_CanvasPoint:
                {
                    return new Vector2(
                        (options.CanvasPoint.x - next.x) / next.width,
                        (options.CanvasPoint.y - next.y) / next.height);
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
        /// alignment가 Custom이 아니면 저장된 pivot 값이 쓰이지 않는다. 열거값에 해당하는 피봇을 돌려준다.
        /// 이 변환을 빼먹으면 Center로 맞춰 둔 스프라이트가 좌하단으로 튄다.
        /// </summary>
        public static Vector2 EffectivePivot(SpriteRect sprite)
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

        /// <summary>창과 자동화가 같은 형식으로 결과를 보고한다.</summary>
        public static string BuildReport(IList<TrimPlan> plans, TrimOptions options, bool applied,
            string backupDirectory)
        {
            var text = new StringBuilder();
            text.AppendLine(applied == true ? "적용 완료" : "미리 계산 — 아직 변경하지 않았습니다");

            var scopeText = options.PivotOnly == true ? "피봇만 변경" : options.Scope.ToString();
            text.AppendLine($"탐색 범위: {scopeText}  피봇 방식: {options.PivotMode}  "
                + $"알파 기준: {options.AlphaThreshold}  여백: {options.Padding}");
            text.AppendLine();

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            var count = 0;

            for (var i = 0; i < plans.Count; i++)
            {
                for (var j = 0; j < plans[i].Changes.Count; j++)
                {
                    var a = plans[i].Changes[j].NewAnchor;
                    count++;

                    if (a.x < minX)
                    {
                        minX = a.x;
                    }

                    if (a.x > maxX)
                    {
                        maxX = a.x;
                    }

                    if (a.y < minY)
                    {
                        minY = a.y;
                    }

                    if (a.y > maxY)
                    {
                        maxY = a.y;
                    }
                }
            }

            if (count > 0)
            {
                text.AppendLine($"기준점 절대 좌표 X: {minX:0.#} ~ {maxX:0.#} (편차 {maxX - minX:0.#}px)");
                text.AppendLine($"기준점 절대 좌표 Y: {minY:0.#} ~ {maxY:0.#} (편차 {maxY - minY:0.#}px)");
                text.AppendLine("편차가 0이 아니면 재생 중 그만큼 흔들립니다.");
                text.AppendLine();
            }

            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                text.AppendLine($"{Path.GetFileName(plan.Path)}: 전체 {plan.Rects.Length}, "
                    + $"변경 {plan.Touched.Count}, 기준 이상 픽셀 없음 {plan.Empty}");

                for (var j = 0; j < plan.Changes.Count && j < REPORT_SAMPLE_MAX; j++)
                {
                    var c = plan.Changes[j];
                    text.AppendLine($"  {c.SpriteName}");
                    text.AppendLine($"    Rect  {RectText(c.OldRect)} -> {RectText(c.NewRect)}");
                    text.AppendLine($"    피봇  ({c.OldPivot.x:0.####}, {c.OldPivot.y:0.####})"
                        + $" -> ({c.NewPivot.x:0.####}, {c.NewPivot.y:0.####})");
                    text.AppendLine($"    기준점 ({c.OldAnchor.x:0.#}, {c.OldAnchor.y:0.#})"
                        + $" -> ({c.NewAnchor.x:0.#}, {c.NewAnchor.y:0.#})");
                }

                if (plan.Changes.Count > REPORT_SAMPLE_MAX)
                {
                    text.AppendLine($"  ... 외 {plan.Changes.Count - REPORT_SAMPLE_MAX}개");
                }
            }

            if (applied == true && string.IsNullOrEmpty(backupDirectory) == false)
            {
                text.AppendLine();
                text.AppendLine("백업: " + backupDirectory);
            }

            return text.ToString();
        }

        private static string RectText(Rect r)
        {
            return $"x{r.x:0} y{r.y:0} w{r.width:0} h{r.height:0}";
        }
    }
}
