using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpritePingPongUltra : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;                      // 5�R�}�i_0.._4�j������
    [SerializeField, Range(1f, 1000f)] private float framesPerSecond = 240f; // ������ł��グ���܂�
    [SerializeField] private bool useUnscaledTime = false;         // �ꎞ��~������������������ true

    private SpriteRenderer sr;
    private int[] path;   // ��: [0,1,2,3,4,3,2,1]
    private float phase;  // �o�߃t���[�����������Œ~��

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        BuildPath();
        if (path.Length > 0 && frames.Length > 0) sr.sprite = frames[path[0]];
    }

    void OnValidate() { BuildPath(); }

    void BuildPath()
    {
        if (frames == null || frames.Length == 0) { path = System.Array.Empty<int>(); return; }
        if (frames.Length == 1) { path = new int[] { 0 }; return; }
        int n = frames.Length;
        path = new int[n * 2 - 2];
        int k = 0;
        for (int i = 0; i < n; i++) path[k++] = i;     // 0..4
        for (int i = n - 2; i > 0; i--) path[k++] = i;     // 3..1 �ŐܕԂ�
    }

    public void SetSpeed(float fps) => framesPerSecond = fps;

    void Update()
    {
        if (path == null || path.Length == 0) return;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        phase += framesPerSecond * dt;                 // 1�b������ "framesPerSecond" �R�}�O�i
        int index = (int)phase % path.Length;          // �`��t���[�����Ɉˑ������u���ɃX�L�b�v��
        GetComponent<SpriteRenderer>().sprite = frames[path[index]];
        if (phase > 1_000_000f) phase = 0f;            // ���l�����剻���Ȃ��悤�Ɉꉞ�N���b�v
    }
}
