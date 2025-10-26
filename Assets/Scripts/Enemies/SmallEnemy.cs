using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class SmallEnemy : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] float baseSpeed = 2.2f;        // ��{���x
    [SerializeField] float speedScale = 1.0f;       // ��Փx��̍�
    [SerializeField] float dashMul = 1.0f;        // �_�b�V�����̔{���i�ʏ�=1�j

    [Header("Steering (�����t��)")]
    [SerializeField, Range(0f, 1f)] float separationSteer = 0.6f; // �����x�N�g���̍����
    [SerializeField] float noiseClamp = 0.02f;                    // �����m�C�Y�i���ˊp�̗h�炬�j
    [SerializeField] LayerMask wallMask;                          // �O����ǂ̃��C��
    [SerializeField] float reflectProbe = 0.2f;                   // ���˃��C�̎˒�

    // �O���b�h�ړ��i= �����ł͂Ȃ� transform �𒼐ړ������j
    Vector2 posCell;   // �ʒu�i�Z��/���[���h�ǂ���ł��^�pOK�j
    Vector2 velCell;   // ���x�x�N�g���i�����~���x�j

    Rigidbody2D rb;
    EnemySeparation2D sep;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sep = GetComponent<EnemySeparation2D>();

        // �����͈ړ��Ɏg��Ȃ����A���S���ݒ�
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        posCell = transform.position;
        velCell = Vector2.right * baseSpeed; // �������i�C�Ӂj
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ---- 1) ���݂̑O���i���x������ΐ��K���A������ΉE�����j ----
        Vector2 forward = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : Vector2.right;

        // ---- 2) �ǃq�b�g�Ŕ��� ----
        forward = ReflectIfHit(forward);

        // ---- 3) �X�e�A�i���� + �����m�C�Y�j�������čŏI������ ----
        Vector2 steer = ComputeSteer(forward, Time.time);
        Vector2 dir = (forward + steer).normalized;

        // ---- 4) �����x���Z�o ----
        float sp = baseSpeed * speedScale * dashMul;

        // ---- 5) ���x�x�N�g���X�V�i�O���b�h�ړ��j ----
        velCell = dir * sp;

        // ---- 6) �ʒu��i�߂� transform �ɔ��f ----
        posCell += velCell * dt;
        ApplyTransform(posCell);
    }

    void FixedUpdate()
    {
        // �� B�āFUpdate �Ŋm�肵���u�����~���x�i= velCell�j�v�����̂܂ܕ����֓n��
        if (sep != null)
        {
            sep.SetDesiredVelocity(velCell);
        }

        // ���ӁFrb.velocity �͏��������Ȃ��i�ړ��̓O���b�h����̂��߁j
    }

    // ---- �X�e�A�i�����t���j�F���� + �����m�C�Y ----
    Vector2 ComputeSteer(Vector2 forward, float t)
    {
        Vector2 result = Vector2.zero;

        // �����iLastRepel�j������փ~�b�N�X
        if (sep != null && sep.LastRepel.sqrMagnitude > 1e-5f)
        {
            result += sep.LastRepel.normalized * separationSteer;
        }

        // �����m�C�Y�i���ˊp�̗h�炬�j
        if (noiseClamp > 0f)
        {
            float nx = (Mathf.PerlinNoise(t * 1.3f, 0.123f) - 0.5f) * 2f * noiseClamp;
            float ny = (Mathf.PerlinNoise(0.456f, t * 1.7f) - 0.5f) * 2f * noiseClamp;
            result += new Vector2(nx, ny);
        }

        return result;
    }

    // ---- �O���̕ǂŔ��� ----
    Vector2 ReflectIfHit(Vector2 forward)
    {
        if (reflectProbe <= 0f) return forward;

        Vector2 p = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(p, forward, reflectProbe, wallMask);
        if (hit.collider)
        {
            Vector2 r = Vector2.Reflect(forward, hit.normal).normalized;

            // ���˃x�N�g���ɂ������m�C�Y
            if (noiseClamp > 0f)
            {
                float e = (Mathf.PerlinNoise(Time.time * 1.9f, 0.89f) - 0.5f) * 2f * noiseClamp;
                r = (r + new Vector2(e, -e)).normalized;
            }
            return r;
        }
        return forward;
    }

    // ---- �ʒu�K�p�i�O���b�h�����[���h�j ----
    void ApplyTransform(Vector2 pos)
    {
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // �i�s�����̉����ivelCell �x�[�X�j
        Vector2 dir = (velCell.sqrMagnitude > 1e-6f) ? velCell.normalized : Vector2.right;

        Gizmos.color = Color.cyan;  // ����
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)dir * 0.6f);

        Gizmos.color = Color.green; // ���x�̖ڈ�
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(dir * 0.4f));
    }

    public void ResetEnemy()
    {
        // �����ŏ����ʒu���Ԃɖ߂�����������
        // ��: transform.position = �����ʒu;
        // �K�v�ɉ����đ��̏�Ԃ����Z�b�g
    }
#endif
}
