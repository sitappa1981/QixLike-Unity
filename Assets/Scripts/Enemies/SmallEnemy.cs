using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[DisallowMultipleComponent]
public class SmallEnemy : MonoBehaviour
{
    public enum Variant { TypeA, TypeB_Dash, TypeC_Hop, TypeD_DashHop }

    [Header("Variant")]
    [SerializeField] Variant variant = Variant.TypeA;

    [Header("Speed")]
    [SerializeField] float baseSpeed = 3.6f;
    [SerializeField] float speedScale = 1.0f;     // GameFlow から SetSpeedScale
    [SerializeField] float dashMul = 1.0f;      // Dash/Hop で更新

    [Header("Steering (Separation/Noise)")]
    [SerializeField, Range(0f, 1f)] float separationSteer = 0.06f;
    [SerializeField] float steerMax = 0.5f;
    [SerializeField] float noiseClamp = 0.03f;
    [SerializeField, Range(0f, 0.2f)] float speedJitterRange = 0.06f;

    [Header("Boundary / Reflect")]
    [SerializeField] LayerMask wallMask;          // Boundary を割当
    [SerializeField] float skin = 0.02f;          // 壁との最小すき間

    [Header("Dash (TypeB/TypeD)")]
    [SerializeField] Vector2 dashIntervalRange = new Vector2(1.0f, 2.0f);
    [SerializeField] float dashDuration = 0.25f;
    [SerializeField] float dashMultiplier = 2.4f;

    [Header("Hop (TypeC/TypeD)")]
    [SerializeField] Vector2 hopRunRange = new Vector2(0.7f, 1.2f);
    [SerializeField] float hopPauseTime = 0.40f;
    [SerializeField] float hopBurstTime = 0.18f;
    [SerializeField] float hopMultiplier = 2.6f;

    // ---- 内部状態 ----
    Vector2 desiredVel;           // 行きたい速度
    Vector2 currentVel;           // 実際の速度（分離へ渡す）
    Vector2 dirFallback = Vector2.right;

    int noiseSeed;
    float speedJitter = 1f;

    float dashEndT, dashNextT;

    // ★ HopState はここだけ！
    enum HopState { Run, Pause, Burst }
    HopState hopState = HopState.Run;
    float hopStateEndT;
    float hopMul = 1f;

    Rigidbody2D rb;
    CircleCollider2D cc;
    EnemySeparation2D sep;
    ContactFilter2D boundaryFilter;
    readonly RaycastHit2D[] castHits = new RaycastHit2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CircleCollider2D>();
        sep = GetComponent<EnemySeparation2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;

        boundaryFilter = new ContactFilter2D { useLayerMask = true, useTriggers = true };
        boundaryFilter.SetLayerMask(wallMask);

        noiseSeed = Mathf.Abs(GetInstanceID());
        speedJitter = 1f + (Hash01(noiseSeed) * 2f - 1f) * speedJitterRange;

        InitDash();
        InitHop();

        currentVel = Vector2.right * baseSpeed;
        desiredVel = currentVel;
    }

    void OnEnable() { var em = QixLike.EnemyManager.Instance; if (em) em.Register(this); }
    void OnDisable() { var em = QixLike.EnemyManager.Instance; if (em) em.Unregister(this); }

    // ---- ステア・ダッシュなどで「行きたい速度」を作る ----
    void Update()
    {
        float t = Time.time;

        UpdateDash();
        UpdateHop();

        Vector2 forward = currentVel.sqrMagnitude > 1e-6f ? currentVel.normalized : dirFallback;

        Vector2 steer = ComputeSteer(forward, t);
        Vector2 dir = (forward + steer).sqrMagnitude > 1e-8f ? (forward + steer).normalized : forward;

        float sp = baseSpeed * speedScale * speedJitter * dashMul * hopMul;
        desiredVel = dir * sp;

        dirFallback = dir;
    }

    // ---- 物理で確定移動（壁ヒット時は直前で停止→反射）----
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 move = desiredVel * dt;
        float dist = move.magnitude;

        if (dist > 0f)
        {
            Vector2 dir = move / dist;

            int hitCount = rb.Cast(dir, boundaryFilter, castHits, dist + skin);
            if (hitCount > 0)
            {
                float minDist = float.MaxValue;
                RaycastHit2D nearest = castHits[0];
                for (int i = 0; i < hitCount; i++)
                    if (castHits[i].distance < minDist) { minDist = castHits[i].distance; nearest = castHits[i]; }

                float safe = Mathf.Max(0f, nearest.distance - skin);
                rb.MovePosition(rb.position + dir * safe);

                Vector2 reflDir = Vector2.Reflect(dir, nearest.normal).normalized;
                currentVel = reflDir * desiredVel.magnitude;
                desiredVel = currentVel;

                sep?.SetDesiredVelocity(currentVel);
                return; // このFixedはここで終了（突破しない）
            } else
            {
                rb.MovePosition(rb.position + move);
            }
        }

        currentVel = desiredVel;
        sep?.SetDesiredVelocity(currentVel);
    }

    // ===== GameFlow / EnemyManager 連携 =====
    public void SetSpeedScale(float scale) => speedScale = Mathf.Clamp(scale, 0.1f, 5f);

    public void ResetEnemy()
    {
        currentVel = Vector2.right * baseSpeed;
        desiredVel = currentVel;
        dashMul = 1f; hopMul = 1f;
        InitDash(); InitHop();
    }

    public void ExternalNudge(Vector2 delta)
    {
        rb.MovePosition(rb.position + delta);
    }

    // ====== Variant: Dash ======
    void InitDash()
    {
        dashMul = 1f;
        dashEndT = 0f;
        dashNextT = Time.time + Random.Range(dashIntervalRange.x, dashIntervalRange.y);
    }
    void UpdateDash()
    {
        if (variant != Variant.TypeB_Dash && variant != Variant.TypeD_DashHop) { dashMul = 1f; return; }
        float t = Time.time;
        if (t >= dashEndT) dashMul = 1f;
        if (t >= dashNextT)
        {
            dashMul = dashMultiplier;
            dashEndT = t + dashDuration;
            dashNextT = t + Random.Range(dashIntervalRange.x, dashIntervalRange.y);
        }
    }

    // ====== Variant: Hop ======
    void InitHop()
    {
        hopMul = 1f;
        hopState = HopState.Run;
        hopStateEndT = Time.time + Random.Range(hopRunRange.x, hopRunRange.y);
    }
    void UpdateHop()
    {
        if (variant != Variant.TypeC_Hop && variant != Variant.TypeD_DashHop) { hopMul = 1f; return; }

        float t = Time.time;
        switch (hopState)
        {
            case HopState.Run:
                hopMul = 1f;
                if (t >= hopStateEndT) { hopState = HopState.Pause; hopStateEndT = t + hopPauseTime; }
                break;
            case HopState.Pause:
                hopMul = 0f;
                if (t >= hopStateEndT) { hopState = HopState.Burst; hopStateEndT = t + hopBurstTime; }
                break;
            case HopState.Burst:
                hopMul = hopMultiplier;
                if (t >= hopStateEndT) { hopState = HopState.Run; hopStateEndT = t + Random.Range(hopRunRange.x, hopRunRange.y); }
                break;
        }
    }

    // ===== ステア（分離＋ノイズ） =====
    Vector2 ComputeSteer(Vector2 forward, float t)
    {
        Vector2 res = Vector2.zero;

        if (sep != null && sep.LastRepel.sqrMagnitude > 1e-5f)
        {
            Vector2 repel = sep.LastRepel * separationSteer; // 非正規化
            if (repel.magnitude > steerMax) repel = repel.normalized * steerMax;
            res += repel;
        }

        if (noiseClamp > 0f)
        {
            float nx = (Mathf.PerlinNoise((t + noiseSeed * 0.1337f) * 1.3f, 0.123f + noiseSeed * 0.017f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(0.456f + noiseSeed * 0.031f, (t + noiseSeed * 0.257f) * 1.7f) - 0.5f) * 2f;
            res += new Vector2(nx, ny) * noiseClamp;
        }
        return res;
    }

    static float Hash01(int s) => Mathf.Abs(Mathf.Sin(s * 12.3456789f)) % 1f;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 d = currentVel.sqrMagnitude > 1e-6f ? currentVel.normalized : Vector2.right;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)d * 0.6f);
    }
#endif
}
