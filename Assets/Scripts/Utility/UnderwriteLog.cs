using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Underwrite.Utility
{
    public enum UnderwriteLogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 에디터에서는 모든 로그, 플레이어에서는 UNDERWRITE_LOGS 심볼과 includeInBuild가 모두 켜진 로그를 출력합니다.
    /// 심볼 없는 플레이어에서는 호출과 인자 평가가 생략되므로 인자에 필수 동작을 넣지 마세요.
    /// 메시지는 한국어로 씁니다. 사용자에게 보여줄 문자열은 로그가 아니라 StringConstants의 상수를 사용하세요.
    /// </summary>
    public static class UnderwriteLog
    {
        public static bool Enabled { get; set; } = true;
        public static UnderwriteLogLevel MinimumLevel { get; set; } = UnderwriteLogLevel.Info;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSettings()
        {
            Enabled = true;
            MinimumLevel = UnderwriteLogLevel.Info;
        }

        /// <summary>
        /// 무거운 문자열을 만들기 전에 이 로그가 실제로 출력될지 먼저 확인할 때 사용합니다.
        /// </summary>
        public static bool IsEnabled(bool includeInBuild = false, UnderwriteLogLevel level = UnderwriteLogLevel.Info)
        {
#if UNITY_EDITOR
            return Enabled && level >= MinimumLevel;
#elif UNDERWRITE_LOGS
            return includeInBuild && Enabled && level >= MinimumLevel;
#else
            return false;
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("UNDERWRITE_LOGS")]
        [HideInCallstack]
        public static void Info(string message, bool includeInBuild = false,
            UnityEngine.Object context = null, [CallerMemberName] string member = "",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Write(UnderwriteLogLevel.Info, message, includeInBuild, context, member, file, line);
        }

        [Conditional("UNITY_EDITOR"), Conditional("UNDERWRITE_LOGS")]
        [HideInCallstack]
        public static void Warning(string message, bool includeInBuild = false,
            UnityEngine.Object context = null, [CallerMemberName] string member = "",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Write(UnderwriteLogLevel.Warning, message, includeInBuild, context, member, file, line);
        }

        [Conditional("UNITY_EDITOR"), Conditional("UNDERWRITE_LOGS")]
        [HideInCallstack]
        public static void Error(string message, bool includeInBuild = false,
            UnityEngine.Object context = null, [CallerMemberName] string member = "",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Write(UnderwriteLogLevel.Error, message, includeInBuild, context, member, file, line);
        }

        [HideInCallstack]
        private static void Write(UnderwriteLogLevel level, string message, bool includeInBuild,
            UnityEngine.Object context, string member, string file, int line)
        {
            if (IsEnabled(includeInBuild, level) == false)
            {
                return;
            }

            var text = $"[Underwrite][{level}] {message} ({Path.GetFileName(file)}:{line} {member})";

            switch (level)
            {
                case UnderwriteLogLevel.Warning:
                {
                    UnityEngine.Debug.LogWarning(text, context);
                    break;
                }
                case UnderwriteLogLevel.Error:
                {
                    UnityEngine.Debug.LogError(text, context);
                    break;
                }
                default:
                {
                    UnityEngine.Debug.Log(text, context);
                    break;
                }
            }
        }
    }
}
