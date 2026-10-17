using System.Collections.Generic;
using Underwrite.Market;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Underwrite.Village
{
    /// <summary>
    /// 영지의 하루(2026-10-17 사용자 요청 — 어두워지고 밝아지고 등불이 켜지게).
    ///
    /// 영지 시계(<see cref="BreakClock"/>)의 시각으로 전역 빛의 색·밝기를 바꾸고, 가로등을 켜고 끈다.
    /// 새벽 분홍 → 낮 흰빛 → 해질녘 주황 → 저녁 보라 → 밤 푸른빛. 열쇠 시각 사이는 부드럽게 잇는다.
    /// 대장간 화로처럼 늘 타는 불은 건드리지 않는다(이름이 가로등이 아닌 불빛).
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        private const string LAMP_NAME = "Lamp Light";

        private struct Key
        {
            public float Hour;
            public Color Color;
            public float Intensity;

            /// <summary>가로등이 켜진 정도. 0 꺼짐, 1 완전히 켜짐.</summary>
            public float Lamps;

            public Key(float hour, Color color, float intensity, float lamps)
            {
                Hour = hour;
                Color = color;
                Intensity = intensity;
                Lamps = lamps;
            }
        }

        // 시각 순서. 마지막은 첫 열쇠와 같아 자정을 부드럽게 넘는다.
        private static readonly Key[] KEYS =
        {
            new Key(0f, new Color(0.36f, 0.44f, 0.85f), 0.32f, 1f),
            new Key(5f, new Color(0.36f, 0.44f, 0.85f), 0.32f, 1f),
            new Key(6.5f, new Color(1f, 0.8f, 0.78f), 0.72f, 0.5f),
            new Key(8f, new Color(1f, 0.98f, 0.95f), 1f, 0f),
            new Key(16f, new Color(1f, 0.98f, 0.95f), 1f, 0f),
            new Key(18f, new Color(1f, 0.8f, 0.58f), 0.88f, 0.3f),
            new Key(19.5f, new Color(0.78f, 0.6f, 0.78f), 0.62f, 0.85f),
            new Key(21f, new Color(0.36f, 0.44f, 0.85f), 0.32f, 1f),
            new Key(24f, new Color(0.36f, 0.44f, 0.85f), 0.32f, 1f)
        };

        /// <summary>밤의 정도. 0 낮, 1 깊은 밤. 창문 불빛·반딧불이가 읽는다(가로등이 켜진 정도와 같다).</summary>
        public static float NightLevel { get; private set; }

        // 이 정도 넘게 어두워지면 노점 장수와 문 앞 주민은 들어간다. 밤거리에는 용병단만 남는다.
        private const float STREET_EMPTY_LEVEL = 0.9f;

        private Light2D Sun;
        private readonly List<VillageNpc> Townsfolk = new List<VillageNpc>();
        private bool StreetEmpty;
        private readonly List<LampFlicker> Lamps = new List<LampFlicker>();

        private void Start()
        {
            foreach (var light in FindObjectsByType<Light2D>())
            {
                if (light.lightType == Light2D.LightType.Global && Sun == null)
                {
                    Sun = light;
                }
            }

            foreach (var lamp in FindObjectsByType<LampFlicker>())
            {
                if (lamp.gameObject.name == LAMP_NAME)
                {
                    Lamps.Add(lamp);
                }
            }

            foreach (var npc in FindObjectsByType<VillageNpc>())
            {
                if (npc.Kind == Enumeration.NpcRole.Role_Resident || npc.Kind == Enumeration.NpcRole.Role_Vendor)
                {
                    Townsfolk.Add(npc);
                }
            }

            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            var clock = BreakClock.Current;

            if (clock == null)
            {
                return;
            }

            var minutes = ((clock.BreakMinuteOfDay % 1440) + 1440) % 1440;
            var hour = (minutes + clock.BreakSecond / 60f) / 60f;
            var key = Sample(hour);

            if (Sun != null)
            {
                Sun.color = key.Color;
                Sun.intensity = key.Intensity;
            }

            NightLevel = key.Lamps;
            var empty = key.Lamps >= STREET_EMPTY_LEVEL;

            if (empty != StreetEmpty)
            {
                StreetEmpty = empty;

                for (var i = 0; i < Townsfolk.Count; i++)
                {
                    if (Townsfolk[i] != null)
                    {
                        Townsfolk[i].gameObject.SetActive(empty == false);
                    }
                }
            }

            for (var i = 0; i < Lamps.Count; i++)
            {
                if (Lamps[i] != null)
                {
                    Lamps[i].Level = key.Lamps;
                }
            }
        }

        private static Key Sample(float hour)
        {
            for (var i = 0; i < KEYS.Length - 1; i++)
            {
                var from = KEYS[i];
                var to = KEYS[i + 1];

                if (hour < from.Hour || hour > to.Hour)
                {
                    continue;
                }

                var t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from.Hour, to.Hour, hour));
                return new Key(hour, Color.Lerp(from.Color, to.Color, t), Mathf.Lerp(from.Intensity, to.Intensity, t),
                    Mathf.Lerp(from.Lamps, to.Lamps, t));
            }

            return KEYS[0];
        }
    }
}
