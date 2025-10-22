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
            // ��ʒ�����80x45���s�b�^�����߂邽�߁A������(-39.5,-22.5)�ɑ�����
            if (gridRoot != null)
            {
                gridRoot.transform.position = new Vector3(
                  -GameConsts.GridW / 2f + 0.5f,
                  -GameConsts.GridH / 2f + 0.5f,
                  0f
                );
            }

            // ��U�N���A
            overlayTilemap.ClearAllTiles();
            wallTilemap.ClearAllTiles();

            // �I�[�o�[���C�S�ʕ~���l�߁i���j
            for (int x = 0; x < GameConsts.GridW; x++)
            {
                for (int y = 0; y < GameConsts.GridH; y++)
                {
                    overlayTilemap.SetTile(new Vector3Int(x, y, 0), tileOverlay);
                }
            }

            // �O����ǂɁi���O���[�j
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
