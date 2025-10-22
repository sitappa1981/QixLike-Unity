using UnityEngine;
using UnityEngine.Tilemaps;

namespace QixLike
{
    public class TrailBlinker : MonoBehaviour
    {
        [SerializeField] private Tilemap trailTilemap;
        [SerializeField] private Color onColor = new Color(0f, 1f, 1f, 1f);     // シアン
        [SerializeField] private Color offColor = new Color(0f, 1f, 1f, 0.35f); // 薄く
        [SerializeField, Range(1f, 30f)] private float blinkHz = 8f;

        private bool _active = false;
        private float _t;
        private bool _phase;

        public void SetActive(bool v)
        {
            _active = v;
            if (!trailTilemap) return;
            // 非アクティブ時は常に濃い色に戻す
            if (!v) trailTilemap.color = onColor;
        }

        void Update()
        {
            if (!_active || !trailTilemap) return;
            _t += Time.deltaTime * blinkHz;          // blinkHz = 1 なら 1秒で1サイクル
            float t = 0.5f + 0.5f * Mathf.Sin(_t * Mathf.PI * 2f); // 0..1
            trailTilemap.color = Color.Lerp(offColor, onColor, t);
        }

    }
}
