using System.Collections.Generic;
using UnityEngine;

namespace QixLike.Enemies
{
    /// <summary>
    /// 小型敵どうしの重なりをフレーム末尾で解消する軽量ソルバ。
    /// 物理Rigidbody2Dは使わず、Transformを最小限だけ押し戻します。
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        [Tooltip("解像度を上げるほどガッチリ離れます（2?3がおすすめ）。")]
        [Range(1, 6)] public int solverIterations = 2;

        [Tooltip("1回の解消で押し戻す強さのスケール。通常は1でOK。")]
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

            // 数回の反復で重なりをほぼ解消
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

                        // めり込み判定
                        if (dist > 0.0001f && dist < minDist)
                        {
                            Vector2 nrm = d / dist;
                            float penetration = (minDist - dist) * pushScale;

                            // 双方を半分ずつ反対方向に押し戻す（最小移動ベクトル）
                            Vector2 move = nrm * (penetration * 0.5f);
                            a.ExternalNudge(-move);
                            b.ExternalNudge(move);          // ← 単項 + を削除

                            // 少しだけ法線成分を弾く
                            a.ExternalBounce(-nrm, 0.15f);
                            b.ExternalBounce(nrm, 0.15f);   // ← 単項 + を削除
                        }
                    }
                }
            }
        }
    }
}
