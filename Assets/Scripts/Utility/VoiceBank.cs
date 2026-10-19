using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Utility
{
    /// <summary>
    /// 인물의 짧은 한마디(바크, 2026-10-19 사장님 — 대사 전부가 아니라 한마디부터).
    /// 대화창의 이름으로 목소리를 찾고, 표정(감정)에 맞는 한마디를 고른다. 목소리가 없는 사람(주인공·서기 등)은 조용하다.
    /// 소리 파일은 처음 부를 때 한 번 읽어 둔다.
    /// </summary>
    public static class VoiceBank
    {
        // 대화창에 뜨는 이름 → 목소리 폴더.
        private static Dictionary<string, string> VoiceBySpeaker;

        // 목소리 폴더 + 감정 → 한마디 묶음. 없는 파일은 null로 남는다.
        private static readonly Dictionary<string, AudioClip[]> Clips = new Dictionary<string, AudioClip[]>();

        private static int LastVariant = -1;

        /// <summary>그 사람이 그 감정으로 한마디 한다. 목소리가 없으면 아무것도 하지 않는다.</summary>
        public static void Play(string speaker, PortraitEmotion emotion)
        {
            if (Constants.AUDIO_VOICE_ENABLED == false || string.IsNullOrEmpty(speaker) == true)
            {
                return;
            }

            EnsureSpeakers();

            string voice;

            if (VoiceBySpeaker.TryGetValue(speaker, out voice) == false)
            {
                return;
            }

            var clips = ClipsOf(voice, emotion);

            // 그 감정의 한마디가 없으면 보통 얼굴의 것으로.
            if (Count(clips) == 0 && emotion != PortraitEmotion.Emotion_Neutral)
            {
                clips = ClipsOf(voice, PortraitEmotion.Emotion_Neutral);
            }

            var clip = Pick(clips);

            if (clip != null)
            {
                GameAudio.PlayVoice(clip);
            }
        }

        private static void EnsureSpeakers()
        {
            if (VoiceBySpeaker != null)
            {
                return;
            }

            VoiceBySpeaker = new Dictionary<string, string>();

            for (var i = 0; i < StringConstants.HERO_NAMES.Length && i < StringConstants.AUDIO_VOICE_HEROES.Length; i++)
            {
                VoiceBySpeaker[StringConstants.HERO_NAMES[i]] = StringConstants.AUDIO_VOICE_HEROES[i];
            }

            VoiceBySpeaker[StringConstants.TEXT_FOLK_INNKEEPER] = StringConstants.AUDIO_VOICE_INNKEEPER;
            VoiceBySpeaker[StringConstants.TEXT_FOLK_ARCHIVIST] = StringConstants.AUDIO_VOICE_ARCHIVIST;
            VoiceBySpeaker[StringConstants.TEXT_FOLK_SMITH] = StringConstants.AUDIO_VOICE_SMITH;
            VoiceBySpeaker[StringConstants.TEXT_FOLK_BROKER] = StringConstants.AUDIO_VOICE_BROKER;
            VoiceBySpeaker[StringConstants.TEXT_FOLK_STEWARD] = StringConstants.AUDIO_VOICE_STEWARD;
            VoiceBySpeaker[StringConstants.TEXT_INFORMANT] = StringConstants.AUDIO_VOICE_INFORMANT;
        }

        private static AudioClip[] ClipsOf(string voice, PortraitEmotion emotion)
        {
            var index = (int)emotion;

            if (index < 0 || index >= StringConstants.AUDIO_VOICE_EMOTIONS.Length)
            {
                return null;
            }

            var key = voice + "/" + StringConstants.AUDIO_VOICE_EMOTIONS[index];
            AudioClip[] clips;

            if (Clips.TryGetValue(key, out clips) == true)
            {
                return clips;
            }

            clips = new AudioClip[Constants.AUDIO_VOICE_VARIANTS];

            for (var i = 0; i < clips.Length; i++)
            {
                clips[i] = Resources.Load<AudioClip>(StringConstants.AUDIO_VOICE_ROOT + key + "_" + i);
            }

            Clips.Add(key, clips);
            return clips;
        }

        private static int Count(AudioClip[] clips)
        {
            var count = 0;

            if (clips == null)
            {
                return 0;
            }

            for (var i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        // 바로 앞에 튼 번호는 피한다. 같은 말이 연달아 나오면 금방 질린다.
        private static AudioClip Pick(AudioClip[] clips)
        {
            var count = Count(clips);

            if (count == 0)
            {
                return null;
            }

            var start = Random.Range(0, clips.Length);

            for (var step = 0; step < clips.Length; step++)
            {
                var i = (start + step) % clips.Length;

                if (clips[i] != null && (i != LastVariant || count == 1))
                {
                    LastVariant = i;
                    return clips[i];
                }
            }

            return null;
        }
    }
}
