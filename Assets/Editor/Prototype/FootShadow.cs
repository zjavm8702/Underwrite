using UnityEditor;
using UnityEngine;

namespace Underwrite.Editor
{
    /// <summary>
    /// 발밑 접지 그림자(2026-10-18 — 외곽선 대신 사람을 바닥에 세운다). 주인공 프리팹의 그림자와 같은 그림·재질.
    /// 발이 피벗인 사람(일하는 사람·용병단 단원)에게 붙인다. 주인공은 프리팹에 이미 있다.
    /// </summary>
    public static class FootShadow
    {
        private const string SPRITE = "Assets/UI/Images/character_shadow.png";
        private const string MATERIAL = "Assets/Materials/CharacterShadow.mat";

        // 주인공 그림자와 같은 크기(사람 축척 0.27 아래의 지역 크기).
        private static readonly Vector3 SCALE = new Vector3(1.5f, 1.15f, 1f);
        private static readonly Color TINT = new Color(1f, 1f, 1f, 0.95f);

        public static void Add(Transform person)
        {
            var go = new GameObject("Shadow");
            go.transform.SetParent(person, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = SCALE;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL);
            renderer.color = TINT;
            renderer.sortingOrder = -1;
        }
    }
}
