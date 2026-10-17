namespace Underwrite.Enumeration
{
    /// <summary>
    /// 필드 캐릭터가 하는 몸짓. 애니메이션이 늘어나면 여기에 더한다(2026-10-17 사용자: 애니메이션을 많이 만든다).
    ///
    /// Animator에 같은 이름의 상태가 있으면 그것을 재생한다. 없으면 코드로 몸을 조금 움직여 대신한다.
    /// 그래서 그림이 나오기 전에도 상호작용이 보이고, 그림이 들어오면 상태 하나만 추가하면 바뀐다.
    /// </summary>
    public enum ActorMotion
    {
        /// <summary>두 주먹을 들고 기뻐한다. 주인공·NPC 모두 Animator에 Cheer 상태가 있다.</summary>
        Motion_Cheer,

        /// <summary>고개를 끄덕인다. 수긍·인사.</summary>
        Motion_Nod,

        /// <summary>몸을 좌우로 떤다. 놀람·거절.</summary>
        Motion_Shake,

        /// <summary>제자리에서 깡충. 반가움·신남.</summary>
        Motion_Hop
    }

    /// <summary>머리 위에 뜨는 감정 표시.</summary>
    public enum ActorEmote
    {
        Emote_Exclaim,
        Emote_Question,
        Emote_Dots,
        Emote_Music,
        Emote_Heart,
        Emote_Coin
    }
}
