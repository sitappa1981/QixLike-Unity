using System.Collections.Generic;
using UnityEngine;

namespace QixLike
{
    public sealed class EnemyManager : MonoBehaviour
    {
        public void SetAllEnemiesSpeedScale(float scale)
        {
            foreach (var e in enemies) if (e) e.SetSpeedScale(scale);
        }

        public void ResetAllEnemies()
        {
            foreach (var e in enemies) if (e) e.ResetEnemy();
        }
        public static EnemyManager Instance { get; private set; }

        readonly HashSet<SmallEnemy> enemies = new HashSet<SmallEnemy>();

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Register(SmallEnemy e) { if (e) enemies.Add(e); }
        public void Unregister(SmallEnemy e) { if (e) enemies.Remove(e); }

        /// <summary>近すぎる敵同士を少しだけ押し離して重なりを防ぐ</summary>
        public void ResolveOverlaps(float minDistance = 0.45f, float pushStrength = 2f)
        {
            if (enemies.Count <= 1) return;

            var list = ListCache;
            list.Clear(); list.AddRange(enemies);

            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    var a = list[i]; var b = list[j];
                    if (!a || !b) continue;

                    var pa = a.transform.position;
                    var pb = b.transform.position;
                    var delta = (Vector2)(pb - pa);
                    float dist = delta.magnitude;
                    float min = minDistance;
                    if (dist < 1e-4f) delta = Random.insideUnitCircle.normalized * 0.001f;

                    if (dist < min)
                    {
                        var dir = delta.normalized;
                        float push = (min - dist) * 0.5f * pushStrength * Time.deltaTime;
                        a.ExternalNudge(-dir * push);
                        b.ExternalNudge(dir * push);
                    }
                }
        }

        static readonly List<SmallEnemy> ListCache = new List<SmallEnemy>();

        void LateUpdate()
        {
            // ここで毎フレーム重なり解消（必要なら係数は調整）
            ResolveOverlaps();
        }
    }
}
