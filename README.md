# Steam Frame Controller Fix — FML Mod

A small Resonite mod that moves common Steam Frame controller actions onto the physical button positions requested by players. The remap happens inside Resonite's action binding layer: it does not pretend that one raw controller button is another button, so ProtoFlux can continue reading the physical controls.

## Current layout

The same positions are used on both hands:

| Physical Steam Frame control | Resonite action |
| --- | --- |
| Trigger | Primary |
| Bumper | Secondary |
| X / left pad position | Context menu |
| A / down pad position | Dash / Userspace toggle |
| Y / up pad position | Jump |
| B / right pad position | Existing Resonite behavior |

Other controller bindings are kept as the game's existing bindings.

## What changes, and what does not

The mod patches `FrooxEngine.FrameController` action binding behavior. It does **not** patch `FrameController.ApplyState` or edit the raw `FrameControllerState` values. In the installed Resonite build, the physical inputs are represented by named digital fields such as `BumperClick`, `Down_A_Click`, `Right_B_Click`, `Left_X_Click`, and `Up_Y_Click`.

Thus, pressing X still produces the raw X/left input for ProtoFlux, while Resonite's built-in context-menu action is associated with that input on either hand. The remap is local to the client running this mod; it does not add a new Data Model type or require other session participants to install it.

## Build requirements

- .NET 10 SDK
- A local Resonite install containing `FrooxEngine.dll`, `Elements.Core.dll`, and `Renderite.Shared.dll`
- NuGet access for `ResoniteModLoader` 5.0.1 and Harmony 2.4.2

## Build

From the project directory:

```bash
dotnet build -c Release SteamFrameControls.csproj -p:Deploy=false
```

The compiled DLL is written to `bin/Release/net10.0/SteamFrameControls.dll`. The project is configured not to deploy during a normal build.

If Resonite is installed somewhere unusual, pass its install directory explicitly:

```bash
dotnet build -c Release SteamFrameControls.csproj -p:Deploy=false -p:ResonitePath="/path/to/Resonite/"
```

To build and copy the DLL into the RML folder, explicitly opt into deployment:

```bash
dotnet build -c Release SteamFrameControls.csproj -p:Deploy=true
```

You can override the destination when needed:

```bash
dotnet build -c Release SteamFrameControls.csproj -p:Deploy=true -p:DeployPath="/path/to/Resonite/rml_mods"
```

## Installing / enabling

This source targets **ResoniteModLoader (RML)**. It expects RML to be installed and enabled using its normal `-LoadAssembly Libraries/ResoniteModLoader.dll` launch option. Place `SteamFrameControls.dll` in Resonite's `rml_mods` directory, then restart Resonite. Check the newest Resonite log for `SteamFrameControls` to confirm the mod loaded and its patches installed.

## Compatibility and testing status

The patch targets the local Resonite `2026.10.29.1239` API shape. It compiled successfully against that install. The action bindings have not yet been verified in a live headset session, so contributors should test each action and confirm the raw ProtoFlux button fields still report their physical buttons after launch.

Resonite updates may change method names or implementation details. After an update, rebuild against the new local game assemblies and verify the bindings before relying on the mod.

## Project files

- `SteamFrameControls.cs` — mod entry point and Harmony patches.
- `SteamFrameControls.csproj` — .NET target, package references, game-assembly references, opt-in deploy target.
- `docs/IMPLEMENTATION_NOTES.md` — inspected game behavior, patch points, and verification notes.

## License

No license has been declared for this repository. Ask the repository owner before reusing or redistributing the code.
