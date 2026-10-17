namespace Underwrite.Utility
{
    /// <summary>
    /// 이름 뒤의 조사를 받침에 맞춘다. 새 용병단 이름이 끝없이 바뀌므로 문장에 조사를 박아 둘 수 없다.
    /// 틀에는 <c>{0}{이}</c>·<c>{0}{은}</c>·<c>{0}{을}</c>·<c>{0}{과}</c>로 적는다. 남은 <c>{0}</c>은 이름만 넣는다.
    /// 한국어 전용이다 — 언어 설정이 들어오면 언어마다 따로 처리한다.
    /// </summary>
    public static class Josa
    {
        private const char HANGUL_FIRST = '가';
        private const char HANGUL_LAST = '힣';
        private const int FINALS_PER_SYLLABLE = 28;

        public static string Format(string template, string name)
        {
            if (string.IsNullOrEmpty(template) == true)
            {
                return string.Empty;
            }

            var batchim = HasFinal(name);
            return template
                .Replace("{0}{이}", name + (batchim == true ? "이" : "가"))
                .Replace("{0}{은}", name + (batchim == true ? "은" : "는"))
                .Replace("{0}{을}", name + (batchim == true ? "을" : "를"))
                .Replace("{0}{과}", name + (batchim == true ? "과" : "와"))
                .Replace("{0}", name);
        }

        /// <summary>마지막 글자에 받침이 있는가. 한글이 아니면 있는 것으로 본다.</summary>
        public static bool HasFinal(string word)
        {
            if (string.IsNullOrEmpty(word) == true)
            {
                return true;
            }

            var last = word[word.Length - 1];

            if (last < HANGUL_FIRST || last > HANGUL_LAST)
            {
                return true;
            }

            return (last - HANGUL_FIRST) % FINALS_PER_SYLLABLE != 0;
        }
    }
}
