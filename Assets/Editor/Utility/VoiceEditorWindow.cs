using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Underwrite.Global;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Underwrite.Editor.Utility
{
    /// <summary>
    /// 음성·대사 편집(2026-10-19 사장님 — 「누가 누군지 알아들을 수 있게」, 「음성과 대사 수정 툴」).
    ///
    /// 왼쪽에서 인물을 고르면 초상·이름·역할과 함께 목소리 설명, 감정별 한마디 열네 줄이 뜬다.
    /// ▶은 듣기, ⟳은 그 줄을 다시 만들기(고친 대사로, 누를 때마다 다른 느낌). 「목소리 새로 설계」는 설명을 바꿨을 때 그 사람 전부를 다시.
    /// 생성은 저장소 밖 가상환경(~/tts/qwen3)의 Qwen3-TTS를 뒤에서 돌리고(Tools/voice/edit_barks.py), 끝나면 소리 파일을 바로 갈아 끼운다.
    /// 대사 표는 Tools/voice/barks_v1.json — 다시 만들기 전에 저장한다.
    /// </summary>
    public sealed class VoiceEditorWindow : EditorWindow
    {
        private const string BARKS_JSON = "Tools/voice/barks_v1.json";
        private const string SCRIPT_FOLDER = "Tools/voice";
        private const string SCRIPT = "edit_barks.py";
        private const string PYTHON = "tts/qwen3/Scripts/python.exe";
        private const string FACE_FOLDER = "Assets/UI/Images/Faces/";
        private const string VOICE_FOLDER = "Assets/Resources/";
        private const float GAP_SECONDS = 0.35f;
        private const int VARIANTS = 2;

        private static readonly Color ChangedColor = new Color(0.95f, 0.75f, 0.3f);

        private struct Speaker
        {
            public string Voice;
            public string Name;
            public string Role;
            public string Face;

            public Speaker(string voice, string name, string role, string face)
            {
                Voice = voice;
                Name = name;
                Role = role;
                Face = face;
            }
        }

        // 순서는 PortraitEmotion(StringConstants.AUDIO_VOICE_EMOTIONS)과 같다.
        private static readonly string[] EmotionNames = { "보통", "기쁨", "불안", "화남", "놀람", "슬픔", "안도" };

        private static readonly Speaker[] Speakers =
        {
            new Speaker("edwin", "에드윈", "용사단 · 학자(기록과 작전)", "scholar"),
            new Speaker("kaira", "카이라", "용사단 · 검사(선봉)", "sword"),
            new Speaker("elaria", "엘라리아", "용사단 · 엘프 마도사", "elf"),
            new Speaker("godric", "고드릭", "용사단 · 방패", "warrior"),
            new Speaker("fio", "피오", "용사단 · 요정 치유사", "fairy"),
            new Speaker("innkeeper", "여관 주인", "영지 · 여관", "folk_innkeeper"),
            new Speaker("archivist", "기록 담당", "영지 · 길드 기록실", "folk_archivist"),
            new Speaker("smith", "대장장이", "영지 · 대장간", "folk_smith"),
            new Speaker("broker", "중개인", "영지 · 시장 게시판", "folk_broker"),
            new Speaker("steward", "영주관 집사", "영지 · 영주관", "folk_steward"),
            new Speaker("informant", "정보상", "지하 · 카운터 뒤", "informant")
        };

        // barks_v1.json 전체와 그 안의 voices.
        private Dictionary<string, object> Root;
        private Dictionary<string, object> Voices;

        // 마지막으로 소리를 만든(또는 불러온) 때의 대사 — 이것과 다르면 「바뀐 줄」이다.
        private readonly Dictionary<string, string> Generated = new Dictionary<string, string>();

        private int Selected;
        private bool IsDirty;
        private Label Status;
        private Label Log;
        private VisualElement Detail;
        private Button SaveButton;
        private Button RegenChangedButton;

        // 듣기 줄
        private readonly Queue<AudioClip> Pending = new Queue<AudioClip>();
        private readonly Queue<string> PendingText = new Queue<string>();
        private double NextAt;

        // 생성 프로세스
        private Process Running;
        private readonly ConcurrentQueue<string> Output = new ConcurrentQueue<string>();
        private readonly List<string> RunningFiles = new List<string>();
        private readonly StringBuilder LogText = new StringBuilder();

        [MenuItem("Tools/Underwrite/Voice/음성·대사 편집")]
        private static void Open()
        {
            var window = GetWindow<VoiceEditorWindow>();
            window.titleContent = new GUIContent("음성·대사 편집");
            window.minSize = new Vector2(980f, 560f);
        }

        private void OnEnable()
        {
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            StopAll();
        }

        private void CreateGUI()
        {
            LoadBarks();

            var bar = Row();
            bar.style.paddingLeft = 8f;
            bar.style.paddingTop = 6f;
            bar.style.paddingBottom = 6f;
            bar.Add(new Button(StopAll) { text = "■ 멈춤" });
            SaveButton = new Button(Save) { text = "저장" };
            bar.Add(SaveButton);
            RegenChangedButton = new Button(RegenerateChanged) { text = "바뀐 줄 모두 다시 만들기" };
            bar.Add(RegenChangedButton);
            Status = new Label();
            Status.style.unityTextAlign = TextAnchor.MiddleLeft;
            Status.style.marginLeft = 10f;
            Status.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar.Add(Status);
            rootVisualElement.Add(bar);

            var body = Row();
            body.style.flexGrow = 1f;
            rootVisualElement.Add(body);

            var list = new ScrollView();
            list.style.width = 230f;
            list.style.borderRightWidth = 1f;
            list.style.borderRightColor = new Color(0f, 0f, 0f, 0.3f);

            for (var i = 0; i < Speakers.Length; i++)
            {
                var index = i;
                var item = SpeakerCard(Speakers[i], 40f);
                item.RegisterCallback<ClickEvent>(_ => Select(index));
                list.Add(item);
            }

            body.Add(list);

            Detail = new ScrollView();
            Detail.style.flexGrow = 1f;
            Detail.style.paddingLeft = 10f;
            body.Add(Detail);

            Log = new Label();
            Log.style.whiteSpace = WhiteSpace.Normal;
            Log.style.paddingLeft = 8f;
            Log.style.height = 70f;
            Log.style.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
            rootVisualElement.Add(Log);

            Select(Selected);
            RefreshBar();
        }

        // ── 화면 ─────────

        private void Select(int index)
        {
            Selected = index;
            var speaker = Speakers[index];
            var person = Person(speaker.Voice);
            Detail.Clear();

            if (person == null)
            {
                Detail.Add(new Label($"대사 표에 {speaker.Voice}가 없습니다."));
                return;
            }

            Detail.Add(SpeakerCard(speaker, 96f));

            var actions = Row();
            actions.Add(new Button(() => PlayAll(speaker)) { text = "▶ 차례로 듣기" });
            var redesign = new Button(() => Redesign(speaker)) { text = "목소리 새로 설계(이 사람 전부 다시)" };
            redesign.SetEnabled(Running == null);
            actions.Add(redesign);
            Detail.Add(actions);

            Detail.Add(Caption("목소리 설명 — 영어로 적을수록 잘 듣는다(나이·성별·높낮이·말버릇)"));
            Detail.Add(Field(person, "design", true));
            Detail.Add(Caption("기준 대사 — 목소리를 설계할 때 읽히는 한 줄"));
            Detail.Add(Field(person, "ref_text", false));

            Detail.Add(Caption("감정별 한마디 — 고친 줄은 노랗게. 속도 1.3 = 1.3배 빠르게(음 높이는 그대로). "
                + "연출(영어)을 쓰면 그 지시대로 연기한다 — 예: laugh heartily, a quick natural chuckle. ▶ 듣기 · ⟳ 다시 만들기"));
            var barks = BarksOf(speaker.Voice);

            for (var e = 0; e < EmotionNames.Length; e++)
            {
                var emotion = StringConstants.AUDIO_VOICE_EMOTIONS[e];

                if (barks == null || barks.TryGetValue(emotion, out var value) == false || value is List<object> lines == false)
                {
                    continue;
                }

                for (var v = 0; v < lines.Count && v < VARIANTS; v++)
                {
                    var row = Row();
                    row.style.alignItems = Align.Center;
                    var name = new Label(v == 0 ? EmotionNames[e] : string.Empty);
                    name.style.width = 40f;
                    name.style.unityFontStyleAndWeight = FontStyle.Bold;
                    row.Add(name);
                    row.Add(LineEditor(speaker, emotion, e, lines, v));
                    Detail.Add(row);
                }
            }
        }

        private VisualElement LineEditor(Speaker speaker, string emotion, int emotionIndex, List<object> lines, int variant)
        {
            var box = Row();
            box.style.flexGrow = 1f;
            box.style.alignItems = Align.Center;
            var key = KeyOf(speaker.Voice, emotion, variant);
            var entry = LineOf(lines[variant]);
            var field = new TextField();
            field.value = entry.Text;
            field.style.flexGrow = 1f;
            var speed = new FloatField();
            speed.value = entry.Speed;
            speed.style.width = 46f;
            speed.tooltip = "빠르기(1 = 그대로, 1.3 = 1.3배 빠르게)";
            var style = new TextField();
            style.value = entry.Style;
            style.style.width = 230f;
            style.tooltip = "연출(영어). 비우면 기준 목소리를 그대로 복제한다. 쓰면 목소리 설계 모드로 만들어 그 사람 목소리와 달라질 수 있다(10/19 측정: 웃음 줄에서 음 높이가 2~3배까지 벗어남) — 듣고 판단";
            Mark(field, key);

            // 세 칸 중 무엇이 바뀌어도 줄 하나로 다시 적는다. 속도·연출이 기본이면 글만 남긴다.
            EventCallback<ChangeEvent<string>> onText = _ => Store(lines, variant, field, speed, style, key);
            field.RegisterValueChangedCallback(onText);
            style.RegisterValueChangedCallback(onText);
            speed.RegisterValueChangedCallback(_ => Store(lines, variant, field, speed, style, key));
            box.Add(field);
            box.Add(speed);
            box.Add(style);
            box.Add(new Button(() => PlayLine(speaker, emotionIndex, variant)) { text = "▶" });
            var regen = new Button(() => Regenerate(new List<string> { key })) { text = "⟳", tooltip = "이 줄 다시 만들기(누를 때마다 다른 느낌)" };
            regen.SetEnabled(Running == null);
            box.Add(regen);
            return box;
        }

        private struct Line
        {
            public string Text;
            public float Speed;
            public string Style;
        }

        private static Line LineOf(object entry)
        {
            var line = new Line { Text = string.Empty, Speed = 1f, Style = string.Empty };

            if (entry is string text)
            {
                line.Text = text;
                return line;
            }

            if (entry is Dictionary<string, object> map)
            {
                line.Text = map.TryGetValue("text", out var t) == true ? t as string : string.Empty;
                line.Speed = map.TryGetValue("speed", out var v) == true && v is double d ? (float)d : 1f;
                line.Style = map.TryGetValue("style", out var st) == true ? st as string : string.Empty;
            }

            return line;
        }

        private void Store(List<object> lines, int variant, TextField field, FloatField speed, TextField style, string key)
        {
            var hasSpeed = Mathf.Abs(speed.value - 1f) > 0.001f;
            var hasStyle = string.IsNullOrWhiteSpace(style.value) == false;

            if (hasSpeed == false && hasStyle == false)
            {
                lines[variant] = field.value;
            }
            else
            {
                var map = new Dictionary<string, object> { { "text", field.value } };

                if (hasSpeed == true)
                {
                    map["speed"] = (double)Mathf.Round(speed.value * 100f) / 100.0;
                }

                if (hasStyle == true)
                {
                    map["style"] = style.value.Trim();
                }

                lines[variant] = map;
            }

            IsDirty = true;
            Mark(field, key);
            RefreshBar();
        }

        private void Mark(TextField field, string key)
        {
            var changed = IsChanged(key);
            field.style.borderLeftWidth = changed == true ? 4f : 0f;
            field.style.borderLeftColor = ChangedColor;
        }

        private TextField Field(Dictionary<string, object> person, string key, bool multiline)
        {
            var field = new TextField();
            field.multiline = multiline;
            field.value = person.TryGetValue(key, out var value) == true ? value as string : string.Empty;
            field.style.whiteSpace = WhiteSpace.Normal;
            field.RegisterValueChangedCallback(change =>
            {
                person[key] = change.newValue;
                IsDirty = true;
                RefreshBar();
            });
            return field;
        }

        private VisualElement SpeakerCard(Speaker speaker, float size)
        {
            var card = Row();
            card.style.alignItems = Align.Center;
            card.style.paddingTop = 3f;
            card.style.paddingBottom = 3f;

            var face = new VisualElement();
            face.style.width = size;
            face.style.height = size;
            face.style.marginRight = 8f;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FACE_FOLDER + "face_" + speaker.Face + "_neutral.png");

            if (texture != null)
            {
                face.style.backgroundImage = new StyleBackground(texture);
                face.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }

            card.Add(face);

            var names = new VisualElement();
            var name = new Label(speaker.Name);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.fontSize = size > 60f ? 18f : 13f;
            names.Add(name);
            names.Add(new Label(speaker.Role));
            card.Add(names);
            return card;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            return row;
        }

        private static Label Caption(string text)
        {
            var label = new Label(text);
            label.style.marginTop = 8f;
            label.style.unityFontStyleAndWeight = FontStyle.Italic;
            return label;
        }

        private void RefreshBar()
        {
            if (Status == null)
            {
                return;
            }

            var changed = ChangedKeys().Count;
            SaveButton.SetEnabled(IsDirty == true);
            RegenChangedButton.SetEnabled(Running == null && changed > 0);
            RegenChangedButton.text = $"바뀐 줄 모두 다시 만들기 ({changed})";

            if (Running != null)
            {
                Status.text = "만드는 중… (모델을 불러오는 데 30초 남짓)";
            }
            else if (IsDirty == true)
            {
                Status.text = "저장하지 않은 수정이 있다";
            }
        }

        // ── 대사 표 ─────────

        private void LoadBarks()
        {
            Root = null;
            Voices = null;
            Generated.Clear();
            var path = ProjectPath(BARKS_JSON);

            if (File.Exists(path) == false)
            {
                return;
            }

            Root = MiniJson.Parse(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>;
            Voices = Root != null && Root.TryGetValue("voices", out var voices) == true ? voices as Dictionary<string, object> : null;

            // 지금 있는 소리는 지금 대사로 만든 것으로 본다.
            for (var i = 0; i < Speakers.Length; i++)
            {
                var barks = BarksOf(Speakers[i].Voice);

                if (barks == null)
                {
                    continue;
                }

                foreach (var pair in barks)
                {
                    if (pair.Value is List<object> lines)
                    {
                        for (var v = 0; v < lines.Count; v++)
                        {
                            Generated[KeyOf(Speakers[i].Voice, pair.Key, v)] = Signature(lines[v]);
                        }
                    }
                }
            }
        }

        private void Save()
        {
            if (Root == null)
            {
                return;
            }

            File.WriteAllText(ProjectPath(BARKS_JSON), MiniJson.Serialize(Root), new UTF8Encoding(false));
            IsDirty = false;
            Status.text = "저장했다";
            RefreshBar();
        }

        private Dictionary<string, object> Person(string voice)
        {
            return Voices != null && Voices.TryGetValue(voice, out var person) == true ? person as Dictionary<string, object> : null;
        }

        private Dictionary<string, object> BarksOf(string voice)
        {
            var person = Person(voice);
            return person != null && person.TryGetValue("barks", out var barks) == true ? barks as Dictionary<string, object> : null;
        }

        private string TextOf(string key)
        {
            var parts = key.Split(':');
            var file = parts[1];
            var cut = file.LastIndexOf('_');
            var barks = BarksOf(parts[0]);

            if (barks == null || barks.TryGetValue(file.Substring(0, cut), out var value) == false || value is List<object> lines == false)
            {
                return string.Empty;
            }

            var variant = int.Parse(file.Substring(cut + 1));
            return variant < lines.Count ? LineOf(lines[variant]).Text : string.Empty;
        }

        // 소리를 다시 만들어야 하는지 가르는 값 — 글·빠르기·연출 중 하나라도 바뀌면 다르다.
        private static string Signature(object entry)
        {
            var line = LineOf(entry);
            return line.Text + "|" + line.Speed.ToString("0.00") + "|" + line.Style;
        }

        private string SignatureOf(string key)
        {
            var parts = key.Split(':');
            var file = parts[1];
            var cut = file.LastIndexOf('_');
            var barks = BarksOf(parts[0]);

            if (barks == null || barks.TryGetValue(file.Substring(0, cut), out var value) == false || value is List<object> lines == false)
            {
                return string.Empty;
            }

            var variant = int.Parse(file.Substring(cut + 1));
            return variant < lines.Count ? Signature(lines[variant]) : string.Empty;
        }

        private bool IsChanged(string key)
        {
            return Generated.TryGetValue(key, out var made) == false || made != SignatureOf(key);
        }

        private List<string> ChangedKeys()
        {
            var keys = new List<string>();

            foreach (var pair in Generated)
            {
                if (pair.Value != SignatureOf(pair.Key))
                {
                    keys.Add(pair.Key);
                }
            }

            return keys;
        }

        private static string KeyOf(string voice, string emotion, int variant)
        {
            return voice + ":" + emotion + "_" + variant;
        }

        // ── 다시 만들기 ─────────

        private void RegenerateChanged()
        {
            Regenerate(ChangedKeys());
        }

        private void Regenerate(List<string> keys)
        {
            if (keys.Count == 0)
            {
                return;
            }

            Save();
            Launch("--lines " + string.Join(",", keys), keys);
        }

        private void Redesign(Speaker speaker)
        {
            var ok = EditorUtility.DisplayDialog("목소리 새로 설계",
                $"{speaker.Name}의 기준 목소리를 설명대로 새로 만들고 한마디 열네 줄을 모두 다시 만든다. 지금 목소리는 사라진다.", "다시 만든다", "그만둔다");

            if (ok == false)
            {
                return;
            }

            Save();
            var keys = new List<string>();

            foreach (var pair in Generated)
            {
                if (pair.Key.StartsWith(speaker.Voice + ":", StringComparison.Ordinal) == true)
                {
                    keys.Add(pair.Key);
                }
            }

            Launch("--redesign " + speaker.Voice, keys);
        }

        private void Launch(string arguments, List<string> keys)
        {
            if (Running != null)
            {
                return;
            }

            var python = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), PYTHON);

            if (File.Exists(python) == false)
            {
                EditorUtility.DisplayDialog("음성 생성기 없음", $"{python}이 없다. Documents/Claude/Underwrite_음성_조사.md의 설치를 먼저.", "확인");
                return;
            }

            var seed = UnityEngine.Random.Range(1, 1000000);
            var info = new ProcessStartInfo(python, $"-X utf8 {SCRIPT} {arguments} --seed {seed}")
            {
                WorkingDirectory = ProjectPath(SCRIPT_FOLDER),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            RunningFiles.Clear();
            RunningFiles.AddRange(keys);
            LogText.Clear();
            Running = new Process { StartInfo = info, EnableRaisingEvents = true };
            Running.OutputDataReceived += (_, line) => Enqueue(line.Data, false);
            Running.ErrorDataReceived += (_, line) => Enqueue(line.Data, true);
            Running.Start();
            Running.BeginOutputReadLine();
            Running.BeginErrorReadLine();
            Select(Selected);
            RefreshBar();
        }

        // 표준 오류는 경고투성이라 「Error」가 든 줄만 보인다.
        private void Enqueue(string line, bool isError)
        {
            if (string.IsNullOrEmpty(line) == true)
            {
                return;
            }

            if (isError == true && line.Contains("Error") == false)
            {
                return;
            }

            Output.Enqueue(line);
        }

        private void Tick()
        {
            while (Output.TryDequeue(out var line) == true)
            {
                LogText.AppendLine(line);
                var lines = LogText.ToString().Split('\n');
                Log.text = string.Join("\n", lines, Math.Max(0, lines.Length - 5), Math.Min(5, lines.Length));
            }

            if (Running != null && Running.HasExited == true)
            {
                FinishRun();
            }

            if (Pending.Count == 0 || EditorApplication.timeSinceStartup < NextAt)
            {
                return;
            }

            var clip = Pending.Dequeue();
            var text = PendingText.Dequeue();
            Play(clip);
            Status.text = text;
            NextAt = EditorApplication.timeSinceStartup + clip.length + GAP_SECONDS;
        }

        private void FinishRun()
        {
            var ok = Running.ExitCode == 0;
            Running.Dispose();
            Running = null;

            if (ok == true)
            {
                for (var i = 0; i < RunningFiles.Count; i++)
                {
                    var key = RunningFiles[i];
                    Generated[key] = SignatureOf(key);
                    AssetDatabase.ImportAsset(VoicePath(key), ImportAssetOptions.ForceUpdate);
                }

                Status.text = $"다시 만들었다 — {RunningFiles.Count}줄";
            }
            else
            {
                Status.text = "만들지 못했다 — 아래 기록을 본다";
                Debug.LogError("음성 생성 실패:\n" + LogText);
            }

            RunningFiles.Clear();
            Select(Selected);
            RefreshBar();
        }

        // ── 듣기 ─────────

        private void PlayLine(Speaker speaker, int emotion, int variant)
        {
            StopAll();
            Enqueue(speaker, emotion, variant);
        }

        private void PlayAll(Speaker speaker)
        {
            StopAll();

            for (var e = 0; e < EmotionNames.Length; e++)
            {
                Enqueue(speaker, e, 0);
            }
        }

        private void Enqueue(Speaker speaker, int emotion, int variant)
        {
            var key = KeyOf(speaker.Voice, StringConstants.AUDIO_VOICE_EMOTIONS[emotion], variant);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(key));

            if (clip == null)
            {
                Debug.LogWarning($"음성 파일이 없다: {VoicePath(key)}");
                return;
            }

            Pending.Enqueue(clip);
            PendingText.Enqueue($"{speaker.Name} · {EmotionNames[emotion]} — 「{TextOf(key)}」");
            NextAt = 0d;
        }

        private void StopAll()
        {
            Pending.Clear();
            PendingText.Clear();
            Invoke("StopAllPreviewClips", null);
        }

        // 에디터에서 소리를 미리 듣는 길은 내부 API(AudioUtil)뿐이다.
        private static void Play(AudioClip clip)
        {
            Invoke("StopAllPreviewClips", null);
            Invoke("PlayPreviewClip", new object[] { clip, 0, false });
        }

        private static void Invoke(string method, object[] args)
        {
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AudioUtil");
            var found = type == null ? null : type.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (found == null)
            {
                Debug.LogWarning($"AudioUtil.{method}을 찾지 못했다 — 이 Unity 버전에서 바뀌었을 수 있다.");
                return;
            }

            found.Invoke(null, args);
        }

        private static string VoicePath(string key)
        {
            var parts = key.Split(':');
            return VOICE_FOLDER + StringConstants.AUDIO_VOICE_ROOT + parts[0] + "/" + parts[1] + ".wav";
        }

        private static string ProjectPath(string relative)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), relative);
        }
    }
}
