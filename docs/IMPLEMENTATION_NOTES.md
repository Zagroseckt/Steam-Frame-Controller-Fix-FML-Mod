# Implementation notes

## Goal

Resonite's Steam Frame support exposes distinct physical inputs, but some built-in actions were assigned to awkward or overloaded inputs. SteamVR button-to-button rebinding was not suitable for this fix because it changes the raw input observed by Resonite and ProtoFlux. This mod changes the association between the physical Frame inputs and Resonite's built-in action nodes instead.

## Inspected baseline

The mod was developed against the installed Resonite build `2026.10.29.1239`. Decompiling `FrooxEngine.FrameController` showed:

- `ActionPrimary` already returns `TriggerClick`.
- `ActionSecondary` returned `ThumbstickClick`.
- `Bind(InteractionHandlerInputs)` bound `Secondary` to `ThumbstickClick`.
- `Bind(InteractionHandlerInputs)` bound `Menu` to `Left_X_Click` on the right hand, but `Right_B_Click` on the left hand.
- `UserspaceToggle` was bound to `ViewMenuClick`.
- `BindNodeActions` associated the `Jump` action with `ThumbstickClick`.
- `ApplyState(FrameControllerState, ...)` copies state values into distinct controller input fields, including the bumper, thumbstick click, and each labeled pad/button position.

The target inputs for this layout are `BumperClick`, `Down_A_Click`, `Left_X_Click`, and `Up_Y_Click`. B/right is deliberately left alone because no new action for B was requested.

## Patch summary

`FrameControllerBindPatch` transpiles the existing binding method to change only the relevant physical input references:

- Secondary's interaction binding obtains `BumperClick`.
- Dash/Userspace toggle obtains `Down_A_Click`.
- The left controller's context-menu input is moved from `Right_B_Click` to `Left_X_Click`, matching the right controller's X position.

The `ActionSecondary` and `ActionMenu` property getters are also patched to return the matching physical fields for callers that use the controller's standard action properties directly.

`FrameControllerJumpPatch` intercepts only `BindNodeActions` calls named `Jump`. It assigns the `Up_Y_Click` node for the active side, handling the two node forms found in the game's method (`LeftRightSelector<bool>` and `AnyInput`). Other action node names continue through the game's original code.

## Raw-input preservation

No patch targets `ApplyState`, `FrameControllerState`, or ProtoFlux. The original fields continue to receive their respective physical button states. The mod changes only the action bindings that consume those inputs.

## Build and runtime verification

The source built successfully with .NET SDK 10.0.113 against the installed game assemblies, with zero warnings and zero errors. The built DLL was copied to the local `rml_mods` folder during development. It still requires a subsequent Resonite launch and physical controller testing to verify the full runtime behavior, including ProtoFlux raw-input visibility.

## Updating for new Resonite builds

1. Re-inspect `FrameController`, its `Bind` method, `BindNodeActions`, and `ApplyState` in the new `FrooxEngine.dll`.
2. Confirm the renamed/changed input fields and action methods.
3. Rebuild against that install's `FrooxEngine.dll`, `Elements.Core.dll`, and `Renderite.Shared.dll`.
4. Launch with RML and check logs for mod load/patch errors.
5. Test Primary, Secondary, context menu, Dash, and Jump on both hands.
6. In ProtoFlux, verify X/A/Y/Bumper still appear as their original physical inputs.
