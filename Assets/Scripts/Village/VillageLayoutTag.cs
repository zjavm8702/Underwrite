using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 영지 사물이 배치표의 몇 번째 항목에서 왔는지 적어 둔다(2026-10-18 — 영지 배치 툴).
    /// 사장님이 씬에서 옮기고 복제하고 지운 결과를 툴이 덮어쓰기 파일로 저장할 때 이 번호로 원본을 찾는다.
    /// 게임 실행 중에는 아무 일도 하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VillageLayoutTag : MonoBehaviour
    {
        [Tooltip("원본 배치표(layout.json) parts 배열의 순번.")]
        [SerializeField] private int Source;

        [Tooltip("사물 그림 파일 이름. 원본이 바뀌었는지 맞춰 보는 데 쓴다.")]
        [SerializeField] private string File;

        [Tooltip("장소 이름(inn·smith·guild·manor·board·well·stall). 집과 소품은 비어 있다.")]
        [SerializeField] private string Place;

        public int SourceIndex
        {
            get
            {
                return Source;
            }
        }

        public string FileName
        {
            get
            {
                return File;
            }
        }

        public string PlaceName
        {
            get
            {
                return Place;
            }
        }

        public void Configure(int source, string file, string place)
        {
            Source = source;
            File = file;
            Place = place;
        }
    }
}
