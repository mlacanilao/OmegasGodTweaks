using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace OmegasGodTweaks;

internal static class AIIdlePatch
{
    private const int ApostleElementId = 1227;
    private const int HarmonyElementId = 1272;

    internal static IEnumerable<CodeInstruction> RunTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(collection: instructions);
        MethodInfo? evalueTotal = AccessTools.Method(
            type: typeof(Party),
            name: nameof(Party.EvalueTotal),
            parameters: new[] { typeof(int), typeof(System.Func<Chara, bool>) });
        MethodInfo? hasElement = AccessTools.Method(
            type: typeof(Card),
            name: nameof(Card.HasElement),
            parameters: new[] { typeof(int), typeof(bool) });
        MethodInfo? setEnemy = AccessTools.Method(
            type: typeof(Chara),
            name: nameof(Chara.SetEnemy),
            parameters: new[] { typeof(Chara) });
        MethodInfo? setEnemyUnlessApostleInfightingDisabled = AccessTools.Method(
            type: typeof(AIIdlePatch),
            name: nameof(SetEnemyUnlessApostleInfightingDisabled));
        MethodInfo? shouldRunApostleInfightingOwnerCheck = AccessTools.Method(
            type: typeof(AIIdlePatch),
            name: nameof(ShouldRunApostleInfightingOwnerCheck));

        if (evalueTotal == null ||
            hasElement == null ||
            setEnemy == null ||
            setEnemyUnlessApostleInfightingDisabled == null ||
            shouldRunApostleInfightingOwnerCheck == null)
        {
            OmegasGodTweaks.LogError(message: "AI_Idle.Run transpiler failed: required member lookup failed.");
            return instructionList;
        }

        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructionList);

        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(predicate: instruction => LoadsInt(instruction: instruction, value: ApostleElementId)),
            new CodeMatch(predicate: LoadsFalse),
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: hasElement))
        });

        bool matchedApostleOwnerCheck = codeMatcher.IsValid;
        bool matchedHarmonyThreshold = false;
        bool matchedApostleThreshold = false;
        int ownerCheckPosition = -1;
        int setEnemyPosition = -1;

        if (matchedApostleOwnerCheck == true)
        {
            codeMatcher.Advance(offset: 2);
            ownerCheckPosition = codeMatcher.Pos;

            codeMatcher.MatchStartForward(matches: new[]
            {
                new CodeMatch(predicate: instruction => LoadsInt(instruction: instruction, value: HarmonyElementId))
            });

            matchedHarmonyThreshold = codeMatcher.IsValid;
            if (matchedHarmonyThreshold == true)
            {
                codeMatcher.MatchStartForward(matches: new[]
                {
                    new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: evalueTotal))
                });

                matchedApostleThreshold = codeMatcher.IsValid;
            }
        }

        if (matchedApostleThreshold == true)
        {
            codeMatcher.MatchStartForward(matches: new[]
            {
                new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: setEnemy))
            });

            if (codeMatcher.IsValid == true)
            {
                setEnemyPosition = codeMatcher.Pos;
            }
        }

        if (ownerCheckPosition < 0 ||
            matchedApostleThreshold == false ||
            setEnemyPosition < 0)
        {
            OmegasGodTweaks.LogError(message: "AI_Idle.Run transpiler failed to match the apostle infighting target; apostle infighting remains vanilla.");
            return instructionList;
        }

        IList<CodeInstruction> patchedInstructions = codeMatcher.Instructions();

        CodeInstruction ownerCheckInstruction = patchedInstructions[ownerCheckPosition];
        ownerCheckInstruction.opcode = OpCodes.Call;
        ownerCheckInstruction.operand = shouldRunApostleInfightingOwnerCheck;

        CodeInstruction setEnemyInstruction = patchedInstructions[setEnemyPosition];
        setEnemyInstruction.opcode = OpCodes.Call;
        setEnemyInstruction.operand = setEnemyUnlessApostleInfightingDisabled;

        return patchedInstructions;
    }

    private static bool CallsMethod(CodeInstruction instruction, MethodInfo method)
    {
        return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
               Equals(objA: instruction.operand, objB: method);
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

    private static bool LoadsFalse(CodeInstruction instruction)
    {
        return instruction.opcode == OpCodes.Ldc_I4_0;
    }

    private static bool ShouldRunApostleInfightingOwnerCheck(Card owner, int elementId, bool ignoreDefault)
    {
        if (owner == null)
        {
            return false;
        }

        if (owner.HasElement(elementId, ignoreDefault) == false)
        {
            return false;
        }

        if (elementId != ApostleElementId)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.DisableApostleInfighting.Value == false)
        {
            return true;
        }

        Chara? ownerChara = owner as Chara;
        if (ownerChara == null)
        {
            return true;
        }

        if (ownerChara.IsPCParty == false)
        {
            return true;
        }

        FeatureTestLog.Log(
            feature: "Disable Apostle Infighting",
            detail: "enabled; skipped apostle infighting branch for owner=" +
                    ownerChara.uid.ToString());
        return false;
    }

    private static Chara SetEnemyUnlessApostleInfightingDisabled(Chara owner, Chara target)
    {
        if (ShouldSkipApostleInfighting(owner: owner, target: target) == true)
        {
            FeatureTestLog.Log(
                feature: "Disable Apostle Infighting",
                detail: "enabled; skipped SetEnemy between apostles owner=" +
                        owner.uid.ToString() +
                        ", target=" +
                        target.uid.ToString());
            return target;
        }

        return owner.SetEnemy(c: target);
    }

    private static bool ShouldSkipApostleInfighting(Chara owner, Chara target)
    {
        if (OmegasGodTweaksConfig.DisableApostleInfighting.Value == false)
        {
            return false;
        }

        if (owner == null ||
            target == null)
        {
            return false;
        }

        if (owner.IsPCParty == false)
        {
            return false;
        }

        if (target.IsPCParty == false)
        {
            return false;
        }

        if (owner.Evalue(ele: ApostleElementId) <= 0)
        {
            return false;
        }

        if (target.Evalue(ele: ApostleElementId) <= 0)
        {
            return false;
        }

        return true;
    }
}
