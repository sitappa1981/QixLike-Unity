# QixLike-Unity — AI Context

## 1. 概要 / Goal
- Qix / Gals Panic 系の **陣取りアクション**。
- プレイヤーは外周の壁から壁へ線（トレイル）を引き、囲い込みに成功した領域を塗りつぶして **Fill%** を上げる。
- 内部を動き回る敵に触れる／描画中のトレイルに触れられるとミス（残機-1、演出あり）。
- 制限時間内に目標 Fill% を達成するとステージクリア。

## 2. 環境 / Versions
- Unity **6.2.0f1**
- 2D URP（Sprite-Lit-Default）
- TextMeshPro（SDF フォント）
- 基本グリッド：**1セル=16px**、Tilemap 使用
- 画面/UI：CanvasScaler = Scale With Screen Size, Reference 1600x900

## 3. 実行方法 / How to Run
1. シーン **Scenes/Game** を開く。
2. 再生（▶）。
3. 操作：矢印キーで1セル移動。外周上は自由移動、内側へ踏み出すと描画開始。
   - **P** = ポーズ、**R** = リセット。
4. 画面左上：Fill%、ハート（残機）。画面下中央：**デジタル（PocketCalculator）風のカウントダウン**。

## 4. ディレクトリ構成（抜粋）
Assets/
Art/
Tiles/ … white_16 ほか
Fonts/
POCKC___ SDF.asset … Pocket Calculator の SDF
PocketCalc_Amber.mat … 7セグ風アンバー調マテリアル
Prefabs/
Enemies/
EnemySmall_TypeA/B/C/D
Scripts/
Core/
GameConsts.cs
GridBootstrap.cs
SceneLocator.cs … シーン参照集約(任意)
Gameplay/
PlayerController.cs
CaptureSystem.cs
GameFlow.cs
GameHUD.cs
TrailBlinker.cs
SpritePingPongUltra.cs
Enemies/
SmallEnemy.cs
EnemyManager.cs
Scenes/
Game.unity


## 5. 主要シーン / Prefab / スクリプト
- **Scene**: `Game`
  - **GameRoot**: `GridBootstrap`, `CaptureSystem`, `TrailBlinker`, `GameFlow`
  - **Grid**: OverlayTilemap / WallTilemap / TrailTilemap（SortingLayers: Overlay, Walls, Trail）
  - **Player**: `PlayerController`（描画中は移動速度スケール可、被弾時はハート分解落下の演出）
  - **CanvasHUD**:
    - HUDGroup: FillText, LivesIcons（ハートアイコン列）
    - TimerGroup: **TimerBigText**（TMP, PocketCalc_Amber マテリアル）
  - **EnemySystem**:
    - `SceneLocator`（Grid/Tilemaps/GameRoot/Player の参照ハブ）
    - ※`EnemyManager` は今後ここに付ける想定（スポーン&分離・管理）

- **Script 概要**
  - `GridBootstrap`：外周枠の生成/初期化
  - `CaptureSystem`：囲い込み計算＆塗りつぶし、Fill% ログ出力
  - `PlayerController`：1セル刻み移動／描画制御／描画中スピード低減
  - `TrailBlinker`：トレイルの点滅（色/Hz 変更可）
  - `SpritePingPongUltra`：プレイヤーの高速アニメ再生
  - `GameHUD`：Fill%・TimerBigText 更新
  - `SmallEnemy`：壁反射・外周沿い・タイプA/B/C/Dの振る舞いフラグ、トレイル接触ミス判定
  - `EnemyManager`：敵の一括管理（スポーン・**敵同士の重なり回避**・今後のステージ構成読み込み）

## 6. セーブ / 入力 / デバッグ
- セーブ：未実装
- 入力：Keyboard（矢印/P/R）
- デバッグ：
  - `CaptureSystem` が囲い込み毎に `fill=` ログ出力
  - 敵のチューニング値は `SmallEnemy` / `EnemyManager` の Inspector で調整

## 7. 命名・規約（抜粋）
- 名前空間：`QixLike`（`Core`, `Gameplay`, `Enemies` をフォルダで分離）
- 1ファイル1クラス、PascalCase、SerializeField は先頭にまとめる

## 8. 次の一手（ざっくり）
1. `EnemyManager` を EnemySystem に追加し、有効化（敵分離を常時実行）
2. `StageConfig`（ScriptableObject）で「各タイプの敵と個数」を定義 → `EnemyManager` がスポーン
3. `SmallEnemy` の参照自動割当（`SceneLocator` 経由）を最終化
4. 調整：敵分離の押し返し強度/半径、TrailHit クールダウン、タイプ別パラメータ

## 9. 未決事項 / Open Questions
- ステージクリア条件（Fill% しきい値、タイムボーナス）
- 効果音/BGM、演出（囲い込み時の波紋・フラッシュ）
- 端末ターゲット（PC/モバイル）と入力系の拡張
