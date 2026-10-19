using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Save;
using Underwrite.Story;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 인트로. 웹툰 1~6화를 **움직이는 웹툰**으로 보여 준다(2026-10-18 — 10/18 토론 「앞부분은 연출로」, 선택지 중 과장이 고름).
    /// 한 장(세로로 긴 네 칸)을 통째로 보이지 않고 한 칸씩 화면 가득 띄워, 넘길 때 다음 칸으로 미끄러져 내려가고
    /// 칸이 떠 있는 동안 아주 천천히 다가간다. 원작의 그림과 글을 그대로 쓴다.
    ///
    /// 다 읽으면 마을(여관 앞)에서 시작한다. 웹툰 5~6화의 지하거래소 발견은 플레이어가 직접 겪는다.
    /// 새 게임이므로 씬을 넘나드는 기록(시장, 영지, 후원, 용사단)을 모두 비우고 시작한다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class IntroController : MonoBehaviour
    {
        [Tooltip("웹툰 페이지. 순서대로 보여 준다.")]
        [SerializeField] private List<Texture2D> Pages = new List<Texture2D>();

        // 인트로는 웹툰 1~4장(멸망·떨어짐·가난·무력감)까지만 보인다(2026-10-18 사장님 — 지하시장은 게임 안에서 발견해야 한다).
        // 5~6장은 영지에서 창고를 찾는 장면에 쓴다.
        private int ShownCount
        {
            get
            {
                return Mathf.Min(Pages.Count, Constants.INTRO_PAGES_SHOWN);
            }
        }

        [Tooltip("이어하기 때 시장을 표에서 다시 짜는 데 쓴다.")]
        [SerializeField] private GameDataSet Data;

        [Header("처음 화면")]
        [Tooltip("처음 화면 배경(지하 방).")]
        [SerializeField] private Texture2D TitleBackground;
        [Tooltip("단추 그림(9칸 자르기, 영지 단추와 같은 것).")]
        [SerializeField] private Texture2D ButtonNormal;
        [SerializeField] private Texture2D ButtonPressed;

        private const int BUTTON_SLICE = 48;
        private const float BUTTON_SLICE_SCALE = 0.25f;

        // 처음 화면이 떠 있는 동안은 웹툰이 넘어가지 않는다.
        private VisualElement Title;
        private VisualElement Confirm;
        private Button VolumeButton;

        private VisualElement Frame;
        private VisualElement Page;
        private Label Count;
        private int Index;
        private int PanelIndex;
        private Vector2 PanFrom;
        private Vector2 PanTo;
        private float PanTime;
        private float ZoomTime;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            if (root == null)
            {
                return;
            }

            root.Clear();

            var veil = new VisualElement();
            veil.style.position = Position.Absolute;
            veil.style.left = 0f;
            veil.style.right = 0f;
            veil.style.top = 0f;
            veil.style.bottom = 0f;
            veil.style.backgroundColor = new Color(0.06f, 0.05f, 0.04f);
            veil.RegisterCallback<ClickEvent>(_ => Next());
            root.Add(veil);

            // 한 칸이 보이는 창. 칸의 가로세로 비율(장 폭 : 장 높이의 1/4)을 지킨다.
            Frame = new VisualElement();
            Frame.style.position = Position.Absolute;
            Frame.style.left = Length.Percent(12f);
            Frame.style.right = Length.Percent(12f);
            Frame.style.overflow = Overflow.Hidden;
            Frame.pickingMode = PickingMode.Ignore;
            Frame.RegisterCallback<GeometryChangedEvent>(_ => LayoutFrame());
            veil.Add(Frame);

            // 창 뒤에서 위아래로 움직이는 장 한 장 전체.
            Page = new VisualElement();
            Page.style.position = Position.Absolute;
            Page.style.left = 0f;
            Page.style.right = 0f;
            Page.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            Page.pickingMode = PickingMode.Ignore;
            Frame.Add(Page);

            Count = new Label();
            Count.style.position = Position.Absolute;
            Count.style.right = 36f;
            Count.style.bottom = 32f;
            Count.style.color = new Color(0.85f, 0.8f, 0.72f);
            Count.style.fontSize = 18f;
            Count.pickingMode = PickingMode.Ignore;
            veil.Add(Count);

            var skip = new Button(Finish);
            skip.text = StringConstants.TEXT_INTRO_SKIP;
            skip.style.position = Position.Absolute;
            skip.style.right = 36f;
            skip.style.top = 32f;
            veil.Add(skip);

            Index = 0;
            Show();
            BuildTitle(root);
        }

        // ── 처음 화면 (2026-10-19 데모 점검) ─────────

        /// <summary>
        /// 이어하기(저장이 있을 때) · 새로 시작 · 소리 · 종료. 웹툰은 「새로 시작」을 고른 뒤에 흐른다.
        /// 저장이 있는데 새로 시작하면 한 번 묻는다 — 이전 판은 날짜를 붙여 따로 보관된다(SaveSystem.ArchiveForNewGame).
        /// </summary>
        private void BuildTitle(VisualElement root)
        {
            Title = Cover(new Color(0.03f, 0.025f, 0.02f, 1f));

            if (TitleBackground != null)
            {
                Title.style.backgroundImage = new StyleBackground(TitleBackground);
                Title.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            }

            // 배경을 어둡게 눌러 글자가 먼저 읽히게.
            var shade = Cover(new Color(0.02f, 0.015f, 0.01f, 0.62f));
            shade.style.justifyContent = Justify.Center;
            shade.style.paddingLeft = Length.Percent(9f);
            Title.Add(shade);

            var name = new Label(StringConstants.TEXT_TITLE_NAME);
            name.style.fontSize = 64f;
            name.style.color = new Color(0.96f, 0.9f, 0.76f);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            shade.Add(name);

            var sub = new Label(StringConstants.TEXT_TITLE_SUB + "  ·  " + StringConstants.TEXT_TITLE_DEMO);
            sub.style.fontSize = 20f;
            sub.style.letterSpacing = 6f;
            sub.style.color = new Color(0.78f, 0.68f, 0.5f);
            sub.style.marginBottom = 48f;
            shade.Add(sub);

            var when = SaveSystem.PeekWhen();

            if (when != null)
            {
                shade.Add(TitleButton(string.Format(StringConstants.TEXT_INTRO_CONTINUE, when), Continue));
            }

            shade.Add(TitleButton(StringConstants.TEXT_TITLE_NEW, () =>
            {
                if (when == null)
                {
                    CloseTitle();
                    return;
                }

                OpenConfirm(root);
            }));

            VolumeButton = TitleButton(VolumeText(), () =>
            {
                GameAudio.CycleVolume();
                VolumeButton.text = VolumeText();
            });
            shade.Add(VolumeButton);
            shade.Add(TitleButton(StringConstants.TEXT_TITLE_QUIT, Application.Quit));
            root.Add(Title);
        }

        private static string VolumeText()
        {
            return string.Format(StringConstants.TEXT_MENU_VOLUME, StringConstants.TEXT_VOLUME_STEPS[GameAudio.VolumeStep]);
        }

        private void OpenConfirm(VisualElement root)
        {
            Confirm = Cover(new Color(0f, 0f, 0f, 0.7f));
            Confirm.style.alignItems = Align.Center;
            Confirm.style.justifyContent = Justify.Center;

            var text = new Label(StringConstants.TEXT_TITLE_NEW_CONFIRM);
            text.style.fontSize = 22f;
            text.style.color = new Color(0.96f, 0.9f, 0.76f);
            text.style.marginBottom = 24f;
            Confirm.Add(text);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.Add(TitleButton(StringConstants.TEXT_TITLE_NEW_OK, () =>
            {
                Confirm.RemoveFromHierarchy();
                CloseTitle();
            }));
            row.Add(TitleButton(StringConstants.TEXT_TITLE_CANCEL, () => Confirm.RemoveFromHierarchy()));
            Confirm.Add(row);
            root.Add(Confirm);
        }

        private void CloseTitle()
        {
            if (Title != null)
            {
                Title.RemoveFromHierarchy();
                Title = null;
            }
        }

        private Button TitleButton(string text, System.Action onClick)
        {
            var button = new Button(onClick);
            button.text = text;
            button.style.width = 360f;
            button.style.height = 58f;
            button.style.marginTop = 6f;
            button.style.marginBottom = 6f;
            button.style.marginRight = 12f;
            button.style.fontSize = 20f;
            button.style.color = new Color(0.97f, 0.94f, 0.88f);
            button.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;

            if (ButtonNormal != null)
            {
                button.style.backgroundImage = new StyleBackground(ButtonNormal);
                button.style.unitySliceLeft = BUTTON_SLICE;
                button.style.unitySliceRight = BUTTON_SLICE;
                button.style.unitySliceTop = BUTTON_SLICE;
                button.style.unitySliceBottom = BUTTON_SLICE;
                button.style.unitySliceScale = BUTTON_SLICE_SCALE;

                // 누르는 동안 눌린 그림.
                button.RegisterCallback<PointerDownEvent>(_ => SetButtonImage(button, ButtonPressed), TrickleDown.TrickleDown);
                button.RegisterCallback<PointerUpEvent>(_ => SetButtonImage(button, ButtonNormal));
                button.RegisterCallback<PointerLeaveEvent>(_ => SetButtonImage(button, ButtonNormal));
            }

            return button;
        }

        private static void SetButtonImage(Button button, Texture2D image)
        {
            if (image != null)
            {
                button.style.backgroundImage = new StyleBackground(image);
            }
        }

        private static VisualElement Cover(Color color)
        {
            var cover = new VisualElement();
            cover.style.position = Position.Absolute;
            cover.style.left = 0f;
            cover.style.right = 0f;
            cover.style.top = 0f;
            cover.style.bottom = 0f;
            cover.style.backgroundColor = color;
            return cover;
        }

        private void Update()
        {
            Animate(Time.unscaledDeltaTime);

            if (Title != null)
            {
                return;
            }

            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame == true || keyboard.enterKey.wasPressedThisFrame == true
                || keyboard.rightArrowKey.wasPressedThisFrame == true)
            {
                Next();
            }
        }

        // 다음 칸으로. 장의 마지막 칸이면 다음 장의 첫 칸.
        private void Next()
        {
            if (PanelIndex < PanelCount() - 1)
            {
                PanelIndex++;
                PanFrom = PanTo;
                PanTo = PanelSpan(PanelIndex);
                PanTime = 0f;
                ZoomTime = 0f;
                ShowCount();
                return;
            }

            Index++;

            if (Index >= ShownCount)
            {
                Finish();
                return;
            }

            Show();
        }

        private int PanelCount()
        {
            var table = Constants.INTRO_PANELS;
            return Index < table.Length ? table[Index].Length / 2 : Constants.INTRO_PANELS_PER_PAGE;
        }

        // 칸의 위와 높이(원본 장의 비율, 0~1). 표에 없는 장은 고르게 나눈다.
        private Vector2 PanelSpan(int panel)
        {
            var table = Constants.INTRO_PANELS;

            if (Index < table.Length && Pages[Index] != null)
            {
                var rows = table[Index];
                float height = Pages[Index].height;
                return new Vector2(rows[panel * 2] / height, (rows[panel * 2 + 1] - rows[panel * 2]) / height);
            }

            var share = 1f / Constants.INTRO_PANELS_PER_PAGE;
            return new Vector2(panel * share, share);
        }

        // 장 전체의 크기를 창 폭에 맞춘다. 창의 높이는 Animate가 칸마다 맞춘다.
        private void LayoutFrame()
        {
            if (Frame == null || Index >= ShownCount || Pages[Index] == null)
            {
                return;
            }

            var page = Pages[Index];
            Page.style.height = Frame.resolvedStyle.width * page.height / page.width;
        }

        // 칸을 옮길 때 미끄러지듯 내려가며 창도 그 칸의 높이로 바뀐다. 칸이 떠 있는 동안 아주 천천히 다가간다.
        private void Animate(float delta)
        {
            if (Frame == null || Page == null || Index >= ShownCount || Pages[Index] == null)
            {
                return;
            }

            PanTime = Mathf.Min(1f, PanTime + delta / Constants.INTRO_PAN_SECONDS);
            ZoomTime += delta;

            var eased = Mathf.SmoothStep(0f, 1f, PanTime);
            var span = Vector2.Lerp(PanFrom, PanTo, eased);
            var pageHeight = Frame.resolvedStyle.width * Pages[Index].height / Pages[Index].width;
            var frameHeight = span.y * pageHeight;
            var parent = Frame.parent == null ? 0f : Frame.parent.resolvedStyle.height;

            Frame.style.height = frameHeight;
            Frame.style.top = Mathf.Max(0f, (parent - frameHeight) * 0.5f);
            Page.style.top = -span.x * pageHeight;

            var zoom = 1f + Mathf.Min(ZoomTime / Constants.INTRO_ZOOM_SECONDS, 1f) * Constants.INTRO_ZOOM_AMOUNT;
            Frame.style.scale = new Scale(new Vector3(zoom, zoom, 1f));
        }

        private void ShowCount()
        {
            Count.text = string.Format(StringConstants.TEXT_INTRO_COUNT, Index + 1, ShownCount);
        }

        private void Show()
        {
            if (ShownCount == 0)
            {
                UnderwriteLog.Warning("인트로 페이지가 없어 바로 시작합니다.", context: this);
                Finish();
                return;
            }

            Page.style.backgroundImage = new StyleBackground(Pages[Index]);
            PanelIndex = 0;
            PanFrom = PanelSpan(0);
            PanTo = PanFrom;
            PanTime = 1f;
            ZoomTime = 0f;
            LayoutFrame();
            ShowCount();
        }

        /// <summary>저장한 판을 이어서 연다. 불러오지 못하면 새 판으로.</summary>
        private void Continue()
        {
            if (SaveSystem.Load(Data) == false)
            {
                UnderwriteLog.Warning("저장을 불러오지 못해 처음부터 시작합니다.", context: this);
                Finish();
                return;
            }

            SceneManager.LoadScene(SceneRoutes.VILLAGE);
        }

        private void Finish()
        {
            SaveSystem.ArchiveForNewGame();
            BreakClock.Simulation = null;
            BreakClock.Current = null;
            PatronageLedger.Reset();
            HeroRoster.Reset();
            StoryProgress.Reset();
            BandCodex.Reset();
            QuestLog.Reset();
            SeasonPlan.Reset();
            PartyCasting.Reset();

            // 웹툰을 다 본 뒤에는 마을에서 시작한다. 지하거래소는 여관 일을 하다 직접 찾는다.
            SceneManager.LoadScene(SceneRoutes.VILLAGE);
        }
    }
}
