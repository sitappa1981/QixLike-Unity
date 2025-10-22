using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpritePingPong : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Range(4f, 60f)] private float fps = 12f;   // �� �X���C�_�[
    [SerializeField, Range(0.25f, 4f)] private float speedMultiplier = 1f; // �� �{��

    private SpriteRenderer sr;
    private int idx = 0, dir = +1;
    private float acc;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0) sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        float effectiveFps = Mathf.Max(1f, fps) * speedMultiplier;
        acc += Time.deltaTime;
        float step = 1f / effectiveFps;
        if (acc >= step)
        {
            acc -= step;
            idx += dir;
            if (idx >= frames.Length - 1) { idx = frames.Length - 1; dir = -1; } else if (idx <= 0) { idx = 0; dir = +1; }
            sr.sprite = frames[idx];
        }
    }

    // ��Ԃɉ����ĊO������ς��������p�i��F�`�撆���������j
    public void SetSpeedMultiplier(float mul) => speedMultiplier = mul;
}
