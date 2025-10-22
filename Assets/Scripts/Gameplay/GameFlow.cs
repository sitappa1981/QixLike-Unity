using UnityEngine;
using UnityEngine.SceneManagement;

namespace QixLike
{
    public class GameFlow : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private CaptureSystem captureSystem;
        [SerializeField] private GameHUD hud;
        [SerializeField] private PlayerController player;
        [SerializeField] private SmallEnemy enemy;

        [Header("Stage Settings")]
        [SerializeField] private int startLives = 3;
        [SerializeField] private float timeLimitSec = 120f;

        [Header("Round Reset Options")]
        [Tooltip("���E���h�ĊJ���ɓG�������ʒu�֖߂����iOFF�Ȃ�G�͂��̂܂ܓ���������j")]
        [SerializeField] private bool resetEnemyOnRound = false;   // �� ����OFF

        [Header("Round Start Slow (Enemies only)")]
        [SerializeField] private bool slowEnemyOnRoundStart = true;
        [SerializeField, Range(0.1f, 1f)] private float roundSlowMultiplier = 0.6f; // 0.6=60%
        [SerializeField] private float roundSlowDuration = 5f;                     // 5�b

        private int lives;
        private float timeRemain;
        private bool paused;

        void Start()
        {
            lives = startLives;
            timeRemain = timeLimitSec;
            Time.timeScale = 1f;
            paused = false;
            RefreshHUD();

            // �Q�[���J�n����̃X���[�i�G�̂݁j
            StartCoroutine(CoEnemyRoundSlow());
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.pKey.wasPressedThisFrame) TogglePause();
                if (kb.rKey.wasPressedThisFrame) ReloadScene();
            }

            if (paused) return;

            timeRemain -= Time.deltaTime;
            if (timeRemain <= 0f)
            {
                timeRemain = 0f;
                TogglePause(true);
                Debug.Log("Time Up!");
            }
            RefreshHUD();
        }

        // ?? �~�X���΁i���ɐG�ꂽ�^�`�撆�ɖ{�̐ڐG�j??
        public void OnEnemyHitTrail()
        {
            if (paused) return;
            StartCoroutine(CoMissAndReset());
        }
        public void OnPlayerHitDuringDraw()
        {
            if (paused) return;
            StartCoroutine(CoMissAndReset());
        }

        // ?? �~�X�����F���o�������C�t���X���[�����Z�b�g ?? 
        System.Collections.IEnumerator CoMissAndReset()
        {
            if (player) player.PlayDeathVFX();             // �j�Љ��o

            OnLoseLife();
            if (lives == 0) yield break;                   // �Q�[���I�[�o�[

            yield return StartCoroutine(CoSlowMo(0.2f, 0.8f)); // �S�̃X���[�i�����ԁj

            ResetRound();                                  // �v���C���[�����������i�G�͐ݒ�ɏ]���j

            // ���E���h�J�n�X���[�i�G�̂݁j�͂��̂܂ܓK�p
            StartCoroutine(CoEnemyRoundSlow());
        }

        public void OnLoseLife()
        {
            lives = Mathf.Max(0, lives - 1);
            RefreshHUD();
            if (lives == 0)
            {
                TogglePause(true);
                Debug.Log("Game Over");
            }
        }

        void ResetRound()
        {
            if (player) player.RoundReset();
            if (resetEnemyOnRound && enemy) enemy.ResetEnemy(); // �� �t���O�Őؑ�
        }

        // ?? ���E���h�J�n���ゾ���G���X���[ ?? 
        System.Collections.IEnumerator CoEnemyRoundSlow()
        {
            if (!slowEnemyOnRoundStart || enemy == null) yield break;
            enemy.SetSpeedScale(roundSlowMultiplier);
            yield return new WaitForSecondsRealtime(roundSlowDuration);
            enemy.SetSpeedScale(1f);
        }

        // ?? �S�̃X���[�i���o�p�j??
        System.Collections.IEnumerator CoSlowMo(float scale, float durationRealtime)
        {
            float prev = Time.timeScale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(durationRealtime);
            Time.timeScale = paused ? 0f : 1f;
        }

        // ?? HUD & ��{���� ?? 
        void RefreshHUD()
        {
            float fill = captureSystem ? captureSystem.FillRatio : 0f;
            if (hud) hud.UpdateHUD(fill, lives, timeRemain);
        }

        void TogglePause(bool forcePause = false)
        {
            paused = forcePause ? true : !paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        void ReloadScene()
        {
            Time.timeScale = 1f;
            var cur = SceneManager.GetActiveScene();
            SceneManager.LoadScene(cur.buildIndex);
        }
    }
}
