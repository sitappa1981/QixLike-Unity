using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace QixLike
{
    public class CaptureSystem : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private Tilemap overlayTilemap; // �����S��
        [SerializeField] private Tilemap wallTilemap;    // �ǁi�m�ۃZ���j
        [SerializeField] private Tilemap trailTilemap;   // �`�撆�̐�

        [Header("Tiles")]
        [SerializeField] private TileBase wallTile;      // Tile_Solid �����蓖��

        public float FillRatio { get; private set; }     // �m�ۗ�(0..1)

        static readonly Vector2Int[] Neigh4 = {
      new Vector2Int(1,0), new Vector2Int(-1,0),
      new Vector2Int(0,1), new Vector2Int(0,-1)
    };

        public void ApplyCapture(List<Vector2Int> trailCells)
        {
            if (trailCells == null || trailCells.Count == 0) return;

            int W = GameConsts.GridW;
            int H = GameConsts.GridH;

            // 1) solid�}�b�v�쐬�i�ǁ{�g���C����ǈ����j
            bool[,] solid = new bool[W, H];
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    if (wallTilemap.HasTile(new Vector3Int(x, y, 0))) solid[x, y] = true;
                }
            }
            foreach (var c in trailCells)
            {
                if (InBounds(c, W, H)) solid[c.x, c.y] = true;
            }

            // 2) ��solid�̘A�����������ׂă��x�����O
            //    �� �ł��傫������=�O���A����ȊO��ߊl
            int[,] comp = new int[W, H]; // -1: ������ / 0..k: ����ID
            for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) comp[x, y] = -1;

            List<int> compSizes = new List<int>();
            Queue<Vector2Int> q = new Queue<Vector2Int>();
            int compId = 0;

            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    if (solid[x, y] || comp[x, y] != -1) continue;

                    // �V����������BFS
                    int size = 0;
                    comp[x, y] = compId; q.Enqueue(new Vector2Int(x, y));
                    while (q.Count > 0)
                    {
                        var p = q.Dequeue(); size++;
                        for (int i = 0; i < 4; i++)
                        {
                            var n = p + Neigh4[i];
                            if (n.x < 0 || n.x >= W || n.y < 0 || n.y >= H) continue;
                            if (solid[n.x, n.y] || comp[n.x, n.y] != -1) continue;
                            comp[n.x, n.y] = compId; q.Enqueue(n);
                        }
                    }
                    compSizes.Add(size);
                    compId++;
                }
            }

            // �����������i�S��solid�j�Ȃ炻�̂܂܃��Z�b�g��������
            if (compId == 0)
            {
                foreach (var c in trailCells)
                {
                    if (!InBounds(c, W, H)) continue;
                    wallTilemap.SetTile(new Vector3Int(c.x, c.y, 0), wallTile);
                    trailTilemap.SetTile(new Vector3Int(c.x, c.y, 0), null);
                }
                trailCells.Clear();
                UpdateFillRatio();
#if UNITY_EDITOR
                Debug.Log("Capture applied: no empty components.");
#endif
                return;
            }

            // 3) �ő听���i=�O���j��I�сA����ȊO�̐�����ߊl
            int keepId = 0;
            int keepSize = compSizes[0];
            for (int i = 1; i < compSizes.Count; i++)
            {
                if (compSizes[i] > keepSize) { keepSize = compSizes[i]; keepId = i; }
            }

            int captured = 0;
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    int id = comp[x, y];
                    if (id != -1 && id != keepId)
                    {
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile); // �ǉ�
                        overlayTilemap.SetTile(new Vector3Int(x, y, 0), null);   // ���𔍂���
                        captured++;
                    }
                }
            }

            // 4) �g���C����ǂ֏��i����Trail������
            foreach (var c in trailCells)
            {
                if (!InBounds(c, W, H)) continue;
                wallTilemap.SetTile(new Vector3Int(c.x, c.y, 0), wallTile);
                trailTilemap.SetTile(new Vector3Int(c.x, c.y, 0), null);
            }
            trailCells.Clear();

            // 5) �m�ۗ����X�V
            UpdateFillRatio();

#if UNITY_EDITOR
            Debug.Log($"Capture applied: comps={compId}, keep={keepId}({keepSize}), captured={captured}, fill={(FillRatio * 100f):F1}%");
#endif
        }



        void UpdateFillRatio()
        {
            int W = GameConsts.GridW, H = GameConsts.GridH;
            int interior = (W - 2) * (H - 2);
            int overlayRemain = 0;
            for (int x = 1; x < W - 1; x++)
            {
                for (int y = 1; y < H - 1; y++)
                {
                    if (overlayTilemap.HasTile(new Vector3Int(x, y, 0))) overlayRemain++;
                }
            }
            FillRatio = 1f - (overlayRemain / (float)interior);
        }


        static bool InBounds(Vector2Int c, int W, int H)
          => c.x >= 0 && c.x < W && c.y >= 0 && c.y < H;
    }
}
