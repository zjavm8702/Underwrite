using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>나무 둘레의 반딧불이. 밤의 정도만큼 나온다 — 낮에는 하나도 없다.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class NightFireflies : MonoBehaviour
    {
        [SerializeField] private float MaxRate = 1.6f;

        // 모듈(struct)은 들고 있지 않는다 — Play 중 스크립트를 다시 읽으면 비어서 예외가 난다. 시스템만 들고 그때그때 꺼낸다.
        private ParticleSystem Particles;
        private float Rate = -1f;

        private void Awake()
        {
            Particles = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            if (Particles == null)
            {
                Particles = GetComponent<ParticleSystem>();
            }

            var rate = MaxRate * DayNightCycle.NightLevel;

            if (rate == Rate)
            {
                return;
            }

            Rate = rate;
            var emission = Particles.emission;
            emission.rateOverTime = rate;
        }
    }
}
