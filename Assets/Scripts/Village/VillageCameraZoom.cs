using Underwrite.Global;
using Underwrite.Utility;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Underwrite.Village
{
    /// <summary>
    /// 마우스 휠로 영지 카메라를 당겨 본다(2026-10-17 사용자 요청).
    ///
    /// 처음 보이는 크기가 가장 먼 거리다. 거기서 더 물러나면 지도 바깥이 비치고 사람이 너무 작아진다.
    /// 당기면 인물과 사물이 커져 표정 없는 필드 캐릭터도 몸짓이 읽힌다.
    ///
    /// 휠은 한 칸씩 끊기지만 화면은 부드럽게 따라간다. 크기가 바뀌면 경계 상자(Confiner)의 계산을 다시 하게 한다.
    /// 그러지 않으면 당긴 뒤에도 먼 거리의 경계로 가둬 화면 가장자리에서 카메라가 멈춘다.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class VillageCameraZoom : MonoBehaviour
    {
        [Tooltip("가장 가까이 당겼을 때의 Orthographic Size. 작을수록 크게 보인다.")]
        [SerializeField] private float NearestSize = Constants.VILLAGE_ZOOM_NEAREST;

        private CinemachineCamera Vcam;
        private CinemachineConfiner2D Confiner;
        private float FarthestSize;
        private float Target;

        /// <summary>
        /// 휠을 잠시 막는다. 대화·시계처럼 화면 위에 무언가 떠 있을 때 휠이 카메라로 새지 않게 한다.
        /// 영지 화면이 매 프레임 알려 준다.
        /// </summary>
        public static bool Blocked { get; set; }

        private void Awake()
        {
            Vcam = GetComponent<CinemachineCamera>();
            Confiner = GetComponent<CinemachineConfiner2D>();
            FarthestSize = Vcam.Lens.OrthographicSize;
            Target = FarthestSize;

            if (NearestSize >= FarthestSize)
            {
                UnderwriteLog.Warning("가장 가까운 크기가 가장 먼 크기보다 커서 확대를 쓰지 않습니다.", context: this);
                enabled = false;
            }
        }

        private void Update()
        {
            var mouse = Mouse.current;

            if (mouse != null && Blocked == false)
            {
                var scroll = mouse.scroll.ReadValue().y;

                // 휠 한 칸은 보통 120이다. 부호만 보고 한 단계씩 움직인다. 터치패드의 잔 움직임은 무시한다.
                if (scroll > Constants.VILLAGE_ZOOM_SCROLL_THRESHOLD)
                {
                    Target -= Constants.VILLAGE_ZOOM_STEP;
                }
                else if (scroll < -Constants.VILLAGE_ZOOM_SCROLL_THRESHOLD)
                {
                    Target += Constants.VILLAGE_ZOOM_STEP;
                }

                Target = Mathf.Clamp(Target, NearestSize, FarthestSize);
            }

            var lens = Vcam.Lens;
            var current = lens.OrthographicSize;

            if (Mathf.Abs(current - Target) < 0.001f)
            {
                return;
            }

            lens.OrthographicSize = Mathf.Lerp(current, Target, 1f - Mathf.Exp(-Constants.VILLAGE_ZOOM_SMOOTH * Time.deltaTime));
            Vcam.Lens = lens;

            if (Confiner != null)
            {
                Confiner.InvalidateLensCache();
            }
        }
    }
}
