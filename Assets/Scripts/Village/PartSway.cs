using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 나무·덩굴을 바람에 살짝 흔든다.
    ///
    /// 캐릭터만 숨 쉬고 세상이 사진처럼 멈춰 있으면 둘이 따로 논다.
    /// 피벗이 발밑이므로 회전하면 밑동은 그대로 있고 위로 갈수록 크게 움직인다.
    /// 줄기까지 통째로 기우는 한계가 있어 각도를 1도 아래로 둔다. 잎만 흔들려면 셰이더가 필요하다.
    /// </summary>
    public sealed class PartSway : MonoBehaviour
    {
        [Tooltip("최대 기울기(도). 1도를 넘으면 줄기가 꺾여 보인다.")]
        [SerializeField] private float Amplitude = 0.6f;

        [Tooltip("한 번 왕복하는 시간(초).")]
        [SerializeField] private float Period = 4.5f;

        private float Phase;
        private Quaternion BaseRotation;

        private void Awake()
        {
            // 나무마다 박자가 같으면 숲 전체가 한 몸처럼 움직인다.
            Phase = UnityEngine.Random.value * Mathf.PI * 2f;
            BaseRotation = transform.localRotation;
        }

        private void Update()
        {
            if (Period <= 0f)
            {
                return;
            }

            var t = Time.time * Mathf.PI * 2f / Period + Phase;
            // 두 박자를 섞어 너무 규칙적인 시계추처럼 보이지 않게 한다.
            var angle = Amplitude * (0.7f * Mathf.Sin(t) + 0.3f * Mathf.Sin(t * 2.3f));
            transform.localRotation = BaseRotation * Quaternion.Euler(0f, 0f, angle);
        }
    }
}
