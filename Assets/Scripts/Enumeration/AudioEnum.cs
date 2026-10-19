namespace Underwrite.Enumeration
{
    /// <summary>효과음. 순서는 StringConstants.AUDIO_EFFECTS와 같다.</summary>
    public enum SoundEffect
    {
        /// <summary>단추를 누를 때.</summary>
        Sound_Click,

        /// <summary>장부·주문표·수첩을 펼칠 때.</summary>
        Sound_Page,

        /// <summary>돈이 오갈 때(품삯, 체결).</summary>
        Sound_Coins,

        /// <summary>장소에 들어설 때.</summary>
        Sound_Door,

        /// <summary>잘못 짚었거나 쫓겨났을 때.</summary>
        Sound_Close
    }

    /// <summary>음량을 따로 맞추는 소리 갈래(설정 창). 순서는 StringConstants.PREF_VOLUMES·TEXT_SETTINGS_CHANNELS와 같다.</summary>
    public enum AudioChannel
    {
        Channel_Music,
        Channel_Effects,
        Channel_Voice
    }
}
