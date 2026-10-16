using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Underwrite.Editor.AnimationTools
{
    [Serializable]
    public sealed class DirectionalAnimationSettings
    {
        public string sourceFolder = "Assets/Images/Characters/Protagonist/Cheer";
        public string motionName = "Cheer";
        public string characterName = "Protagonist";
        public string outputFolder = "Assets/Animations/Generated/Protagonist";
        public SpriteLibraryAsset library;
        public AnimatorController controller;
        public string resolverPath = "";
        public string parameterX = "DirectionX";
        public string parameterY = "DirectionY";
        public int layerIndex;
        public float framesPerSecond = 8f;
        public string durationsMs = "240,120,120,120,300,180,180,300";
        public bool loop;
        public bool allowUpdate;
    }

    public sealed class DirectionalAnimationPlan
    {
        public string libraryPath, controllerPath;
        public SpriteLibraryAsset library;
        public AnimatorController controller;
        public readonly List<DirectionFrames> directions = new List<DirectionFrames>();
        public float[] durations;
        public readonly List<string> updates = new List<string>();
        public float Length => durations.Sum();
    }

    public sealed class DirectionFrames
    {
        public string code, category, clipPath;
        public Vector2 position;
        public Sprite[] sprites;
    }

    public static class DirectionalAnimationBuilder
    {
        public static readonly string[] Codes = { "S", "SE", "E", "NE", "N", "NW", "W", "SW" };
        static readonly string[] Names = { "South", "Southeast", "East", "Northeast", "North", "Northwest", "West", "Southwest" };
        static readonly Vector2[] Positions = {
            new Vector2(0,-1), new Vector2(1,-1).normalized, new Vector2(1,0), new Vector2(1,1).normalized,
            new Vector2(0,1), new Vector2(-1,1).normalized, new Vector2(-1,0), new Vector2(-1,-1).normalized
        };

        public static DirectionalAnimationPlan Analyze(DirectionalAnimationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            RequireAssetFolder(settings.sourceFolder, true);
            RequireAssetFolder(settings.outputFolder, false);
            RequireName(settings.motionName, "동작 이름");
            RequireName(settings.characterName, "캐릭터 이름");
            if (string.IsNullOrWhiteSpace(settings.parameterX) || string.IsNullOrWhiteSpace(settings.parameterY) || settings.parameterX == settings.parameterY)
                throw new InvalidOperationException("방향 파라미터 두 개에 서로 다른 이름을 입력하세요.");
            if (settings.resolverPath.StartsWith("/") || settings.resolverPath.EndsWith("/") || settings.resolverPath.Contains("//"))
                throw new InvalidOperationException("Resolver 경로는 Animator 기준 상대 경로입니다. 같은 오브젝트라면 비워두세요.");
            var plan = new DirectionalAnimationPlan {
                libraryPath = settings.library ? AssetDatabase.GetAssetPath(settings.library) : settings.outputFolder + "/" + settings.characterName + ".spriteLib",
                controllerPath = settings.controller ? AssetDatabase.GetAssetPath(settings.controller) : settings.outputFolder + "/" + settings.characterName + ".controller"
            };
            if (!plan.libraryPath.EndsWith(".spriteLib", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Sprite Library는 편집 가능한 .spriteLib 에셋을 선택하세요.");
            plan.library = settings.library ? settings.library : AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(plan.libraryPath);
            plan.controller = settings.controller ? settings.controller : AssetDatabase.LoadAssetAtPath<AnimatorController>(plan.controllerPath);
            CheckAssetType<SpriteLibraryAsset>(plan.libraryPath);
            CheckAssetType<AnimatorController>(plan.controllerPath);
            if (plan.controller)
            {
                if (settings.layerIndex < 0 || settings.layerIndex >= plan.controller.layers.Length)
                    throw new InvalidOperationException("선택한 Animator 레이어 번호가 없습니다.");
                if (plan.controller.layers[settings.layerIndex].syncedLayerIndex >= 0)
                    throw new InvalidOperationException("동기화 레이어 대신 독립 레이어를 선택하세요.");
                foreach (var name in new[] { settings.parameterX, settings.parameterY })
                    if (plan.controller.parameters.Any(p => p.name == name && p.type != AnimatorControllerParameterType.Float))
                        throw new InvalidOperationException(name + " 파라미터가 Float가 아닙니다. 기존 타입은 자동 변경하지 않습니다.");
                var states = plan.controller.layers[settings.layerIndex].stateMachine.states.Where(s => s.state.name == settings.motionName).ToArray();
                if (states.Length > 1) throw new InvalidOperationException("같은 이름의 상태가 여러 개 있습니다: " + settings.motionName);
                if (states.Length == 1) plan.updates.Add("Animator 상태: " + settings.motionName);
            }
            else if (settings.layerIndex != 0) throw new InvalidOperationException("새 Controller의 레이어 번호는 0을 사용하세요.");

            var existingCategories = plan.library ? new HashSet<string>(plan.library.GetCategoryNames()) : new HashSet<string>();
            for (int i = 0; i < Codes.Length; i++)
            {
                string folder = settings.sourceFolder + "/" + Codes[i];
                RequireAssetFolder(folder, true);
                var paths = Directory.GetFiles(folder).Where(IsTextureFile).Select(p => p.Replace('\\', '/')).ToList();
                paths.Sort(EditorUtility.NaturalCompare);
                var sprites = new List<Sprite>();
                foreach (string path in paths)
                {
                    var entries = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
                    entries.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
                    if (entries.Count == 0)
                        throw new InvalidOperationException(path + "에 Sprite가 없습니다. Texture Type을 Sprite로 설정하고 Multiple이면 먼저 Slice하세요. 임포트 설정은 자동 변경하지 않습니다.");
                    sprites.AddRange(entries);
                }
                if (sprites.Count == 0) throw new InvalidOperationException(folder + "에 프레임이 없습니다.");
                if (sprites.Any(s => string.IsNullOrEmpty(s.name)) || sprites.Select(s => s.name).Distinct().Count() != sprites.Count)
                    throw new InvalidOperationException(folder + "의 Sprite 이름이 비어 있거나 중복됩니다. 라벨이 겹치지 않도록 이름을 바꾸세요.");
                var direction = new DirectionFrames {
                    code = Codes[i], category = settings.motionName + "_" + Names[i], position = Positions[i], sprites = sprites.ToArray(),
                    clipPath = settings.outputFolder + "/" + settings.motionName + "/" + settings.motionName + "_" + Codes[i] + ".anim"
                };
                CheckAssetType<AnimationClip>(direction.clipPath);
                var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(direction.clipPath);
                if (existingClip && AnimationUtility.GetCurveBindings(existingClip).Any(b => b.type == typeof(SpriteResolver) && b.path != settings.resolverPath))
                    throw new InvalidOperationException(direction.clipPath + "의 기존 Resolver 경로가 다릅니다. 같은 경로를 사용하거나 새 저장 폴더를 선택하세요.");
                if (File.Exists(direction.clipPath)) plan.updates.Add("클립: " + direction.clipPath);
                if (existingCategories.Contains(direction.category)) plan.updates.Add("Library 카테고리: " + direction.category);
                plan.directions.Add(direction);
            }
            if (plan.directions.Select(d => d.sprites.Length).Distinct().Count() != 1)
                throw new InvalidOperationException("8방향 프레임 수가 다릅니다. " + string.Join(", ", plan.directions.Select(d => d.code + "=" + d.sprites.Length)));
            plan.durations = ParseDurations(settings.durationsMs, settings.framesPerSecond, plan.directions[0].sprites.Length);
            return plan;
        }

        public static string Build(DirectionalAnimationSettings settings)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Play 모드와 컴파일이 끝난 뒤 실행하세요.");
            var plan = Analyze(settings);
            if (plan.updates.Count > 0 && !settings.allowUpdate)
                throw new InvalidOperationException("이미 같은 동작이 있습니다. 검사 결과를 확인한 뒤 ‘같은 동작 갱신 허용’을 켜세요.");
            var targets = new List<string> { plan.libraryPath, plan.controllerPath };
            targets.AddRange(plan.directions.Select(d => d.clipPath));
            foreach (string path in targets)
                if (File.Exists(path) && !AssetDatabase.IsOpenForEdit(path))
                    throw new InvalidOperationException("쓰기 가능한 에셋이 아닙니다: " + path);
            // File copies are recovery snapshots only; all asset authoring below uses Editor APIs.
            string backup = "Library/UnderwriteAnimationBuilder/" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var existed = targets.Where(File.Exists).ToHashSet();
            // The .spriteLib importer calls AddCategoryLabel/SetDirty while producing its
            // runtime asset. That flag is not an unsaved edit of the source library.
            // Its current categories are merged below through the source-asset factory.
            var dirty = targets.Select(AssetDatabase.LoadMainAssetAtPath)
                .Where(o => o && !(o is SpriteLibraryAsset) && EditorUtility.IsDirty(o)).ToArray();
            if (dirty.Length > 0)
                throw new InvalidOperationException("Controller 또는 Clip에 저장하지 않은 변경이 있습니다. 먼저 저장하세요: " + string.Join(", ", dirty.Select(o => AssetDatabase.GetAssetPath(o))));
            if (Resources.FindObjectsOfTypeAll<EditorWindow>().Any(w => w.hasUnsavedChanges && w.GetType().FullName.Contains("SpriteLibraryEditorWindow")))
                throw new InvalidOperationException("Sprite Library 편집 창에 저장하지 않은 변경이 있습니다. 해당 창에서 Save한 뒤 다시 생성하세요.");
            Directory.CreateDirectory(backup);
            foreach (string path in existed)
            {
                string copy = backup + "/" + path;
                Directory.CreateDirectory(Path.GetDirectoryName(copy));
                File.Copy(path, copy, false);
            }
            var created = new List<string>();
            try
            {
                EnsureFolder(settings.outputFolder);
                EnsureFolder(settings.outputFolder + "/" + settings.motionName);
                var merged = ReadCategories(plan.library);
                foreach (var direction in plan.directions)
                {
                    merged.RemoveAll(c => c.name == direction.category);
                    merged.Add(new SpriteLibraryCategory(direction.category, direction.sprites.Select(s => new SpriteLibraryLabel(s.name, s))));
                }
                string parentLibraryPath = PrimaryLibraryPath(plan.libraryPath);
                if (!existed.Contains(plan.libraryPath)) created.Add(plan.libraryPath);
                SpriteLibrarySourceAssetFactory.Create(plan.libraryPath, merged, parentLibraryPath);
                AssetDatabase.ImportAsset(plan.libraryPath, ImportAssetOptions.ForceSynchronousImport);
                var library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(plan.libraryPath);
                if (!library) throw new InvalidOperationException("Sprite Library 저장에 실패했습니다.");

                var clips = new List<AnimationClip>();
                foreach (var direction in plan.directions)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(direction.clipPath);
                    if (!clip)
                    {
                        clip = new AnimationClip { name = settings.motionName + "_" + direction.code };
                        created.Add(direction.clipPath);
                        AssetDatabase.CreateAsset(clip, direction.clipPath);
                    }
                    PopulateClip(clip, direction, settings, plan.durations, library);
                    AssetDatabase.SaveAssetIfDirty(clip);
                    clips.Add(clip);
                }
                var controller = plan.controller;
                if (!controller)
                {
                    created.Add(plan.controllerPath);
                    controller = AnimatorController.CreateAnimatorControllerAtPath(plan.controllerPath);
                }
                EnsureParameter(controller, settings.parameterX, 0);
                EnsureParameter(controller, settings.parameterY, -1);
                var machine = controller.layers[settings.layerIndex].stateMachine;
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == settings.motionName);
                if (!state)
                {
                    state = machine.AddState(settings.motionName, new Vector3(340, 100 + machine.states.Length * 70));
                    state.writeDefaultValues = false;
                }
                // Replace only this state's motion. Other states and transition conditions stay intact.
                var tree = new BlendTree {
                    name = settings.motionName + "_8Directions", blendType = BlendTreeType.SimpleDirectional2D,
                    blendParameter = settings.parameterX, blendParameterY = settings.parameterY, useAutomaticThresholds = false
                };
                AssetDatabase.AddObjectToAsset(tree, controller);
                for (int i = 0; i < clips.Count; i++) tree.AddChild(clips[i], plan.directions[i].position);
                var previousTree = state.motion as BlendTree;
                state.motion = tree;
                if (previousTree && previousTree.name == tree.name && AssetDatabase.GetAssetPath(previousTree) == plan.controllerPath && !IsUsedElsewhere(controller, state, previousTree))
                    UnityEngine.Object.DestroyImmediate(previousTree, true);
                EditorUtility.SetDirty(state);
                EditorUtility.SetDirty(machine);
                EditorUtility.SetDirty(tree);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssetIfDirty(controller);
                return "완료: 8개 카테고리, " + plan.directions.Sum(d => d.sprites.Length) + "개 라벨, 클립 8개, 2D Blend Tree 1개\n"
                    + "Library: " + plan.libraryPath + "\nController: " + plan.controllerPath
                    + "\n기존 상태·전환 조건 유지. 새 상태의 진입/복귀 조건은 Animator에서 설정하세요.\n복구 사본: " + backup;
            }
            catch
            {
                foreach (string path in created.AsEnumerable().Reverse())
                    if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                foreach (string path in existed)
                {
                    File.Copy(backup + "/" + path, path, true);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
                throw;
            }
        }

        static void PopulateClip(AnimationClip clip, DirectionFrames direction, DirectionalAnimationSettings settings, float[] durations, SpriteLibraryAsset library)
        {
            var temp = new GameObject("AnimationBuilder Hash Probe") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                temp.AddComponent<SpriteRenderer>();
                temp.AddComponent<SpriteLibrary>().spriteLibraryAsset = library;
                var resolver = temp.AddComponent<SpriteResolver>();
                var serialized = new SerializedObject(resolver);
                var keys = new List<Keyframe>();
                float time = 0;
                for (int i = 0; i < direction.sprites.Length; i++)
                {
                    if (!resolver.SetCategoryAndLabel(direction.category, direction.sprites[i].name))
                        throw new InvalidOperationException("해석할 수 없는 Sprite 라벨: " + direction.category + "/" + direction.sprites[i].name);
                    serialized.Update();
                    var hash = serialized.FindProperty("m_SpriteHash");
                    if (hash == null) throw new InvalidOperationException("현재 2D Animation 패키지의 SpriteResolver 기록 방식이 다릅니다.");
                    // Discrete integer curves use float bit patterns, not a numeric int-to-float cast.
                    float value = BitConverter.ToSingle(BitConverter.GetBytes(hash.intValue), 0);
                    keys.Add(new Keyframe(time, value, float.PositiveInfinity, float.PositiveInfinity));
                    time += durations[i];
                }
                keys.Add(new Keyframe(time, keys[keys.Count - 1].value, float.PositiveInfinity, float.PositiveInfinity));
                var binding = EditorCurveBinding.DiscreteCurve(settings.resolverPath, typeof(SpriteResolver), "m_SpriteHash");
                AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keys.ToArray()));
                clip.frameRate = 60;
                var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                clipSettings.loopTime = settings.loop;
                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
                EditorUtility.SetDirty(clip);
            }
            finally { UnityEngine.Object.DestroyImmediate(temp); }
        }

        static bool IsUsedElsewhere(AnimatorController controller, AnimatorState except, Motion target)
        {
            foreach (var layer in controller.layers)
                if (MachineUses(layer.stateMachine, except, target)) return true;
            return false;
        }
        static bool MachineUses(AnimatorStateMachine machine, AnimatorState except, Motion target)
        {
            if (machine.states.Any(s => s.state != except && MotionUses(s.state.motion, target))) return true;
            return machine.stateMachines.Any(m => MachineUses(m.stateMachine, except, target));
        }
        static bool MotionUses(Motion motion, Motion target)
        {
            if (motion == target) return true;
            return motion is BlendTree tree && tree.children.Any(c => MotionUses(c.motion, target));
        }
        static List<SpriteLibraryCategory> ReadCategories(SpriteLibraryAsset library)
        {
            if (!library) return new List<SpriteLibraryCategory>();
            return library.GetCategoryNames().Select(c => new SpriteLibraryCategory(c,
                library.GetCategoryLabelNames(c).Select(l => new SpriteLibraryLabel(l, library.GetSprite(c, l))))).ToList();
        }
        static string PrimaryLibraryPath(string libraryPath)
        {
            if (!File.Exists(libraryPath)) return null;
            var loaded = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(libraryPath);
            try
            {
                foreach (var obj in loaded)
                {
                    var property = new SerializedObject(obj).FindProperty("m_PrimaryLibraryGUID");
                    if (property != null && !string.IsNullOrEmpty(property.stringValue)) return AssetDatabase.GUIDToAssetPath(property.stringValue);
                }
                return null;
            }
            finally { foreach (var obj in loaded) if (obj) UnityEngine.Object.DestroyImmediate(obj); }
        }
        static void EnsureParameter(AnimatorController controller, string name, float defaultValue)
        {
            if (controller.parameters.Any(p => p.name == name)) return;
            controller.AddParameter(new AnimatorControllerParameter { name = name, type = AnimatorControllerParameterType.Float, defaultFloat = defaultValue });
        }
        public static float[] ParseDurations(string input, float fps, int count)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                if (float.IsNaN(fps) || float.IsInfinity(fps) || fps <= 0 || fps > 120) throw new InvalidOperationException("FPS는 0보다 크고 120 이하여야 합니다.");
                return Enumerable.Repeat(1f / fps, count).ToArray();
            }
            var parts = input.Split(new[] { ',', ';', ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != count) throw new InvalidOperationException("시간 목록은 프레임 수(" + count + ")와 같아야 합니다.");
            return parts.Select(p => {
                if (!float.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out float ms) || float.IsNaN(ms) || float.IsInfinity(ms) || ms < 1000f / 120f || ms > 60000)
                    throw new InvalidOperationException("프레임 시간은 8.334~60000ms 범위의 숫자로 입력하세요: " + p);
                return ms / 1000f;
            }).ToArray();
        }
        static bool IsTextureFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".png" || ext == ".psd" || ext == ".psb" || ext == ".tga" || ext == ".jpg";
        }
        static void RequireName(string name, string label)
        {
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\') || name == "." || name == "..")
                throw new InvalidOperationException(label + "에 유효한 파일 이름을 입력하세요.");
        }
        static void RequireAssetFolder(string path, bool mustExist)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains('\\') || path.Split('/').Any(p => p == ".." || p == "." || p.Length == 0))
                throw new InvalidOperationException("폴더 경로는 Assets/로 시작하는 프로젝트 경로여야 합니다: " + path);
            string full = Path.GetFullPath(path);
            string root = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || (mustExist && !AssetDatabase.IsValidFolder(path)))
                throw new InvalidOperationException("프로젝트 안의 유효한 폴더가 아닙니다: " + path);
        }
        static void CheckAssetType<T>(string path) where T : UnityEngine.Object
        {
            if (File.Exists(path) && !AssetDatabase.LoadAssetAtPath<T>(path)) throw new InvalidOperationException("동일 경로에 다른 형식의 파일이 있습니다: " + path);
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)))) throw new IOException("폴더 생성 실패: " + path);
        }
    }
}
