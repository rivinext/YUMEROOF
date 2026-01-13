# Sun Y-axis Slider セットアップ手順

このドキュメントは、シーン上の Light（`DayNightLighting` + `SunYAxisOverride`）と UI スライダーをインスペクタで紐付ける手順をまとめたものです。

## 前提

- Light（Directional Light など）に `DayNightLighting` と `SunYAxisOverride` が追加されていること。
- UI スライダー用の GameObject がシーン内にあること。

## 手順

1. **Light の準備**
   1. シーン内の Light を選択し、`DayNightLighting` と `SunYAxisOverride` がアタッチされていることを確認します。
   2. まだ追加されていない場合は、`Add Component` から `DayNightLighting` と `SunYAxisOverride` を追加します。

2. **UI スライダーの準備**
   1. UI の Slider オブジェクトを選択します（例: `Canvas/Environment/SunYAxisSlider`）。
   2. Slider を操作するためのスクリプトとして `SunYAxisSliderController` を追加します。

3. **参照の紐付け**
   1. `SunYAxisSliderController` の `Y Axis Slider` に対象の `Slider` コンポーネントをドラッグ＆ドロップします。
   2. `Sun Y Axis Override` に Light 側の `SunYAxisOverride` をドラッグ＆ドロップします。

4. **動作確認**
   1. 再生すると、スライダーが Light の現在の Y 角度と同期されます。
   2. スライダーを動かすと、`SunYAxisOverride` によって Light の Y 軸回転が更新されます。
