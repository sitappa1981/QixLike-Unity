using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QixLike
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Texts (top-left)")]
        [SerializeField] private TextMeshProUGUI fillText;
        [SerializeField] private TextMeshProUGUI timeTextSmall;          // 使わなければ未割当でOK
        [SerializeField] private TextMeshProUGUI livesTextOptional;      // 使わない想定（起動時に一度だけ非表示）

        [Header("Lives (icons)")]
        [SerializeField] private RectTransform livesIconsRoot;
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private Vector2 iconSize = new Vector2(24, 24);
        [SerializeField] private int maxIcons = 10;

        [Header("Timer (bottom-center)")]
        [SerializeField] private TextMeshProUGUI timerBigText;           // TimerBigText を割り当て

        private readonly List<Image> pool = new List<Image>();

        void Awake()
        {
            // 起動時に LivesText を一度だけ無効化（誤割り当てを避ける）
            if (livesTextOptional) livesTextOptional.gameObject.SetActive(false);
            // 下部タイマーは確実に表示ONで開始
            if (timerBigText) timerBigText.gameObject.SetActive(true);
        }

        public void UpdateHUD(float fill01, int lives, float timeRemain)
        {
            // 左上
            if (fillText) fillText.text = $"Fill: {(fill01 * 100f):F1}%";
            if (timeTextSmall) timeTextSmall.text = $"Time: {Mathf.Max(0f, timeRemain):F1}s";

            // 残機アイコン
            if (livesIconsRoot && heartSprite)
            {
                lives = Mathf.Clamp(lives, 0, maxIcons);
                // プール拡張
                while (pool.Count < lives)
                {
                    var go = new GameObject("LifeIcon", typeof(RectTransform), typeof(Image));
                    var rt = (RectTransform)go.transform; rt.SetParent(livesIconsRoot, false); rt.sizeDelta = iconSize;
                    var img = go.GetComponent<Image>(); img.sprite = heartSprite; img.preserveAspect = true;
                    pool.Add(img);
                }
                // 表示切替
                for (int i = 0; i < pool.Count; i++)
                {
                    bool visible = i < lives; var img = pool[i];
                    if (img.gameObject.activeSelf != visible) img.gameObject.SetActive(visible);
                    ((RectTransform)img.transform).sizeDelta = iconSize;
                }
            }

            // 下中央タイマー（mm:ss）
            if (timerBigText)
            {
                int total = Mathf.Max(0, Mathf.CeilToInt(timeRemain));
                int m = total / 60, s = total % 60;
                timerBigText.text = $"{m:00}:{s:00}";
            }
        }
    }
}
