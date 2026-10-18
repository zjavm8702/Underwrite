using System;
using System.Collections.Generic;
using System.IO;
using Underwrite.Utility;
using UnityEngine;

namespace Underwrite.Editor
{
    /// <summary>
    /// 영지 배치 덮어쓰기(2026-10-18 — 사장님이 직접 배치할 툴).
    ///
    /// Codex 납품 배치표(`Outputs/VillagePartsFull_vN/layout.json`)는 건드리지 않는다. 대신 사장님이 씬에서 옮기고
    /// 복제하고 지운 결과를 「남길 사물 목록 = 원본 순번 + 새 발밑 좌표」로 이 파일에 적는다.
    /// 영지 씬을 다시 깔 때 원본을 읽은 뒤 이 목록대로 사물을 다시 만든다. 문·불빛·연기·충돌 좌표는 발밑이 움직인 만큼 함께 옮긴다.
    /// 원본 납품 판(Base)이 바뀌면 순번이 어긋나므로 쓰지 않고 경고한다.
    /// </summary>
    [Serializable]
    public sealed class VillageLayoutOverride
    {
        /// <summary>프로젝트 루트 기준 경로. 과장 소유 폴더(Tools)에 둔다.</summary>
        public const string PATH = "Tools/layouts/village_layout_override.json";

        /// <summary>이 덮어쓰기를 만든 원본 납품 폴더 이름(예: VillagePartsFull_v2).</summary>
        public string Base;

        public List<Entry> Parts = new List<Entry>();

        [Serializable]
        public sealed class Entry
        {
            /// <summary>원본 배치표 parts의 순번. 같은 순번이 여러 번 나오면 복제한 사물이다.</summary>
            public int Source;

            /// <summary>원본과 맞춰 보는 그림 파일 이름.</summary>
            public string File;

            /// <summary>새 발밑 좌표(지도 1배 픽셀, 왼쪽 위 원점).</summary>
            public float X;
            public float Y;
        }

        public static bool Exists
        {
            get
            {
                return File.Exists(PATH);
            }
        }

        /// <summary>덮어쓰기 파일을 읽는다. 없거나 읽지 못하면 null.</summary>
        public static VillageLayoutOverride Load()
        {
            if (Exists == false)
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<VillageLayoutOverride>(File.ReadAllText(PATH));
            }
            catch (Exception e)
            {
                UnderwriteLog.Error($"영지 배치 덮어쓰기를 읽지 못했습니다: {e.Message}");
                return null;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PATH));
            File.WriteAllText(PATH, JsonUtility.ToJson(this, true));
        }

        public static void Delete()
        {
            if (Exists == true)
            {
                File.Delete(PATH);
            }
        }
    }
}
