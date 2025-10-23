using UnityEngine;
using UnityEngine.Tilemaps;

namespace QixLike
{
    // いちばん早く初期化されるように
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneLocator : MonoBehaviour
    {
        // 取得用プロパティ
        public static Grid Grid { get; private set; }
        public static Tilemap OverlayTilemap { get; private set; }
        public static Tilemap WallTilemap { get; private set; }
        public static Tilemap TrailTilemap { get; private set; }
        public static GameFlow GameFlow { get; private set; }
        public static PlayerController Player { get; private set; }

        // Inspector で任意に差し替え可
        [Header("Scene Refs (optional)")]
        [SerializeField] Grid grid;
        [SerializeField] Tilemap overlayTilemap;
        [SerializeField] Tilemap wallTilemap;
        [SerializeField] Tilemap trailTilemap;
        [SerializeField] GameFlow gameFlow;
        [SerializeField] PlayerController player;

        void Awake()
        {
            // 事前にアサインされていればそれを優先、なければシーンから検索
            Grid = grid ? grid : Object.FindFirstObjectByType<Grid>();
            OverlayTilemap = overlayTilemap ? overlayTilemap : GameObject.Find("OverlayTilemap")?.GetComponent<Tilemap>();
            WallTilemap = wallTilemap ? wallTilemap : GameObject.Find("WallTilemap")?.GetComponent<Tilemap>();
            TrailTilemap = trailTilemap ? trailTilemap : GameObject.Find("TrailTilemap")?.GetComponent<Tilemap>();
            GameFlow = gameFlow ? gameFlow : Object.FindFirstObjectByType<GameFlow>();
            Player = player ? player : Object.FindFirstObjectByType<PlayerController>();
        }
    }
}
