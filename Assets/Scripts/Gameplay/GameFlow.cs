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
        [Tooltip("ラウンド再開時に敵を初期位置へ戻すか（OFFなら敵はそのまま動き続ける）")]
        [SerializeField] private bool resetEnemyOnRound = false;   // ← 既定OFF

        [Header("Round Start Slow (Enemies only)")]
        [SerializeField] private bool slowEnemyOnRoundStart = true;
        [SerializeField, Range(0.1f, 1f)] private float roundSlowMultiplier = 0.6f; // 0.6=60%
        [SerializeField] private float roundSlowDuration = 5f;                     // 5秒

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

            // ゲーム開始直後のスロー（敵のみ）
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

        // ?? ミス発火（線に触れた／描画中に本体接触）??
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

        // ?? ミス処理：演出→減ライフ→スロー→リセット ?? 
        System.Collections.IEnumerator CoMissAndReset()
        {
            if (player) player.PlayDeathVFX();             // 破片演出

            OnLoseLife();
            if (lives == 0) yield break;                   // ゲームオーバー

            yield return StartCoroutine(CoSlowMo(0.2f, 0.8f)); // 全体スロー（実時間）

            ResetRound();                                  // プレイヤーだけ初期化（敵は設定に従う）

            // ラウンド開始スロー（敵のみ）はそのまま適用
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
            if (resetEnemyOnRound && enemy) enemy.ResetEnemy(); // ← フラグで切替
        }

        // ?? ラウンド開始直後だけ敵をスロー ?? 
        System.Collections.IEnumerator CoEnemyRoundSlow()
        {
            if (!slowEnemyOnRoundStart || enemy == null) yield break;
            enemy.SetSpeedScale(roundSlowMultiplier);
            yield return new WaitForSecondsRealtime(roundSlowDuration);
            enemy.SetSpeedScale(1f);
        }

        // ?? 全体スロー（演出用）??
        System.Collections.IEnumerator CoSlowMo(float scale, float durationRealtime)
        {
            float prev = Time.timeScale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(durationRealtime);
            Time.timeScale = paused ? 0f : 1f;
        }

        // ?? HUD & 基本操作 ?? 
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
