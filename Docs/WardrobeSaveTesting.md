# Wardrobe Save Initialization Testing

## Manual
1. 起動前に既存スロットでワードローブの装備を変更し、セーブされている状態にする（`SaveGameManager` が `CurrentSlotKey` を保持したままゲームを終了）。
2. 同じスロットでゲームを再開し、ワードローブ画面を開く。初期装備の itemId が自動適用されず、前回セーブした装備がそのまま表示されることを確認する。
3. 新規スロットやワードローブセーブが存在しないスロットで起動し、初期装備（`initialHairItemId` など）が自動で装備されることを確認する。

## Automated
- PlayMode テストで一時ディレクトリに `StorySaveData` または `CreativeSaveData` の JSON を作成し、`SaveGameManager.SetCurrentSlotKey` でそのスロットを指すように設定する。
- `WardrobeOnePieceCoordinator` をテストシーンに配置し、`Start` を実行（`MonoBehaviourTest` などで待機）する。
- `hasWardrobeSelections` が `true` のセーブを用意した場合は `ApplyInitialEquipment` が呼ばれず、`WardrobeUIController.GetSelectedItem` が `null` のままでも装備が変化しないことをアサートする。
- セーブに `hasWardrobeSelections` が含まれない場合は従来通り初期装備が適用されることを確認し、`WardrobeOnePieceCoordinator.SetHasWardrobeSave(false)` がデフォルトで動作することを検証する。
- スロット切り替えパスを追加し、`SaveGameManager.Load` で A スロットをロード→ヘア・アクセ・アイウェア・トップス各タブで別のアイテムを選択→ワードローブを閉じてスロット B をロード→同じ手順で異なるアイテムを選択→再度スロット A に戻したときに、`WardrobeUIController.GetSelectionSaveEntries(slotKey)` が A と B で保存した itemId をそれぞれ保持し、`ApplySelectionEntries(selections, slotKey)` で現在のスロット以外の装備が混ざらないことを確認する。
