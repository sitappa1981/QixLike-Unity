using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class SmallEnemy : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] float baseSpeed = 2.2f;        // 基本速度
    [SerializeField] float speedScale = 1.0f;       // 難易度や個体差
    [SerializeField] float dashMul = 1.0f;        // ダッシュ時の倍率（通常=1）

    [Header("Steering (方向付け)")]
    [SerializeField, Range(0f, 1f)] float separationSteer = 0.6f; // 分離ベクトルの混ぜ具合
    [SerializeField] float noiseClamp = 0.02f;                    // 微小ノイズ（反射角の揺らぎ）
    [SerializeField] LayerMask wallMask;                          // 外周や壁のレイヤ
    [SerializeField] float reflectProbe = 0.2f;                   // 反射レイの射程

    // グリッド移動（= 物理ではなく transform を直接動かす）
    Vector2 posCell;   // 位置（セル/ワールドどちらでも運用OK）
    Vector2 velCell;   // 速度ベクトル（方向×速度）

    Rigidbody2D rb;
    EnemySeparation2D sep;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sep = GetComponent<EnemySeparation2D>();

        // 物理は移動に使わないが、安全側設定
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        posCell = transform.position;
        velCell = Vector2.right * baseSpeed; // 初期化（任意）
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ---- 1) 現在の前方（速度があれば正規化、無ければ右向き） ----
        Vector2 forward = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : Vector2.right;

        // ---- 2) 壁ヒットで反射 ----
        forward = ReflectIfHit(forward);

        // ---- 3) ステア（分離 + 微小ノイズ）を加えて最終方向へ ----
        Vector2 steer = ComputeSteer(forward, Time.time);
        Vector2 dir = (forward + steer).normalized;

        // ---- 4) 実速度を算出 ----
        float sp = baseSpeed * speedScale * dashMul;

        // ---- 5) 速度ベクトル更新（グリッド移動） ----
        velCell = dir * sp;

        // ---- 6) 位置を進めて transform に反映 ----
        posCell += velCell * dt;
        ApplyTransform(posCell);
    }

    void FixedUpdate()
    {
        // ★ B案：Update で確定した「方向×速度（= velCell）」をそのまま分離へ渡す
        if (sep != null)
        {
            sep.SetDesiredVelocity(velCell);
        }

        // 注意：rb.velocity は書き換えない（移動はグリッド制御のため）
    }

    // ---- ステア（方向付け）：分離 + 微小ノイズ ----
    Vector2 ComputeSteer(Vector2 forward, float t)
    {
        Vector2 result = Vector2.zero;

        // 分離（LastRepel）を方向へミックス
        if (sep != null && sep.LastRepel.sqrMagnitude > 1e-5f)
        {
            result += sep.LastRepel.normalized * separationSteer;
        }

        // 微小ノイズ（反射角の揺らぎ）
        if (noiseClamp > 0f)
        {
            float nx = (Mathf.PerlinNoise(t * 1.3f, 0.123f) - 0.5f) * 2f * noiseClamp;
            float ny = (Mathf.PerlinNoise(0.456f, t * 1.7f) - 0.5f) * 2f * noiseClamp;
            result += new Vector2(nx, ny);
        }

        return result;
    }

    // ---- 前方の壁で反射 ----
    Vector2 ReflectIfHit(Vector2 forward)
    {
        if (reflectProbe <= 0f) return forward;

        Vector2 p = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(p, forward, reflectProbe, wallMask);
        if (hit.collider)
        {
            Vector2 r = Vector2.Reflect(forward, hit.normal).normalized;

            // 反射ベクトルにも微小ノイズ
            if (noiseClamp > 0f)
            {
                float e = (Mathf.PerlinNoise(Time.time * 1.9f, 0.89f) - 0.5f) * 2f * noiseClamp;
                r = (r + new Vector2(e, -e)).normalized;
            }
            return r;
        }
        return forward;
    }

    // ---- 位置適用（グリッド→ワールド） ----
    void ApplyTransform(Vector2 pos)
    {
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 進行方向の可視化（velCell ベース）
        Vector2 dir = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : Vector2.right;

        Gizmos.color = Color.cyan;  // 方向
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)dir * 0.6f);

        Gizmos.color = Color.green; // 速度の目安
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(dir * 0.4f));
    }

    public void ResetEnemy()
    {
        // ここで初期位置や状態に戻す処理を実装
        // 例: transform.position = 初期位置;
        // 必要に応じて他の状態もリセット
    }
#endif
}
