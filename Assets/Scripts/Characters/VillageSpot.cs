using Underwrite.Enumeration;
using UnityEngine;

namespace Underwrite.Characters
{
    /// <summary>
    /// 영지에서 들어갈 수 있는 자리. 기획서 §5가 정한 여섯 곳이 여기에 해당한다.
    ///
    /// 기획서 §11의 "자금 및 정보 모으기"는 메뉴에서 고르는 것이 아니라
    /// 걸어가서 만나는 것이다. 그래야 영지가 지도가 아니라 장소가 된다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class VillageSpot : MonoBehaviour
    {
        [Tooltip("이 자리에서 무엇을 하는가.")]
        [SerializeField] private VillageActivity Activity = VillageActivity.Activity_Gather;

        [Tooltip("이 자리에서 얻는 정보의 출처. 소식에 이 이름이 붙는다.")]
        [SerializeField] private InfoSource Source = InfoSource.Source_TavernTalk;

        [Tooltip("화면에 띄울 이름.")]
        [SerializeField] private string DisplayName = "여관";

        [Tooltip("한 번 묻는 데 드는 돈.")]
        [SerializeField] private float Cost = 20f;

        /// <summary>플레이어가 지금 이 자리에 닿아 있는가.</summary>
        public bool PlayerInside { get; private set; }

        public InfoSource InfoSource
        {
            get
            {
                return Source;
            }
        }

        /// <summary>이 자리에서 할 수 있는 일. 정보를 캐는 곳만 있는 것이 아니다.</summary>
        public VillageActivity Kind
        {
            get
            {
                return Activity;
            }
        }

        public string Name
        {
            get
            {
                return DisplayName;
            }
        }

        public float Price
        {
            get
            {
                return Cost;
            }
        }

        private void Reset()
        {
            var collider = GetComponent<Collider2D>();

            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other) == false)
            {
                return;
            }

            PlayerInside = true;
            VillageSpotEvents.RaiseEnter(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other) == false)
            {
                return;
            }

            PlayerInside = false;
            VillageSpotEvents.RaiseExit(this);
        }

        private static bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            // 몸통에 Rigidbody2D가 달려 있으므로 부모까지 올라가 확인한다.
            return other.GetComponentInParent<PlayerCharacterInput>() != null;
        }
    }
}
