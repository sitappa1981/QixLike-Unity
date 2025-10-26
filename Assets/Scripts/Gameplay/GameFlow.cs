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
        [Tooltip("ラウンド再開時に敵を初期位置へ戻すか（OFFなら敵はそのまま動き続ける）")]
        [SerializeField] private bool resetEnemyOnRound = false;

        [Header("Round Start Slow (Enemies only)")]
        [SerializeField] private bool slowEnemyOnRoundStart = true;
        [SerializeField, Range(0.1f, 1f)] private float roundSlowMultiplier = 0.6f; // 60%
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

        void RefreshHUD()
        {
            if (!hud) return;

            hud.SetLives(lives);
            hud.SetTime(timeRemain);

            // CaptureSystem の Fill% が実装されている場合のみ更新（リフレクションで動的取得）
            float? fill = TryGetFillPercentDynamic(captureSystem);
            if (fill.HasValue) hud.SetFill(fill.Value);
        }

        // ======= イベント =======
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

        // ミス処理：演出→減ライフ→（全体スロー）→リセット
        System.Collections.IEnumerator CoMissAndReset()
        {
            if (player) player.PlayDeathVFX();

            OnLoseLife();
            if (lives == 0) yield break; // ゲームオーバー

            yield return StartCoroutine(CoSlowMo(0.2f, 0.8f)); // 実時間で全体スロー

            ResetRound(); // プレイヤー初期化（敵は設定に従う）

            // ラウンド開始スロー（敵のみ）
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
                    // Unity 6.2 推奨 API
                    var all = FindObjectsByType<SmallEnemy>(FindObjectsSortMode.None);
                    foreach (var e in all) e.ResetEnemy();
                }
            }
        }

        // ラウンド開始直後だけ敵をスロー
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
                // Unity 6.2 推奨 API
                var all = FindObjectsByType<SmallEnemy>(FindObjectsSortMode.None);
                foreach (var e in all) e.SetSpeedScale(scale);
            }
        }

        // 全体スロー（演出用）
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

        // ==== CaptureSystem から Fill% を“あれば”取得（名前で動的参照） ====
        float? TryGetFillPercentDynamic(object obj)
        {
            if (obj == null) return null;
            var t = obj.GetType();

            // public float FillPercent { get; } を探す
            var prop = t.GetProperty("FillPercent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(float))
            {
                return (float)prop.GetValue(obj);
            }

            // public float GetFillPercent() も許可
            var m = t.GetMethod("GetFillPercent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, Type.EmptyTypes, null);
            if (m != null && m.ReturnType == typeof(float))
            {
                return (float)m.Invoke(obj, null);
            }

            return null; // 見つからなければHUD更新はスキップ
        }
    }
}
