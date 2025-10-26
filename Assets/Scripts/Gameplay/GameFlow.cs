using System;
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

        [Header("Stage Settings")]
        [SerializeField] private int startLives = 3;
        [SerializeField] private float timeLimitSec = 120f;

        [Header("Round Reset Options")]
        [Tooltip("���E���h�ĊJ���ɓG�������ʒu�֖߂����iOFF�Ȃ�G�͂��̂܂ܓ���������j")]
        [SerializeField] private bool resetEnemyOnRound = false;

        [Header("Round Start Slow (Enemies only)")]
        [SerializeField] private bool slowEnemyOnRoundStart = true;
        [SerializeField, Range(0.1f, 1f)] private float roundSlowMultiplier = 0.6f; // 60%
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

        void RefreshHUD()
        {
            if (!hud) return;

            hud.SetLives(lives);
            hud.SetTime(timeRemain);

            // CaptureSystem �� Fill% ����������Ă���ꍇ�̂ݍX�V�i���t���N�V�����œ��I�擾�j
            float? fill = TryGetFillPercentDynamic(captureSystem);
            if (fill.HasValue) hud.SetFill(fill.Value);
        }

        // ======= �C�x���g =======
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

        // �~�X�����F���o�������C�t���i�S�̃X���[�j�����Z�b�g
        System.Collections.IEnumerator CoMissAndReset()
        {
            if (player) player.PlayDeathVFX();

            OnLoseLife();
            if (lives == 0) yield break; // �Q�[���I�[�o�[

            yield return StartCoroutine(CoSlowMo(0.2f, 0.8f)); // �����ԂőS�̃X���[

            ResetRound(); // �v���C���[�������i�G�͐ݒ�ɏ]���j

            // ���E���h�J�n�X���[�i�G�̂݁j
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

            if (resetEnemyOnRound)
            {
                var em = EnemyManager.Instance;
                if (em) em.ResetAllEnemies();
                else
                {
                    // Unity 6.2 ���� API
                    var all = FindObjectsByType<SmallEnemy>(FindObjectsSortMode.None);
                    foreach (var e in all) e.ResetEnemy();
                }
            }
        }

        // ���E���h�J�n���ゾ���G���X���[
        System.Collections.IEnumerator CoEnemyRoundSlow()
        {
            if (!slowEnemyOnRoundStart) yield break;

            BroadcastSpeedScale(roundSlowMultiplier);
            yield return new WaitForSecondsRealtime(roundSlowDuration);
            BroadcastSpeedScale(1f);
        }

        void BroadcastSpeedScale(float scale)
        {
            var em = EnemyManager.Instance;
            if (em) em.SetAllEnemiesSpeedScale(scale);
            else
            {
                // Unity 6.2 ���� API
                var all = FindObjectsByType<SmallEnemy>(FindObjectsSortMode.None);
                foreach (var e in all) e.SetSpeedScale(scale);
            }
        }

        // �S�̃X���[�i���o�p�j
        System.Collections.IEnumerator CoSlowMo(float scale, float durationRealtime)
        {
            float prev = Time.timeScale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(durationRealtime);
            Time.timeScale = prev;
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

        // ==== CaptureSystem ���� Fill% ���g����΁h�擾�i���O�œ��I�Q�Ɓj ====
        float? TryGetFillPercentDynamic(object obj)
        {
            if (obj == null) return null;
            var t = obj.GetType();

            // public float FillPercent { get; } ��T��
            var prop = t.GetProperty("FillPercent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(float))
            {
                return (float)prop.GetValue(obj);
            }

            // public float GetFillPercent() ������
            var m = t.GetMethod("GetFillPercent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, Type.EmptyTypes, null);
            if (m != null && m.ReturnType == typeof(float))
            {
                return (float)m.Invoke(obj, null);
            }

            return null; // ������Ȃ����HUD�X�V�̓X�L�b�v
        }
    }
}
