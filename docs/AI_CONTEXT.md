# AI_CONTEXT — QixLike-Unity

## 概要
- タイトル: QixLike-Unity
- 目的: Qix/ギャルズパニック系の陣取りを Unity 6.2（URP, Pixel Perfect）で実装
- Unity: 6.2.0f1 相当 / URP / Pixel Perfect Camera（Assets Pixels Per Unit = 16）

## シーン/レイヤ構成
- `Grid`
  - `WallTilemap` … **Boundary レイヤ**（TilemapCollider2D + Composite[Outlines]）
  - `OverlayTilemap` … 見た目オーバーレイ（コライダなし）
  - `TrailTilemap` … プレイヤーの引き線（トレイル判定に使用）
- `CanvasHUD` … Fill%、残機、タイマー、結果パネル
- レイヤ: `Default / Enemy / Boundary`（重要：**Enemy × Enemy 衝突OFF**）

## 主なスクリプト/システム
- `SmallEnemy` … 敵の基礎挙動（グリッド座標 `posCell/velCell` を更新し、`transform.position` を直接反映）
- `EnemySeparation2D` … 近傍回避（分離）。**LastRepel** を公開し、SmallEnemy 側のステアにミックス
- `GameFlow` … 状態管理（M2で導入予定：Countdown/Playing/Clear/GameOver）
- `CaptureSystem` … 面積占有率/Fill%（M2で導入予定）
- `EnemyPreset`（SO） … 速度や分離・Dash/Hop のプリセット（M2で導入予定）
- `StageConfig`（SO） … 制限時間/目標Fill%/敵構成（M2で導入予定）
- `SceneLocator` … 代表的参照（Grid, Tilemaps, GameFlow, Player など）をシングルトンで取得

## 分離（敵どうし非貫通）の設計要点
- **運用方針**：移動は **グリッド制御（SmallEnemy.Update）**、分離は **方向付け（ステア）** に反映  
  - `EnemySeparation2D` は `OverlapCircleNonAlloc` で近傍敵をサンプリング → 反発ベクトル `repel` を計算  
  - `repel` は `LastRepel` で公開。`SmallEnemy.Update()` の `ComputeSteer()` 後に `steer += sep.LastRepel.normalized * w`（w=0.6 など）  
  - 物理移動 `rb.velocity` は使わない（グリッド制御を優先）

## パラメータの初期値（推奨）
- `EnemySeparation2D`  
  - `separationRadius=1.2, separationForce=10, damping=0.25`  
  - `boundaryProbe=0.30, boundaryLock=0.70`  
  - `enemyMask=Enemy, boundaryMask=Boundary`
- チューニング指針  
  - 密集で重なる → `separationForce↑ / separationRadius↑`  
  - 小刻み振動 → `damping↑`  
  - 外へ抜けやすい → `boundaryLock↑ / boundaryProbe↑`

## ゲームループ（M2～）
1. Countdown → Playing  
2. プレイヤーがラインを引いて閉じる → Fill% 更新（敵の居ない側を塗る）  
3. `goalFillPercent` 到達 → Clear、残機0または時間切れ → GameOver  
4. 結果パネル（スコア/タイム）表示、Next/Retry

## コード規約（簡易）
- Update（見た目/入力）と FixedUpdate（物理/サンプリング）の責務分離
- 分離は **“方向付け”** に限定（rbの直接移動は行わない）
- タイルや地形は **TilemapCollider2D + Composite** を優先
- PR 粒度: `feat(separation)`, `feat(stage)`, `feat(flow)`, `feat(ui)` など **スコープを小さく**

## 受け入れ基準（M2）
- 20体でも重なり・ワープ・ブルブルが出ない  
- Fill%/残機/時間のゲームループが成立  
- `StageConfig` 差し替えのみで難度が切替  

