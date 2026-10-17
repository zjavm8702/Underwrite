using System.Collections.Generic;
using Underwrite.Enumeration;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 영지에 선 사람 하나. 다가가 말을 걸 수 있다.
    ///
    /// 영지가 비어 있으면 지도가 아니라 무대 세트로 보인다. 건물마다 사람이 있고, 노점에 장수가 있고,
    /// 모험단이 무리 지어 걸어야 "사람이 사는 영지"가 된다. 그래서 할 말은 짧게 두고 수를 늘린다.
    /// </summary>
    public sealed class VillageNpc : MonoBehaviour
    {
        private static readonly List<VillageNpc> AllNpcs = new List<VillageNpc>();

        private NpcWalker Walker;

        [SerializeField] private NpcRole Role = NpcRole.Role_Resident;

        [Tooltip("말할 때 이름줄에 뜬다.")]
        [SerializeField] private string DisplayName = "주민";

        [Tooltip("어디 사람인가. 이름 아래 꼬리표로 뜬다. 모험단이면 단 이름.")]
        [SerializeField] private string Affiliation = string.Empty;

        [Tooltip("말을 걸면 이 가운데 하나를 말한다.")]
        [TextArea]
        [SerializeField] private List<string> Lines = new List<string>();

        /// <summary>영지에 서 있는 모든 사람. 가장 가까운 사람을 찾는 데 쓴다.</summary>
        public static IReadOnlyList<VillageNpc> All
        {
            get
            {
                return AllNpcs;
            }
        }

        public NpcRole Kind
        {
            get
            {
                return Role;
            }
        }

        public string Name
        {
            get
            {
                return DisplayName;
            }
        }

        public string Group
        {
            get
            {
                return Affiliation;
            }
        }

        /// <summary>발이 닿은 자리. 걷는 사람이면 그 발을, 아니면 루트를 쓴다.</summary>
        public Vector2 Foot
        {
            get
            {
                return Walker != null ? Walker.Foot : (Vector2)transform.position;
            }
        }

        public void Configure(NpcRole role, string displayName, string affiliation, List<string> lines)
        {
            Role = role;
            DisplayName = displayName;
            Affiliation = affiliation;
            Lines = lines;
        }

        /// <summary>할 말 하나를 고른다. 없으면 빈 문자열.</summary>
        public string PickLine()
        {
            if (Lines == null || Lines.Count == 0)
            {
                return string.Empty;
            }

            return Lines[UnityEngine.Random.Range(0, Lines.Count)];
        }

        private void Awake()
        {
            Walker = GetComponent<NpcWalker>();
        }

        private void OnEnable()
        {
            AllNpcs.Add(this);
        }

        private void OnDisable()
        {
            AllNpcs.Remove(this);
        }
    }
}
