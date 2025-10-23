# TODO — QixLike-Unity

## ルール
- 1タスク＝1目的。小刻みに **実装 → 目視テスト → コミット**。
- 既存挙動が変わる場合は「再現手順」「期待結果」を簡記。

---

## NOW（着手中）
### T-101 EnemyManager をシーンに追加
- EnemySystem に `EnemyManager` を Add。
- Params：SeparationRadius / SeparationStrength / UpdateHz。
- 参照は `SceneLocator` を利用。

### T-102 敵同士の重なり回避（常時）
- `EnemyManager` が全敵を走査し、近距離ペアへ相互押し返しベクトルを適用。
- 壁反射との優先順位を確認（反射→分離の順）。

### T-103 SmallEnemy の参照自動割当の仕上げ
- `Awake/OnEnable` で `SceneLocator` から Grid/Tilemaps/GameFlow/Player を埋める。
- Inspector で手動割当済みなら上書きしない。

### T-104 ステージ構成：ScriptableObject
- `StageConfig`（CreateAssetMenu）
  - `targetFill`, `timeLimitSec`, `List<SpawnEntry> { prefab, count, speedScale … }`
- `EnemyManager` が StageConfig を読み込み、タイプA/B/C/D を自動スポーン。
- 生成位置：内部ランダム（外周から一定距離）＋相互距離の最小確保。

### T-105 HUD の最終化
- 左上の旧 `TimeText` を削除（TimerBigText のみ）。
- PocketCalc_Amber のプリセット確定（桁間隔/アウトライン/Underlay 値の統一）。

---

## BACKLOG
- B-201 スコア/コンボとリザルト画面
- B-202 追加エネミー（弾・分裂・通路優先など）
- B-203 囲い込み演出（波紋/パーティクル）
- B-204 サウンド（SE/BGM）とミキサールーティング
- B-205 入力拡張（Pad/モバイル）
- B-206 設定メニュー（音量・スピード・配色）
- B-207 セーブ/ロード（クリア率・ハイスコア）

---

## DONE
- D-101 グリッド/外周初期化（`GridBootstrap`）
- D-102 囲い込みアルゴリズム & Fill% ログ（`CaptureSystem`）
- D-103 プレイヤー：1セル移動・描画制御・描画中減速
- D-104 被弾演出：ハート分解落下 & リスポーン
- D-105 トレイル点滅（色/Hz、黄）
- D-106 敵：壁反射/外周沿い、Trail 接触ミス判定
- D-107 敵：被弾後も位置リセットせず継続移動
- D-108 残機アイコン（ハート列）
- D-109 TimerBigText（PocketCalculator フォント）下中央表示
- D-110 敵タイプ A/B/C/D の Prefab 作成
