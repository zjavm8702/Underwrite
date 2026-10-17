using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 원형 시계. 바늘을 끌어 몇 시까지 시간을 보낼지 고른다(2026-10-17 사용자 요청).
    ///
    /// 「시간 넘기기」가 다음 장 직전으로 한 번에 뛰어 버리면, 영지에서 쓸 수 있는 시간이라는 자원이
    /// 손에 잡히지 않는다. 시계 위에 지금부터 다음 장까지의 구간을 칠해 두고, 그 안에서 원하는 시각을 고르게 한다.
    ///
    /// 장 사이는 길어야 12시간이므로 12시간 문자판을 쓴다. 분은 하루 기준(0~1439를 넘을 수 있다)으로 받는다.
    /// </summary>
    public sealed class TimeDial : VisualElement
    {
        // 바늘이 멈추는 간격(분).
        private const int SNAP_MINUTES = 10;
        private const int HALF_DAY = 12 * 60;

        private static readonly Color FACE = new Color(0.95f, 0.91f, 0.82f);
        private static readonly Color RIM = new Color(0.36f, 0.26f, 0.17f);
        private static readonly Color RANGE = new Color(0.86f, 0.74f, 0.52f);
        private static readonly Color CHOSEN = new Color(0.55f, 0.33f, 0.16f);
        private static readonly Color INK = new Color(0.16f, 0.13f, 0.09f);

        private int Now;
        private int Limit;
        private int Selected;
        private bool Dragging;

        /// <summary>고른 시각이 바뀌었다. 하루 기준 분을 넘긴다.</summary>
        public event Action<int> Changed;

        /// <summary>고른 시각(하루 기준 분).</summary>
        public int SelectedMinute
        {
            get
            {
                return Selected;
            }
        }

        public TimeDial()
        {
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            // 시 숫자. 그리기 대신 라벨로 둬야 글꼴이 다른 화면과 같다.
            for (var hour = 1; hour <= 12; hour++)
            {
                var label = new Label(hour.ToString());
                label.AddToClassList("time-dial-hour");
                label.pickingMode = PickingMode.Ignore;
                label.userData = hour;
                Add(label);
            }

            RegisterCallback<GeometryChangedEvent>(_ => PlaceHourLabels());
        }

        /// <summary>
        /// 고를 수 있는 구간을 정한다. 지금부터 <paramref name="limit"/>까지. 처음 바늘은 끝에 둔다.
        /// 끝에 두는 이유: 이 단추를 누르는 사람은 대개 다음 장까지 보내려는 사람이다.
        /// </summary>
        public void SetRange(int now, int limit)
        {
            Now = now;
            Limit = Math.Max(now, limit);
            Selected = Limit;
            MarkDirtyRepaint();
            NotifyChanged();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            Dragging = true;
            this.CapturePointer(evt.pointerId);
            Pick(evt.localPosition);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (Dragging == false)
            {
                return;
            }

            Pick(evt.localPosition);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            Dragging = false;
            this.ReleasePointer(evt.pointerId);
        }

        // 누른 자리의 각도를 시각으로 바꾼다. 지금보다 앞이면 반 바퀴 뒤로 본다.
        private void Pick(Vector2 local)
        {
            var center = contentRect.center;
            var offset = local - center;

            if (offset.sqrMagnitude < 1f)
            {
                return;
            }

            // 12시가 위, 시계 방향으로 커진다.
            var angle = Mathf.Atan2(offset.x, -offset.y) * Mathf.Rad2Deg;

            if (angle < 0f)
            {
                angle += 360f;
            }

            var dialMinute = Mathf.RoundToInt(angle / 360f * HALF_DAY);
            var delta = ((dialMinute - Now % HALF_DAY) % HALF_DAY + HALF_DAY) % HALF_DAY;
            var minute = Now + Mathf.RoundToInt((float)delta / SNAP_MINUTES) * SNAP_MINUTES;

            // 구간 밖을 누르면 가까운 끝에 붙인다. 끝을 넘어선 자리는 대개 "끝까지"라는 뜻이다.
            if (minute > Limit)
            {
                var pastLimit = delta - (Limit - Now);
                var beforeNow = HALF_DAY - delta;
                minute = pastLimit < beforeNow ? Limit : Now;
            }

            if (minute == Selected)
            {
                return;
            }

            Selected = minute;
            MarkDirtyRepaint();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            var handler = Changed;

            if (handler != null)
            {
                handler(Selected);
            }
        }

        private void PlaceHourLabels()
        {
            var rect = contentRect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f * 0.78f;

            foreach (var child in Children())
            {
                if (child.userData is int == false)
                {
                    continue;
                }

                var hour = (int)child.userData;
                var radians = hour / 12f * Mathf.PI * 2f;
                var x = rect.center.x + Mathf.Sin(radians) * radius;
                var y = rect.center.y - Mathf.Cos(radians) * radius;
                child.style.position = Position.Absolute;
                child.style.left = x - 14f;
                child.style.top = y - 14f;
                child.style.width = 28f;
                child.style.height = 28f;
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - 4f;

            if (radius <= 0f)
            {
                return;
            }

            var painter = context.painter2D;

            // 문자판
            painter.fillColor = FACE;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            // 지금부터 다음 장 직전까지 — 쓸 수 있는 시간
            DrawArc(painter, center, radius * 0.62f, Now, Limit, RANGE, radius * 0.2f);

            // 지금부터 고른 시각까지 — 쓸 시간
            DrawArc(painter, center, radius * 0.62f, Now, Selected, CHOSEN, radius * 0.2f);

            // 테두리와 눈금
            painter.strokeColor = RIM;
            painter.lineWidth = 4f;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            painter.lineWidth = 2f;

            for (var hour = 0; hour < 12; hour++)
            {
                var radians = hour / 12f * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
                painter.BeginPath();
                painter.MoveTo(center + direction * radius * 0.9f);
                painter.LineTo(center + direction * radius);
                painter.Stroke();
            }

            // 바늘
            var hand = MinuteToDirection(Selected);
            painter.strokeColor = INK;
            painter.lineWidth = 5f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(center + hand * radius * 0.7f);
            painter.Stroke();

            painter.fillColor = INK;
            painter.BeginPath();
            painter.Arc(center, 7f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
        }

        private static void DrawArc(Painter2D painter, Vector2 center, float radius, int from, int to, Color color,
            float width)
        {
            if (to <= from)
            {
                return;
            }

            // Painter2D의 각도는 3시가 0도이고 시계 방향으로 커진다. 12시를 0으로 맞추려면 90도를 뺀다.
            var start = (from % HALF_DAY) / (float)HALF_DAY * 360f - 90f;
            var sweep = Mathf.Min(359.9f, (to - from) / (float)HALF_DAY * 360f);

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Butt;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(start), Angle.Degrees(start + sweep));
            painter.Stroke();
        }

        private static Vector2 MinuteToDirection(int minute)
        {
            var radians = (minute % HALF_DAY) / (float)HALF_DAY * Mathf.PI * 2f;
            return new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
        }
    }
}
