using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UIElements;

namespace Underwrite.Editor.AnimationTools
{
    public sealed class DirectionalAnimationBuilderWindow : EditorWindow
    {
        [SerializeField] DirectionalAnimationSettings settings = new DirectionalAnimationSettings();
        ObjectField sourceField, libraryField, controllerField;
        TextField motionField, characterField, outputField, pathField, durationField;
        FloatField fpsField;
        Toggle loopField, updateField;
        HelpBox status;
        VisualElement results;
        Button buildButton;

        [MenuItem("Tools/Underwrite/Animation/8 Direction Builder")]
        public static void Open()
        {
            var window = GetWindow<DirectionalAnimationBuilderWindow>("8방향 애니메이션");
            window.minSize = new Vector2(540, 620);
            window.Show();
        }

        public void CreateGUI()
        {
            if (settings == null) settings = new DirectionalAnimationSettings();
            if (!settings.library && settings.characterName == "Protagonist")
                settings.library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>("Assets/SpriteLibrarys/Protagonist.spriteLib");
            if (!settings.controller && settings.characterName == "Protagonist")
                settings.controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Controller/Controller.controller");
            rootVisualElement.Clear();
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = scroll.style.paddingRight = 12;
            scroll.style.paddingTop = scroll.style.paddingBottom = 10;
            rootVisualElement.Add(scroll);
            var title = new Label("Sprite Library → 방향별 Clip → 2D Blend Tree");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8;
            scroll.Add(title);
            scroll.Add(new HelpBox("동작 폴더 아래 S·SE·E·NE·N·NW·W·SW를 읽습니다. Sprite 이름을 라벨로 사용하며 숫자 순서로 정렬합니다. PNG·Trim·피봇·임포트 설정은 바꾸지 않습니다.", HelpBoxMessageType.Info));

            sourceField = new ObjectField("동작 이미지 폴더") { objectType = typeof(DefaultAsset), allowSceneObjects = false, value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(settings.sourceFolder) };
            sourceField.RegisterValueChangedCallback(e => {
                string path = AssetDatabase.GetAssetPath(e.newValue);
                settings.sourceFolder = path;
                if (AssetDatabase.IsValidFolder(path))
                {
                    string previousCharacter = settings.characterName;
                    settings.motionName = Path.GetFileName(path);
                    settings.characterName = Path.GetFileName(Path.GetDirectoryName(path));
                    if (previousCharacter != settings.characterName)
                    {
                        settings.library = null;settings.controller = null;
                        settings.outputFolder = "Assets/Animations/Generated/" + settings.characterName;
                        libraryField.SetValueWithoutNotify(null);controllerField.SetValueWithoutNotify(null);
                        outputField.SetValueWithoutNotify(settings.outputFolder);
                        settings.allowUpdate = false;updateField.SetValueWithoutNotify(false);
                    }
                    motionField.SetValueWithoutNotify(settings.motionName);
                    characterField.SetValueWithoutNotify(settings.characterName);
                    SetTiming(settings.motionName.Equals("Cheer", StringComparison.OrdinalIgnoreCase) ? "환호 8장 (1.56초)" : "균등 FPS");
                }
                Invalidate();
            });
            scroll.Add(sourceField);
            motionField = Text("동작 / 상태 이름", settings.motionName, v => settings.motionName = v);
            characterField = Text("캐릭터 이름 (새 에셋용)", settings.characterName, v => settings.characterName = v);
            scroll.Add(motionField); scroll.Add(characterField);

            libraryField = new ObjectField("Sprite Library (선택)") { objectType = typeof(SpriteLibraryAsset), allowSceneObjects = false, value = settings.library };
            libraryField.RegisterValueChangedCallback(e => { settings.library = e.newValue as SpriteLibraryAsset; Invalidate(); });
            scroll.Add(libraryField);
            controllerField = new ObjectField("Animator Controller (선택)") { objectType = typeof(AnimatorController), allowSceneObjects = false, value = settings.controller };
            controllerField.RegisterValueChangedCallback(e => { settings.controller = e.newValue as AnimatorController; Invalidate(); });
            scroll.Add(controllerField);
            scroll.Add(new HelpBox("Library·Controller를 비우면 저장 폴더에 캐릭터 이름으로 생성합니다. 기존 에셋을 선택하면 해당 동작만 추가합니다. 선택 오브젝트에서 가져오기로 현재 캐릭터 설정을 채울 수도 있습니다.", HelpBoxMessageType.None));
            scroll.Add(new Button(UseSelection) { text = "선택 오브젝트에서 Library·Controller·Resolver 경로 가져오기" });

            outputField = Text("클립 저장 루트 폴더", settings.outputFolder, v => settings.outputFolder = v);
            outputField.tooltip = "예: Assets/Animations/Clib → Cheer/Cheer_S.anim 등. 새 캐릭터는 별도 저장 폴더를 권장합니다.";
            scroll.Add(outputField);
            var timing = new Foldout { text = "재생 시간", value = true };
            var preset = new DropdownField("시간 프리셋", new System.Collections.Generic.List<string> { "균등 FPS", "작은 호흡 6장 (1.2초)", "기존 눈깜빡임 6장 (2.26초)", "걷기 8장 (0.8초)", "환호 8장 (1.56초)" }, settings.durationsMs == "240,120,120,120,300,180,180,300" ? 4 : 0);
            preset.RegisterValueChangedCallback(e => SetTiming(e.newValue));
            timing.Add(preset);
            fpsField = new FloatField("FPS (시간 목록이 비었을 때)") { value = settings.framesPerSecond };
            fpsField.RegisterValueChangedCallback(e => { settings.framesPerSecond = e.newValue; Invalidate(); });
            timing.Add(fpsField);
            durationField = Text("프레임별 시간 (ms, 선택)", settings.durationsMs, v => settings.durationsMs = v);
            durationField.tooltip = "쉼표로 각 프레임의 유지 시간을 입력합니다. 비우면 FPS를 사용합니다.";
            timing.Add(durationField);
            loopField = new Toggle("Loop Time (반복 재생)") { value = settings.loop };
            loopField.RegisterValueChangedCallback(e => { settings.loop = e.newValue; Invalidate(); });
            timing.Add(loopField);scroll.Add(timing);

            var advanced = new Foldout { text = "방향 파라미터와 경로", value = false };
            advanced.Add(Text("가로 Float 파라미터", settings.parameterX, v => settings.parameterX = v));
            advanced.Add(Text("세로 Float 파라미터", settings.parameterY, v => settings.parameterY = v));
            pathField = Text("Animator → Resolver 상대 경로", settings.resolverPath, v => settings.resolverPath = v);
            pathField.tooltip = "같은 GameObject라면 비웁니다. 예: Visual/Body. 선택 오브젝트에서 가져오기로 계산할 수 있습니다.";
            advanced.Add(pathField);
            var layer = new IntegerField("Controller 레이어 번호 (0부터)") { value = settings.layerIndex };
            layer.RegisterValueChangedCallback(e => { settings.layerIndex = e.newValue; Invalidate(); });
            advanced.Add(layer);scroll.Add(advanced);
            updateField = new Toggle("같은 동작 갱신 허용 (해당 카테고리·클립·상태)") { value = settings.allowUpdate };
            updateField.RegisterValueChangedCallback(e => { settings.allowUpdate = e.newValue; Invalidate(); });
            scroll.Add(updateField);

            status = new HelpBox("1. 설정 → 2. 검사 → 3. 생성. 새 상태의 진입/복귀 조건은 Animator에서 설정합니다.", HelpBoxMessageType.Info);
            results = new VisualElement();
            var buttons = new VisualElement();buttons.style.flexDirection = FlexDirection.Row;buttons.style.marginTop = 8;
            var scan = new Button(Scan) { text = "1. 8방향 검사 / 생성 미리보기" };scan.style.flexGrow = 1;
            buildButton = new Button(Build) { text = "2. Library + Clip + Blend Tree 생성" };buildButton.style.flexGrow = 1;
            buildButton.SetEnabled(false);
            buttons.Add(scan);buttons.Add(buildButton);scroll.Add(buttons);scroll.Add(status);scroll.Add(results);
        }

        TextField Text(string label, string value, Action<string> change)
        {
            var field = new TextField(label) { value = value };
            field.RegisterValueChangedCallback(e => { change(e.newValue); Invalidate(); });
            return field;
        }
        void Invalidate()
        {
            if (buildButton == null) return;
            buildButton.SetEnabled(false);results.Clear();status.text = "설정이 바뀌었습니다. 다시 검사하세요.";status.messageType = HelpBoxMessageType.Info;
        }
        void SetTiming(string preset)
        {
            switch (preset)
            {
                case "작은 호흡 6장 (1.2초)": settings.durationsMs = "200,200,200,200,200,200";settings.loop = true;break;
                case "기존 눈깜빡임 6장 (2.26초)": settings.durationsMs = "900,350,80,110,120,700";settings.loop = true;break;
                case "걷기 8장 (0.8초)": settings.durationsMs = "100,100,100,100,100,100,100,100";settings.loop = true;break;
                case "환호 8장 (1.56초)": settings.durationsMs = "240,120,120,120,300,180,180,300";settings.loop = false;break;
                default: settings.durationsMs = "";break;
            }
            durationField?.SetValueWithoutNotify(settings.durationsMs);loopField?.SetValueWithoutNotify(settings.loop);Invalidate();
        }
        void UseSelection()
        {
            try
            {
                var selected = Selection.activeGameObject;
                if (!selected) throw new InvalidOperationException("Hierarchy에서 Animator가 있는 캐릭터를 선택하세요.");
                var animator = selected.GetComponent<Animator>() ?? selected.GetComponentInParent<Animator>() ?? selected.GetComponentInChildren<Animator>(true);
                if (!animator) throw new InvalidOperationException("선택한 캐릭터에서 Animator를 찾지 못했습니다.");
                var resolvers = animator.GetComponentsInChildren<SpriteResolver>(true);
                var selectedResolver = selected.GetComponent<SpriteResolver>();
                var resolver = selectedResolver && selectedResolver.transform.IsChildOf(animator.transform) ? selectedResolver : resolvers.Length == 1 ? resolvers[0] : null;
                if (!resolver) throw new InvalidOperationException("Resolver가 여러 개라면 사용할 SpriteResolver 오브젝트를 직접 선택하세요.");
                var library = resolver.GetComponentInParent<SpriteLibrary>();
                var selectedController = animator.runtimeAnimatorController as AnimatorController;
                if (!selectedController) throw new InvalidOperationException("AnimatorController가 필요합니다. Override Controller는 원본 Controller를 직접 선택하세요.");
                settings.controller = selectedController;
                settings.library = library ? library.spriteLibraryAsset : null;
                settings.resolverPath = AnimationUtility.CalculateTransformPath(resolver.transform, animator.transform);
                libraryField.SetValueWithoutNotify(settings.library);controllerField.SetValueWithoutNotify(settings.controller);pathField.SetValueWithoutNotify(settings.resolverPath);Invalidate();
            }
            catch (Exception e) { Error(e); }
        }
        public void Scan()
        {
            results.Clear();buildButton.SetEnabled(false);
            try
            {
                var plan = DirectionalAnimationBuilder.Analyze(settings);
                foreach (var direction in plan.directions)
                {
                    var row = new Label(direction.code + "  |  " + direction.sprites.Length + "장  |  " + direction.category + "  |  " + direction.sprites.First().name + " → " + direction.sprites.Last().name);
                    row.style.whiteSpace = WhiteSpace.Normal;row.style.marginTop = 5;results.Add(row);
                }
                var paths = new Label("Library: " + plan.libraryPath + "\nController: " + plan.controllerPath + "\nClips: " + settings.outputFolder + "/" + settings.motionName + "\nResolver 경로: " + (settings.resolverPath.Length == 0 ? "(Animator와 같은 오브젝트)" : settings.resolverPath));
                paths.style.whiteSpace = WhiteSpace.Normal;paths.style.marginTop = 8;results.Add(paths);
                bool blocked = plan.updates.Count > 0 && !settings.allowUpdate;
                status.text = "총 " + plan.directions.Sum(d => d.sprites.Length) + "장 · 방향당 " + plan.directions[0].sprites.Length + "프레임 · " + plan.Length.ToString("0.###") + "초 · " + (settings.loop ? "반복" : "일회")
                    + (plan.updates.Count > 0 ? "\n갱신 대상:\n" + string.Join("\n", plan.updates) + (blocked ? "\n위 내용을 확인하고 ‘같은 동작 갱신 허용’을 켜세요." : "") : "\n같은 이름의 동작이 없어 새로 추가합니다.");
                status.messageType = blocked ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
                buildButton.SetEnabled(!blocked && !EditorApplication.isPlayingOrWillChangePlaymode);
            }
            catch (Exception e) { Error(e); }
        }
        void Build()
        {
            buildButton.SetEnabled(false);
            try
            {
                status.text = DirectionalAnimationBuilder.Build(settings);status.messageType = HelpBoxMessageType.Info;
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(settings.outputFolder + "/" + settings.motionName + "/" + settings.motionName + "_S.anim"));
            }
            catch (Exception e) { Error(e);Debug.LogException(e); }
        }
        void Error(Exception e) { status.text = e.Message;status.messageType = HelpBoxMessageType.Error; }
    }
}
