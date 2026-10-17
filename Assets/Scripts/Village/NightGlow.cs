using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 밤에만 켜지는 그림(건물 창문 불빛). 하루의 흐름이 정한 밤의 정도(<see cref="DayNightCycle.NightLevel"/>)만큼 보인다.
    /// 건물 바로 앞 순서에 그린다 — 건물의 앞뒤 순서는 YSortRenderer가 정하므로 그 값을 따라간다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class NightGlow : MonoBehaviour
    {
        private SpriteRenderer Glow;
        private SpriteRenderer Owner;

        private void Awake()
        {
            Glow = GetComponent<SpriteRenderer>();
            Owner = transform.parent == null ? null : transform.parent.GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            var level = DayNightCycle.NightLevel;
            Glow.enabled = level > 0.01f;

            if (Glow.enabled == false)
            {
                return;
            }

            var color = Glow.color;
            color.a = level;
            Glow.color = color;

            if (Owner != null)
            {
                Glow.sortingOrder = Owner.sortingOrder + 1;
            }
        }
    }
}
