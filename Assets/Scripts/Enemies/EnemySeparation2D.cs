using UnityEngine;

/// <summary>
/// 敵どうし非貫通（分離）: 近傍の敵を避ける「反発ベクトル」を計算して LastRepel に保持する。
/// ※ B案: 実際の移動は SmallEnemy 側（グリッド移動）。ここでは rb.velocity を変更しない。
/// 使い方:
///   - SmallEnemy.FixedUpdate() で sep.SetDesiredVelocity(velCell) を毎フレーム呼ぶ
///   - SmallEnemy.Update() のステア計算で steer += sep.LastRepel.normalized * 係数;
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeparation2D : MonoBehaviour
{
    [Header("Separation")]
    [SerializeField] float separationRadius = 1.2f;       // 近傍の敵をサンプルする半径
    [SerializeField] float separationForce = 10f;        // 反発の強さ（加速度イメージ）
    [SerializeField] LayerMask enemyMask;                 // 敵レイヤ（Enemy）

    [Header("Boundary Lock")]
    [SerializeField] LayerMask boundaryMask;              // 外周（Boundary）レイヤ
    [SerializeField] float boundaryProbe = 0.30f;         // 外周検出の射程
    [SerializeField, Range(0f, 1f)]
    float boundaryLock = 0.70f;                           // 外向き成分の抑制率（0=無効, 1=強）

    // ---- 出力/入力 ----
    /// <summary>SmallEnemy 側のステアに混ぜる最終反発ベクトル（境界ロック適用後）。</summary>
    public Vector2 LastRepel { get; private set; }

    /// <summary>SmallEnemy 側から渡される「希望速度（方向×速度）」。</summary>
    Vector2 desiredVel;

    // ---- 内部 ----
    Rigidbody2D rb;
    // NonAlloc 用のバッファ（GC抑制）
    readonly Collider2D[] hits = new Collider2D[32];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // 希望速度（未設定なら現velを参照、どちらも無ければゼロ）
        Vector2 vDesired =
            desiredVel.sqrMagnitude > 1e-6f ? desiredVel :
            rb.linearVelocity.sqrMagnitude > 1e-6f ? rb.linearVelocity :
            Vector2.zero;

        // --- 近傍分離 ---
        Vector2 repel = ComputeRepulsion();

        // --- 境界ロック（外向き成分を抑制） ---
        if (TryGetBoundaryNormal(out Vector2 outwardNormal))
        {
            // vDesired + repel の外向き（境界から外に出やすい）成分を間引く
            float outward = Vector2.Dot(vDesired + repel, outwardNormal);
            if (outward > 0f)
            {
                repel -= outwardNormal * outward * boundaryLock;
            }
        }

        // 出力：SmallEnemy 側のステアで利用
        LastRepel = repel;

        // 次フレーム用にリセット（毎 FixedUpdate で SetDesiredVelocity を呼んでもらう）
        desiredVel = Vector2.zero;

        // ★ 注意：B案では rb.velocity を更新しない（移動は SmallEnemy が担当）
        // rb.velocity = ...
    }

    /// <summary>
    /// SmallEnemy 側からそのフレームの「方向×速度」を受け取る。
    /// 例: sep.SetDesiredVelocity(velCell);
    /// </summary>
    public void SetDesiredVelocity(Vector2 vel)
    {
        desiredVel = vel;
    }

    // ---- 近傍の敵からの反発ベクトルを計算（NonAlloc） ----
    Vector2 ComputeRepulsion()
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = enemyMask };
        int n = Physics2D.OverlapCircle((Vector2)transform.position, separationRadius, filter, hits);
        if (n <= 0) return Vector2.zero;

        Vector2 self = rb.position;
        Vector2 sum = Vector2.zero;
        int cnt = 0;

        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (!h) continue;
            if (h.attachedRigidbody == rb) continue; // 自分自身

            Vector2 toSelf = self - (Vector2)h.transform.position;
            float d = toSelf.magnitude;
            if (d < 0.0001f) continue;

            // 近いほど強いウェイト（smoothstep 逆）
            float t = Mathf.Clamp01(d / separationRadius);    // 0(近)→1(遠)
            float w = 1f - (t * t * (3f - 2f * t));           // 1→0 の曲線
            sum += (toSelf / d) * w;
            cnt++;
        }

        if (cnt == 0) return Vector2.zero;

        Vector2 dir = sum / Mathf.Max(1, cnt);
        Vector2 repel = dir.normalized * separationForce;
        return repel;
    }

    // ---- 外周（Boundary）法線のおおまかな取得 ----
    bool TryGetBoundaryNormal(out Vector2 outwardNormal)
    {
        outwardNormal = Vector2.zero;
        Vector2 p = rb.position;

        // 十字方向に短距離レイ
        var dirs = new Vector2[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        for (int i = 0; i < dirs.Length; i++)
        {
            var hit = Physics2D.Raycast(p, dirs[i], boundaryProbe, boundaryMask);
            if (hit.collider)
            {
                // 外周から外向き（=壁→自分 方向）
                outwardNormal = (p - hit.point).normalized;
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 分離半径の可視化
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // 最終反発ベクトルの可視化
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Vector3 a = transform.position;
        Vector3 b = a + (Vector3)(LastRepel.normalized * 0.6f);
        Gizmos.DrawLine(a, b);
    }
#endif
}
