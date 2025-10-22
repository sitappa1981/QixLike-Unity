using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace QixLike
{
    public class CaptureSystem : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private Tilemap overlayTilemap; // 黒い全面
        [SerializeField] private Tilemap wallTilemap;    // 壁（確保セル）
        [SerializeField] private Tilemap trailTilemap;   // 描画中の線

        [Header("Tiles")]
        [SerializeField] private TileBase wallTile;      // Tile_Solid を割り当て

        public float FillRatio { get; private set; }     // 確保率(0..1)

        static readonly Vector2Int[] Neigh4 = {
      new Vector2Int(1,0), new Vector2Int(-1,0),
      new Vector2Int(0,1), new Vector2Int(0,-1)
    };

        public void ApplyCapture(List<Vector2Int> trailCells)
        {
            if (trailCells == null || trailCells.Count == 0) return;

            int W = GameConsts.GridW;
            int H = GameConsts.GridH;

            // 1) solidマップ作成（壁＋トレイルを壁扱い）
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

            // 2) 非solidの連結成分をすべてラベリング
            //    → 最も大きい成分=外側、それ以外を捕獲
            int[,] comp = new int[W, H]; // -1: 未割当 / 0..k: 成分ID
            for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) comp[x, y] = -1;

            List<int> compSizes = new List<int>();
            Queue<Vector2Int> q = new Queue<Vector2Int>();
            int compId = 0;

            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    if (solid[x, y] || comp[x, y] != -1) continue;

                    // 新しい成分をBFS
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

            // 成分が無い（全面solid）ならそのままリセット処理だけ
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

            // 3) 最大成分（=外側）を選び、それ以外の成分を捕獲
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
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile); // 壁化
                        overlayTilemap.SetTile(new Vector3Int(x, y, 0), null);   // 黒を剥がす
                        captured++;
                    }
                }
            }

            // 4) トレイルを壁へ昇格しつつTrailを消す
            foreach (var c in trailCells)
            {
                if (!InBounds(c, W, H)) continue;
                wallTilemap.SetTile(new Vector3Int(c.x, c.y, 0), wallTile);
                trailTilemap.SetTile(new Vector3Int(c.x, c.y, 0), null);
            }
            trailCells.Clear();

            // 5) 確保率を更新
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
