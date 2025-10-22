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
        [SerializeField] private TextMeshProUGUI timeTextSmall;          // �g��Ȃ���Ζ�������OK
        [SerializeField] private TextMeshProUGUI livesTextOptional;      // �g��Ȃ��z��i�N�����Ɉ�x������\���j

        [Header("Lives (icons)")]
        [SerializeField] private RectTransform livesIconsRoot;
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private Vector2 iconSize = new Vector2(24, 24);
        [SerializeField] private int maxIcons = 10;

        [Header("Timer (bottom-center)")]
        [SerializeField] private TextMeshProUGUI timerBigText;           // TimerBigText �����蓖��

        private readonly List<Image> pool = new List<Image>();

        void Awake()
        {
            // �N������ LivesText ����x�����������i�늄�蓖�Ă������j
            if (livesTextOptional) livesTextOptional.gameObject.SetActive(false);
            // �����^�C�}�[�͊m���ɕ\��ON�ŊJ�n
            if (timerBigText) timerBigText.gameObject.SetActive(true);
        }

        public void UpdateHUD(float fill01, int lives, float timeRemain)
        {
            // ����
            if (fillText) fillText.text = $"Fill: {(fill01 * 100f):F1}%";
            if (timeTextSmall) timeTextSmall.text = $"Time: {Mathf.Max(0f, timeRemain):F1}s";

            // �c�@�A�C�R��
            if (livesIconsRoot && heartSprite)
            {
                lives = Mathf.Clamp(lives, 0, maxIcons);
                // �v�[���g��
                while (pool.Count < lives)
                {
                    var go = new GameObject("LifeIcon", typeof(RectTransform), typeof(Image));
                    var rt = (RectTransform)go.transform; rt.SetParent(livesIconsRoot, false); rt.sizeDelta = iconSize;
                    var img = go.GetComponent<Image>(); img.sprite = heartSprite; img.preserveAspect = true;
                    pool.Add(img);
                }
                // �\���ؑ�
                for (int i = 0; i < pool.Count; i++)
                {
                    bool visible = i < lives; var img = pool[i];
                    if (img.gameObject.activeSelf != visible) img.gameObject.SetActive(visible);
                    ((RectTransform)img.transform).sizeDelta = iconSize;
                }
            }

            // �������^�C�}�[�imm:ss�j
            if (timerBigText)
            {
                int total = Mathf.Max(0, Mathf.CeilToInt(timeRemain));
                int m = total / 60, s = total % 60;
                timerBigText.text = $"{m:00}:{s:00}";
            }
        }
    }
}
