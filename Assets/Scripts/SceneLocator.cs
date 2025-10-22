using UnityEngine;
using UnityEngine.Tilemaps;
using QixLike.Gameplay;   // �� GameFlow / PlayerController ���g�����߂ɒǉ�

namespace QixLike.Core
{
    // �Ȃ�ׂ����������������悤��
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
            // �������Ă��Ȃ���΃V�[������E��
            Grid ??= FindFirstObjectByType<Grid>();
            Flow ??= FindFirstObjectByType<GameFlow>();
            Player ??= FindFirstObjectByType<PlayerController>();

            // �^�C���}�b�v�͖��O�Ŕ���i���Ȃ��̃V�[�����ɍ��킹�Ă���܂��j
            foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (tm == null) continue;
                if (tm.name == "WallTilemap") Wall = tm;
                else if (tm.name == "TrailTilemap") Trail = tm;
            }
        }
    }
}
