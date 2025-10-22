using UnityEngine;
using UnityEngine.Tilemaps;

namespace QixLike
{
    public class GridBootstrap : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private Grid gridRoot;
        [SerializeField] private Tilemap overlayTilemap;
        [SerializeField] private Tilemap wallTilemap;

        [Header("Tiles")]
        [SerializeField] private TileBase tileOverlay;
        [SerializeField] private TileBase tileWall;

        void Awake()
        {
            // 画面中央に80x45をピッタリ収めるため、左下を(-39.5,-22.5)に揃える
            if (gridRoot != null)
            {
                gridRoot.transform.position = new Vector3(
                  -GameConsts.GridW / 2f + 0.5f,
                  -GameConsts.GridH / 2f + 0.5f,
                  0f
                );
            }

            // 一旦クリア
            overlayTilemap.ClearAllTiles();
            wallTilemap.ClearAllTiles();

            // オーバーレイ全面敷き詰め（黒）
            for (int x = 0; x < GameConsts.GridW; x++)
            {
                for (int y = 0; y < GameConsts.GridH; y++)
                {
                    overlayTilemap.SetTile(new Vector3Int(x, y, 0), tileOverlay);
                }
            }

            // 外周を壁に（薄グレー）
            for (int x = 0; x < GameConsts.GridW; x++)
            {
                wallTilemap.SetTile(new Vector3Int(x, 0, 0), tileWall);
                wallTilemap.SetTile(new Vector3Int(x, GameConsts.GridH - 1, 0), tileWall);
            }
            for (int y = 0; y < GameConsts.GridH; y++)
            {
                wallTilemap.SetTile(new Vector3Int(0, y, 0), tileWall);
                wallTilemap.SetTile(new Vector3Int(GameConsts.GridW - 1, y, 0), tileWall);
            }
        }
    }
}
