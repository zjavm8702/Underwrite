using System.IO;
using Underwrite.Underworld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Underwrite.Editor
{
    /// <summary>
    /// 지하 방 테이블을 사람이 앉은 그림으로 바꾼다(2026-10-19, Codex `Outputs/UnderworldTablesSeated_v1`).
    ///
    /// 사장님 판정 「그림도 문제」 — 따로 그린 투자꾼을 의자에 얹은 티가 났다. 테이블·의자·사람을 한 장으로 그린 그림으로 바꾸고,
    /// 따로 세웠던 투자꾼은 그림만 숨긴다(자리는 남겨 혼잣말 말풍선이 그 위에 뜬다).
    /// 새 그림은 원래 테이블과 같은 캔버스·피봇이라 가져오기 설정을 그대로 베낀다.
    /// </summary>
    public static class UnderworldSeatedTables
    {
        private const string SOURCE = "Outputs/UnderworldTablesSeated_v1";
        private const string ROOM = "Assets/Images/Underworld/Room";
        private const string SCENE = "Assets/Scenes/Underworld.unity";
        private const string PEOPLE_ROOT = "Room People";
        private const string INFORMANT = "Informant";
        private static readonly string[] Tables = { "table_round_a", "table_round_b", "table_round_c", "table_round_d" };

        [MenuItem("Underwrite/지하 방 앉은 테이블로 바꾸기")]
        public static void Apply()
        {
            foreach (var table in Tables)
            {
                File.Copy(Path.Combine(SOURCE, table + "_seated.png"), Path.Combine(ROOM, table + "_seated.png"), true);
            }

            AssetDatabase.Refresh();

            foreach (var table in Tables)
            {
                CopyImport(ROOM + "/" + table + ".png", ROOM + "/" + table + "_seated.png");
            }

            var scene = EditorSceneManager.OpenScene(SCENE);
            var swapped = 0;

            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (renderer.sprite == null)
                {
                    continue;
                }

                foreach (var table in Tables)
                {
                    if (renderer.sprite.name == table)
                    {
                        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ROOM + "/" + table + "_seated.png");
                        EditorUtility.SetDirty(renderer);
                        swapped++;
                    }
                }
            }

            // 따로 세운 투자꾼은 그림만 숨긴다. 혼잣말(CharacterActor)은 그 자리에서 뜬다.
            var people = GameObject.Find(PEOPLE_ROOT);

            if (people != null)
            {
                foreach (Transform person in people.transform)
                {
                    if (person.name == INFORMANT)
                    {
                        continue;
                    }

                    person.gameObject.SetActive(true);

                    foreach (var body in person.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        body.enabled = false;
                        EditorUtility.SetDirty(body);
                    }

                    var figure = person.GetComponent<SeatedFigure>();

                    if (figure != null)
                    {
                        figure.enabled = false;
                        EditorUtility.SetDirty(figure);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"지하 방 테이블을 앉은 그림으로 바꿨습니다: {swapped}개");
        }

        private static void CopyImport(string fromPath, string toPath)
        {
            var from = (TextureImporter)AssetImporter.GetAtPath(fromPath);
            var to = (TextureImporter)AssetImporter.GetAtPath(toPath);
            var settings = new TextureImporterSettings();
            from.ReadTextureSettings(settings);
            to.SetTextureSettings(settings);
            to.spritePixelsPerUnit = from.spritePixelsPerUnit;
            to.textureCompression = from.textureCompression;
            to.mipmapEnabled = from.mipmapEnabled;
            to.SaveAndReimport();
        }
    }
}
