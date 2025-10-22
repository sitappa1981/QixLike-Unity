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
        [Tooltip("1�}�X�i�ނ܂ł̊�Ԋu�i�b�j")]
        [SerializeField] private float stepInterval = 0.08f;

        [Header("Drawing Slowdown")]
        [Tooltip("����`���Ă���Ԃ̑��x�{���i1=����, 0.5=�����j")]
        [SerializeField, Range(0.5f, 1f)] private float drawSpeedMultiplier = 0.7f;

        // ���
        private Vector2Int cell;                 // ���݃Z��
        private float stepTimer;                 // ����1�}�X�ړ��܂ł̃^�C�}�[
        private bool isDrawing;                  // ����`���Ă���Œ���
        private readonly List<Vector2Int> trailCells = new List<Vector2Int>();

        // �O���Q�Ɨp
        public bool IsDrawing => isDrawing;

        void Start()
        {
            // ���O���̒����ɏ������i�O�����ǂł���O��j
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

            // �ꎞ��~����GameFlow����Time.timeScale=0�ɂ����B�����ł͒ʏ�ǂ���Timer��i�߂�
            stepTimer += Time.deltaTime;

            // �`�撆�����ړ��Ԋu�𒷂����x��
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

            // �΂ߓ��͖͂����i�Ō�ɉ����ꂽ���D��ɂ������ꍇ�͍H�v�j
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

            // �ړ�
            cell = next;
            SnapToCell();

            // �`�惂�[�h�J�n�i�O���������֓��ݏo�����u�ԁj
            if (!isDrawing && wasOnWall && !willBeOnWall)
            {
                isDrawing = true;
                trailCells.Clear();
                if (trailBlinker) trailBlinker.SetActive(true);

                // 1���ڂ̃Z���ɐ�������
                trailTilemap.SetTile((Vector3Int)cell, trailTile);
                trailCells.Add(cell);
                return;
            }

            // �`�撆�̑��Ձi�����ɂ���Ԃ������������j
            if (isDrawing && !willBeOnWall)
            {
                // �����Z���̏d���o�^�������
                if (trailTilemap.GetTile((Vector3Int)cell) != trailTile)
                {
                    trailTilemap.SetTile((Vector3Int)cell, trailTile);
                    trailCells.Add(cell);
                }
            }

            // �O���ɖ߂�����͂����ݏ���
            if (isDrawing && willBeOnWall)
            {
                isDrawing = false;
                if (trailBlinker) trailBlinker.SetActive(false);

                // CaptureSystem�փg���C�����X�g��n���i������Trail�����܂ōs���j
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
            // �O���b�h���_ + �Z�����S�i+0.5, +0.5�j
            Vector3 origin = gridRoot ? gridRoot.transform.position : Vector3.zero;
            transform.position = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        // ?? ���o�t�b�N�i�O��ǉ����j??

        public Vector2 GetWorldCenter()
        {
            Vector3 origin = gridRoot ? gridRoot.transform.position : Vector3.zero;
            return (Vector2)(origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f));
        }

        public void RoundReset()
        {
            isDrawing = false;
            if (trailBlinker) trailBlinker.SetActive(false);

            // ���������̐�������
            foreach (var c in trailCells)
                trailTilemap.SetTile((Vector3Int)c, null);
            trailCells.Clear();

            // �ʒu�������ɖ߂�
            cell = new Vector2Int(0, GameConsts.GridH / 2);
            SnapToCell();

            // �X�v���C�g���ĕ\���i���S���o�Ŕ�\���ɂ��Ă��邽�߁j
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
                  tilesX: 4, tilesY: 4,     // �s�[�X��
                  pieceSpeed: 3f, radial: 1.5f,
                  gravity: 15f, life: 1.2f
                );
                sr.enabled = false; // ��������B���iRoundReset�Ŗ߂��j
            }
        }
    }
}
