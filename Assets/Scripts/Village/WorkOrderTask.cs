using System.Collections.Generic;
using Underwrite.Data;
using Underwrite.Global;

namespace Underwrite.Village
{
    /// <summary>
    /// 주문 나르기(2026-10-17 사장님 결정 — 선택지만 눌러 보상을 받지 않고 실제로 해야 끝나는 일).
    ///
    /// 주문표 몇 장을 잠깐 보여 준 뒤 가리고, 기억대로 쟁반을 채워 내놓는다. 맞힌 만큼 품삯을 받는다.
    /// 여관(술·음식 → 테이블), 대장간(장비 → 숙소), 길드(전리품 → 정산 대상)가 같은 틀에 물건과 행선지만 바꿔 쓴다.
    /// </summary>
    public sealed class WorkOrderTask
    {
        public readonly List<WorkOrder> Orders = new List<WorkOrder>();
        public readonly string[] Items;

        /// <summary>내놓은 주문 수.</summary>
        public int Served { get; private set; }

        /// <summary>맞게 내놓은 주문 수.</summary>
        public int Correct { get; private set; }

        public bool IsDone
        {
            get
            {
                return Served >= Orders.Count;
            }
        }

        /// <summary>맞힌 비율. 0~1.</summary>
        public float Accuracy
        {
            get
            {
                return Orders.Count == 0 ? 1f : (float)Correct / Orders.Count;
            }
        }

        /// <summary>지금 채울 주문. 다 내놓았으면 null.</summary>
        public WorkOrder Current
        {
            get
            {
                return IsDone == true ? null : Orders[Served];
            }
        }

        private WorkOrderTask(string[] items)
        {
            Items = items;
        }

        /// <summary>
        /// 주문표를 짠다. 한 장에 물건 두세 가지, 한 가지에 1~2개. 행선지는 겹치지 않는다.
        /// </summary>
        public static WorkOrderTask Create(string[] items, string[] destinations, int orderCount, System.Random rng)
        {
            var task = new WorkOrderTask(items);
            var places = new List<string>(destinations);

            for (var i = 0; i < orderCount && places.Count > 0; i++)
            {
                var pick = rng.Next(places.Count);
                var order = new WorkOrder { Destination = places[pick], Counts = new int[items.Length] };
                places.RemoveAt(pick);

                var kinds = rng.Next(Constants.WORK_ITEMS_MIN, Constants.WORK_ITEMS_MAX + 1);

                for (var k = 0; k < kinds; k++)
                {
                    order.Counts[rng.Next(items.Length)] += 1;
                }

                task.Orders.Add(order);
            }

            return task;
        }

        /// <summary>쟁반을 내놓는다. 지금 주문과 개수까지 같으면 맞다.</summary>
        public bool Serve(int[] tray)
        {
            var order = Current;

            if (order == null)
            {
                return false;
            }

            var right = true;

            for (var i = 0; i < order.Counts.Length; i++)
            {
                var have = tray != null && i < tray.Length ? tray[i] : 0;

                if (have != order.Counts[i])
                {
                    right = false;
                    break;
                }
            }

            Served++;

            if (right == true)
            {
                Correct++;
            }

            return right;
        }
    }
}
