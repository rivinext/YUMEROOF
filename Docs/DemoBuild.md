# DEMOビルド設定

## Scripting Define Symbols
- プレイヤー設定の **Scripting Define Symbols** に `DEMO_VERSION` を追加してください。
- DEMOビルドでは `UIMenuManager` がストーリー要素を無効化し、クリエイティブモードのみ利用可能になります。

## ビルド手順の追加項目
1. Unityの **File > Build Settings** から対象プラットフォームを選択。
2. **Player Settings** を開き、`Scripting Define Symbols` に `DEMO_VERSION` を入力（既存のシンボルがある場合はセミコロンで区切って追加）。
3. 必要に応じてアセットバンドルやシーンリストを確認した上でビルドを実行してください。

`DEMO_VERSION` を外せば通常ビルドとなり、ストーリーモード関連のUIが再度有効化されます。
