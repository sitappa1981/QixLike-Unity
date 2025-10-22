using System.Collections.Generic;
using UnityEngine;

namespace QixLike.Enemies
{
    /// <summary>
    /// ���^�G�ǂ����̏d�Ȃ���t���[�������ŉ�������y�ʃ\���o�B
    /// ����Rigidbody2D�͎g�킸�ATransform���ŏ������������߂��܂��B
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        [Tooltip("�𑜓x���グ��قǃK�b�`������܂��i2?3���������߁j�B")]
        [Range(1, 6)] public int solverIterations = 2;

        [Tooltip("1��̉����ŉ����߂������̃X�P�[���B�ʏ��1��OK�B")]
        [Range(0.5f, 2f)] public float pushScale = 1f;

        private readonly List<SmallEnemy> _enemies = new();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void Register(SmallEnemy e)
        {
            if (e != null && !_enemies.Contains(e)) _enemies.Add(e);
        }

        public void Unregister(SmallEnemy e)
        {
            _enemies.Remove(e);
        }

        void LateUpdate()
        {
            int n = _enemies.Count;
            if (n < 2) return;

            // ����̔����ŏd�Ȃ���قډ���
            for (int iter = 0; iter < solverIterations; iter++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    var a = _enemies[i];
                    if (a == null) continue;

                    Vector2 pa = a.transform.position;
                    float ra = a.RadiusWorld;

                    for (int j = i + 1; j < n; j++)
                    {
                        var b = _enemies[j];
                        if (b == null) continue;

                        Vector2 pb = b.transform.position;
                        float rb = b.RadiusWorld;

                        Vector2 d = pb - pa;
                        float dist = d.magnitude;
                        float minDist = ra + rb;

                        // �߂荞�ݔ���
                        if (dist > 0.0001f && dist < minDist)
                        {
                            Vector2 nrm = d / dist;
                            float penetration = (minDist - dist) * pushScale;

                            // �o���𔼕������Ε����ɉ����߂��i�ŏ��ړ��x�N�g���j
                            Vector2 move = nrm * (penetration * 0.5f);
                            a.ExternalNudge(-move);
                            b.ExternalNudge(move);          // �� �P�� + ���폜

                            // ���������@��������e��
                            a.ExternalBounce(-nrm, 0.15f);
                            b.ExternalBounce(nrm, 0.15f);   // �� �P�� + ���폜
                        }
                    }
                }
            }
        }
    }
}
