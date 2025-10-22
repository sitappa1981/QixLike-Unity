using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

namespace QixLike
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private Grid gridRoot;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap trailTilemap;
        [SerializeField] private TileBase trailTile;
        [SerializeField] private CaptureSystem captureSystem;
        [SerializeField] private TrailBlinker trailBlinker;

        [Header("Move (cells/step)")]
        [Tooltip("1マス進むまでの基準間隔（秒）")]
        [SerializeField] private float stepInterval = 0.08f;

        [Header("Drawing Slowdown")]
        [Tooltip("線を描いている間の速度倍率（1=等速, 0.5=半速）")]
        [SerializeField, Range(0.5f, 1f)] private float drawSpeedMultiplier = 0.7f;

        // 状態
        private Vector2Int cell;                 // 現在セル
        private float stepTimer;                 // 次の1マス移動までのタイマー
        private bool isDrawing;                  // 線を描いている最中か
        private readonly List<Vector2Int> trailCells = new List<Vector2Int>();

        // 外部参照用
        public bool IsDrawing => isDrawing;

        void Start()
        {
            // 左外周の中央に初期化（外周が壁である前提）
            cell = new Vector2Int(0, GameConsts.GridH / 2);
            SnapToCell();

            isDrawing = false;
            trailCells.Clear();
            if (trailBlinker) trailBlinker.SetActive(false);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // 一時停止中はGameFlow側でTime.timeScale=0にされる。ここでは通常どおりTimerを進める
            stepTimer += Time.deltaTime;

            // 描画中だけ移動間隔を長く＝遅く
            float curInterval = stepInterval / (isDrawing ? drawSpeedMultiplier : 1f);

            if (stepTimer >= curInterval)
            {
                stepTimer = 0f;
                if (TryGetInput(out Vector2Int dir))
                {
                    TryStep(dir);
                }
            }
        }

        bool TryGetInput(out Vector2Int dir)
        {
            var kb = Keyboard.current;
            dir = Vector2Int.zero;
            if (kb == null) return false;

            // 斜め入力は無視（最後に押された軸優先にしたい場合は工夫可）
            if (kb.upArrowKey.isPressed) dir = Vector2Int.up;
            else if (kb.downArrowKey.isPressed) dir = Vector2Int.down;
            else if (kb.leftArrowKey.isPressed) dir = Vector2Int.left;
            else if (kb.rightArrowKey.isPressed) dir = Vector2Int.right;

            return dir != Vector2Int.zero;
        }

        void TryStep(Vector2Int dir)
        {
            Vector2Int next = cell + dir;
            if (!InBounds(next)) return;

            bool wasOnWall = wallTilemap.HasTile((Vector3Int)cell);
            bool willBeOnWall = wallTilemap.HasTile((Vector3Int)next);

            // 移動
            cell = next;
            SnapToCell();

            // 描画モード開始（外周→内側へ踏み出した瞬間）
            if (!isDrawing && wasOnWall && !willBeOnWall)
            {
                isDrawing = true;
                trailCells.Clear();
                if (trailBlinker) trailBlinker.SetActive(true);

                // 1歩目のセルに線を引く
                trailTilemap.SetTile((Vector3Int)cell, trailTile);
                trailCells.Add(cell);
                return;
            }

            // 描画中の足跡（内側にいる間だけ線を引く）
            if (isDrawing && !willBeOnWall)
            {
                // 同じセルの重複登録を避ける
                if (trailTilemap.GetTile((Vector3Int)cell) != trailTile)
                {
                    trailTilemap.SetTile((Vector3Int)cell, trailTile);
                    trailCells.Add(cell);
                }
            }

            // 外周に戻ったら囲い込み処理
            if (isDrawing && willBeOnWall)
            {
                isDrawing = false;
                if (trailBlinker) trailBlinker.SetActive(false);

                // CaptureSystemへトレイルリストを渡す（内部でTrail消去まで行う）
                if (captureSystem != null)
                    captureSystem.ApplyCapture(trailCells);

                trailCells.Clear();
            }
        }

        bool InBounds(Vector2Int c)
        {
            return (c.x >= 0 && c.x < GameConsts.GridW && c.y >= 0 && c.y < GameConsts.GridH);
        }

        void SnapToCell()
        {
            // グリッド原点 + セル中心（+0.5, +0.5）
            Vector3 origin = gridRoot ? gridRoot.transform.position : Vector3.zero;
            transform.position = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        // ?? 演出フック（前回追加分）??

        public Vector2 GetWorldCenter()
        {
            Vector3 origin = gridRoot ? gridRoot.transform.position : Vector3.zero;
            return (Vector2)(origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f));
        }

        public void RoundReset()
        {
            isDrawing = false;
            if (trailBlinker) trailBlinker.SetActive(false);

            // 引きかけの線を消す
            foreach (var c in trailCells)
                trailTilemap.SetTile((Vector3Int)c, null);
            trailCells.Clear();

            // 位置を初期に戻す
            cell = new Vector2Int(0, GameConsts.GridH / 2);
            SnapToCell();

            // スプライトを再表示（死亡演出で非表示にしているため）
            var sr = GetComponent<SpriteRenderer>();
            if (sr) sr.enabled = true;
        }

        public void PlayDeathVFX()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr && sr.enabled)
            {
                PlayerShatterEffect.Play(
                  src: sr,
                  tilesX: 4, tilesY: 4,     // ピース数
                  pieceSpeed: 3f, radial: 1.5f,
                  gravity: 15f, life: 1.2f
                );
                sr.enabled = false; // いったん隠す（RoundResetで戻す）
            }
        }
    }
}
