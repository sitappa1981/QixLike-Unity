using UnityEngine;

namespace QixLike
{
    public static class PlayerShatterEffect
    {
        // tilesX/Y: 分割数。4x4 なら 16ピース
        public static void Play(SpriteRenderer src, int tilesX = 4, int tilesY = 4,
                                float pieceSpeed = 3f, float radial = 1.5f,
                                float gravity = 15f, float life = 1.2f)
        {
            if (src == null || src.sprite == null) return;

            var tex = src.sprite.texture;
            var rect = src.sprite.textureRect;     // 元スプライトの矩形（テクスチャ上）
            float ppu = src.sprite.pixelsPerUnit;

            // プレイヤー位置（ワールド）
            Vector3 origin = src.transform.position;

            // ピボット（rect内のピクセル座標）。これを基準にローカル位置を出す
            Vector2 pivotPx = src.sprite.pivot;

            float tileW = rect.width / tilesX;
            float tileH = rect.height / tilesY;

            for (int iy = 0; iy < tilesY; iy++)
            {
                for (int ix = 0; ix < tilesX; ix++)
                {
                    var sub = new Rect(rect.x + ix * tileW, rect.y + iy * tileH, tileW, tileH);

                    // 分割スプライト作成（元テクスチャを共有）
                    var pieceSprite = Sprite.Create(tex, sub, new Vector2(0.5f, 0.5f), ppu);

                    // ピースのローカルオフセット（ワールド単位）
                    float localX = ((sub.x + sub.width * 0.5f) - (rect.x + pivotPx.x)) / ppu;
                    float localY = ((sub.y + sub.height * 0.5f) - (rect.y + pivotPx.y)) / ppu;
                    Vector3 piecePos = origin + new Vector3(localX, localY, 0f);

                    var go = new GameObject("ShatterPiece", typeof(SpriteRenderer), typeof(SpriteShatterChunk));
                    var psr = go.GetComponent<SpriteRenderer>();
                    psr.sprite = pieceSprite;
                    psr.sortingLayerID = src.sortingLayerID;
                    psr.sortingOrder = src.sortingOrder + 1; // 元より前に
                    psr.material = src.sharedMaterial;   // 同じ見た目に
                    psr.color = src.color;

                    go.transform.position = piecePos;

                    // ランダム初速（ちょっと外側＋少し上向き → 重力で落ちる）
                    Vector2 rand = Random.insideUnitCircle * radial;
                    Vector2 v0 = new Vector2(rand.x, Mathf.Abs(rand.y) + 0.5f) * pieceSpeed;

                    var chunk = go.GetComponent<SpriteShatterChunk>();
                    chunk.Init(v0, gravity, life, Random.Range(-360f, 360f));
                }
            }
        }
    }
}
