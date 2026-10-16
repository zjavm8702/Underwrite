using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Underwrite.Data
{
    /// <summary>표 한 줄에 해당하는 문구 하나.</summary>
    [Serializable]
    public sealed class TextEntry
    {
        [Tooltip("코드에서 쓰는 열쇠. StringConstants의 상수 이름과 같게 둔다.")]
        public string Key;

        [TextArea]
        public string Korean;

        [Tooltip("{0}처럼 값이 끼어드는 문구인지. 켜져 있으면 그대로 쓰면 안 된다.")]
        public bool HasFormat;
    }

    /// <summary>
    /// 화면에 나가는 글자. 코드는 열쇠만 들고 뜻은 이 표에서 가져온다.
    ///
    /// 열쇠 상수는 <see cref="Global.StringConstants"/>에 그대로 둔다.
    /// 코드가 날문자열을 들고 있지 않게 하는 것이 목적이지, 상수를 없애려는 것이 아니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UITextTable", menuName = "Underwrite/문구 표")]
    public sealed class UITextTable : ScriptableObject
    {
        private readonly Dictionary<string, string> Lookup = new Dictionary<string, string>();

        [SerializeField] private List<TextEntry> Entries = new List<TextEntry>();

        public IReadOnlyList<TextEntry> Rows
        {
            get
            {
                return Entries;
            }
        }

        public void Replace(List<TextEntry> rows)
        {
            Entries = rows;
            Lookup.Clear();
        }

        /// <summary>
        /// 표의 문구를 <see cref="Global.StringConstants"/>에 덮어쓴다. 장을 열기 전에 한 번 부른다.
        ///
        /// 코드는 상수 이름으로 문구를 참조하고, 뜻은 여기서 들어온다.
        /// 표에 없는 열쇠는 그냥 건너뛴다. 코드에 적힌 기본값이 그대로 남으므로
        /// 표가 비어 있어도 화면에 빈칸이나 열쇠 이름이 뜨지 않는다.
        /// </summary>
        public void Apply()
        {
            var fields = typeof(Global.StringConstants).GetFields(
                BindingFlags.Public | BindingFlags.Static);

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                if (string.IsNullOrEmpty(entry.Key) == true || entry.Korean == null)
                {
                    continue;
                }

                for (var f = 0; f < fields.Length; f++)
                {
                    var field = fields[f];

                    if (field.IsLiteral == true || field.FieldType != typeof(string))
                    {
                        continue;
                    }

                    if (field.Name != entry.Key)
                    {
                        continue;
                    }

                    field.SetValue(null, entry.Korean);
                    break;
                }
            }
        }

        /// <summary>
        /// 열쇠로 문구를 찾는다. 없으면 열쇠를 그대로 돌려준다.
        /// 화면에 열쇠가 보이면 표에 줄이 빠진 것이라 바로 눈에 띈다.
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key) == true)
            {
                return string.Empty;
            }

            if (Lookup.Count != Entries.Count)
            {
                Rebuild();
            }

            string value;

            if (Lookup.TryGetValue(key, out value) == true)
            {
                return value;
            }

            return key;
        }

        private void Rebuild()
        {
            Lookup.Clear();

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                if (string.IsNullOrEmpty(entry.Key) == true)
                {
                    continue;
                }

                Lookup[entry.Key] = entry.Korean;
            }
        }
    }
}
