using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Story;
using Underwrite.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 인트로. 웹툰 1~6화를 한 장씩 넘긴다.
    ///
    /// 다 읽으면 마을(여관 앞)에서 시작한다. 웹툰 5~6화의 지하거래소 발견은 플레이어가 직접 겪는다.
    /// 새 게임이므로 씬을 넘나드는 기록(시장, 영지, 후원, 용사단)을 모두 비우고 시작한다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class IntroController : MonoBehaviour
    {
        [Tooltip("웹툰 페이지. 순서대로 보여 준다.")]
        [SerializeField] private List<Texture2D> Pages = new List<Texture2D>();

        private VisualElement Page;
        private Label Count;
        private int Index;

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

            // 세로로 긴 웹툰 한 장을 화면 높이에 맞춘다. 네 칸이 한눈에 들어온다.
            Page = new VisualElement();
            Page.style.position = Position.Absolute;
            Page.style.left = 0f;
            Page.style.right = 0f;
            Page.style.top = 24f;
            Page.style.bottom = 24f;
            Page.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            Page.pickingMode = PickingMode.Ignore;
            veil.Add(Page);

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
        }

        private void Update()
        {
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

        private void Next()
        {
            Index++;

            if (Index >= Pages.Count)
            {
                Finish();
                return;
            }

            Show();
        }

        private void Show()
        {
            if (Pages.Count == 0)
            {
                UnderwriteLog.Warning("인트로 페이지가 없어 바로 시작합니다.", context: this);
                Finish();
                return;
            }

            Page.style.backgroundImage = new StyleBackground(Pages[Index]);
            Count.text = string.Format(StringConstants.TEXT_INTRO_COUNT, Index + 1, Pages.Count);
        }

        /// <summary>새 게임을 연다. 이전 판의 기록이 남아 있으면 비운다.</summary>
        private void Finish()
        {
            BreakClock.Simulation = null;
            BreakClock.Current = null;
            PatronageLedger.Reset();
            HeroRoster.Reset();
            StoryProgress.Reset();
            BandCodex.Reset();

            // 웹툰을 다 본 뒤에는 마을에서 시작한다. 지하거래소는 여관 일을 하다 직접 찾는다.
            SceneManager.LoadScene(SceneRoutes.VILLAGE);
        }
    }
}
