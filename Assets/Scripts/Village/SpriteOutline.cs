using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 캐릭터 외곽선(2026-10-17 사용자 결정 — 카툰 셰이더 중 외곽선).
    ///
    /// 그림이 몸에 딱 맞게 잘려 있어(여백 없음) 그림을 부풀리는 셰이더로는 바깥이 잘린다.
    /// 그래서 몸 그림의 실루엣을 8방향으로 조금씩 밀어 몸 뒤에 깐다. 그림 원본과 설정은 건드리지 않는다.
    /// 걷기·대기 프레임이 바뀌면 따라 바꾼다. 조명에 반응하지 않는 짙은 갈색 선이다.
    /// </summary>
    public sealed class SpriteOutline : MonoBehaviour
    {
        private const string BODY_NAME = "Body";
        private const string MATERIAL_RESOURCE = "SpriteSilhouette";
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");

        private static readonly Vector2[] DIRECTIONS =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f),
            new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f)
        };

        private static Material Silhouette;

        private SpriteRenderer Body;
        private SpriteRenderer[] Copies;
        private Sprite LastSprite;
        private bool LastFlipX;

        private void Awake()
        {
            Body = FindBody();

            if (Body == null)
            {
                enabled = false;
                return;
            }

            if (Silhouette == null)
            {
                // 불러온 머티리얼을 그대로 고치면 에디터에서 자산이 바뀐다. 복사본에 색을 넣는다.
                var source = Resources.Load<Material>(MATERIAL_RESOURCE);

                if (source != null)
                {
                    Silhouette = new Material(source);
                    Silhouette.SetColor(FillColorId, Constants.OUTLINE_COLOR);
                }
            }

            if (Silhouette == null)
            {
                enabled = false;
                return;
            }

            // 몸보다 한 칸 아래, 그림자보다 위. 같은 SortingGroup 안이라 다른 사람과 섞이지 않는다.
            var order = Body.sortingOrder;
            Body.sortingOrder = order + 1;

            var scale = Mathf.Max(0.0001f, Mathf.Abs(Body.transform.lossyScale.x));
            Copies = new SpriteRenderer[DIRECTIONS.Length];

            for (var i = 0; i < DIRECTIONS.Length; i++)
            {
                var go = new GameObject("Outline");
                go.transform.SetParent(Body.transform, false);
                go.transform.localPosition = (Vector3)(DIRECTIONS[i] * Constants.OUTLINE_WIDTH_UNITS / scale);

                var copy = go.AddComponent<SpriteRenderer>();
                copy.sharedMaterial = Silhouette;
                copy.sortingLayerID = Body.sortingLayerID;
                copy.sortingOrder = order;
                Copies[i] = copy;
            }

            Sync();
        }

        private void LateUpdate()
        {
            if (Body.sprite != LastSprite || Body.flipX != LastFlipX)
            {
                Sync();
            }

            var visible = Body.enabled == true;

            if (Copies[0].enabled != visible)
            {
                for (var i = 0; i < Copies.Length; i++)
                {
                    Copies[i].enabled = visible;
                }
            }
        }

        private void Sync()
        {
            LastSprite = Body.sprite;
            LastFlipX = Body.flipX;

            for (var i = 0; i < Copies.Length; i++)
            {
                Copies[i].sprite = Body.sprite;
                Copies[i].flipX = Body.flipX;
                Copies[i].flipY = Body.flipY;
            }
        }

        private SpriteRenderer FindBody()
        {
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject.name == BODY_NAME)
                {
                    return renderer;
                }
            }

            return null;
        }
    }
}
