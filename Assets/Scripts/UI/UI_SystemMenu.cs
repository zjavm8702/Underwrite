using System;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 설정 창 — 음악·효과음·목소리 음량, 처음 화면으로, 게임 종료(2026-10-19 데모 점검: 소리를 따로 맞출 수 없었고, 지하·거래판엔 메뉴가 없었다).
    ///
    /// 어느 장면에서든 맨 위에 뜨도록 자기 화면층(UIDocument)을 따로 만든다 — 영지·지하 방·거래판의 화면이 서로 달라도 같은 창이다.
    /// 모양은 Resources/UI/SystemMenu.uss 하나.
    /// </summary>
    public sealed class UI_SystemMenu
    {
        private const int SORT_ORDER = 1000;
        private const string HOST_NAME = "System Menu";

        private readonly VisualElement Veil;
        private readonly Label Note;
        private readonly VisualElement LeaveButtons;
        private Action BeforeLeave;

        /// <summary>창이 열렸다·닫혔다(거래판이 장을 멈추고 되돌리는 데 쓴다).</summary>
        public event Action Opened;
        public event Action Closed;

        public bool IsOpen
        {
            get
            {
                return Veil.style.display == DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// 이 장면의 설정 창을 만든다. 화면 설정(PanelSettings)은 장면에 있는 화면 하나에서 빌린다.
        /// <paramref name="canLeave"/>면 처음 화면으로·게임 종료를 보인다 — 나가기 전에 <paramref name="beforeLeave"/>(저장 등)를 부른다.
        /// </summary>
        public static UI_SystemMenu Create(bool canLeave, Action beforeLeave)
        {
            // 드문 일(장면마다 한 번)이라 그때 찾는다.
            var existing = UnityEngine.Object.FindAnyObjectByType<UIDocument>();

            if (existing == null || existing.panelSettings == null)
            {
                UnderwriteLog.Warning("설정 창을 띄울 화면 설정을 찾지 못했습니다.");
                return null;
            }

            var host = new GameObject(HOST_NAME);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = existing.panelSettings;
            document.sortingOrder = SORT_ORDER;
            return new UI_SystemMenu(document.rootVisualElement, canLeave, beforeLeave);
        }

        private UI_SystemMenu(VisualElement root, bool canLeave, Action beforeLeave)
        {
            BeforeLeave = beforeLeave;
            root.pickingMode = PickingMode.Ignore;

            var style = Resources.Load<StyleSheet>(StringConstants.SYSTEM_MENU_STYLE);

            if (style != null)
            {
                root.styleSheets.Add(style);
            }

            Veil = new VisualElement();
            Veil.AddToClassList("system-veil");
            Veil.style.display = DisplayStyle.None;
            // 창 밖을 누르면 닫는다.
            Veil.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == Veil)
                {
                    Close();
                }
            });
            root.Add(Veil);

            var panel = new VisualElement();
            panel.AddToClassList("system-panel");
            Veil.Add(panel);

            var title = new Label(StringConstants.TEXT_SETTINGS);
            title.AddToClassList("system-title");
            panel.Add(title);

            panel.Add(VolumeRow(AudioChannel.Channel_Music));
            panel.Add(VolumeRow(AudioChannel.Channel_Effects));
            // 목소리는 쓰지 않기로 했다(10/19 사장님 — 기계음). 켜면 음량 줄도 다시 보인다.
            if (Constants.AUDIO_VOICE_ENABLED == true)
            {
                panel.Add(VolumeRow(AudioChannel.Channel_Voice));
            }

            Note = NoteLabel(string.Empty);
            Note.style.display = DisplayStyle.None;
            panel.Add(Note);

            var buttons = new VisualElement();
            buttons.AddToClassList("system-buttons");
            panel.Add(buttons);

            LeaveButtons = new VisualElement();
            LeaveButtons.style.display = canLeave == true ? DisplayStyle.Flex : DisplayStyle.None;
            LeaveButtons.Add(MenuButton(StringConstants.TEXT_MENU_TO_TITLE, () => Leave(false)));
            LeaveButtons.Add(MenuButton(StringConstants.TEXT_MENU_QUIT, () => Leave(true)));
            buttons.Add(LeaveButtons);
            buttons.Add(MenuButton(StringConstants.TEXT_SETTINGS_CLOSE, Close));

            GameAudio.AttachClicks(root);
        }

        /// <summary>창 아래에 붙는 작은 안내(예: 장 중에 나가면 저장되지 않는다). 비우면 숨긴다.</summary>
        public void SetNote(string text)
        {
            Note.text = text ?? string.Empty;
            Note.style.display = string.IsNullOrEmpty(text) == true ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void Toggle()
        {
            if (IsOpen == true)
            {
                Close();
                return;
            }

            Open();
        }

        public void Open()
        {
            if (IsOpen == true)
            {
                return;
            }

            Veil.style.display = DisplayStyle.Flex;
            GameAudio.Play(SoundEffect.Sound_Page);
            var opened = Opened;

            if (opened != null)
            {
                opened();
            }
        }

        public void Close()
        {
            if (IsOpen == false)
            {
                return;
            }

            Veil.style.display = DisplayStyle.None;
            var closed = Closed;

            if (closed != null)
            {
                closed();
            }
        }

        private void Leave(bool quit)
        {
            if (BeforeLeave != null)
            {
                BeforeLeave();
            }

            if (quit == true)
            {
                Application.Quit();
                return;
            }

            SceneManager.LoadScene(SceneRoutes.INTRO);
        }

        private static VisualElement VolumeRow(AudioChannel channel)
        {
            var row = new VisualElement();
            row.AddToClassList("system-row");

            var label = new Label(StringConstants.TEXT_SETTINGS_CHANNELS[(int)channel]);
            label.AddToClassList("system-label");
            row.Add(label);

            var slider = new Slider(0f, 1f);
            slider.AddToClassList("system-slider");
            slider.value = GameAudio.VolumeOf(channel);
            row.Add(slider);

            var value = new Label(Percent(slider.value));
            value.AddToClassList("system-value");
            row.Add(value);

            slider.RegisterValueChangedCallback(change =>
            {
                GameAudio.SetVolume(channel, change.newValue);
                value.text = Percent(change.newValue);
            });

            return row;
        }

        private static string Percent(float value)
        {
            return string.Format(StringConstants.TEXT_SETTINGS_PERCENT, Mathf.RoundToInt(value * 100f));
        }

        private static Label NoteLabel(string text)
        {
            var note = new Label(text);
            note.AddToClassList("system-note");
            return note;
        }

        private static Button MenuButton(string text, Action onClick)
        {
            var button = new Button(onClick);
            button.text = text;
            button.AddToClassList("system-button");
            return button;
        }
    }
}
