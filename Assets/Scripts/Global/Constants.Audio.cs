namespace Underwrite.Global
{
    /// <summary>
    /// 소리 파일 경로(Resources 기준, 확장자 없음). 모두 CC0 — 출처는 `Documents/Claude/Underwrite_에셋_출처.md`.
    /// </summary>
    public static partial class StringConstants
    {
        public const string AUDIO_MUSIC_VILLAGE = "Audio/Music/music_village";
        public const string AUDIO_MUSIC_UNDERGROUND = "Audio/Music/music_underground";

        // 순서는 SoundEffect와 같다.
        public static readonly string[] AUDIO_EFFECTS =
        {
            "Audio/Sfx/sfx_click",
            "Audio/Sfx/sfx_page",
            "Audio/Sfx/sfx_coins",
            "Audio/Sfx/sfx_door",
            "Audio/Sfx/sfx_close"
        };
    }
}
