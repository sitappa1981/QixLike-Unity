using System.Collections.Generic;      // ‚à‚µŽg‚Á‚Ä‚¢‚ê‚Î
using UnityEngine;
using UnityEngine.Tilemaps;

namespace QixLike
{
    public class SmallEnemy : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] Grid gridRoot;
        [SerializeField] Tilemap wallTilemap;
        [SerializeField] Tilemap trailTilemap;
        [SerializeField] GameFlow gameFlow;
        [SerializeField] PlayerController player;

        [Header("Move")]
        [SerializeField, Range(0.5f, 20f)] float speed = 6f;
        [SerializeField, Range(0.1f, 0.49f)] float radius = 0.35f;
        [SerializeField] Vector2 initialDir = new(1f, 0.7f);

        [Header("Hit")]
        [SerializeField, Range(0f, 0.5f)] float trailHitCooldown = 0.2f;
        [SerializeField, Range(0.1f, 0.6f)] float playerRadius = 0.40f;

        [Header("Steering")]
        [SerializeField, Range(0.1f, 12f)] float steerResponsiveness = 6f;
        [SerializeField, Range(0f, 1f)] float jitterAmplitude = 0.20f;
        [SerializeField, Range(0.1f, 6f)] float jitterHz = 1.3f;

        [Header("Type B Dash")]
        [SerializeField] bool enableDash = false;
        [SerializeField, Range(1.2f, 6f)] float dashSpeedMultiplier = 2.8f;
        [SerializeField, Range(0.1f, 1.2f)] float dashDuration = 0.6f;
        [SerializeField] Vector2 dashCooldownRange = new(3f, 6f);
        [SerializeField, Range(0.05f, 1.0f)] float telegraphTime = 0.35f;
        [SerializeField] Color telegraphColor = new(1f, 0.6f, 0.1f, 1f);
        [SerializeField, Range(1f, 1.6f)] float telegraphScale = 1.15f;
        [SerializeField, Range(0f, 0.3f)] float telegraphJitterBoost = 0.1f;
        [SerializeField, Range(0.05f, 0.5f)] float dashRecoverTime = 0.15f;

        [Header("Type C Hop")]
        [SerializeField] bool enableHop = false;
        [SerializeField, Range(1, 4)] int hopCells = 2;
        [SerializeField] Vector2 hopCooldownRange = new(6f, 10f);
        [SerializeField, Range(0.05f, 1.0f)] float hopTelegraphTime = 0.35f;
        [SerializeField] Color hopTelegraphColor = new(0.4f, 0.85f, 1f, 1f);
        [SerializeField, Range(1f, 1.6f)] float hopTelegraphScale = 1.12f;
        [SerializeField, Range(0.05f, 0.5f)] float hopRecoverTime = 0.12f;

        [Header("Debug")]
        [SerializeField] bool debugLogs = false;

        Vector2 posCell, velCell, prevPosCell;
        float baseSpeed, speedScale = 1f, trailHitTimer;
        int stuckFrames;
        const float minMovePerFrame = 0.015f;
        float jitterPhaseX, jitterPhaseY;

        enum ActState { Idle, TelegraphDash, Dash, RecoverDash, TelegraphHop, Hop, RecoverHop }
        ActState act = ActState.Idle;
        float actTimer, nextDashCooldown, nextHopCooldown, dashMul = 1f;
        Vector2 dashDir = Vector2.zero;

        public float RadiusWorld => radius;

        SpriteRenderer sr; Color srBaseColor = Color.white; Vector3 baseScale = Vector3.one;

        public void AssignSceneRefs()
        {
            if (gridRoot == null) gridRoot = SceneLocator.Grid;
            if (wallTilemap == null) wallTilemap = SceneLocator.WallTilemap;
            if (trailTilemap == null) trailTilemap = SceneLocator.TrailTilemap;
            if (gameFlow == null) gameFlow = SceneLocator.GameFlow;
            if (player == null) player = SceneLocator.Player;
        }

        void Awake() { AssignSceneRefs(); }

        void Start()
        {
            baseSpeed = speed;
            posCell = new Vector2(GameConsts.GridW * 0.5f, GameConsts.GridH * 0.5f);
            var dir0 = (initialDir.sqrMagnitude > 1e-4f) ? initialDir.normalized : new Vector2(1f, 0.5f);
            velCell = dir0 * baseSpeed;

            prevPosCell = posCell; ApplyTransform();

            jitterPhaseX = Random.value * 10f; jitterPhaseY = Random.value * 10f;

            sr = GetComponent<SpriteRenderer>();
            if (sr) { srBaseColor = sr.color; baseScale = transform.localScale; }

            ResetDashCooldown(); ResetHopCooldown();
        }

        void Update()
        {
            trailHitTimer -= Time.deltaTime;

            UpdateActionState();

            var dir = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : new Vector2(1f, 0.5f);
            float sp = baseSpeed * speedScale * dashMul;

            if (act != ActState.Dash && act != ActState.Hop)
            {
                Vector2 steer = ComputeSteer(dir, Time.time);
                if (act == ActState.TelegraphDash && telegraphJitterBoost > 0f)
                    steer += ComputeJitter(Time.time) * telegraphJitterBoost;

                Vector2 desired = (dir + steer);
                if (desired.sqrMagnitude < 1e-6f) desired = dir;
                desired.Normalize();

                float turn = Mathf.Clamp01(steerResponsiveness * Time.deltaTime);
                dir = Vector2.Lerp(dir, desired, turn).normalized;
            }

            velCell = dir * sp;

            float dt = Time.deltaTime;
            Vector2 next = posCell + velCell * dt;

            if (!CollidesAt(next, out bool hitWhole))
            {
                posCell = next;
            } else
            {
                bool hitX = CollidesAt(new Vector2(posCell.x + velCell.x * dt, posCell.y), out bool hitTrailX);
                bool hitY = CollidesAt(new Vector2(posCell.x, posCell.y + velCell.y * dt), out bool hitTrailY);

                if (hitX) { velCell.x = -velCell.x; if (act == ActState.Dash) dashDir.x = -dashDir.x; }
                if (hitY) { velCell.y = -velCell.y; if (act == ActState.Dash) dashDir.y = -dashDir.y; }

                if ((hitWhole || hitTrailX || hitTrailY) && trailHitTimer <= 0f && gameFlow)
                {
                    if (debugLogs) Debug.Log("Enemy hit TRAIL");
                    gameFlow.OnEnemyHitTrail();
                    trailHitTimer = trailHitCooldown;
                }

                next = posCell + velCell * dt;
                if (!CollidesAt(next, out _)) posCell = next;
            }

            float moved = (posCell - prevPosCell).magnitude;
            if (moved < minMovePerFrame) stuckFrames++; else stuckFrames = 0;
            prevPosCell = posCell;

            ApplyTransform();

            if (player && player.IsDrawing && gameFlow && trailHitTimer <= 0f)
            {
                Vector2 enemyW = transform.position;
                Vector2 playerW = player.GetWorldCenter();
                float sumR = radius + playerRadius;
                if ((enemyW - playerW).sqrMagnitude <= sumR * sumR)
                {
                    if (debugLogs) Debug.Log("Enemy hit PLAYER (drawing)");
                    gameFlow.OnPlayerHitDuringDraw();
                    trailHitTimer = trailHitCooldown;
                }
            }
        }

        // --- ˆÈ‰ºA‚ ‚È‚½‚Ì”Å‚»‚Ì‚Ü‚Ü ---
        void UpdateActionState()
        {
            float dt = Time.deltaTime;

            switch (act)
            {
                case ActState.Idle:
                    dashMul = 1f;
                    nextDashCooldown -= dt; nextHopCooldown -= dt;
                    if (enableDash && nextDashCooldown <= 0f) { EnterTelegraphDash(); break; }
                    if (enableHop && nextHopCooldown <= 0f) { EnterTelegraphHop(); break; }
                    break;

                case ActState.TelegraphDash:
                    actTimer -= dt; dashMul = 1f;
                    if (sr)
                    {
                        float phase = 1f - Mathf.Clamp01(actTimer / Mathf.Max(0.001f, telegraphTime));
                        float pulse = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
                        sr.color = Color.Lerp(srBaseColor, telegraphColor, pulse);
                        transform.localScale = baseScale * Mathf.Lerp(1f, telegraphScale, pulse);
                    }
                    if (actTimer <= 0f) EnterDash();
                    break;

                case ActState.Dash:
                    actTimer -= dt; dashMul = dashSpeedMultiplier;
                    if (sr) sr.color = Color.Lerp(srBaseColor, telegraphColor, 0.35f);
                    if (actTimer <= 0f) EnterRecoverDash();
                    break;

                case ActState.RecoverDash:
                    actTimer -= dt; dashMul = 1f;
                    if (sr)
                    {
                        float t = 1f - Mathf.Clamp01(actTimer / Mathf.Max(0.001f, dashRecoverTime));
                        sr.color = Color.Lerp(srBaseColor, srBaseColor, t);
                        transform.localScale = Vector3.Lerp(transform.localScale, baseScale, t);
                    }
                    if (actTimer <= 0f) { if (sr) { sr.color = srBaseColor; transform.localScale = baseScale; } act = ActState.Idle; ResetDashCooldown(); }
                    break;

                case ActState.TelegraphHop:
                    actTimer -= dt; dashMul = 1f;
                    if (sr)
                    {
                        float phase = 1f - Mathf.Clamp01(actTimer / Mathf.Max(0.001f, hopTelegraphTime));
                        float pulse = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
                        sr.color = Color.Lerp(srBaseColor, hopTelegraphColor, pulse);
                        transform.localScale = baseScale * Mathf.Lerp(1f, hopTelegraphScale, pulse);
                    }
                    if (actTimer <= 0f) EnterHop();
                    break;

                case ActState.Hop:
                    EnterRecoverHop();
                    break;

                case ActState.RecoverHop:
                    actTimer -= dt; dashMul = 1f;
                    if (sr)
                    {
                        float t = 1f - Mathf.Clamp01(actTimer / Mathf.Max(0.001f, hopRecoverTime));
                        sr.color = Color.Lerp(srBaseColor, srBaseColor, t);
                        transform.localScale = Vector3.Lerp(transform.localScale, baseScale, t);
                    }
                    if (actTimer <= 0f) { if (sr) { sr.color = srBaseColor; transform.localScale = baseScale; } act = ActState.Idle; ResetHopCooldown(); }
                    break;
            }
        }

        void EnterTelegraphDash() { act = ActState.TelegraphDash; actTimer = telegraphTime; }
        void EnterDash()
        {
            act = ActState.Dash; actTimer = dashDuration;
            Vector2 cur = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : new Vector2(1f, 0.5f);
            float sx = Mathf.Abs(cur.x) < 0.2f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(cur.x);
            float sy = Mathf.Abs(cur.y) < 0.2f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(cur.y);
            dashDir = new Vector2(sx, sy).normalized;
            if (dashDir.sqrMagnitude < 0.5f) dashDir = new Vector2(1f, 1f).normalized;
        }
        void EnterRecoverDash() { act = ActState.RecoverDash; actTimer = dashRecoverTime; }

        void EnterTelegraphHop() { act = ActState.TelegraphHop; actTimer = hopTelegraphTime; }
        void EnterHop()
        {
            Vector2 dir = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : new Vector2(1f, 0.5f);
            int sx = (Mathf.Abs(dir.x) < 0.2f) ? 0 : (dir.x > 0 ? 1 : -1);
            int sy = (Mathf.Abs(dir.y) < 0.2f) ? 0 : (dir.y > 0 ? 1 : -1);
            if (sx == 0 && sy == 0) { sx = 1; sy = 0; }
            Vector2Int step = new(sx, sy);

            Vector2 target = posCell;
            for (int i = 1; i <= hopCells; i++)
            {
                Vector2 cand = posCell + (Vector2)(step * i);
                bool dummy = false;
                if (IsSolidAtCell(cand, ref dummy)) break;
                target = cand;
            }
            posCell = target;
            ApplyTransform();

            act = ActState.Hop;
        }
        void EnterRecoverHop() { act = ActState.RecoverHop; actTimer = hopRecoverTime; }

        Vector2 ComputeSteer(Vector2 currentDir, float t)
        {
            Vector2 steer = ComputeJitter(t) * 0.5f;

            float x = posCell.x, y = posCell.y;
            float minDx = Mathf.Min(Mathf.Abs(x - 1f), Mathf.Abs((GameConsts.GridW - 2f) - x));
            float minDy = Mathf.Min(Mathf.Abs(y - 1f), Mathf.Abs((GameConsts.GridH - 2f) - y));
            float d = Mathf.Min(minDx, minDy);
            if (stuckFrames >= 8 && d <= 2f)
            {
                Vector2 toCenter = new(GameConsts.GridW * 0.5f - x, GameConsts.GridH * 0.5f - y);
                steer += toCenter.normalized;
            }
            return steer;
        }
        Vector2 ComputeJitter(float t)
        {
            if (jitterAmplitude <= 0f || jitterHz <= 0f) return Vector2.zero;
            float px = (t + jitterPhaseX) * jitterHz * Mathf.PI * 2f;
            float py = (t + jitterPhaseY) * jitterHz * Mathf.PI * 2f * 0.87f;
            return new Vector2(Mathf.Sin(px), Mathf.Cos(py)) * jitterAmplitude;
        }

        bool CollidesAt(Vector2 p) => CollidesAt(p, out _);
        bool CollidesAt(Vector2 p, out bool hitTrail)
        {
            hitTrail = false;
            if (IsSolidAtCell(p, ref hitTrail)) return true;

            const int samples = 12;
            for (int i = 0; i < samples; i++)
            {
                float ang = (Mathf.PI * 2f) * (i / (float)samples);
                Vector2 s = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
                if (IsSolidAtCell(s, ref hitTrail)) return true;
            }
            return false;
        }

        bool IsSolidAtCell(Vector2 pCell, ref bool hitTrail)
        {
            int x = Mathf.RoundToInt(pCell.x), y = Mathf.RoundToInt(pCell.y);
            if (x < 0 || x >= GameConsts.GridW || y < 0 || y >= GameConsts.GridH) return true;
            var c = new Vector3Int(x, y, 0);
            if (wallTilemap && wallTilemap.HasTile(c)) return true;
            if (trailTilemap && trailTilemap.HasTile(c)) { hitTrail = true; return true; }
            return false;
        }

        void ApplyTransform()
        {
            var origin = gridRoot ? gridRoot.transform.position : Vector3.zero;
            Vector2 center = posCell + new Vector2(0.5f, 0.5f);
            transform.position = origin + (Vector3)center;
        }

        public void ResetEnemy()
        {
            posCell = new Vector2(GameConsts.GridW * 0.5f, GameConsts.GridH * 0.5f);
            var dir = Random.insideUnitCircle.normalized; if (dir == Vector2.zero) dir = new Vector2(1f, 0.5f);
            velCell = dir * baseSpeed;
            ApplyTransform();
            prevPosCell = posCell; stuckFrames = 0;
            jitterPhaseX = Random.value * 10f; jitterPhaseY = Random.value * 10f;
            act = ActState.Idle; dashMul = 1f; dashDir = Vector2.zero;
            if (sr) { sr.color = srBaseColor; transform.localScale = baseScale; }
            ResetDashCooldown(); ResetHopCooldown();
        }

        public void SetSpeedScale(float scale) => speedScale = Mathf.Max(0f, scale);
        public void ExternalNudge(Vector2 delta) => transform.position += (Vector3)delta;

        void OnEnable() { if (EnemyManager.Instance) EnemyManager.Instance.Register(this); }
        void OnDisable() { if (EnemyManager.Instance) EnemyManager.Instance.Unregister(this); }

        void ResetDashCooldown()
        {
            if (!enableDash) { nextDashCooldown = Mathf.Infinity; return; }
            float a = Mathf.Min(dashCooldownRange.x, dashCooldownRange.y);
            float b = Mathf.Max(dashCooldownRange.x, dashCooldownRange.y);
            nextDashCooldown = Random.Range(a, b);
        }
        void ResetHopCooldown()
        {
            if (!enableHop) { nextHopCooldown = Mathf.Infinity; return; }
            float a = Mathf.Min(hopCooldownRange.x, hopCooldownRange.y);
            float b = Mathf.Max(hopCooldownRange.x, hopCooldownRange.y);
            nextHopCooldown = Random.Range(a, b);
        }
    }
}
