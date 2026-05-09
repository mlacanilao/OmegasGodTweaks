using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace OmegasGodTweaks;

internal static class ButtonElementPatch
{
    internal static IEnumerable<CodeInstruction> SetGridTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo? elementIsGlobalElementGetter = AccessTools.PropertyGetter(type: typeof(Element), name: nameof(Element.IsGlobalElement));
        MethodInfo? cardDeityIdGetter = AccessTools.PropertyGetter(type: typeof(Card), name: nameof(Card.c_idDeity));
        MethodInfo? eClassPcGetter = AccessTools.PropertyGetter(type: typeof(EClass), name: nameof(EClass.pc));
        MethodInfo? charaFaithGetter = AccessTools.PropertyGetter(type: typeof(Chara), name: nameof(Chara.faith));
        MethodInfo? religionIdGetter = AccessTools.PropertyGetter(type: typeof(Religion), name: nameof(Religion.id));
        MethodInfo? stringInequality = AccessTools.Method(type: typeof(string), name: "op_Inequality", parameters: new[] { typeof(string), typeof(string) });
        MethodInfo? shouldHideGlobalElementForFaith = AccessTools.Method(type: typeof(ButtonElementPatch), name: nameof(ShouldHideGlobalElementForFaith));

        if (elementIsGlobalElementGetter == null ||
            cardDeityIdGetter == null ||
            eClassPcGetter == null ||
            charaFaithGetter == null ||
            religionIdGetter == null ||
            stringInequality == null ||
            shouldHideGlobalElementForFaith == null)
        {
            OmegasGodTweaks.LogError(message: "ButtonElement.SetGrid transpiler failed: required member lookup failed.");
            return instructions;
        }

        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructions);

        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: elementIsGlobalElementGetter)),
            new CodeMatch(predicate: IsFalseBranch),
            new CodeMatch(predicate: instruction => instruction.IsLdloc()),
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: cardDeityIdGetter)),
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: eClassPcGetter)),
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: charaFaithGetter)),
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: religionIdGetter)),
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: stringInequality))
        });

        bool replacedDeityGate = codeMatcher.IsValid;
        if (replacedDeityGate == true)
        {
            codeMatcher.Advance(offset: 2);
            CodeInstruction loadThing = CopyInstructionWithoutLabelsOrBlocks(instruction: codeMatcher.Instruction);
            codeMatcher.Advance(offset: 5);
            CodeInstruction deityGateInstruction = codeMatcher.Instruction;
            deityGateInstruction.opcode = OpCodes.Call;
            deityGateInstruction.operand = shouldHideGlobalElementForFaith;
            codeMatcher.Insert(instructions: new[]
            {
                loadThing
            });
        }

        if (replacedDeityGate == false)
        {
            OmegasGodTweaks.LogError(message: "ButtonElement.SetGrid transpiler failed to match the deity display gate; equipment grid display remains vanilla.");
        }

        return codeMatcher.Instructions();
    }

    private static bool IsFalseBranch(CodeInstruction instruction)
    {
        return instruction.opcode == OpCodes.Brfalse ||
               instruction.opcode == OpCodes.Brfalse_S;
    }

    private static bool CallsMethod(CodeInstruction instruction, MethodInfo method)
    {
        return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
               Equals(objA: instruction.operand, objB: method);
    }

    private static CodeInstruction CopyInstructionWithoutLabelsOrBlocks(CodeInstruction instruction)
    {
        return new CodeInstruction(opcode: instruction.opcode, operand: instruction.operand);
    }

    private static bool ShouldHideGlobalElementForFaith(string itemDeityId, string currentFaithId, Thing thing)
    {
        if (itemDeityId != currentFaithId)
        {
            if (OmegasGodTweaksConfig.UnlockGodArtifactFactionEffects.Value == false)
            {
                return true;
            }

            if (thing == null ||
                thing.HasTag(tag: CTAG.godArtifact) == false)
            {
                return true;
            }

            if (GodFaithStateService.IsJoinedGodId(godId: itemDeityId) == true)
            {
                FeatureTestLog.Log(
                    feature: "Unlock God Artifact Faction Effects",
                    detail: "enabled; ButtonElement display allowed joined god artifact=" +
                            FeatureTestLog.GetThingId(thing: thing) +
                            ", deityId=" +
                            (itemDeityId ?? string.Empty));
                return false;
            }

            return true;
        }

        return false;
    }
}
