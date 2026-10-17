using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>나무 둘레의 반딧불이. 밤의 정도만큼 나온다 — 낮에는 하나도 없다.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class NightFireflies : MonoBehaviour
    {
        [SerializeField] private float MaxRate = 1.6f;

        private ParticleSystem.EmissionModule Emission;

        private void Awake()
        {
            Emission = GetComponent<ParticleSystem>().emission;
        }

        private void Update()
        {
            Emission.rateOverTime = MaxRate * DayNightCycle.NightLevel;
        }
    }
}
