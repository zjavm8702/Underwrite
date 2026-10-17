using Underwrite.Global;
using Underwrite.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace Underwrite.Village
{
    /// <summary>
    /// 발밑 높이로 앞뒤를 정한다.
    ///
    /// 내려다보는 화면에서는 화면 아래쪽에 선 것이 앞에 있다. 가로등 뒤를 지나는 사람은
    /// 가로등에 가려져야 하고, 앞을 지나는 사람은 가로등을 가려야 한다.
    /// 배경이 한 장짜리면 이것이 불가능해서 사람이 그림 위에 붙인 스티커처럼 보였다.
    ///
    /// SortingGroup이 있으면 그것을, 없으면 SpriteRenderer를 정렬한다.
    /// 그림자·몸처럼 여러 장으로 된 캐릭터는 SortingGroup으로 묶어야 한 덩어리로 정렬된다.
    /// </summary>
    public sealed class YSortRenderer : MonoBehaviour
    {
        [Tooltip("루트에서 발이 닿는 자리까지의 세로 거리(월드 유닛). 피벗이 발밑이면 0.")]
        [SerializeField] private float FootOffset;

        [Tooltip("움직이지 않는 사물이면 켠다. 한 번만 정렬하고 매 프레임 계산을 멈춘다.")]
        [SerializeField] private bool IsStatic = true;

        private SortingGroup Group;
        private SpriteRenderer Sprite;

        /// <summary>발밑 높이에 해당하는 정렬 순서. 아래에 있을수록 크다(앞이다).</summary>
        public static int OrderFor(float footY)
        {
            return -Mathf.RoundToInt(footY * Constants.VILLAGE_SORT_PER_UNIT);
        }

        /// <summary>에디터 도구가 만들 때 값을 넣는다. 인스펙터에서 고쳐도 된다.</summary>
        public void Configure(float footOffset, bool isStatic)
        {
            FootOffset = footOffset;
            IsStatic = isStatic;
        }

        private void Awake()
        {
            Group = GetComponent<SortingGroup>();
            Sprite = GetComponent<SpriteRenderer>();

            if (Group == null && Sprite == null)
            {
                UnderwriteLog.Warning("SortingGroup도 SpriteRenderer도 없어 앞뒤 정렬을 건너뜁니다.", context: this);
                enabled = false;
            }
        }

        private void Start()
        {
            Apply();

            if (IsStatic == true)
            {
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            var order = OrderFor(transform.position.y + FootOffset);

            if (Group != null)
            {
                Group.sortingOrder = order;
                return;
            }

            Sprite.sortingOrder = order;
        }
    }
}
