# TODO — QixLike-Unity (Unity 6.2)

## 0. 完了済み / Done
- [x] Boundary（外周）を Boundary レイヤの Collider2D で構成（TilemapCollider2D + Composite / もしくは Box4枚）
- [x] Physics 2D レイヤマトリクス：Enemy × Enemy を OFF
- [x] 小型敵 SmallEnemy の基本挙動（反射・テレグラフDash/Hop など）
- [x] 分離用コンポーネント EnemySeparation2D の導入
- [x] GitHub リポジトリ公開および反映

## 1. 進行中 / In Progress
- [ ] SmallEnemy と分離の**整合**（グリッド移動との併用）
  - [ ] `EnemySeparation2D.LastRepel` を公開して **SmallEnemy.Update でステアにミックス**
  - [ ] rb.velocity による移動は使わず、グリッド移動のままにする（※現行方針）
  - [ ] 多体（5体/20体）での密集テスト：重なり/ブルブル/外周での抜けを確認
  - [ ] 調整ガイド  
        - separationForce 10→12〜14（押し返し）  
        - separationRadius 1.2→1.4（近づき具合）  
        - damping 0.25→0.30〜0.45（振動抑制）  
        - boundaryLock 0.70→0.80〜0.90（外周抑制）  
        - boundaryProbe 0.30→0.35〜0.45（外周検出）

## 2. 次の実装 / Next Up（M2: ゲーム化）
- [ ] ステージ定義（ScriptableObject）
  - [ ] `StageConfig`：制限時間 / 目標Fill% / 敵構成（Preset + Count + Spawn）
  - [ ] `EnemyPreset`：速度・ダッシュ/ホップ可否・分離パラメータ
- [ ] GameFlow（状態管理）
  - [ ] Countdown → Playing → Clear / GameOver
  - [ ] `CaptureSystem` と連携して Fill% 達成判定
- [ ] HUD/演出
  - [ ] Fill%/残機/タイマー表示
  - [ ] 結果パネル（Clear / GameOver / Next / Retry）
- [ ] 敵バリエーション
  - [ ] TypeB: Dash、TypeC: Hop、TypeD: 複合
  - [ ] 速度・難度スケールとプリセット調整

## 3. バグ/技術的負債 / Tech Debt
- [ ] 分離コスト最適化（OverlapCircleNonAlloc の半径/頻度の見直し）
- [ ] 角でのハマり対策とステア重みの上限
- [ ] 物理とグリッドの混在リスクの明文化（rb.velocity を使う系は封印）

## 4. リリース準備 / Milestone
- [ ] v0.2.0（M2 完了）
  - [ ] 受け入れ基準：  
        - 20体でも重なり・ワープ・ブルブルなし  
        - Fill%/時間/残機のゲームループ成立  
        - 3段階ステージが `StageConfig` だけで切替可能
- [ ] ドキュメント更新（AI_CONTEXT / README / CONTRIBUTING）
