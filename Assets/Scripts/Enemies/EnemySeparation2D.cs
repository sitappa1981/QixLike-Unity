using UnityEngine;

/// <summary>
/// 敵どうし非貫通（分離）: 近傍の敵を避ける反発ベクトルを計算して LastRepel に保持。
/// B案: 実移動は SmallEnemy（グリッド移動）。ここでは rb.linearVelocity を変更しない。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeparation2D : MonoBehaviour
{
    [Header("Separation")]
    [SerializeField] float separationRadius = 1.1f;      // 1.0〜1.2
    [SerializeField] float separationForce = 11f;       // 10〜12
    [SerializeField] LayerMask enemyMask;                // Enemy

    [Header("Boundary Lock")]
    [SerializeField] LayerMask boundaryMask;             // Boundary
    [SerializeField] float boundaryProbe = 0.35f;        // 0.30〜0.45
    [SerializeField, Range(0f, 1f)]
    float boundaryLock = 0.60f;                          // 0.55〜0.65

    // 出力/入力
    public Vector2 LastRepel { get; private set; }
    Vector2 desiredVel;

    // 内部
    Rigidbody2D rb;
    readonly Collider2D[] hits = new Collider2D[32];
    ContactFilter2D filter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        filter = new ContactFilter2D { useLayerMask = true, useTriggers = true };
        filter.SetLayerMask(enemyMask);
    }

    void FixedUpdate()
    {
        // 希望速度（未設定なら現 linearVelocity、無ければ0）
        Vector2 vDesired =
            desiredVel.sqrMagnitude > 1e-6f ? desiredVel :
            rb.linearVelocity.sqrMagnitude > 1e-6f ? rb.linearVelocity :
            Vector2.zero;

        // 近傍分離
        Vector2 repel = ComputeRepulsion();

        // 境界ロック：★壁へ向かう成分だけ抑える（= 壁から離れる内向き成分は削らない）
        if (TryGetBoundaryInwardNormal(out Vector2 inwardNormal))
        {
            // 壁の法線（壁→自分 方向 = フィールド内向き）
            // 壁へ向かうベクトル = -(inwardNormal)
            Vector2 towardWallNormal = -inwardNormal;

            float towardWall = Vector2.Dot(vDesired + repel, towardWallNormal);
            if (towardWall > 0f)
            {
                // 壁へ突っ込む成分だけ抑制。接線や内向きは温存。
                repel -= towardWallNormal * towardWall * boundaryLock;
            }
        }

        LastRepel = repel;
        desiredVel = Vector2.zero;
    }

    /// <summary>SmallEnemy から毎FixedUpdateで「方向×速度」を受け取る</summary>
    public void SetDesiredVelocity(Vector2 vel) => desiredVel = vel;

    Vector2 ComputeRepulsion()
    {
        int n = Physics2D.OverlapCircle((Vector2)transform.position, separationRadius, filter, hits);
        if (n <= 0) return Vector2.zero;

        Vector2 self = rb.position;
        Vector2 sum = Vector2.zero; int cnt = 0;

        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (!h) continue;
            if (h.attachedRigidbody == rb) continue;

            Vector2 toSelf = self - (Vector2)h.transform.position;
            float d = toSelf.magnitude; if (d < 0.0001f) continue;

            // 近いほど強いウェイト（逆smoothstep）
            float t = Mathf.Clamp01(d / separationRadius);       // 0(近)→1(遠)
            float w = 1f - (t * t * (3f - 2f * t));              // 1→0
            sum += (toSelf / d) * w;
            cnt++;
        }

        if (cnt == 0) return Vector2.zero;
        Vector2 dir = sum / Mathf.Max(1, cnt);
        return dir.normalized * separationForce;
    }

    // 壁→自分の向き（= フィールド内向き）を取得
    bool TryGetBoundaryInwardNormal(out Vector2 inwardNormal)
    {
        inwardNormal = Vector2.zero;
        Vector2 p = rb.position;
        var dirs = new Vector2[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

        for (int i = 0; i < dirs.Length; i++)
        {
            var hit = Physics2D.Raycast(p, dirs[i], boundaryProbe, boundaryMask);
            if (hit.collider)
            {
                inwardNormal = (p - hit.point).normalized; // 壁→自分（内向き）
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Vector3 a = transform.position;
        Vector3 b = a + (Vector3)(LastRepel.sqrMagnitude > 1e-6f ? LastRepel.normalized * 0.6f : Vector2.zero);
        Gizmos.DrawLine(a, b);
    }
#endif
}
