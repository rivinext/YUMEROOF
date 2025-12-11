# Dev Item Injection Usage

Dev item injection can now run in all build configurations. Use it to pre-populate inventories for demo or QA flows, including release builds.

## How to enable
- In the **DevItemInjector** prefab or scene instance, leave **Enable Injection** checked (default).
- Configure the `Furniture Items` and `Material Items` lists with the IDs and quantities required for the build.

## How to disable
- Toggle off **Enable Injection** in the **DevItemInjector** component before creating the build.
- Or add the scripting define symbol `DISABLE_DEV_ITEM_INJECTION` to the Player settings to skip spawning/injecting items at runtime without modifying scenes.

## Verification
For demo and production builds, create a new save slot and confirm that the specified items appear in the inventory after the first load. If `DISABLE_DEV_ITEM_INJECTION` is set or **Enable Injection** is unchecked, the inventory should remain unaffected.
