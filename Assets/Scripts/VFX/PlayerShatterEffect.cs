using UnityEngine;

namespace QixLike
{
    public static class PlayerShatterEffect
    {
        // tilesX/Y: �������B4x4 �Ȃ� 16�s�[�X
        public static void Play(SpriteRenderer src, int tilesX = 4, int tilesY = 4,
                                float pieceSpeed = 3f, float radial = 1.5f,
                                float gravity = 15f, float life = 1.2f)
        {
            if (src == null || src.sprite == null) return;

            var tex = src.sprite.texture;
            var rect = src.sprite.textureRect;     // ���X�v���C�g�̋�`�i�e�N�X�`����j
            float ppu = src.sprite.pixelsPerUnit;

            // �v���C���[�ʒu�i���[���h�j
            Vector3 origin = src.transform.position;

            // �s�{�b�g�irect���̃s�N�Z�����W�j�B�������Ƀ��[�J���ʒu���o��
            Vector2 pivotPx = src.sprite.pivot;

            float tileW = rect.width / tilesX;
            float tileH = rect.height / tilesY;

            for (int iy = 0; iy < tilesY; iy++)
            {
                for (int ix = 0; ix < tilesX; ix++)
                {
                    var sub = new Rect(rect.x + ix * tileW, rect.y + iy * tileH, tileW, tileH);

                    // �����X�v���C�g�쐬�i���e�N�X�`�������L�j
                    var pieceSprite = Sprite.Create(tex, sub, new Vector2(0.5f, 0.5f), ppu);

                    // �s�[�X�̃��[�J���I�t�Z�b�g�i���[���h�P�ʁj
                    float localX = ((sub.x + sub.width * 0.5f) - (rect.x + pivotPx.x)) / ppu;
                    float localY = ((sub.y + sub.height * 0.5f) - (rect.y + pivotPx.y)) / ppu;
                    Vector3 piecePos = origin + new Vector3(localX, localY, 0f);

                    var go = new GameObject("ShatterPiece", typeof(SpriteRenderer), typeof(SpriteShatterChunk));
                    var psr = go.GetComponent<SpriteRenderer>();
                    psr.sprite = pieceSprite;
                    psr.sortingLayerID = src.sortingLayerID;
                    psr.sortingOrder = src.sortingOrder + 1; // �����O��
                    psr.material = src.sharedMaterial;   // ���������ڂ�
                    psr.color = src.color;

                    go.transform.position = piecePos;

                    // �����_�������i������ƊO���{��������� �� �d�͂ŗ�����j
                    Vector2 rand = Random.insideUnitCircle * radial;
                    Vector2 v0 = new Vector2(rand.x, Mathf.Abs(rand.y) + 0.5f) * pieceSpeed;

                    var chunk = go.GetComponent<SpriteShatterChunk>();
                    chunk.Init(v0, gravity, life, Random.Range(-360f, 360f));
                }
            }
        }
    }
}
