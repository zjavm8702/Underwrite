using System.Collections.Generic;
using System.IO;
using Underwrite.Editor.Utility;
using Underwrite.Global;
using Underwrite.Underworld;
using Underwrite.Village;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Underwrite.Editor
{
    /// <summary>
    /// 지하 방 사람들(Codex 납품 `Outputs/UnderworldPeople_v2`)을 지하 방 씬에 앉힌다(2026-10-19).
    ///
    /// 1. 프레임 PNG를 Assets/Images/Underworld/People로 들이고 발밑 기준점·크기를 맞춘다(배치표 staging.json의 display_scale).
    /// 2. 씬의 임시 투자꾼·정보상(주인공 그림에 색만 입힌 서 있는 사람)을 지우고, 배치표 자리에 앉은 사람을 세운다.
    /// 3. UnderworldRoom의 투자꾼 목록(말풍선)·몸짓 목록·정보상을 새 사람으로 잇는다.
    /// 다시 돌려도 된다 — 같은 이름의 사람을 지우고 다시 만든다.
    /// </summary>
    public static class UnderworldPeopleImporter
    {
        private const string SOURCE = "Outputs/UnderworldPeople_v2";
        private const string TARGET = "Assets/Images/Underworld/People";
        private const string SCENE = "Assets/Scenes/Underworld.unity";
        private const string LAYOUT = "Outputs/UnderworldRoom_v6/layout.json";

        // 앉은 사람의 발밑(방 그림 1배). 대리 배치표의 seat_ground에 발을 두면 엉덩이가 의자 옆 허공에 떴다(사장님 10/19) —
        // 과장이 프레임에서 엉덩이 자리를 재서 방석 가운데에 맞춘 값(Tools/art/seat_fit.py).
        private const string SEAT_FIT = "Tools/art/seat_fit.json";
        private const string PEOPLE_ROOT = "Room People";
        private const string INFORMANT = "Informant";
        private const int FRAMES = 6;

        // 정보상의 발밑 높이(방 그림 1배 y). 대리 배치표의 810은 실제 화면에서 카운터에 머리만 남았다 —
        // 745면 상반신과 장부가 카운터 위로 보이고, 카운터(발밑 약 864)보다 여전히 뒤다(과장 Play 확인 10/19).
        private const float INFORMANT_GROUND_Y = 745f;

        // 방 그림 1배 픽셀 → 월드(방 바닥판 PPU 72, 2880×1620의 가운데가 원점).
        private const float ROOM_PPU = 72f;
        private const float ROOM_HALF_WIDTH = 20f;
        private const float ROOM_HALF_HEIGHT = 11.25f;

        private static readonly string[] InvestorMotions = { "Idle", "Cheer", "Groan" };
        private static readonly string[] InformantMotions = { "Idle", "Talk", "Nod" };

        [MenuItem("Underwrite/지하 방 사람들 앉히기 (v2)")]
        public static void Import()
        {
            var staging = MiniJson.Parse(File.ReadAllText(Path.Combine(SOURCE, "staging.json"))) as Dictionary<string, object>;
            var timings = MiniJson.Parse(File.ReadAllText(Path.Combine(SOURCE, "timings.json"))) as Dictionary<string, object>;
            var settings = MiniJson.Parse(File.ReadAllText(Path.Combine(SOURCE, "sprite_settings.json"))) as Dictionary<string, object>;
            var layout = MiniJson.Parse(File.ReadAllText(LAYOUT)) as Dictionary<string, object>;
            var seatFit = File.Exists(SEAT_FIT) == true
                ? MiniJson.Parse(File.ReadAllText(SEAT_FIT)) as Dictionary<string, object>
                : new Dictionary<string, object>();
            var placements = (List<object>)staging["placements"];
            var frameMs = (Dictionary<string, object>)timings["frame_ms"];

            // 1. 그림 들이기
            foreach (var item in placements)
            {
                var place = (Dictionary<string, object>)item;
                CopyFrames(place, settings);
            }

            AssetDatabase.Refresh();

            foreach (var item in placements)
            {
                ConfigureImports((Dictionary<string, object>)item, settings);
            }

            AssetDatabase.Refresh();

            // 2. 씬에 앉히기
            var scene = EditorSceneManager.OpenScene(SCENE);
            var room = Object.FindAnyObjectByType<UnderworldRoom>();

            if (room == null)
            {
                Debug.LogError("지하 방 씬에 UnderworldRoom이 없습니다.");
                return;
            }

            var so = new SerializedObject(room);
            RemoveOld(so);

            var parent = GameObject.Find(PEOPLE_ROOT);

            if (parent != null)
            {
                Object.DestroyImmediate(parent);
            }

            parent = new GameObject(PEOPLE_ROOT);
            var investors = new List<CharacterActor>();
            var figures = new List<SeatedFigure>();
            CharacterActor informant = null;

            foreach (var item in placements)
            {
                var place = (Dictionary<string, object>)item;
                var name = (string)place["name"];
                var figure = Build(place, layout, seatFit, frameMs, parent.transform);

                if (name == INFORMANT)
                {
                    informant = figure.GetComponent<CharacterActor>();
                    continue;
                }

                investors.Add(figure.GetComponent<CharacterActor>());
                figures.Add(figure);
            }

            // 3. 방과 잇기
            SetList(so, "Investors", investors);
            SetList(so, "InvestorFigures", figures);
            so.FindProperty("Informant").objectReferenceValue = informant;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"지하 방 사람들을 앉혔습니다: 투자꾼 {investors.Count}, 정보상 {(informant == null ? 0 : 1)}");
        }

        private static string[] MotionsOf(string name)
        {
            return name == INFORMANT ? InformantMotions : InvestorMotions;
        }

        private static string FrameName(string name, string motion, string direction, int frame)
        {
            return $"{name}_{motion}_{direction}_{frame:00}.png";
        }

        private static void CopyFrames(Dictionary<string, object> place, Dictionary<string, object> settings)
        {
            var name = (string)place["name"];
            var direction = (string)place["direction"];
            var folder = Path.Combine(TARGET, name);
            Directory.CreateDirectory(folder);

            foreach (var motion in MotionsOf(name))
            {
                for (var f = 0; f < FRAMES; f++)
                {
                    var file = FrameName(name, motion, direction, f);
                    File.Copy(Path.Combine(SOURCE, name, motion, direction, file), Path.Combine(folder, file), true);
                }
            }
        }

        // 발밑 기준점(캔버스 아래쪽 원점)과, 방에서 보일 크기(display_scale: 프레임 1px이 방 그림 몇 px인가).
        private static void ConfigureImports(Dictionary<string, object> place, Dictionary<string, object> settings)
        {
            var name = (string)place["name"];
            var direction = (string)place["direction"];
            var info = (Dictionary<string, object>)settings[name];
            var canvas = (List<object>)info["canvas"];
            var pivot = (List<object>)info["pivot_bottom_left"];
            var pivotNormal = new Vector2((float)(double)pivot[0] / (float)(double)canvas[0],
                (float)(double)pivot[1] / (float)(double)canvas[1]);
            var scale = (float)(double)place["display_scale"];

            foreach (var motion in MotionsOf(name))
            {
                for (var f = 0; f < FRAMES; f++)
                {
                    var path = Path.Combine(TARGET, name, FrameName(name, motion, direction, f)).Replace('\\', '/');
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.spritePixelsPerUnit = ROOM_PPU / scale;

                    var texture = new TextureImporterSettings();
                    importer.ReadTextureSettings(texture);
                    texture.spriteAlignment = (int)SpriteAlignment.Custom;
                    texture.spritePivot = pivotNormal;
                    importer.SetTextureSettings(texture);
                    importer.SaveAndReimport();
                }
            }
        }

        // 임시 사람(서 있는 색 입힌 주인공 그림)을 지운다.
        private static void RemoveOld(SerializedObject so)
        {
            var list = so.FindProperty("Investors");

            for (var i = 0; i < list.arraySize; i++)
            {
                var actor = list.GetArrayElementAtIndex(i).objectReferenceValue as CharacterActor;

                if (actor != null)
                {
                    Object.DestroyImmediate(actor.gameObject);
                }
            }

            var informant = so.FindProperty("Informant").objectReferenceValue as CharacterActor;

            if (informant != null)
            {
                Object.DestroyImmediate(informant.gameObject);
            }
        }

        private static SeatedFigure Build(Dictionary<string, object> place, Dictionary<string, object> layout,
            Dictionary<string, object> seatFit, Dictionary<string, object> frameMs, Transform parent)
        {
            var name = (string)place["name"];
            var direction = (string)place["direction"];
            var ground = (List<object>)place["seat_ground"];

            var root = new GameObject(name);
            root.transform.SetParent(parent);
            var groundX = (float)(double)ground[0];
            var groundY = name == INFORMANT ? INFORMANT_GROUND_Y : (float)(double)ground[1];

            if (seatFit.TryGetValue(name, out var fit) == true && fit is Dictionary<string, object> fitted)
            {
                var foot = (List<object>)fitted["foot"];
                groundX = (float)(double)foot[0];
                groundY = (float)(double)foot[1];
            }

            root.transform.position = ToWorld(groundX, groundY);

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(root.transform, false);
            var body = bodyObject.AddComponent<SpriteRenderer>();

            var group = root.AddComponent<SortingGroup>();

            // 앉은 사람은 자기 테이블 바로 앞(대리 배치표: 테이블 발밑 다음, 탁자 위 소품 앞).
            // 정보상은 자기 발밑 — 카운터 뒤라 허리 아래가 가린다.
            if (place.TryGetValue("parent", out var table) == true && table is string tableId)
            {
                group.sortingOrder = YSortRenderer.OrderFor(TableFootY(layout, tableId)) + 1;
            }
            else
            {
                root.AddComponent<YSortRenderer>().Configure(0f, true);
            }

            var motions = MotionsOf(name);
            var clips = new StationClip[motions.Length];

            for (var m = 0; m < motions.Length; m++)
            {
                clips[m] = Clip(name, motions[m], direction, (List<object>)frameMs[motions[m]]);
            }

            body.sprite = clips[0].Frames.Count > 0 ? clips[0].Frames[0] : null;
            var figure = root.AddComponent<SeatedFigure>();
            figure.Configure(body, clips[0], clips[1], clips[2]);
            root.AddComponent<CharacterActor>();
            return figure;
        }

        private static StationClip Clip(string name, string motion, string direction, List<object> milliseconds)
        {
            var clip = new StationClip();

            for (var f = 0; f < FRAMES; f++)
            {
                var path = Path.Combine(TARGET, name, FrameName(name, motion, direction, f)).Replace('\\', '/');
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (sprite != null)
                {
                    clip.Frames.Add(sprite);
                    clip.Milliseconds.Add(f < milliseconds.Count ? (int)(double)milliseconds[f] : 150);
                }
            }

            return clip;
        }

        private static float TableFootY(Dictionary<string, object> layout, string tableId)
        {
            foreach (var item in (List<object>)layout["parts"])
            {
                var part = (Dictionary<string, object>)item;

                if ((string)part["id"] == tableId)
                {
                    var foot = (List<object>)part["foot"];
                    return ToWorld((float)(double)foot[0], (float)(double)foot[1]).y;
                }
            }

            return 0f;
        }

        private static Vector3 ToWorld(float x, float y)
        {
            return new Vector3(x / ROOM_PPU - ROOM_HALF_WIDTH, ROOM_HALF_HEIGHT - y / ROOM_PPU, 0f);
        }

        private static void SetList<T>(SerializedObject so, string property, List<T> items) where T : Object
        {
            var list = so.FindProperty(property);
            list.arraySize = items.Count;

            for (var i = 0; i < items.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }
    }
}
