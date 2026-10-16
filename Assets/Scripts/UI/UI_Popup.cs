using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 화면 어디서나 띄우는 공용 팝업.
    ///
    /// 확인이 필요한 일에만 쓴다. 흘려보내도 되는 소식은 쪽지(toast)가 맡는다.
    /// 화면마다 팝업을 따로 만들면 생김새와 동작이 조금씩 어긋나므로 한 벌만 두고 복제한다.
    /// </summary>
    public sealed class UI_Popup
    {
        private const float FADE_SECONDS = 0.16f;
        private const float RISE_SECONDS = 0.20f;
        private const float RISE_FROM = 24f;
        private const float SCALE_FROM = 0.96f;

        private readonly VisualElement Root;
        private readonly VisualElement Veil;
        private readonly VisualElement Panel;
        private readonly Button Confirm;
        private readonly Button Cancel;

        private Action OnConfirm;
        private Action OnCancel;
        private Tween Running;

        /// <summary>지금 떠 있는가. 팝업이 떠 있는 동안에는 뒤 화면을 건드리지 않는다.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 팝업 한 벌을 화면에 붙인다. 붙이기만 하고 보여 주지는 않는다.
        /// </summary>
        public UI_Popup(VisualElement root, VisualTreeAsset tree)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            Root = root;

            var built = tree.CloneTree();
            Veil = built.Q<VisualElement>("popup-veil");
            Panel = built.Q<VisualElement>("popup");
            Confirm = built.Q<Button>("popup-confirm");
            Cancel = built.Q<Button>("popup-cancel");

            // CloneTree가 만든 겉포장은 자리를 차지하지 않아야 한다.
            built.style.position = Position.Absolute;
            built.style.left = 0f;
            built.style.right = 0f;
            built.style.top = 0f;
            built.style.bottom = 0f;
            built.pickingMode = PickingMode.Ignore;

            if (Confirm != null)
            {
                Confirm.clicked += Accept;
            }

            if (Cancel != null)
            {
                Cancel.clicked += Decline;
            }

            Root.Add(built);
            Hide();
        }

        /// <summary>
        /// 팝업을 띄운다. 단추를 하나만 쓰려면 cancelText를 비워 둔다.
        /// </summary>
        public void Show(string tag, string title, string body,
            string confirmText, string cancelText, Action onConfirm, Action onCancel)
        {
            if (Veil == null)
            {
                return;
            }

            SetLabel("popup-tag", tag);
            SetLabel("popup-title", title);
            SetLabel("popup-body", body);

            if (Confirm != null)
            {
                Confirm.text = string.IsNullOrEmpty(confirmText) == true ? "확인" : confirmText;
            }

            if (Cancel != null)
            {
                var useCancel = string.IsNullOrEmpty(cancelText) == false;
                Cancel.EnableInClassList("hidden", useCancel == false);
                Cancel.text = useCancel == true ? cancelText : string.Empty;
            }

            OnConfirm = onConfirm;
            OnCancel = onCancel;
            IsOpen = true;

            Veil.RemoveFromClassList("hidden");
            Veil.pickingMode = PickingMode.Position;
            Veil.parent.pickingMode = PickingMode.Position;

            PlayOpen();
        }

        public void Hide()
        {
            if (Veil == null)
            {
                return;
            }

            KillRunning();
            IsOpen = false;
            Veil.AddToClassList("hidden");
            Veil.pickingMode = PickingMode.Ignore;

            if (Veil.parent != null)
            {
                Veil.parent.pickingMode = PickingMode.Ignore;
            }
        }

        private void Accept()
        {
            var action = OnConfirm;
            Hide();

            if (action != null)
            {
                action();
            }
        }

        private void Decline()
        {
            var action = OnCancel;
            Hide();

            if (action != null)
            {
                action();
            }
        }

        /// <summary>
        /// 떠오르며 또렷해진다. 갑자기 나타나면 무엇이 바뀌었는지 눈이 따라가지 못한다.
        /// 실시간 기준으로 돌린다. 장이 멈춰 있어도 팝업은 움직여야 한다.
        /// </summary>
        private void PlayOpen()
        {
            KillRunning();

            Veil.style.opacity = 0f;
            Panel.style.translate = new Translate(0f, RISE_FROM);
            Panel.style.scale = new Scale(new Vector2(SCALE_FROM, SCALE_FROM));

            var progress = 0f;
            Running = DOTween.To(() => progress, value =>
            {
                progress = value;
                Veil.style.opacity = Mathf.Clamp01(value / FADE_SECONDS * RISE_SECONDS);
                Panel.style.translate = new Translate(0f, Mathf.Lerp(RISE_FROM, 0f, value));

                var scale = Mathf.Lerp(SCALE_FROM, 1f, value);
                Panel.style.scale = new Scale(new Vector2(scale, scale));
            }, 1f, RISE_SECONDS).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void KillRunning()
        {
            if (Running == null)
            {
                return;
            }

            Running.Kill();
            Running = null;
        }

        private void SetLabel(string elementName, string value)
        {
            var label = Veil.Q<Label>(elementName);

            if (label == null)
            {
                return;
            }

            label.text = value == null ? string.Empty : value;
            label.EnableInClassList("hidden", string.IsNullOrEmpty(value));
        }
    }
}
