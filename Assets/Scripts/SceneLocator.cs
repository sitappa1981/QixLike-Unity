using UnityEngine;
using UnityEngine.Tilemaps;
using QixLike.Gameplay;   // ← GameFlow / PlayerController を使うために追加

namespace QixLike.Core
{
    // なるべく早く初期化されるように
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class SceneLocator : MonoBehaviour
    {
        public static Grid Grid { get; private set; }
        public static Tilemap Wall { get; private set; }
        public static Tilemap Trail { get; private set; }
        public static GameFlow Flow { get; private set; }
        public static PlayerController Player { get; private set; }

        private void Awake()
        {
            // 見つかっていなければシーンから拾う
            Grid ??= FindFirstObjectByType<Grid>();
            Flow ??= FindFirstObjectByType<GameFlow>();
            Player ??= FindFirstObjectByType<PlayerController>();

            // タイルマップは名前で判定（あなたのシーン名に合わせてあります）
            foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (tm == null) continue;
                if (tm.name == "WallTilemap") Wall = tm;
                else if (tm.name == "TrailTilemap") Trail = tm;
            }
        }
    }
}
