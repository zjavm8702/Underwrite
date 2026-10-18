using System.Collections.Generic;
using Underwrite.Characters;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Village;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 영지 미니맵(2026-10-18 사장님 — 어떤 NPC와 상점이 어디 있는지 표시).
    /// 오른쪽 위에 작게, M키나 클릭으로 크게 펼친다. 바닥은 영지 바닥 그림을 그대로 줄여 쓴다.
    /// 표지(장소·일하는 사람)는 처음 한 번만 만들고, 매 프레임에는 주인공 점의 자리만 옮긴다 — 할당 없음.
    /// </summary>
    public sealed class UI_VillageMinimap
    {
        private sealed class Marker
        {
            public VisualElement Element;
            public Vector2 Normalized;
            public string Title;
        }

        private struct LightState
        {
            public bool Enabled;
            public Color Color;
            public float Intensity;
        }

        private readonly VisualElement Frame;
        private readonly VisualElement Map;
        private readonly VisualElement PlayerDot;
        private readonly Label Hint;
        private readonly List<Marker> Places = new List<Marker>();
        private readonly List<Marker> People = new List<Marker>();
        private Rect WorldBounds;
        private Texture2D Snapshot;
        private bool Expanded;
        private string Target;

        /// <param name="hideWhileCapturing">지도 그림에 찍히면 안 되는 것(주인공 — 점으로 따로 그린다).</param>
        public UI_VillageMinimap(VisualElement root, SpriteRenderer ground, IEnumerable<VillageSpot> spots,
            IEnumerable<VillageNpc> npcs, GameObject hideWhileCapturing)
        {
            Frame = new VisualElement();
            Frame.AddToClassList("minimap");
            Frame.RegisterCallback<ClickEvent>(evt => Toggle());

            Map = new VisualElement();
            Map.AddToClassList("minimap-map");
            Map.pickingMode = PickingMode.Ignore;
            Frame.Add(Map);

            Hint = new Label(StringConstants.TEXT_MINIMAP_HINT);
            Hint.AddToClassList("minimap-hint");
            Hint.pickingMode = PickingMode.Ignore;
            Frame.Add(Hint);

            if (ground != null && ground.sprite != null)
            {
                var bounds = ground.bounds;
                WorldBounds = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);

                // 건물까지 보이게 영지 전체를 한 번 찍어 지도 그림으로 쓴다. 실패하면 바닥 그림.
                Snapshot = Capture(bounds, hideWhileCapturing);
                Map.style.backgroundImage = Snapshot != null
                    ? new StyleBackground(Snapshot)
                    : new StyleBackground(ground.sprite.texture);
            }

            foreach (var npc in npcs)
            {
                // 일하는 사람(주민·노점)만. 돌아다니는 모험단은 자리가 없다.
                if (npc.Party != null || npc.Kind == NpcRole.Role_Adventurer)
                {
                    continue;
                }

                People.Add(AddMarker(npc.transform.position, npc.Name, "minimap-person"));
            }

            foreach (var spot in spots)
            {
                if (spot.Kind == VillageActivity.Activity_Enter)
                {
                    continue;
                }

                Places.Add(AddMarker(spot.transform.position, spot.Name, "minimap-place"));
            }

            PlayerDot = new VisualElement();
            PlayerDot.AddToClassList("minimap-player");
            PlayerDot.pickingMode = PickingMode.Ignore;
            Map.Add(PlayerDot);

            root.Add(Frame);
            ApplyExpanded();
        }

        public bool IsExpanded
        {
            get
            {
                return Expanded;
            }
        }

        public void Toggle()
        {
            Expanded = !Expanded;
            ApplyExpanded();
        }

        /// <summary>매 프레임. 주인공 자리와 프롤로그의 다음 목적지를 갱신한다.</summary>
        public void Tick(Vector3 player, bool hidden, string target)
        {
            Frame.style.display = hidden == true ? DisplayStyle.None : DisplayStyle.Flex;

            if (hidden == true || WorldBounds.width <= 0f)
            {
                return;
            }

            var at = Normalize(player);
            PlayerDot.style.left = Length.Percent(at.x * 100f);
            PlayerDot.style.top = Length.Percent(at.y * 100f);

            if (target == Target)
            {
                return;
            }

            Target = target;

            foreach (var place in Places)
            {
                place.Element.EnableInClassList("minimap-place--target", target != null && place.Title == target);
            }
        }

        private Marker AddMarker(Vector3 world, string title, string kind)
        {
            var marker = new Marker { Normalized = Normalize(world), Title = title };
            var element = new VisualElement();
            element.AddToClassList("minimap-marker");
            element.AddToClassList(kind);
            element.pickingMode = PickingMode.Ignore;
            element.style.left = Length.Percent(marker.Normalized.x * 100f);
            element.style.top = Length.Percent(marker.Normalized.y * 100f);

            var dot = new VisualElement();
            dot.AddToClassList("minimap-dot");
            dot.pickingMode = PickingMode.Ignore;
            element.Add(dot);

            var label = new Label(title);
            label.AddToClassList("minimap-label");
            label.pickingMode = PickingMode.Ignore;
            element.Add(label);

            Map.Add(element);
            marker.Element = element;
            return marker;
        }

        /// <summary>영지를 떠날 때 찍어 둔 지도 그림을 놓는다.</summary>
        public void Dispose()
        {
            if (Snapshot != null)
            {
                Object.Destroy(Snapshot);
                Snapshot = null;
            }
        }

        // 영지 전체를 위에서 한 장 찍는다. 잠깐 쓰는 카메라·렌더 텍스처는 바로 지운다(처음 한 번, 저빈도).
        private static Texture2D Capture(Bounds bounds, GameObject hidden)
        {
            var renderers = hidden == null ? new SpriteRenderer[0] : hidden.GetComponentsInChildren<SpriteRenderer>();

            // 낮·밤 조명이 지도에 박히면 영지에 들어올 때마다 색이 달라진다. 찍는 동안만 낮 조명으로 맞춘다.
            var lights = Object.FindObjectsByType<Light2D>();
            var states = new LightState[lights.Length];

            for (var i = 0; i < lights.Length; i++)
            {
                states[i] = new LightState { Enabled = lights[i].enabled, Color = lights[i].color, Intensity = lights[i].intensity };

                if (lights[i].lightType == Light2D.LightType.Global)
                {
                    lights[i].color = Color.white;
                    lights[i].intensity = 1f;
                }
                else
                {
                    lights[i].enabled = false;
                }
            }

            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

            var width = Constants.MINIMAP_SNAPSHOT_WIDTH;
            var height = Mathf.RoundToInt(width * bounds.size.y / bounds.size.x);
            var go = new GameObject("Minimap Camera");
            var camera = go.AddComponent<Camera>();
            var target = RenderTexture.GetTemporary(width, height, 24);

            var previous = RenderTexture.active;
            Texture2D texture = null;

            try
            {
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.y;
                camera.aspect = bounds.size.x / bounds.size.y;
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, true);
                return texture;
            }
            catch (System.Exception e)
            {
                // 찍지 못하면 바닥 그림으로 대신한다(부른 쪽).
                Utility.UnderwriteLog.Warning($"미니맵을 찍지 못했습니다: {e.Message}");

                if (texture != null)
                {
                    Object.Destroy(texture);
                }

                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(go);

                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }

                for (var i = 0; i < lights.Length; i++)
                {
                    lights[i].enabled = states[i].Enabled;
                    lights[i].color = states[i].Color;
                    lights[i].intensity = states[i].Intensity;
                }
            }
        }

        // 월드 좌표 → 지도 안의 0~1(왼쪽 위 원점).
        private Vector2 Normalize(Vector3 world)
        {
            if (WorldBounds.width <= 0f || WorldBounds.height <= 0f)
            {
                return Vector2.zero;
            }

            return new Vector2(
                Mathf.Clamp01((world.x - WorldBounds.xMin) / WorldBounds.width),
                Mathf.Clamp01(1f - (world.y - WorldBounds.yMin) / WorldBounds.height));
        }

        private void ApplyExpanded()
        {
            Frame.EnableInClassList("minimap--expanded", Expanded);
            Hint.text = Expanded == true ? StringConstants.TEXT_MINIMAP_HINT_CLOSE : StringConstants.TEXT_MINIMAP_HINT;
        }
    }
}
