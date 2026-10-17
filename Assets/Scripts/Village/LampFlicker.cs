using Underwrite.Utility;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Underwrite.Village
{
    /// <summary>
    /// 등불과 창문 불빛을 조금씩 흔든다.
    ///
    /// 빛은 그림에 그리지 않고 2D 라이트로 붙인다. 그림에 번짐을 그려 넣으면
    /// 그 빛이 캐릭터에는 닿지 않아 배경과 캐릭터가 다른 빛 아래 있는 것처럼 보인다.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class LampFlicker : MonoBehaviour
    {
        [Tooltip("흔들림 폭. 기본 밝기의 몇 배만큼 오르내리는가.")]
        [SerializeField] private float Depth = 0.12f;

        [Tooltip("흔들림 빠르기. 클수록 촛불처럼 빠르게 떤다.")]
        [SerializeField] private float Speed = 2.2f;

        /// <summary>
        /// 켜진 정도. 0이면 꺼지고 1이면 기본 밝기다. 하루의 흐름(DayNightCycle)이 가로등에 넣는다.
        /// 늘 타는 불(대장간 화로)은 1 그대로다.
        /// </summary>
        public float Level { get; set; } = 1f;

        private Light2D Lamp;
        private float BaseIntensity;
        private float Seed;

        private void Awake()
        {
            Lamp = GetComponent<Light2D>();
            BaseIntensity = Lamp.intensity;
            // 등불마다 다른 자리의 잡음을 읽는다. 같으면 모든 불이 함께 깜빡인다.
            Seed = UnityEngine.Random.value * 100f;

            if (BaseIntensity <= 0f)
            {
                UnderwriteLog.Warning("불빛 세기가 0이라 흔들 것이 없습니다.", context: this);
                enabled = false;
            }
        }

        private void Update()
        {
            var noise = Mathf.PerlinNoise(Seed, Time.time * Speed) * 2f - 1f;
            Lamp.intensity = BaseIntensity * Level * (1f + noise * Depth);
            Lamp.enabled = Level > 0.01f;
        }
    }
}
