namespace Underwrite.Global
{
    /// <summary>
    /// 소리 파일 경로(Resources 기준, 확장자 없음). 모두 CC0 — 출처는 `Documents/Claude/Underwrite_에셋_출처.md`.
    /// </summary>
    public static partial class StringConstants
    {
        // 옛 「소리 · 크게/작게/끔」 단계(10/19 전). 새 갈래별 음량이 없으면 이 값에서 옮긴다.
        public const string PREF_VOLUME_STEP = "underwrite_volume_step";

        // 갈래별 음량(0~1). 순서는 AudioChannel과 같다.
        public static readonly string[] PREF_VOLUMES =
        {
            "underwrite_volume_music", "underwrite_volume_effects", "underwrite_volume_voice"
        };

        // 설정 창(2026-10-19 데모 점검 — 소리를 따로 맞출 수 없었고, 지하·거래판엔 메뉴가 없었다).
        public const string SYSTEM_MENU_STYLE = "UI/SystemMenu";
        public static string TEXT_SETTINGS = "설정";
        public static readonly string[] TEXT_SETTINGS_CHANNELS = { "음악", "효과음", "목소리" };
        public static string TEXT_SETTINGS_PERCENT = "{0}%";
        public static string TEXT_SETTINGS_CLOSE = "닫기";
        public static string TEXT_SETTINGS_SESSION_NOTE = "장 중에 나가면 이번 장은 저장되지 않는다(지하로 내려오기 직전으로 돌아간다)";

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

        // 짧은 한마디(바크, 2026-10-19). Resources/Voice/{목소리}/{감정}_{번호}. Qwen3-TTS로 미리 뽑은 wav — Tools/voice.
        public const string AUDIO_VOICE_ROOT = "Audio/Voice/";

        // 감정 파일 이름. 순서는 PortraitEmotion과 같다.
        public static readonly string[] AUDIO_VOICE_EMOTIONS =
        {
            "neutral", "happy", "anxious", "angry", "shocked", "sad", "relieved"
        };

        // 용사 목소리. 순서는 HERO_NAMES와 같다.
        public static readonly string[] AUDIO_VOICE_HEROES = { "edwin", "kaira", "elaria", "godric", "fio" };

        public const string AUDIO_VOICE_INNKEEPER = "innkeeper";
        public const string AUDIO_VOICE_ARCHIVIST = "archivist";
        public const string AUDIO_VOICE_SMITH = "smith";
        public const string AUDIO_VOICE_BROKER = "broker";
        public const string AUDIO_VOICE_STEWARD = "steward";
        public const string AUDIO_VOICE_INFORMANT = "informant";
    }
}
