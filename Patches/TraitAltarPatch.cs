using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using OmegasGodTweaks.UI;

namespace OmegasGodTweaks;

internal static class TraitAltarPatch
{
    private const int GodLoyaltyElementId = 1228;
    private const int OfferingForbiddenElementId = 764;
    private const int HarvestQuestCropFlagId = 115;
    private const int OfferingPietyCapBypassValue = int.MinValue;
    private const string WaterThingId = "water";

    internal static void TrySetActPostfix(TraitAltar altar, ActPlan p)
    {
        if (p == null)
        {
            return;
        }

        if (p.input != ActInput.AllAction)
        {
            return;
        }

        if (altar == null)
        {
            return;
        }

        if (altar.IsBranchAltar == true)
        {
            return;
        }

        Thing? target = altar.owner as Thing;
        if (target == null)
        {
            return;
        }

        if (target.IsInstalled == false)
        {
            return;
        }

        Religion deity = altar.Deity;
        if (deity == null)
        {
            return;
        }

        if (deity.IsEyth == true)
        {
            return;
        }

        if (deity.CanJoin == false)
        {
            return;
        }

        FeatureTestLog.Log(
            feature: "God Altar Stats Action",
            detail: "added action for installed altar deity=" + FeatureTestLog.GetReligionId(religion: deity));
        p.TrySetAct(
            lang: Localization.AltarStats,
            onPerform: delegate
            {
                FeatureTestLog.Log(
                    feature: "God Altar Stats Action",
                    detail: "opened stats dialog for deity=" + FeatureTestLog.GetReligionId(religion: deity));
                GodAltarStatsUI.Show(altar: altar);
                return false;
            },
            tc: target,
            cursor: null,
            dist: 1,
            isHostileAct: false,
            localAct: true,
            canRepeat: false);
    }

    internal static void CanOfferPostfix(TraitAltar altar, Card c, ref bool __result)
    {
        if (EClass.pc == null)
        {
            return;
        }

        Chara pc = EClass.pc;
        if (ShouldUseJoinedAltarOfferingRules(altar: altar, card: c, chara: pc, currentFaith: pc.faith) == false)
        {
            return;
        }

        Thing? thing = c as Thing;
        if (thing == null)
        {
            __result = false;
            return;
        }

        if (__result == true && ShouldUseVanillaGodArtifactOffer(altar: altar, c: pc, t: thing) == true)
        {
            return;
        }

        bool canOffer = CanOfferToAltarDeity(altar: altar, t: thing);
        __result = canOffer;
        FeatureTestLog.Log(
            feature: "Allow Offerings For Joined Non-Current Gods",
            detail: "enabled; CanOffer checked joined altar deity=" +
                    FeatureTestLog.GetReligionId(religion: altar.Deity) +
                    ", thing=" +
                    FeatureTestLog.GetThingId(thing: thing) +
                    ", result=" +
                    canOffer.ToString());
    }

    internal static bool OnOfferPrefix(TraitAltar altar, Chara c, Thing t)
    {
        if (altar == null)
        {
            return true;
        }

        if (c == null)
        {
            return true;
        }

        if (t == null)
        {
            return true;
        }

        if (c.IsPC == false)
        {
            return true;
        }

        if (ShouldUseJoinedAltarOfferingRules(altar: altar, card: t, chara: c, currentFaith: c.faith) == false)
        {
            return true;
        }

        if (ShouldUseVanillaGodArtifactOffer(altar: altar, c: c, t: t) == true)
        {
            return true;
        }

        FeatureTestLog.Log(
            feature: "Allow Offerings For Joined Non-Current Gods",
            detail: "enabled; offering routed to joined altar deity=" +
                    FeatureTestLog.GetReligionId(religion: altar.Deity) +
                    ", thing=" +
                    FeatureTestLog.GetThingId(thing: t));
        c.Say(lang: "god_offer", c1: altar.owner, c2: t, ref1: altar.Deity.Name, ref2: null);
        if (CanOfferToAltarDeity(altar: altar, t: t) == false)
        {
            c.Say(lang: "nothingHappens", c1: altar.owner, c2: t, ref1: null, ref2: null);
            return false;
        }

        Effect.Get(id: "debuff").Play(from: altar.owner.pos, fixY: 0f, to: null, sprite: null);
        c.PlaySound(id: "offering", v: 1f, spatial: true);

        GodFaithStateService.WithPlayerFaithState(
            religion: altar.Deity,
            action: () =>
            {
                altar._OnOffer(c: c, t: t, takeoverMod: 0);
            });

        t.Destroy();
        FaithSaveData.SnapshotCurrent();
        return false;
    }

    internal static IEnumerable<CodeInstruction> OnOfferCoreTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo? elementBase = AccessTools.Field(type: typeof(Element), name: nameof(Element.vBase));
        MethodInfo? getPietyBaseForOfferingGainCap = AccessTools.Method(type: typeof(TraitAltarPatch), name: nameof(GetPietyBaseForOfferingGainCap));
        MethodInfo? getPietyBaseForOfferingClamp = AccessTools.Method(type: typeof(TraitAltarPatch), name: nameof(GetPietyBaseForOfferingClamp));
        MethodInfo? getBool = AccessTools.Method(type: typeof(BaseCard), name: nameof(BaseCard.GetBool), parameters: new[] { typeof(int) });
        MethodInfo? getOfferingKarmaFlag = AccessTools.Method(type: typeof(TraitAltarPatch), name: nameof(GetOfferingKarmaFlag));
        MethodInfo? setBase = AccessTools.Method(type: typeof(ElementContainer), name: nameof(ElementContainer.SetBase), parameters: new[] { typeof(int), typeof(int), typeof(int) });
        MethodInfo? setBasePreservingOfferingOverflow = AccessTools.Method(type: typeof(TraitAltarPatch), name: nameof(SetBasePreservingOfferingOverflow));

        if (elementBase == null ||
            getPietyBaseForOfferingGainCap == null ||
            getPietyBaseForOfferingClamp == null ||
            getBool == null ||
            getOfferingKarmaFlag == null ||
            setBase == null ||
            setBasePreservingOfferingOverflow == null)
        {
            OmegasGodTweaks.LogError(message: "TraitAltar._OnOffer transpiler failed: required member lookup failed.");
            return instructions;
        }

        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructions);

        bool replacedGainCap = ReplaceNextElementBaseRead(
            codeMatcher: codeMatcher,
            elementBase: elementBase,
            replacement: getPietyBaseForOfferingGainCap,
            context: "piety gain cap");
        bool replacedClamp = ReplaceNextElementBaseRead(
            codeMatcher: codeMatcher,
            elementBase: elementBase,
            replacement: getPietyBaseForOfferingClamp,
            context: "piety clamp");
        bool replacedHarvestQuestKarma = ReplaceHarvestQuestOfferingFlagRead(
            codeMatcher: codeMatcher,
            getBool: getBool,
            replacement: getOfferingKarmaFlag);
        bool replacedSetBase = ReplaceSetBaseCall(
            codeMatcher: codeMatcher,
            setBase: setBase,
            replacement: setBasePreservingOfferingOverflow);

        if (replacedGainCap == false ||
            replacedClamp == false ||
            replacedHarvestQuestKarma == false ||
            replacedSetBase == false)
        {
            OmegasGodTweaks.LogError(message: "TraitAltar._OnOffer transpiler failed to match one or more offering piety targets; unmatched behavior remains vanilla.");
        }

        return codeMatcher.Instructions();
    }

    internal static void OnOfferCorePostfix(Chara c)
    {
        if (c == null)
        {
            return;
        }

        if (c.IsPC == false)
        {
            return;
        }

        GodFaithStateService.SnapshotCurrentFaith(chara: c, joined: true);
        if (OmegasGodTweaksConfig.ShowPietyFaithAfterOffering.Value == true)
        {
            GodFaithStateService.ShowPietyFaith(chara: c);
            FeatureTestLog.Log(feature: "Show Piety Faith After Offering", detail: "enabled; displayed piety/faith after offering.");
        }

        FaithSaveData.SnapshotCurrent();
    }

    private static bool CanOfferToAltarDeity(TraitAltar altar, Thing t)
    {
        if (t.HasTag(tag: CTAG.godArtifact) == true && t.c_idDeity == altar.Deity.id)
        {
            return true;
        }

        if (CanOfferBase(t: t) == false)
        {
            return false;
        }

        if (altar.Deity.GetOfferingValue(t: t, num: -1) <= 0)
        {
            return false;
        }

        if (t.isCopy == true)
        {
            return false;
        }

        if (t.HasElement(ele: OfferingForbiddenElementId, includeNagative: false) == true)
        {
            return false;
        }

        return true;
    }

    private static bool ShouldUseVanillaGodArtifactOffer(TraitAltar altar, Chara c, Thing t)
    {
        if (t.HasTag(tag: CTAG.godArtifact) == false)
        {
            return false;
        }

        if (t.c_idDeity == altar.Deity.id)
        {
            return true;
        }

        if (c.IsEyth == true && c.HasElement(ele: GodLoyaltyElementId, includeNagative: false) == true)
        {
            if (altar.IsEyth == true)
            {
                return true;
            }

            if (altar.Deity.IsValidArtifact(id: t.id) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldUseJoinedAltarOfferingRules(TraitAltar altar, Card card, Chara chara, Religion currentFaith)
    {
        if (altar == null)
        {
            return false;
        }

        if (card == null)
        {
            return false;
        }

        if (chara == null)
        {
            return false;
        }

        if (currentFaith == null)
        {
            return false;
        }

        if (altar.Deity == null)
        {
            return false;
        }

        if (OmegasGodTweaksConfig.AllowOfferingsForJoinedNonCurrentGods.Value == false)
        {
            return false;
        }

        if (altar.IsBranchAltar == true)
        {
            return false;
        }

        if (altar.Deity == currentFaith)
        {
            return false;
        }

        if (chara.IsEyth == true &&
            chara.HasElement(ele: GodLoyaltyElementId, includeNagative: false) == false)
        {
            return false;
        }

        if (card.id == WaterThingId)
        {
            return false;
        }

        if (GodFaithStateService.IsJoined(religion: altar.Deity) == false)
        {
            return false;
        }

        return true;
    }

    private static bool CanOfferBase(Thing t)
    {
        if (t.isChara == true)
        {
            return false;
        }

        if (t.trait.CanOnlyCarry == true)
        {
            return false;
        }

        if (t.rarity == Rarity.Artifact)
        {
            return false;
        }

        return true;
    }

    private static bool GetOfferingKarmaFlag(Thing thing, int id)
    {
        if (id != HarvestQuestCropFlagId)
        {
            return thing.GetBool(id: id);
        }

        bool hasHarvestQuestCropFlag = thing.GetBool(id: id);
        FeatureTestLog.Log(
            feature: "Disable Harvest Quest Offering Karma Loss",
            detail: "harvest quest offering karma check; toggle=" +
                    OmegasGodTweaksConfig.DisableHarvestQuestOfferingKarmaLoss.Value.ToString() +
                    ", vanillaHarvestQuestCropFlag=" +
                    hasHarvestQuestCropFlag.ToString() +
                    ", thing=" +
                    FeatureTestLog.GetThingId(thing: thing));

        if (OmegasGodTweaksConfig.DisableHarvestQuestOfferingKarmaLoss.Value == true)
        {
            FeatureTestLog.Log(feature: "Disable Harvest Quest Offering Karma Loss", detail: "enabled; returned false for harvest quest offering karma flag.");
            return false;
        }

        return hasHarvestQuestCropFlag;
    }

    private static bool ReplaceNextElementBaseRead(CodeMatcher codeMatcher, FieldInfo elementBase, MethodInfo replacement, string context)
    {
        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(opcode: OpCodes.Ldfld, operand: elementBase)
        });

        if (codeMatcher.IsValid == false)
        {
            OmegasGodTweaks.LogError(message: $"TraitAltar._OnOffer transpiler failed: {context} vBase read not matched.");
            return false;
        }

        CodeInstruction instruction = codeMatcher.Instruction;
        instruction.opcode = OpCodes.Call;
        instruction.operand = replacement;
        codeMatcher.Advance(offset: 1);
        return true;
    }

    private static bool ReplaceHarvestQuestOfferingFlagRead(CodeMatcher codeMatcher, MethodInfo getBool, MethodInfo replacement)
    {
        codeMatcher.Start();
        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(predicate: IsHarvestQuestCropFlagIdLoad),
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: getBool))
        });

        if (codeMatcher.IsValid == false)
        {
            OmegasGodTweaks.LogError(message: "TraitAltar._OnOffer transpiler failed: harvest quest offering karma flag read not matched.");
            return false;
        }

        codeMatcher.Advance(offset: 1);
        CodeInstruction instruction = codeMatcher.Instruction;
        instruction.opcode = OpCodes.Call;
        instruction.operand = replacement;
        codeMatcher.Advance(offset: 1);
        return true;
    }

    private static bool ReplaceSetBaseCall(CodeMatcher codeMatcher, MethodInfo setBase, MethodInfo replacement)
    {
        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(predicate: instruction => CallsMethod(instruction: instruction, method: setBase))
        });

        if (codeMatcher.IsValid == false)
        {
            OmegasGodTweaks.LogError(message: "TraitAltar._OnOffer transpiler failed: piety SetBase clamp call not matched.");
            return false;
        }

        CodeInstruction instruction = codeMatcher.Instruction;
        instruction.opcode = OpCodes.Call;
        instruction.operand = replacement;
        return true;
    }

    private static bool CallsMethod(CodeInstruction instruction, MethodInfo method)
    {
        if (instruction.opcode != OpCodes.Call &&
            instruction.opcode != OpCodes.Callvirt)
        {
            return false;
        }

        return Equals(objA: instruction.operand, objB: method);
    }

    private static bool IsHarvestQuestCropFlagIdLoad(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldc_I4_S)
        {
            if (instruction.operand is sbyte shortValue && shortValue == HarvestQuestCropFlagId)
            {
                return true;
            }
        }

        if (instruction.opcode == OpCodes.Ldc_I4)
        {
            if (instruction.operand is int value && value == HarvestQuestCropFlagId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldPreserveOfferingOverflow(int overflowExp, Element result)
    {
        if (OmegasGodTweaksConfig.RemoveOfferingOverflowWaste.Value == false)
        {
            return false;
        }

        if (overflowExp <= result.vExp)
        {
            return false;
        }

        return true;
    }

    private static int GetPietyBaseForOfferingGainCap(Element piety)
    {
        if (OmegasGodTweaksConfig.RemovePietyCapFromOfferings.Value == true)
        {
            FeatureTestLog.Log(feature: "Remove Piety Cap From Offerings", detail: "enabled; bypassed offering piety gain cap.");
            return OfferingPietyCapBypassValue;
        }

        return piety.vBase;
    }

    private static int GetPietyBaseForOfferingClamp(Element piety)
    {
        if (OmegasGodTweaksConfig.RemovePietyCapFromOfferings.Value == true)
        {
            FeatureTestLog.Log(feature: "Remove Piety Cap From Offerings", detail: "enabled; bypassed offering piety clamp.");
            return OfferingPietyCapBypassValue;
        }

        return piety.vBase;
    }

    private static Element SetBasePreservingOfferingOverflow(ElementContainer elements, int id, int value, int potential)
    {
        Element piety = elements.GetOrCreateElement(id: id);
        int overflowExp = piety.vExp;
        int beforeBase = piety.vBase;
        Element result = elements.SetBase(id: id, v: value, potential: potential);

        if (OmegasGodTweaksConfig.RemoveOfferingOverflowWaste.Value == true)
        {
            FeatureTestLog.Log(
                feature: "Remove Offering Overflow Waste",
                detail: "enabled; clamp check beforeBase=" +
                        beforeBase.ToString() +
                        ", requestedBase=" +
                        value.ToString() +
                        ", overflowExpBeforeClamp=" +
                        overflowExp.ToString() +
                        ", expAfterClamp=" +
                        result.vExp.ToString());
        }

        if (ShouldPreserveOfferingOverflow(overflowExp: overflowExp, result: result) == true)
        {
            result.vExp = overflowExp;
            FeatureTestLog.Log(
                feature: "Remove Offering Overflow Waste",
                detail: "enabled; restored overflow piety EXP=" + overflowExp.ToString());
        }

        return result;
    }
}
