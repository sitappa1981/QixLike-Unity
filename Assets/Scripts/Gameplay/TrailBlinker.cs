using UnityEngine;
using UnityEngine.Tilemaps;

namespace QixLike
{
    public class TrailBlinker : MonoBehaviour
    {
        [SerializeField] private Tilemap trailTilemap;
        [SerializeField] private Color onColor = new Color(0f, 1f, 1f, 1f);     // �V�A��
        [SerializeField] private Color offColor = new Color(0f, 1f, 1f, 0.35f); // ����
        [SerializeField, Range(1f, 30f)] private float blinkHz = 8f;

        private bool _active = false;
        private float _t;
        private bool _phase;

        public void SetActive(bool v)
        {
            _active = v;
            if (!trailTilemap) return;
            // ��A�N�e�B�u���͏�ɔZ���F�ɖ߂�
            if (!v) trailTilemap.color = onColor;
        }

        void Update()
        {
            if (!_active || !trailTilemap) return;
            _t += Time.deltaTime * blinkHz;          // blinkHz = 1 �Ȃ� 1�b��1�T�C�N��
            float t = 0.5f + 0.5f * Mathf.Sin(_t * Mathf.PI * 2f); // 0..1
            trailTilemap.color = Color.Lerp(offColor, onColor, t);
        }

    }
}
