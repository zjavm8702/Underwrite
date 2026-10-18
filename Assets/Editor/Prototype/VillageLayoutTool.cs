using System.Collections.Generic;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Underwrite.Editor
{
    /// <summary>
    /// 영지 배치 툴(2026-10-18 사장님 요청 — 「내가 배치할 수 있는 툴」).
    ///
    /// 쓰는 법: 영지 씬을 열고 Scene 창에서 사물을 그대로 끌어 옮긴다. 복제는 Ctrl+D, 지우기는 Delete.
    /// 새 사물은 이 창의 목록에서 누른다. 다 됐으면 「저장하고 다시 깔기」 — 덮어쓰기 파일에 적고 영지 씬을 다시 만든다.
    /// 문 앞 칸·일하는 사람·불빛·연기는 다시 깔 때 옮긴 자리에 맞춰 따라온다.
    /// Codex 원본 배치표는 건드리지 않는다(<see cref="VillageLayoutOverride"/>).
    /// </summary>
    public sealed class VillageLayoutTool : EditorWindow
    {
        private const string VILLAGE_SCENE = "Assets/Scenes/Village.unity";
        private const string PARTS_ROOT = "Parts";
        private const string GROUND = "Ground";
        private const float LABEL_OFFSET = 0.6f;

        // 장소 이름표. 배치표의 place 값 → 화면에 띄울 이름.
        private static readonly string[] PLACE_KEYS = { "inn", "smith", "guild", "manor", "board", "well", "stall" };
        private static readonly string[] PLACE_LABELS = { "여관", "대장간", "길드", "영주관", "게시판", "우물", "노점" };

        private bool Snap = true;
        private float SnapPixels = 12f;
        private bool ShowLabels = true;
        private Vector2 Scroll;

        [MenuItem("Underwrite/영지 배치 툴")]
        public static void Open()
        {
            GetWindow<VillageLayoutTool>("영지 배치");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Scene 창에서 사물을 끌어 옮깁니다. 복제 Ctrl+D, 지우기 Delete.\n"
                + "새 사물은 아래 목록에서 누르면 Scene 창 가운데에 생깁니다.\n"
                + "문 앞 칸·일하는 사람·불빛은 「저장하고 다시 깔기」를 눌러야 옮긴 자리로 따라옵니다.",
                MessageType.Info);

            var source = VillagePartsSetup.FullSourceName();
            EditorGUILayout.LabelField("원본 납품", source ?? "(없음)");
            EditorGUILayout.LabelField("덮어쓰기", VillageLayoutOverride.Exists == true ? VillageLayoutOverride.PATH : "없음 — 원본 배치");

            if (IsVillageOpen() == false)
            {
                EditorGUILayout.Space();

                if (GUILayout.Button("영지 씬 열기", GUILayout.Height(32f)) == true)
                {
                    OpenVillage();
                }

                return;
            }

            var tags = FindTags();
            EditorGUILayout.LabelField("사물 수", tags.Count.ToString());

            EditorGUILayout.Space();
            Snap = EditorGUILayout.Toggle("격자에 붙이기", Snap);
            SnapPixels = Mathf.Max(1f, EditorGUILayout.FloatField("격자 간격(지도 픽셀)", SnapPixels));
            ShowLabels = EditorGUILayout.Toggle("장소 이름표 보기", ShowLabels);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("저장만", GUILayout.Height(32f)) == true)
                {
                    Save(tags, source);
                }

                if (GUILayout.Button("저장하고 다시 깔기", GUILayout.Height(32f)) == true)
                {
                    if (Save(tags, source) == true)
                    {
                        Rebuild();
                    }
                }
            }

            if (VillageLayoutOverride.Exists == true && GUILayout.Button("원래 배치로 되돌리기") == true)
            {
                if (EditorUtility.DisplayDialog("영지 배치", "덮어쓰기를 지우고 Codex 원본 배치로 다시 깝니다.", "되돌리기", "취소") == true)
                {
                    VillageLayoutOverride.Delete();
                    Rebuild();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("사물 목록 — 누르면 하나 더 놓습니다", EditorStyles.boldLabel);
            Scroll = EditorGUILayout.BeginScrollView(Scroll);

            foreach (var pair in CountByFile(tags))
            {
                if (GUILayout.Button($"{pair.Key}  ×{pair.Value.Count}") == true)
                {
                    AddCopy(pair.Value[0]);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // 옮기는 동안: 격자에 붙이고, 앞뒤 순서를 발밑 높이로 바로 맞춘다(실행 중에는 YSortRenderer가 하는 일).
        private void OnEditorUpdate()
        {
            if (Application.isPlaying == true || IsVillageOpen() == false)
            {
                return;
            }

            var unit = SnapPixels / VillagePartsSetup.PIXELS_PER_UNIT;

            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<VillageLayoutTag>() == null)
                {
                    continue;
                }

                var position = go.transform.position;

                if (Snap == true)
                {
                    var snapped = new Vector3(Mathf.Round(position.x / unit) * unit, Mathf.Round(position.y / unit) * unit, 0f);

                    if (snapped != position)
                    {
                        Undo.RecordObject(go.transform, "격자에 붙이기");
                        go.transform.position = snapped;
                        position = snapped;
                    }
                }

                var renderer = go.GetComponent<SpriteRenderer>();
                var order = YSortRenderer.OrderFor(position.y);

                if (renderer != null && renderer.sortingOrder != order)
                {
                    renderer.sortingOrder = order;
                }
            }
        }

        private void OnSceneGUI(SceneView view)
        {
            if (ShowLabels == false || Application.isPlaying == true || IsVillageOpen() == false)
            {
                return;
            }

            var style = new GUIStyle(EditorStyles.whiteLargeLabel);
            style.alignment = TextAnchor.MiddleCenter;

            foreach (var tag in FindTags())
            {
                var index = System.Array.IndexOf(PLACE_KEYS, tag.PlaceName);

                if (index < 0)
                {
                    continue;
                }

                Handles.Label(tag.transform.position + Vector3.up * LABEL_OFFSET, PLACE_LABELS[index], style);
            }
        }

        private static bool IsVillageOpen()
        {
            return EditorSceneManager.GetActiveScene().path == VILLAGE_SCENE;
        }

        private static void OpenVillage()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == true)
            {
                EditorSceneManager.OpenScene(VILLAGE_SCENE);
            }
        }

        private static List<VillageLayoutTag> FindTags()
        {
            var result = new List<VillageLayoutTag>();
            var root = GameObject.Find(PARTS_ROOT);

            if (root == null)
            {
                return result;
            }

            root.GetComponentsInChildren(false, result);
            return result;
        }

        // 그림 파일마다 놓인 사물들. 파일 이름순.
        private static SortedDictionary<string, List<VillageLayoutTag>> CountByFile(List<VillageLayoutTag> tags)
        {
            var result = new SortedDictionary<string, List<VillageLayoutTag>>();

            foreach (var tag in tags)
            {
                List<VillageLayoutTag> list;

                if (result.TryGetValue(tag.FileName, out list) == false)
                {
                    list = new List<VillageLayoutTag>();
                    result.Add(tag.FileName, list);
                }

                list.Add(tag);
            }

            return result;
        }

        // 같은 그림의 사물을 하나 복제해 Scene 창 가운데에 놓는다.
        private static void AddCopy(VillageLayoutTag template)
        {
            var copy = Instantiate(template.gameObject, template.transform.parent);
            copy.name = template.gameObject.name;
            var view = SceneView.lastActiveSceneView;
            var at = view != null ? view.pivot : template.transform.position;
            copy.transform.position = new Vector3(at.x, at.y, 0f);
            Undo.RegisterCreatedObjectUndo(copy, "사물 놓기");
            Selection.activeGameObject = copy;
        }

        private static bool Save(List<VillageLayoutTag> tags, string source)
        {
            if (source == null)
            {
                UnderwriteLog.Error("원본 납품(Outputs/VillagePartsFull_vN)이 없어 저장하지 않습니다.");
                return false;
            }

            var ground = GameObject.Find(GROUND);
            var renderer = ground == null ? null : ground.GetComponent<SpriteRenderer>();

            if (renderer == null || renderer.sprite == null)
            {
                UnderwriteLog.Error("영지 바닥(Ground)을 찾지 못해 저장하지 않습니다.");
                return false;
            }

            var groundSize = new Vector2(renderer.sprite.texture.width, renderer.sprite.texture.height);
            var layout = new VillageLayoutOverride { Base = source };

            foreach (var tag in tags)
            {
                var pixel = VillagePartsSetup.ToPixel(tag.transform.position, groundSize);
                layout.Parts.Add(new VillageLayoutOverride.Entry
                {
                    Source = tag.SourceIndex,
                    File = tag.FileName,
                    X = Mathf.Round(pixel.x),
                    Y = Mathf.Round(pixel.y)
                });
            }

            layout.Save();
            AssetDatabase.Refresh();
            UnderwriteLog.Info($"영지 배치를 저장했습니다: 사물 {layout.Parts.Count}개 → {VillageLayoutOverride.PATH}");
            return true;
        }

        private static void Rebuild()
        {
            VillagePartsSetup.BuildFull();
        }
    }
}
