using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeparation2D : MonoBehaviour
{
    [Header("Separation")]
    [SerializeField] float separationRadius = 1.2f;        // �ߖT����̔��a
    [SerializeField] float separationForce = 10f;           // �����Ԃ��̋����i�����x�x�[�X�j
    [SerializeField, Range(0f, 1f)] float damping = 0.25f;   // 0�ŉs��/1�œ݂�
    [SerializeField] LayerMask enemyMask;                   // Enemy ���C��

    [Header("Boundary Lock")]
    [SerializeField] LayerMask boundaryMask;                // �O���̃R���C�_�[���C��
    [SerializeField] float boundaryProbe = 0.30f;           // �ߖT�q�b�g����
    [SerializeField, Range(0f, 1f)] float boundaryLock = 0.70f; // �O���������̗}����

    [Header("Move Base")]
    [SerializeField] float baseSpeed = 2.2f;                // AI���͂��������̖ڈ����x

    Rigidbody2D rb;
    readonly Collider2D[] hits = new Collider2D[32];
    Vector2 desiredVel;                                     // AI����n������]���x

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // ��]���x�����Ă��Ȃ���΁A����ێ� or ���葬�x
        Vector2 vDesired =
            desiredVel.sqrMagnitude > 0.001f ? desiredVel :
            rb.linearVelocity.sqrMagnitude > 0.001f ? rb.linearVelocity :
            Vector2.right * baseSpeed;

        vDesired = vDesired.normalized * Mathf.Max(baseSpeed, vDesired.magnitude);

        // �ߖT�̓G����̔���
        Vector2 repel = ComputeRepulsion();

        // �O���t�߂ŊO�������x��}����
        if (TryGetBoundaryNormal(out Vector2 outwardNormal))
        {
            float outward = Vector2.Dot(vDesired + repel, outwardNormal);
            if (outward > 0f) repel -= outwardNormal * outward * boundaryLock;
        }

        // ���x�X�V�i�Ȃ߂炩�Ɂj
        Vector2 vTarget = vDesired + repel * Time.fixedDeltaTime;
        rb.linearVelocity = Vector2.Lerp(vTarget, rb.linearVelocity, damping);

        // ���t���[���p�Ƀ��Z�b�g�i��FixedUpdate��AI����n���Ă��炤�j
        desiredVel = Vector2.zero;
    }

    Vector2 ComputeRepulsion()
    {
        // �񐄏�API����OverlapCircle�ɕύX
        int n = 0;
        Collider2D[] found = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyMask);
        n = found.Length;
        if (n == 0) return Vector2.zero;

        Vector2 self = rb.position;
        Vector2 sum = Vector2.zero;
        int cnt = 0;

        for (int i = 0; i < n; i++)
        {
            var h = found[i];
            if (h == null || h.attachedRigidbody == rb) continue;

            Vector2 toSelf = self - (Vector2)h.transform.position;
            float d = toSelf.magnitude;
            if (d < 0.0001f) continue;

            // �߂��قǋ����E�F�C�g�ismoothstep �t�j
            float t = Mathf.Clamp01(d / separationRadius);
            float w = 1f - (t * t * (3f - 2f * t));
            sum += (toSelf / d) * w;
            cnt++;
        }

        if (cnt == 0) return Vector2.zero;
        Vector2 dir = sum / cnt;
        return dir.normalized * separationForce; // �����x�x�N�g��
    }

    bool TryGetBoundaryNormal(out Vector2 outwardNormal)
    {
        outwardNormal = Vector2.zero;
        Vector2 p = rb.position;
        var dirs = new Vector2[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        foreach (var d in dirs)
        {
            var hit = Physics2D.Raycast(p, d, boundaryProbe, boundaryMask);
            if (hit.collider)
            {
                outwardNormal = (p - hit.point).normalized; // �O�����猩�ĊO����
                return true;
            }
        }
        return false;
    }

    /// <summary> ����AI����u��]���x�v��FixedUpdate�œn�� </summary>
    public void SetDesiredVelocity(Vector2 vel) => desiredVel = vel;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity);
    }
#endif
}
