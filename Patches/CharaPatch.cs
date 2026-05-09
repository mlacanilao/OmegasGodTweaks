using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace OmegasGodTweaks;

internal static class CharaPatch
{
    private const int VanillaFaithResistanceBonusCap = 20;
    private const int CappedGodBonusElementMinId = 950;
    private const int CappedGodBonusElementMaxExclusiveId = 970;

    private static bool loggedFaithResistanceBonusCapBypass;

    internal static IEnumerable<CodeInstruction> RefreshFaithElementTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructions);
        MethodInfo? getFaithResistanceBonusCap = AccessTools.Method(
            type: typeof(CharaPatch),
            name: nameof(GetFaithResistanceBonusCap));

        if (getFaithResistanceBonusCap == null)
        {
            OmegasGodTweaks.LogError(message: "Chara.RefreshFaithElement transpiler failed: faith resistance cap helper lookup failed.");
            return codeMatcher.Instructions();
        }

        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(predicate: instruction => instruction.IsLdloc()),
            new CodeMatch(predicate: IsLoadVanillaFaithResistanceBonusCap),
            new CodeMatch(predicate: IsBranch),
            new CodeMatch(predicate: instruction => instruction.IsLdloc()),
            new CodeMatch(predicate: instruction => instruction.IsLdloc()),
            new CodeMatch(opcode: OpCodes.Ldelem_I4),
            new CodeMatch(predicate: instruction => LoadsInt(instruction: instruction, value: CappedGodBonusElementMinId)),
            new CodeMatch(predicate: IsBranch),
            new CodeMatch(predicate: instruction => instruction.IsLdloc()),
            new CodeMatch(predicate: instruction => instruction.IsLdloc()),
            new CodeMatch(opcode: OpCodes.Ldelem_I4),
            new CodeMatch(predicate: instruction => LoadsInt(instruction: instruction, value: CappedGodBonusElementMaxExclusiveId)),
            new CodeMatch(predicate: IsBranch),
            new CodeMatch(predicate: IsLoadVanillaFaithResistanceBonusCap),
            new CodeMatch(predicate: IsStoreLocal)
        });

        if (codeMatcher.IsValid == false)
        {
            OmegasGodTweaks.LogError(message: "Chara.RefreshFaithElement transpiler failed to match the faith resistance bonus cap; current-faith resistance bonuses remain vanilla capped.");
            return codeMatcher.Instructions();
        }

        codeMatcher.Advance(offset: 1);
        ReplaceWithFaithResistanceBonusCapCall(instruction: codeMatcher.Instruction, method: getFaithResistanceBonusCap);

        codeMatcher.Advance(offset: 12);
        ReplaceWithFaithResistanceBonusCapCall(instruction: codeMatcher.Instruction, method: getFaithResistanceBonusCap);

        return codeMatcher.Instructions();
    }

    internal static void RefreshFaithElementPostfix(Chara chara)
    {
        if (GodFaithStateService.IsApplyingState == true)
        {
            return;
        }

        GodFaithStateService.AddJoinedFaithElements(chara: chara);
    }

    internal static int GetFaithResistanceBonusCap()
    {
        if (OmegasGodTweaksConfig.RemoveFaithResistanceBonusCap.Value == false)
        {
            return VanillaFaithResistanceBonusCap;
        }

        if (loggedFaithResistanceBonusCapBypass == false)
        {
            loggedFaithResistanceBonusCapBypass = true;
            FeatureTestLog.Log(
                feature: "Remove Faith Resistance Bonus Cap",
                detail: "enabled; replaced vanilla faith resistance bonus cap " +
                        VanillaFaithResistanceBonusCap.ToString() +
                        " with int.MaxValue.");
        }

        return int.MaxValue;
    }

    private static void ReplaceWithFaithResistanceBonusCapCall(CodeInstruction instruction, MethodInfo method)
    {
        instruction.opcode = OpCodes.Call;
        instruction.operand = method;
    }

    private static bool IsLoadVanillaFaithResistanceBonusCap(CodeInstruction instruction)
    {
        return LoadsInt(instruction: instruction, value: VanillaFaithResistanceBonusCap);
    }

    private static bool IsStoreLocal(CodeInstruction instruction)
    {
        return instruction.opcode == OpCodes.Stloc ||
               instruction.opcode == OpCodes.Stloc_0 ||
               instruction.opcode == OpCodes.Stloc_1 ||
               instruction.opcode == OpCodes.Stloc_2 ||
               instruction.opcode == OpCodes.Stloc_3 ||
               instruction.opcode == OpCodes.Stloc_S;
    }

    private static bool IsBranch(CodeInstruction instruction)
    {
        return instruction.opcode.FlowControl == FlowControl.Branch ||
               instruction.opcode.FlowControl == FlowControl.Cond_Branch;
    }

    private static bool LoadsInt(CodeInstruction instruction, int value)
    {
        if (instruction.opcode == OpCodes.Ldc_I4)
        {
            if (instruction.operand is int intValue && intValue == value)
            {
                return true;
            }
        }

        if (instruction.opcode == OpCodes.Ldc_I4_S)
        {
            if (instruction.operand is sbyte byteValue && byteValue == value)
            {
                return true;
            }
        }

        return false;
    }
}
