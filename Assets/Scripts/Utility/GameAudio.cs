using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Underwrite.Utility
{
    /// <summary>
    /// 음악과 효과음(2026-10-18 인터뷰 — 무료 에셋으로, 배선까지 과장이). 에셋은 모두 CC0이고 출처는
    /// `Documents/Claude/Underwrite_에셋_출처.md`에 있다.
    ///
    /// 장면이 바뀌면 그 장면의 음악으로 천천히 갈아 끼운다. 영지는 여관 음악, 지하(방·거래소)는 어두운 음악, 인트로는 조용히.
    /// 씬마다 따로 두면 장면을 넘을 때 음악이 끊기므로, 게임이 시작될 때 하나 만들어 끝까지 둔다.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private static GameAudio instance;
        private static bool IsQuitting;

        private AudioSource Music;
        private AudioSource Effects;
        private AudioClip[] Clips;
        private string MusicPath = string.Empty;
        private float MusicTarget;

        /// <summary>새로 만들지 않고 지금 있는 것을 찾는다. 종료 중이면 없다.</summary>
        public static bool TryGetInstance(out GameAudio audio)
        {
            audio = instance;
            return audio != null && IsQuitting == false;
        }

        /// <summary>효과음 하나. 소리 관리자가 없으면(에디터에서 장면 하나만 띄웠을 때 등) 조용히 넘어간다.</summary>
        public static void Play(SoundEffect effect)
        {
            GameAudio audio;

            if (TryGetInstance(out audio) == true)
            {
                audio.PlayEffect(effect);
            }
        }

        /// <summary>
        /// 화면의 모든 단추에 누르는 소리를 붙인다. 화면이 뜰 때 루트에 한 번만 건다 —
        /// 단추마다 따로 걸면 새로 만드는 단추(선택지·장부 줄)를 빠뜨린다.
        /// </summary>
        public static void AttachClicks(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.RegisterCallback<ClickEvent>(OnAnyClick, TrickleDown.TrickleDown);
        }

        private static void OnAnyClick(ClickEvent evt)
        {
            var element = evt.target as VisualElement;

            while (element != null)
            {
                if (element is Button)
                {
                    Play(SoundEffect.Sound_Click);
                    return;
                }

                element = element.parent;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            IsQuitting = false;

            if (instance != null)
            {
                return;
            }

            var host = new GameObject(nameof(GameAudio));
            DontDestroyOnLoad(host);
            host.AddComponent<GameAudio>();
        }

        private void Awake()
        {
            instance = this;
            Music = gameObject.AddComponent<AudioSource>();
            Music.loop = true;
            Music.playOnAwake = false;
            Music.volume = 0f;
            Effects = gameObject.AddComponent<AudioSource>();
            Effects.playOnAwake = false;

            var paths = StringConstants.AUDIO_EFFECTS;
            Clips = new AudioClip[paths.Length];

            for (var i = 0; i < paths.Length; i++)
            {
                Clips[i] = Resources.Load<AudioClip>(paths[i]);

                if (Clips[i] == null)
                {
                    UnderwriteLog.Warning($"효과음을 찾지 못했습니다: {paths[i]}", context: this);
                }
            }

            SceneManager.activeSceneChanged += OnSceneChanged;
            ChooseMusic(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;

            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            IsQuitting = true;
        }

        private void Update()
        {
            // 음악을 갈아 끼울 때 뚝 끊지 않는다. 줄였다가 바꾸고 다시 키운다.
            var step = Time.unscaledDeltaTime / Constants.AUDIO_MUSIC_FADE_SECONDS;
            Music.volume = Mathf.MoveTowards(Music.volume, MusicTarget, step * Constants.AUDIO_MUSIC_VOLUME);
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            ChooseMusic(next.name);
        }

        private void ChooseMusic(string sceneName)
        {
            string path;

            switch (sceneName)
            {
                case SceneRoutes.VILLAGE:
                {
                    path = StringConstants.AUDIO_MUSIC_VILLAGE;
                    break;
                }
                case SceneRoutes.UNDERWORLD:
                case SceneRoutes.MARKET:
                {
                    path = StringConstants.AUDIO_MUSIC_UNDERGROUND;
                    break;
                }
                default:
                {
                    path = string.Empty;
                    break;
                }
            }

            if (path == MusicPath)
            {
                return;
            }

            MusicPath = path;
            StopAllCoroutines();
            StartCoroutine(SwapMusic(path));
        }

        private System.Collections.IEnumerator SwapMusic(string path)
        {
            MusicTarget = 0f;

            while (Music.volume > 0.001f && Music.isPlaying == true)
            {
                yield return null;
            }

            Music.Stop();

            if (string.IsNullOrEmpty(path) == true)
            {
                yield break;
            }

            var clip = Resources.Load<AudioClip>(path);

            if (clip == null)
            {
                UnderwriteLog.Warning($"음악을 찾지 못했습니다: {path}", context: this);
                yield break;
            }

            Music.clip = clip;
            Music.Play();
            MusicTarget = Constants.AUDIO_MUSIC_VOLUME;
        }

        private void PlayEffect(SoundEffect effect)
        {
            var index = (int)effect;

            if (index < 0 || index >= Clips.Length || Clips[index] == null)
            {
                return;
            }

            Effects.PlayOneShot(Clips[index], Constants.AUDIO_EFFECT_VOLUME);
        }
    }
}
