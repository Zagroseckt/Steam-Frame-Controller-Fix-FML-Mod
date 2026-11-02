using System.Reflection;
using HarmonyLib;
using Renderite.Shared;
using ResoniteModLoader;

namespace SteamFrameControls;

public sealed class SteamFrameControls : ResoniteMod
{
    public override string Name => "SteamFrameControls";
    public override string Author => "Local mod";
    public override string Version => "0.1.0";
    public override string Link => "";

    public override void OnEngineInit()
    {
        var harmony = new Harmony("local.steamframecontrols");
        harmony.PatchAll(typeof(SteamFrameControls).Assembly);
        Msg("SteamFrameControls 0.1.0 loaded; Steam Frame action remap patches installed.");
    }
}

[HarmonyPatch(typeof(FrooxEngine.FrameController), nameof(FrooxEngine.FrameController.Bind))]
internal static class FrameControllerBindPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == System.Reflection.Emit.OpCodes.Ldfld &&
                instruction.operand is FieldInfo field)
            {
                // The interaction Secondary action now reads the physical bumper.
                if (field.Name == "ThumbstickClick")
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(FrameControllerBindPatch), nameof(GetSecondaryButton));
                }

                // UserspaceToggle is Resonite's Dash/main-menu action on this controller.
                if (field.Name == "ViewMenuClick")
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(FrameControllerBindPatch), nameof(GetDashButton));
                }

                // Use the same physical X/left position for the context menu on both hands.
                if (field.Name == "Right_B_Click")
                    instruction.operand = AccessTools.Field(typeof(FrooxEngine.FrameController), "Left_X_Click");
            }

            yield return instruction;
        }
    }

    private static FrooxEngine.Digital GetSecondaryButton(FrooxEngine.FrameController controller) =>
        controller.BumperClick;

    private static FrooxEngine.Digital GetDashButton(FrooxEngine.FrameController controller) =>
        controller.Down_A_Click;
}

[HarmonyPatch(typeof(FrooxEngine.FrameController), "get_ActionSecondary")]
internal static class FrameControllerSecondaryActionPatch
{
    private static void Postfix(FrooxEngine.FrameController __instance, ref FrooxEngine.Digital __result)
    {
        __result = __instance.BumperClick;
    }
}

[HarmonyPatch(typeof(FrooxEngine.FrameController), "get_ActionMenu")]
internal static class FrameControllerMenuActionPatch
{
    private static void Postfix(FrooxEngine.FrameController __instance, ref FrooxEngine.Digital __result)
    {
        __result = __instance.Left_X_Click;
    }
}

[HarmonyPatch(typeof(FrooxEngine.FrameController), nameof(FrooxEngine.FrameController.BindNodeActions))]
internal static class FrameControllerJumpPatch
{
    private static bool Prefix(FrooxEngine.FrameController __instance, FrooxEngine.IInputNode node, string name)
    {
        if (name != "Jump")
            return true;

        // Replace only Resonite's Jump action binding. The raw Y and thumbstick-click
        // controller inputs remain their original physical inputs for ProtoFlux.
        var yButton = FrooxEngine.InputNode.Digital(__instance.Side, "Up_Y_Click");
        var nodeType = node.GetType();

        if (nodeType.IsGenericType && nodeType.GetGenericTypeDefinition() == typeof(FrooxEngine.LeftRightSelector<>))
        {
            var setNode = nodeType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(method => method.Name == "SetNode" &&
                    method.GetParameters() is { Length: 2 } parameters &&
                    parameters[0].ParameterType == typeof(Chirality) &&
                    parameters[1].ParameterType.IsInstanceOfType(yButton));
            if (setNode is not null)
            {
                setNode.Invoke(node, new object[] { __instance.Side, yButton });
                return false;
            }
        }

        if (nodeType == typeof(FrooxEngine.AnyInput))
        {
            var inputs = AccessTools.Field(nodeType, "Inputs")?.GetValue(node)
                ?? AccessTools.Property(nodeType, "Inputs")?.GetValue(node);
            var add = inputs?.GetType().GetMethods()
                .FirstOrDefault(method => method.Name == "Add" &&
                    method.GetParameters() is { Length: 1 } parameters &&
                    parameters[0].ParameterType.IsInstanceOfType(yButton));
            if (inputs is not null && add is not null)
            {
                add.Invoke(inputs, new object[] { yButton });
                return false;
            }
        }

        ResoniteMod.Msg($"SteamFrameControls: couldn't safely remap Jump node type {nodeType.FullName}; leaving its original binding.");
        return true;
    }
}
