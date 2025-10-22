using QixLike.Core; // SceneLocator ���Q��
using QixLike.Enemies;
using UnityEngine;
using UnityEngine.Tilemaps;
using QixLike.Gameplay;   // �� �����ǉ�

namespace QixLike
{
    public class SmallEnemy : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private Grid gridRoot;
        [SerializeField] private Tilemap wallTilemap;      // �� WallTilemap
        [SerializeField] private Tilemap trailTilemap;     // �� TrailTilemap
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private PlayerController player;

        [Header("Move")]
        [SerializeField, Range(0.5f, 20f)] private float speed = 6f;   // ����x�i�Z��/�b�j
        [SerializeField, Range(0.1f, 0.49f)] private float radius = 0.35f;
        [SerializeField] private Vector2 initialDir = new Vector2(1f, 0.7f);

        [Header("Hit")]
        [SerializeField, Range(0f, 0.5f)] private float trailHitCooldown = 0.2f;
        [SerializeField, Range(0.1f, 0.6f)] private float playerRadius = 0.40f;

        [Header("Steering")]
        [SerializeField, Range(0.1f, 12f)] private float steerResponsiveness = 6f;
        [SerializeField, Range(0f, 1f)] private float jitterAmplitude = 0.20f;
        [SerializeField, Range(0.1f, 6f)] private float jitterHz = 1.3f;

        [Header("Anti-Stuck (only when actually stuck)")]
        [SerializeField, Range(0.5f, 6f)] private float edgeHelpRangeCells = 2f;
        [SerializeField, Range(3, 30)] private int stuckFramesThreshold = 8;
        [SerializeField, Range(0f, 3f)] private float edgeHelpStrength = 1.0f;

        [Header("Type B Dash")]
        [SerializeField] private bool enableDash = false;
        [SerializeField, Range(1.2f, 6f)] private float dashSpeedMultiplier = 2.8f;
        [SerializeField, Range(0.1f, 1.2f)] private float dashDuration = 0.6f;
        [SerializeField] private Vector2 dashCooldownRange = new Vector2(3f, 6f);
        [SerializeField, Range(0.05f, 1.0f)] private float telegraphTime = 0.35f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.6f, 0.1f, 1f);
        [SerializeField, Range(1f, 1.6f)] private float telegraphScale = 1.15f;
        [SerializeField, Range(0f, 0.3f)] private float telegraphJitterBoost = 0.1f;
        [SerializeField, Range(0.05f, 0.5f)] private float dashRecoverTime = 0.15f;

        [Header("Type C Hop (2-cell skip pseudo-teleport)")]
        [SerializeField] private bool enableHop = false;
        [SerializeField, Range(1, 4)] private int hopCells = 2;
        [SerializeField] private Vector2 hopCooldownRange = new Vector2(6f, 10f);
        [SerializeField, Range(0.05f, 1.0f)] private float hopTelegraphTime = 0.35f;
        [SerializeField] private Color hopTelegraphColor = new Color(0.4f, 0.85f, 1f, 1f);
        [SerializeField, Range(1f, 1.6f)] private float hopTelegraphScale = 1.12f;
        [SerializeField, Range(0.05f, 0.5f)] private float hopRecoverTime = 0.12f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        // �����^�C��
        private Vector2 posCell, velCell;
        private float baseSpeed, speedScale = 1f;
        private float trailHitTimer;
        private Vector2 prevPosCell; private int stuckFrames;
        private const float minMovePerFrame = 0.015f;
        float jitterPhaseX, jitterPhaseY;

        // �A�N�V������ԁiDash/ Hop ���ʁj
        private enum ActState { Idle, TelegraphDash, Dash, RecoverDash, TelegraphHop, Hop, RecoverHop }
        private ActState act = ActState.Idle;
        private float actTimer;            // ����Ԃ̎c�莞��
        private float nextDashCooldown;
        private float nextHopCooldown;
        private float dashMul = 1f;
        private Vector2 dashDir = Vector2.zero;

        public float RadiusWorld => radius;

        private SpriteRenderer sr; private Color srBaseColor = Color.white; private Vector3 baseScale = Vector3.one;

        // SmallEnemy �N���X�̒��ɒǉ�
        [SerializeField] private bool autoAssignSceneRefs = true;

        // ���� Awake() �̐擪�ŌĂԂ��A������ΐV�K�ɒǉ�
        void Awake()
        {
            if (autoAssignSceneRefs) AssignSceneRefs();
            // �E�E�E���Ȃ��̊����� Awake ����������΂��̉��ɑ�����OK
        }

        // �G�f�B�^��ŉE�N���b�N���j���[��������s�ł���悤�Ɂi�C�Ӂj
#if UNITY_EDITOR
        [ContextMenu("Auto Assign Refs (Editor)")]
        private void AutoAssignRefsInEditor()
        {
            AssignSceneRefs();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        void Start()
        {
            baseSpeed = speed;

            posCell = new Vector2(GameConsts.GridW * 0.5f, GameConsts.GridH * 0.5f);
            var dir0 = (initialDir.sqrMagnitude > 1e-4f) ? initialDir.normalized : new Vector2(1f, 0.5f);
            velCell = dir0 * (baseSpeed * speedScale);

            for (int i = 0; i < 16 && CollidesAt(posCell, out _); i++) posCell += Random.insideUnitCircle * 0.5f;
            prevPosCell = posCell; ApplyTransform();

            jitterPhaseX = Random.value * 10f; jitterPhaseY = Random.value * 10f;

            sr = GetComponent<SpriteRenderer>();
            if (sr) { srBaseColor = sr.color; baseScale = transform.localScale; }

            ResetDashCooldown(); ResetHopCooldown();
        }

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

        void Update()
        {
            trailHitTimer -= Time.deltaTime;

            // ��ԍX�V�i�A�N�V�����̗D��x�FDash �� Hop�j
            UpdateActionState();

            // ���x�E����
            var dir = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : new Vector2(1f, 0.5f);
            float sp = baseSpeed * speedScale * dashMul;

            // �ǎ��i�_�b�V�����E�z�b�v���͌Œ�j
            if (act == ActState.Dash || act == ActState.Hop)
            {
                // �Œ�����iDash�� dashDir�AHop���͏u�Ԉړ��Ȃ̂Œʏ�ړ��ցj
            } else
            {
                Vector2 steer = ComputeSteer(dir, Time.time);
                if (act == ActState.TelegraphDash && telegraphJitterBoost > 0f) steer += ComputeJitter(Time.time) * telegraphJitterBoost;
                Vector2 desired = (dir + steer); if (desired.sqrMagnitude < 1e-6f) desired = dir; desired.Normalize();
                float turn = Mathf.Clamp01(steerResponsiveness * Time.deltaTime);
                dir = Vector2.Lerp(dir, desired, turn).normalized;
            }
            velCell = dir * sp;

            // �ʒu�X�V�{���ˁ{�~�X�ʒm
            float dt = Time.deltaTime;
            Vector2 next = posCell + velCell * dt;

            if (!CollidesAt(next, out bool trailHitWhole))
            {
                posCell = next;
            } else
            {
                bool trailHitX, trailHitY;
                bool hitX = CollidesAt(new Vector2(posCell.x + velCell.x * dt, posCell.y), out trailHitX);
                bool hitY = CollidesAt(new Vector2(posCell.x, posCell.y + velCell.y * dt), out trailHitY);

                if (hitX) { velCell.x = -velCell.x; if (act == ActState.Dash) dashDir.x = -dashDir.x; }
                if (hitY) { velCell.y = -velCell.y; if (act == ActState.Dash) dashDir.y = -dashDir.y; }

                if ((trailHitWhole || trailHitX || trailHitY) && trailHitTimer <= 0f && gameFlow)
                {
                    if (debugLogs) Debug.Log("Enemy hit TRAIL");
                    gameFlow.OnEnemyHitTrail();
                    trailHitTimer = trailHitCooldown;
                }

                // �����߂�
                next = posCell + velCell * dt;
                if (!CollidesAt(next, out _)) posCell = next;
                else
                {
                    Vector2 n = velCell.normalized;
                    for (int i = 0; i < 8; i++) { posCell -= n * 0.02f; if (!CollidesAt(posCell, out _)) break; }
                }
            }

            // �X�^�b�N���o
            float moved = (posCell - prevPosCell).magnitude;
            if (moved < minMovePerFrame) stuckFrames++; else stuckFrames = 0;
            prevPosCell = posCell;

            ApplyTransform();

            // �v���C���[�{�̃q�b�g�i�`�撆�̂݁j
            if (player && player.IsDrawing && gameFlow && trailHitTimer <= 0f)
            {
                Vector2 enemyW = (Vector2)transform.position; Vector2 playerW = player.GetWorldCenter();
                float sumR = radius + playerRadius;
                if ((enemyW - playerW).sqrMagnitude <= sumR * sumR) { if (debugLogs) Debug.Log("Enemy hit PLAYER (drawing)"); gameFlow.OnPlayerHitDuringDraw(); trailHitTimer = trailHitCooldown; }
            }
        }

        // ?? Action ��ԋ@�B�iDash/ Hop�j??
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
                    if (actTimer <= 0f) EnterDash(); break;

                case ActState.Dash:
                    actTimer -= dt; dashMul = dashSpeedMultiplier;
                    if (sr) sr.color = Color.Lerp(srBaseColor, telegraphColor, 0.35f);
                    if (actTimer <= 0f) EnterRecoverDash(); break;

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
                    if (actTimer <= 0f) EnterHop(); break;

                case ActState.Hop:
                    // Hop �͏u�Ԉړ��Ȃ̂ł����͂قڒʂ�Ȃ��iEnterHop���ňʒu���X�V�j
                    EnterRecoverHop(); break;

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
            // �i�s�����̕����ŁA�ő� hopCells ��������ցg�u�Ԉړ��h�B�r���̐��ׂ͌��ł�OK�A���n�_��solid�s�B
            Vector2 dir = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : new Vector2(1f, 0.5f);
            int sx = (Mathf.Abs(dir.x) < 0.2f) ? 0 : (dir.x > 0f ? 1 : -1);
            int sy = (Mathf.Abs(dir.y) < 0.2f) ? 0 : (dir.y > 0f ? 1 : -1);
            if (sx == 0 && sy == 0) { sx = 1; sy = 0; }
            Vector2Int step = new Vector2Int(sx, sy);

            Vector2 target = posCell;
            for (int i = 1; i <= hopCells; i++)
            {
                Vector2 cand = posCell + (Vector2)(step * i);
                bool dummy = false;                      // �� �����ǉ�
                if (IsSolidAtCell(cand, ref dummy))      //   �ȍ~�͂��̂܂�
                    break;
                target = cand;
            }
            posCell = target;  // �u�Ԉړ�
            ApplyTransform();

            act = ActState.Hop;
        }
        void EnterRecoverHop() { act = ActState.RecoverHop; actTimer = hopRecoverTime; }

        // ?? Steering�i�ʏ펞�̂݁j??
        Vector2 ComputeSteer(Vector2 currentDir, float t)
        {
            Vector2 steer = Vector2.zero;
            steer += ComputeJitter(t) * 0.5f; // �����W�b�^�[�i��߁j
            if (stuckFrames >= stuckFramesThreshold && edgeHelpStrength > 0f)
            {
                float x = posCell.x, y = posCell.y;
                float minDx = Mathf.Min(Mathf.Abs(x - 1f), Mathf.Abs((GameConsts.GridW - 2f) - x));
                float minDy = Mathf.Min(Mathf.Abs(y - 1f), Mathf.Abs((GameConsts.GridH - 2f) - y));
                float d = Mathf.Min(minDx, minDy);
                if (d <= edgeHelpRangeCells)
                {
                    Vector2 toCenter = new Vector2(GameConsts.GridW * 0.5f - x, GameConsts.GridH * 0.5f - y).normalized;
                    steer += toCenter * edgeHelpStrength;
                }
            }
            return steer;
        }
        Vector2 ComputeJitter(float t)
        {
            if (jitterAmplitude <= 0f || jitterHz <= 0f) return Vector2.zero;
            float phaseX = (t + jitterPhaseX) * jitterHz * Mathf.PI * 2f;
            float phaseY = (t + jitterPhaseY) * jitterHz * Mathf.PI * 2f * 0.87f;
            return new Vector2(Mathf.Sin(phaseX), Mathf.Cos(phaseY)) * jitterAmplitude;
        }

        void ApplyTransform()
        {
            var origin = gridRoot ? gridRoot.transform.position : Vector3.zero;
            Vector2 center = posCell + new Vector2(0.5f, 0.5f);
            transform.position = origin + (Vector3)center;
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

        public void ResetEnemy()
        {
            posCell = new Vector2(GameConsts.GridW * 0.5f, GameConsts.GridH * 0.5f);
            var dir = Random.insideUnitCircle.normalized; if (dir == Vector2.zero) dir = new Vector2(1f, 0.5f);
            velCell = dir * (baseSpeed * speedScale);
            ApplyTransform();
            prevPosCell = posCell; stuckFrames = 0;
            jitterPhaseX = Random.value * 10f; jitterPhaseY = Random.value * 10f;
            act = ActState.Idle; dashMul = 1f; dashDir = Vector2.zero;
            if (sr) { sr.color = srBaseColor; transform.localScale = baseScale; }
            ResetDashCooldown(); ResetHopCooldown();
        }

        // ���E���h�X���[�{��
        public void SetSpeedScale(float scale) { speedScale = Mathf.Max(0f, scale); }

        /// <summary>�����\���o����̔��������߂��BTransform�����������܂��B</summary>
        public void ExternalNudge(Vector2 delta)
        {
            transform.position += (Vector3)delta;
            // ���������Łu�O�t���[���ʒu�v���L���b�V�����Ă���ꍇ�́A�����œ��l�ɕ␳����OK�B
            // ��: _pos += delta;
        }

        /// <summary>�����������̌y�����ˁB�@�������������������������B</summary>
        public void ExternalBounce(Vector2 normal, float damping)
        {
            // ���x�x�N�g����i�s�����x�N�g�����������Ȃ�A���˂��ۂ����������ς���Ǝ��R�ł��B
            // ��: dir = Vector2.Reflect(dir, normal).normalized * (1f - damping);
            // ���x������������Ă��Ȃ��݌v�Ȃ�A�������Ȃ��Ă�OK�i�����ڂ̉����߂������ŏ\���j�B
        }

        // 3) �o�^/����
        void OnEnable()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.Register(this);
        }
        void OnDisable()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.Unregister(this);
        }

        private void AssignSceneRefs()
        {
            // ��������u��Ȃ�v���߂�B���Ɏ蓮���蓖�čς݂Ȃ�㏑�����܂���B
            if (gridRoot == null) gridRoot = SceneLocator.Grid ?? FindFirstObjectByType<Grid>();
            if (wallTilemap == null) wallTilemap = SceneLocator.Wall ?? FindTilemapByName("WallTilemap");
            if (trailTilemap == null) trailTilemap = SceneLocator.Trail ?? FindTilemapByName("TrailTilemap");
            if (gameFlow == null) gameFlow = SceneLocator.Flow ?? FindFirstObjectByType<Gameplay.GameFlow>();
            if (player == null) player = SceneLocator.Player ?? FindFirstObjectByType<Gameplay.PlayerController>();
        }

        private static Tilemap FindTilemapByName(string name)
        {
            foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                if (tm != null && tm.name == name) return tm;
            return null;
        }

        private void AssignSceneRefs()
        {
            if (gridRoot == null) gridRoot = SceneLocator.Grid ?? FindFirstObjectByType<Grid>();
            if (wallTilemap == null) wallTilemap = SceneLocator.Wall ?? FindTilemapByName("WallTilemap");
            if (trailTilemap == null) trailTilemap = SceneLocator.Trail ?? FindTilemapByName("TrailTilemap");

            // ������ �������C���iGameplay. ���O���j
            if (gameFlow == null) gameFlow = SceneLocator.Flow ?? FindFirstObjectByType<GameFlow>();
            if (player == null) player = SceneLocator.Player ?? FindFirstObjectByType<PlayerController>();
        }
    }
}
