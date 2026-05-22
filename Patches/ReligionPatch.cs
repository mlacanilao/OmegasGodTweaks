using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace OmegasGodTweaks;

internal static class ReligionPatch
{
    private const int VanillaOfferingWeightValueCap = 1000;
    private const int VanillaOfferingLevelBonusCap = 100;
    private const int MaxOfferingLevelBonusCap = int.MaxValue - 150;
    private const int OfferingWeightDivisor = 10;
    private const int VanillaOfferingCategoryMinimum = 50;
    private const int BlessedOfferingBonus = 50;
    private const int OfferingPercent = 100;
    private const float FinalOfferingValueSafetyClamp = 214748370f;
    private const int PietyElementId = 85;
    private const int ApostleFirstPietyThreshold = 15;
    private const int ArtifactFirstPietyThreshold = 30;
    private const int RepeatRewardPietyInterval = 30;
    private const int NoGiftRank = -1;
    private const int MaxRepeatRewardCount = (int.MaxValue - ArtifactFirstPietyThreshold) / RepeatRewardPietyInterval;
    private const int ApostleGiftRank = 1;
    private const int ArtifactGiftRank = 2;
    private const string ApostleRewardHistoryKey = "apostle";
    private const string ArtifactRewardHistoryKey = "artifact";
    [ThreadStatic]
    private static bool checkingRewardGift;
    [ThreadStatic]
    private static int reservedRepeatRewardRank;
    [ThreadStatic]
    private static int previousRewardGiftRank;
    [ThreadStatic]
    private static bool rewardCheckCompleted;
    private static bool loggedOfferingWeightCapBypass;
    private static bool loggedOfferingLevelBonusCapBypass;
    private static bool repeatRewardGrantRecorderReady;

    internal static IEnumerable<CodeInstruction> GetOfferingValueTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        IEnumerable<CodeInstruction> weightCapInstructions = ReplaceOfferingWeightValueCap(
            instructions: instructions,
            context: "Religion.GetOfferingValue");
        return ReplaceOfferingLevelBonusCap(
            instructions: weightCapInstructions,
            context: "Religion.GetOfferingValue");
    }

    internal static IEnumerable<CodeInstruction> GetOfferingValueSetValueTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return ReplaceOfferingWeightValueCap(
            instructions: instructions,
            context: "Religion.GetOfferingValue.SetValue");
    }

    internal static void GetOfferingValuePostfix(Religion religion, Thing t, int num, ref int __result)
    {
        if (__result > 0)
        {
            return;
        }

        if (OmegasGodTweaksConfig.RemoveOfferingCategoryRestrictions.Value == false)
        {
            return;
        }

        if (religion == null)
        {
            return;
        }

        if (t == null)
        {
            return;
        }

        if (IsMeatOffering(t: t) == true)
        {
            return;
        }

        SourceCategory.Row category = t.category;
        if (category == null)
        {
            return;
        }

        if (category.offer <= 0)
        {
            return;
        }

        int fallbackValue = GetFallbackOfferingCategoryValue(t: t, category: category, num: num);
        if (fallbackValue <= 0)
        {
            return;
        }

        __result = fallbackValue;
        FeatureTestLog.Log(
            feature: "Remove Offering Category Restrictions",
            detail: "enabled; fallback category offering value=" +
                    fallbackValue.ToString() +
                    ", thing=" +
                    FeatureTestLog.GetThingId(thing: t) +
                    ", categoryOffer=" +
                    category.offer.ToString() +
                    ", num=" +
                    num.ToString());
    }

    internal static MethodInfo? FindOfferingValueHelper()
    {
        MethodInfo[] methods = typeof(Religion).GetMethods(bindingAttr: BindingFlags.NonPublic | BindingFlags.Static);
        foreach (MethodInfo method in methods)
        {
            if (HasOfferingValueHelperSignature(method: method) == false)
            {
                continue;
            }

            if (ContainsOfferingValueClampConstants(method: method) == false)
            {
                continue;
            }

            return method;
        }

        return null;
    }

    internal static bool JoinFaithPrefix(Religion religion, Chara c, Religion.ConvertType type)
    {
        if (c == null)
        {
            return true;
        }

        if (c.IsPC == false)
        {
            return true;
        }

        bool isChangingFaith = c.faith != religion;
        GodFaithStateService.SnapshotCurrentFaith(chara: c, joined: true);
        bool shouldClearFreshPiety = ShouldClearFreshPietyAfterJoin(isChangingFaith: isChangingFaith, oldFaith: c.faith, newFaith: religion);
        FeatureTestLog.Log(
            feature: "Faith Conversion Flow",
            detail: "JoinFaith prefix; type=" +
                    type.ToString() +
                    ", oldFaith=" +
                    FeatureTestLog.GetReligionId(religion: c.faith) +
                    ", newFaith=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", isChangingFaith=" +
                    isChangingFaith.ToString() +
                    ", shouldClearFreshPiety=" +
                    shouldClearFreshPiety.ToString() +
                    ", oldLive=" +
                    FeatureTestLog.GetFaithSnapshot(chara: c) +
                    ", newHadSavedState=" +
                    FaithSaveData.HasState(godId: religion.id).ToString() +
                    ", newSaved=" +
                    FeatureTestLog.GetSavedState(godId: religion.id));
        return shouldClearFreshPiety;
    }

    internal static void JoinFaithPostfix(Religion religion, Chara c, Religion.ConvertType type, bool shouldClearFreshPiety)
    {
        if (c == null)
        {
            return;
        }

        if (c.IsPC == false)
        {
            return;
        }

        bool hadSavedProgress = FaithSaveData.HasState(godId: religion.id);
        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        state.Joined = true;
        FeatureTestLog.Log(
            feature: "Faith Conversion Flow",
            detail: "JoinFaith postfix start; type=" +
                    type.ToString() +
                    ", newFaith=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", allowMulti=" +
                    OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value.ToString() +
                    ", hadSavedProgress=" +
                    hadSavedProgress.ToString() +
                    ", shouldClearFreshPiety=" +
                    shouldClearFreshPiety.ToString() +
                    ", liveAfterVanilla=" +
                    FeatureTestLog.GetFaithSnapshot(chara: c) +
                    ", savedBeforeApply=" +
                    FeatureTestLog.GetSavedState(godId: religion.id));

        if (hadSavedProgress == true && OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == true)
        {
            FeatureTestLog.Log(
                feature: "Allow Joining Multiple Religions",
                detail: "enabled; restoring saved state for god=" + FeatureTestLog.GetReligionId(religion: religion));
            GodFaithStateService.ApplyStateToPlayer(religion: religion);
            c.RefreshFaithElement();
        }
        else if (shouldClearFreshPiety == true)
        {
            GodFaithStateService.ClearPlayerPiety();
        }

        GodFaithStateService.SnapshotFaith(chara: c, religion: religion, joined: true);
        ElementContainerPatch.RefreshAppliedArtifactEffects();
        FaithSaveData.SnapshotCurrent();
        FeatureTestLog.Log(
            feature: "Faith Conversion Flow",
            detail: "JoinFaith postfix complete; newFaith=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", liveFinal=" +
                    FeatureTestLog.GetFaithSnapshot(chara: c) +
                    ", savedFinal=" +
                    FeatureTestLog.GetSavedState(godId: religion.id));
    }

    internal static bool LeaveFaithPrefix(Religion religion, Chara c, Religion newFaith, Religion.ConvertType type)
    {
        if (c == null)
        {
            return true;
        }

        if (c.IsPC == false)
        {
            return true;
        }

        if (ShouldReplaceLeaveFaith() == false)
        {
            GodFaithStateService.SnapshotFaith(chara: c, religion: religion, joined: false);
            FeatureTestLog.Log(
                feature: "Faith Conversion Flow",
                detail: "LeaveFaith prefix using vanilla path; oldFaith=" +
                        FeatureTestLog.GetReligionId(religion: religion) +
                        ", newFaith=" +
                        FeatureTestLog.GetReligionId(religion: newFaith) +
                        ", type=" +
                        type.ToString() +
                        ", oldSaved=" +
                        FeatureTestLog.GetSavedState(godId: religion.id));
            return true;
        }

        GodFaithStateService.SnapshotFaith(chara: c, religion: religion, joined: true);

        bool moonShadowTrickerySwap = IsMoonShadowTrickerySwap(oldFaith: religion, newFaith: newFaith);
        FeatureTestLog.Log(
            feature: "Faith Conversion Flow",
            detail: "LeaveFaith prefix replacing vanilla path; oldFaith=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", newFaith=" +
                    FeatureTestLog.GetReligionId(religion: newFaith) +
                    ", type=" +
                    type.ToString() +
                    ", allowMulti=" +
                    OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value.ToString() +
                    ", removePunishment=" +
                    OmegasGodTweaksConfig.RemoveConversionPunishment.Value.ToString() +
                    ", moonShadowTrickerySwap=" +
                    moonShadowTrickerySwap.ToString() +
                    ", liveBeforeLeave=" +
                    FeatureTestLog.GetFaithSnapshot(chara: c) +
                    ", oldSaved=" +
                    FeatureTestLog.GetSavedState(godId: religion.id));

        if (religion.IsEyth == false)
        {
            Msg.Say(idLang: "worship2");

            if (ShouldApplyConversionPunishment(moonShadowTrickerySwap: moonShadowTrickerySwap, type: type) == true)
            {
                religion.Punish(c: c);
            }
            else if (moonShadowTrickerySwap == false &&
                     type != Religion.ConvertType.Campaign &&
                     OmegasGodTweaksConfig.RemoveConversionPunishment.Value == true)
            {
                FeatureTestLog.Log(feature: "Remove Conversion Punishment", detail: "enabled; skipped conversion wrath during LeaveFaith.");
            }

            if (moonShadowTrickerySwap == true)
            {
                religion.Talk(idTalk: "regards", c: null, agent: null);
                if (OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == false)
                {
                    c.elements.SetBase(id: PietyElementId, v: c.Evalue(ele: PietyElementId) / 2, potential: 0);
                }
            }
        }

        if (OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == false)
        {
            GodFaithStateService.MarkLeft(religion: religion);
            FeatureTestLog.Log(
                feature: "Allow Joining Multiple Religions",
                detail: "disabled; marked previous god left=" + FeatureTestLog.GetReligionId(religion: religion));
        }
        else
        {
            GodFaithStateService.MarkJoined(religion: religion);
            FeatureTestLog.Log(
                feature: "Allow Joining Multiple Religions",
                detail: "enabled; kept previous god joined=" + FeatureTestLog.GetReligionId(religion: religion));
        }

        c.faction.charaElements.OnLeaveFaith();
        religion.OnLeaveFaith();
        c.RefreshFaithElement();
        FeatureTestLog.Log(
            feature: "Faith Conversion Flow",
            detail: "LeaveFaith prefix complete; oldFaith=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", newFaith=" +
                    FeatureTestLog.GetReligionId(religion: newFaith) +
                    ", liveAfterLeave=" +
                    FeatureTestLog.GetFaithSnapshot(chara: c) +
                    ", oldSaved=" +
                    FeatureTestLog.GetSavedState(godId: religion.id));
        return false;
    }

    internal static bool PunishPrefix(Chara c)
    {
        if (c == null)
        {
            return true;
        }

        if (c.IsPC == false)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.RemoveConversionPunishment.Value == true)
        {
            FeatureTestLog.Log(feature: "Remove Conversion Punishment", detail: "enabled; skipped Religion.Punish for player.");
            return false;
        }

        return true;
    }

    internal static bool PunishTakeOverPrefix(Chara c)
    {
        if (c == null)
        {
            return true;
        }

        if (c.IsPC == false)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.RemoveAltarTakeoverPunishment.Value == true)
        {
            FeatureTestLog.Log(feature: "Remove Altar Takeover Punishment", detail: "enabled; skipped Religion.PunishTakeOver for player.");
            return false;
        }

        return true;
    }

    internal static void GetGiftRankPrefix(Religion religion)
    {
        if (ShouldLogGiftRankFlow() == false)
        {
            return;
        }

        LogGiftRankPrefix(religion: religion);
    }

    internal static void GetGiftRankPostfix(Religion religion, ref int __result)
    {
        int vanillaResult = __result;
        bool changedResult = false;

        if (__result == NoGiftRank && checkingRewardGift == true)
        {
            changedResult = TrySelectRepeatGiftRank(religion: religion, result: ref __result);
        }

        LogGiftRankPostfix(
            religion: religion,
            vanillaResult: vanillaResult,
            finalResult: __result,
            changedResult: changedResult);
    }

    private static bool TrySelectRepeatGiftRank(Religion religion, ref int result)
    {
        if (checkingRewardGift == false)
        {
            return false;
        }

        if (repeatRewardGrantRecorderReady == false)
        {
            return false;
        }

        if (religion == null)
        {
            return false;
        }

        if (EClass.pc == null)
        {
            return false;
        }

        Chara pc = EClass.pc;
        if (pc.faith != religion)
        {
            return false;
        }

        if (GodFaithStateService.IsJoined(religion: religion) == false)
        {
            return false;
        }

        if (religion.IsEyth == true)
        {
            return false;
        }

        if (religion.source.rewards.Length == 0)
        {
            return false;
        }

        int piety = pc.Evalue(ele: PietyElementId);
        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        if (NormalizeRewardCountsFromGiftRank(religion: religion, state: state) == true)
        {
            FaithSaveData.SnapshotCurrent();
        }

        int selectedRank = NoGiftRank;
        int selectedThreshold = int.MaxValue;

        if (religion.source.rewards.Length >= ArtifactGiftRank)
        {
            if (OmegasGodTweaksConfig.RepeatArtifactRewards.Value == true &&
                TryGetAvailableRepeatRewardThreshold(
                    piety: piety,
                    firstThreshold: ArtifactFirstPietyThreshold,
                    repeatInterval: RepeatRewardPietyInterval,
                    paidCount: state.ArtifactRewardCount,
                    threshold: out int artifactThreshold) == true)
            {
                SelectEarlierRepeatReward(
                    rank: ArtifactGiftRank,
                    threshold: artifactThreshold,
                    selectedRank: ref selectedRank,
                    selectedThreshold: ref selectedThreshold);
            }
        }

        if (OmegasGodTweaksConfig.RepeatApostleRewards.Value == true &&
            TryGetAvailableRepeatRewardThreshold(
                piety: piety,
                firstThreshold: ApostleFirstPietyThreshold,
                repeatInterval: RepeatRewardPietyInterval,
                paidCount: state.ApostleRewardCount,
                threshold: out int apostleThreshold) == true)
        {
            SelectEarlierRepeatReward(
                rank: ApostleGiftRank,
                threshold: apostleThreshold,
                selectedRank: ref selectedRank,
                selectedThreshold: ref selectedThreshold);
        }

        if (selectedRank == NoGiftRank)
        {
            return false;
        }

        result = selectedRank;
        SelectRepeatReward(religion: religion, state: state, rank: selectedRank, piety: piety);
        return true;
    }

    private static bool NormalizeRewardCountsFromGiftRank(Religion religion, GodFaithState state)
    {
        bool changed = false;

        if (religion.giftRank >= ApostleGiftRank && state.ApostleRewardCount < 1)
        {
            state.ApostleRewardCount = 1;
            state.RewardHistory[key: ApostleRewardHistoryKey] = state.ApostleRewardCount;
            changed = true;
        }

        if (religion.giftRank >= ArtifactGiftRank && state.ArtifactRewardCount < 1)
        {
            state.ArtifactRewardCount = 1;
            state.RewardHistory[key: ArtifactRewardHistoryKey] = state.ArtifactRewardCount;
            changed = true;
        }

        if (state.GiftRank < religion.giftRank)
        {
            state.GiftRank = religion.giftRank;
            changed = true;
        }

        return changed;
    }

    private static bool ShouldLogGiftRankFlow()
    {
        if (checkingRewardGift == false)
        {
            return false;
        }

        if (OmegasGodTweaksConfig.RepeatApostleRewards.Value == true)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.RepeatArtifactRewards.Value == true)
        {
            return true;
        }

        return false;
    }

    private static void LogGiftRankPrefix(Religion religion)
    {
        FeatureTestLog.Log(
            feature: "GetGiftRank Flow",
            detail: "prefix; god=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", giftRank=" +
                    GetGiftRankForLog(religion: religion).ToString() +
                    ", piety=" +
                    GetPlayerPietyForLog().ToString() +
                    ", rewardsLength=" +
                    GetRewardLengthForLog(religion: religion).ToString() +
                    ", isEyth=" +
                    GetIsEythForLog(religion: religion).ToString() +
                    ", currentFaith=" +
                    FeatureTestLog.GetReligionId(religion: GetCurrentFaithForLog()) +
                    ", joined=" +
                    GetJoinedForLog(religion: religion).ToString() +
                    ", checkingRewardGift=" +
                    checkingRewardGift.ToString());
    }

    private static void LogGiftRankPostfix(Religion religion, int vanillaResult, int finalResult, bool changedResult)
    {
        if (ShouldLogGiftRankFlow() == false)
        {
            return;
        }

        FeatureTestLog.Log(
            feature: "GetGiftRank Flow",
            detail: "postfix; god=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", vanillaResult=" +
                    vanillaResult.ToString() +
                    ", finalResult=" +
                    finalResult.ToString() +
                    ", changedResult=" +
                    changedResult.ToString() +
                    ", giftRank=" +
                    GetGiftRankForLog(religion: religion).ToString() +
                    ", piety=" +
                    GetPlayerPietyForLog().ToString() +
                    ", rewardsLength=" +
                    GetRewardLengthForLog(religion: religion).ToString() +
                    ", selectedRepeatRank=" +
                    reservedRepeatRewardRank.ToString());
    }

    internal static bool TryGetGiftPrefix(Religion religion)
    {
        if (religion == null)
        {
            return true;
        }

        if (EClass.pc == null)
        {
            return true;
        }

        checkingRewardGift = true;
        reservedRepeatRewardRank = NoGiftRank;
        previousRewardGiftRank = religion.giftRank;
        rewardCheckCompleted = false;
        LogRewardCheckStart(religion: religion);
        return true;
    }

    internal static IEnumerable<CodeInstruction> TryGetGiftTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instructionList = new List<CodeInstruction>(collection: instructions);
        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructionList);

        FieldInfo? giftRankField = AccessTools.Field(type: typeof(Religion), name: nameof(Religion.giftRank));
        MethodInfo? recordGiftGrant = AccessTools.Method(
            type: typeof(ReligionPatch),
            name: nameof(RecordGiftGrantFromTryGetGift),
            parameters: new[] { typeof(Religion), typeof(int) });

        if (giftRankField == null || recordGiftGrant == null)
        {
            repeatRewardGrantRecorderReady = false;
            OmegasGodTweaks.LogError(message: "Religion.TryGetGift transpiler: required member lookup failed");
            return codeMatcher.Instructions();
        }

        bool matchedApostleGift = InjectGiftGrantRecorder(
            codeMatcher: codeMatcher,
            giftRankField: giftRankField,
            recordGiftGrant: recordGiftGrant,
            rank: ApostleGiftRank);
        bool matchedArtifactGift = InjectGiftGrantRecorder(
            codeMatcher: codeMatcher,
            giftRankField: giftRankField,
            recordGiftGrant: recordGiftGrant,
            rank: ArtifactGiftRank);
        repeatRewardGrantRecorderReady = matchedApostleGift == true && matchedArtifactGift == true;

        if (matchedApostleGift == false || matchedArtifactGift == false)
        {
            OmegasGodTweaks.LogError(message: "Religion.TryGetGift transpiler: one or more giftRank assignments were not matched; repeat rewards disabled.");
        }

        return codeMatcher.Instructions();
    }

    private static bool InjectGiftGrantRecorder(
        CodeMatcher codeMatcher,
        FieldInfo giftRankField,
        MethodInfo recordGiftGrant,
        int rank)
    {
        codeMatcher.Start();
        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(opcode: OpCodes.Ldarg_0),
            new CodeMatch(opcode: GetLoadGiftRankOpcode(rank: rank)),
            new CodeMatch(opcode: OpCodes.Stfld, operand: giftRankField)
        });

        if (codeMatcher.IsValid == false)
        {
            return false;
        }

        codeMatcher.Advance(offset: 3);
        codeMatcher.InsertAndAdvance(instructions: new[]
        {
            new CodeInstruction(opcode: OpCodes.Ldarg_0),
            new CodeInstruction(opcode: GetLoadGiftRankOpcode(rank: rank)),
            new CodeInstruction(opcode: OpCodes.Call, operand: recordGiftGrant)
        });

        return true;
    }

    private static OpCode GetLoadGiftRankOpcode(int rank)
    {
        if (rank == ApostleGiftRank)
        {
            return OpCodes.Ldc_I4_1;
        }

        if (rank == ArtifactGiftRank)
        {
            return OpCodes.Ldc_I4_2;
        }

        return OpCodes.Ldc_I4_M1;
    }

    private static void RecordGiftGrantFromTryGetGift(Religion religion, int rank)
    {
        if (checkingRewardGift == false)
        {
            return;
        }

        if (rewardCheckCompleted == true)
        {
            LogRewardCheckCompleteSkip(religion: religion, reason: "already completed");
            return;
        }

        rewardCheckCompleted = true;

        if (religion == null)
        {
            FeatureTestLog.Log(feature: "Repeat Prayer Rewards", detail: "gift grant record skipped; religion is null.");
            return;
        }

        FeatureTestLog.Log(
            feature: "Repeat Prayer Rewards",
            detail: "TryGetGift giftRank assignment observed; god=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", rank=" +
                    rank.ToString() +
                    ", currentGiftRank=" +
                    religion.giftRank.ToString() +
                    ", previousGiftRank=" +
                    previousRewardGiftRank.ToString() +
                    ", selectedRepeatRank=" +
                    reservedRepeatRewardRank.ToString());

        if (reservedRepeatRewardRank != NoGiftRank)
        {
            RestorePreviousGiftRank(religion: religion, previousGiftRank: previousRewardGiftRank);
            RecordRewardGrant(religion: religion, rank: reservedRepeatRewardRank);
            return;
        }

        RecordRewardGrant(religion: religion, rank: rank);
    }

    internal static Exception? TryGetGiftFinalizer(Religion religion, Exception? exception)
    {
        if (exception != null)
        {
            LogRewardCheckCompleteSkip(religion: religion, reason: "exception");
            RestorePreviousGiftRank(religion: religion, previousGiftRank: previousRewardGiftRank);
        }

        ClearRewardCheckState();
        return exception;
    }

    private static bool TryGetAvailableRepeatRewardThreshold(
        int piety,
        int firstThreshold,
        int repeatInterval,
        int paidCount,
        out int threshold)
    {
        threshold = 0;

        if (firstThreshold <= 0)
        {
            return false;
        }

        if (repeatInterval <= 0)
        {
            return false;
        }

        threshold = firstThreshold + repeatInterval * GetNormalizedPaidCount(paidCount: paidCount);
        return piety >= threshold;
    }

    private static int GetNormalizedPaidCount(int paidCount)
    {
        if (paidCount < 0)
        {
            return 0;
        }

        if (paidCount > MaxRepeatRewardCount)
        {
            return MaxRepeatRewardCount;
        }

        return paidCount;
    }

    private static void SelectEarlierRepeatReward(int rank, int threshold, ref int selectedRank, ref int selectedThreshold)
    {
        if (threshold > selectedThreshold)
        {
            return;
        }

        if (threshold == selectedThreshold && rank > selectedRank)
        {
            return;
        }

        selectedRank = rank;
        selectedThreshold = threshold;
    }

    private static void SelectRepeatReward(Religion religion, GodFaithState state, int rank, int piety)
    {
        state.Joined = true;
        state.GiftRank = religion.giftRank;
        reservedRepeatRewardRank = rank;

        if (rank == ApostleGiftRank)
        {
            LogSelectedRepeatReward(
                feature: "Repeat Apostle Rewards",
                religion: religion,
                piety: piety,
                paidCount: state.ApostleRewardCount);
        }
        else if (rank == ArtifactGiftRank)
        {
            LogSelectedRepeatReward(
                feature: "Repeat Artifact Rewards",
                religion: religion,
                piety: piety,
                paidCount: state.ArtifactRewardCount);
        }
    }

    private static void LogRewardCheckStart(Religion religion)
    {
        if (OmegasGodTweaksConfig.RepeatApostleRewards.Value == false &&
            OmegasGodTweaksConfig.RepeatArtifactRewards.Value == false)
        {
            return;
        }

        if (EClass.pc == null)
        {
            return;
        }

        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        FeatureTestLog.Log(
            feature: "Repeat Prayer Rewards",
            detail: "TryGetGift prefix start; vanilla will run, god=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", giftRank=" +
                    religion.giftRank.ToString() +
                    ", piety=" +
                    EClass.pc.Evalue(ele: PietyElementId).ToString() +
                    ", apostleCount=" +
                    state.ApostleRewardCount.ToString() +
                    ", artifactCount=" +
                    state.ArtifactRewardCount.ToString());
    }

    private static void LogRewardCheckCompleteSkip(Religion religion, string reason)
    {
        FeatureTestLog.Log(
            feature: "Repeat Prayer Rewards",
            detail: "TryGetGift complete skipped; reason=" +
                    reason +
                    ", god=" +
                    FeatureTestLog.GetReligionId(religion: religion));
    }

    private static int GetGiftRankForLog(Religion religion)
    {
        if (religion == null)
        {
            return NoGiftRank;
        }

        return religion.giftRank;
    }

    private static int GetPlayerPietyForLog()
    {
        if (EClass.pc == null)
        {
            return NoGiftRank;
        }

        return EClass.pc.Evalue(ele: PietyElementId);
    }

    private static int GetRewardLengthForLog(Religion religion)
    {
        if (religion == null)
        {
            return NoGiftRank;
        }

        if (religion.source == null)
        {
            return NoGiftRank;
        }

        if (religion.source.rewards == null)
        {
            return NoGiftRank;
        }

        return religion.source.rewards.Length;
    }

    private static bool GetIsEythForLog(Religion religion)
    {
        if (religion == null)
        {
            return false;
        }

        return religion.IsEyth;
    }

    private static Religion? GetCurrentFaithForLog()
    {
        if (EClass.pc == null)
        {
            return null;
        }

        return EClass.pc.faith;
    }

    private static bool GetJoinedForLog(Religion religion)
    {
        if (religion == null)
        {
            return false;
        }

        return GodFaithStateService.IsJoined(religion: religion);
    }

    private static void RestorePreviousGiftRank(Religion religion, int previousGiftRank)
    {
        if (previousGiftRank > religion.giftRank)
        {
            religion.giftRank = previousGiftRank;
        }
    }

    private static void RecordRewardGrant(Religion religion, int rank)
    {
        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        state.Joined = true;
        state.GiftRank = religion.giftRank;

        if (rank == ApostleGiftRank)
        {
            state.ApostleRewardCount = GetIncrementedRepeatRewardCount(count: state.ApostleRewardCount);
            state.RewardHistory[key: ApostleRewardHistoryKey] = state.ApostleRewardCount;
            LogRepeatRewardGranted(
                feature: "Repeat Apostle Rewards",
                religion: religion,
                newCount: state.ApostleRewardCount);
        }
        else if (rank == ArtifactGiftRank)
        {
            state.ArtifactRewardCount = GetIncrementedRepeatRewardCount(count: state.ArtifactRewardCount);
            state.RewardHistory[key: ArtifactRewardHistoryKey] = state.ArtifactRewardCount;
            LogRepeatRewardGranted(
                feature: "Repeat Artifact Rewards",
                religion: religion,
                newCount: state.ArtifactRewardCount);
        }

        FaithSaveData.SnapshotCurrent();
    }

    private static int GetIncrementedRepeatRewardCount(int count)
    {
        int normalizedCount = GetNormalizedPaidCount(paidCount: count);
        if (normalizedCount >= MaxRepeatRewardCount)
        {
            return MaxRepeatRewardCount;
        }

        return normalizedCount + 1;
    }

    private static void LogSelectedRepeatReward(string feature, Religion religion, int piety, int paidCount)
    {
        FeatureTestLog.Log(
            feature: feature,
            detail: "enabled; selected repeat reward for god=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", piety=" +
                    piety.ToString() +
                    ", paidCount=" +
                    paidCount.ToString());
    }

    private static void LogRepeatRewardGranted(string feature, Religion religion, int newCount)
    {
        FeatureTestLog.Log(
            feature: feature,
            detail: "reward granted for god=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", newCount=" +
                    newCount.ToString());
    }

    private static void ClearRewardCheckState()
    {
        checkingRewardGift = false;
        reservedRepeatRewardRank = NoGiftRank;
        previousRewardGiftRank = NoGiftRank;
        rewardCheckCompleted = false;
    }

    private static IEnumerable<CodeInstruction> ReplaceOfferingWeightValueCap(IEnumerable<CodeInstruction> instructions, string context)
    {
        MethodInfo? getOfferingWeightValueCap = AccessTools.Method(type: typeof(ReligionPatch), name: nameof(GetOfferingWeightValueCap));
        if (getOfferingWeightValueCap == null)
        {
            OmegasGodTweaks.LogError(message: context + " transpiler failed: required method lookup failed.");
            return instructions;
        }

        MethodInfo? clampInt = AccessTools.Method(type: typeof(Mathf), name: nameof(Mathf.Clamp), parameters: new[] { typeof(int), typeof(int), typeof(int) });
        if (clampInt == null)
        {
            OmegasGodTweaks.LogError(message: context + " transpiler failed: Mathf.Clamp(int,int,int) lookup failed.");
            return instructions;
        }

        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructions);
        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(predicate: IsOfferingWeightValueMinLoad),
            new CodeMatch(predicate: IsVanillaOfferingWeightValueCapLoad),
            new CodeMatch(OpCodes.Call, clampInt)
        });

        if (codeMatcher.IsValid == false)
        {
            OmegasGodTweaks.LogError(message: context + " transpiler failed: offering weight value clamp pattern not matched.");
            return codeMatcher.Instructions();
        }

        codeMatcher.Advance(offset: 1);
        CodeInstruction instruction = codeMatcher.Instruction;
        instruction.opcode = OpCodes.Call;
        instruction.operand = getOfferingWeightValueCap;
        return codeMatcher.Instructions();
    }

    private static IEnumerable<CodeInstruction> ReplaceOfferingLevelBonusCap(IEnumerable<CodeInstruction> instructions, string context)
    {
        MethodInfo? getOfferingLevelBonusCap = AccessTools.Method(type: typeof(ReligionPatch), name: nameof(GetOfferingLevelBonusCap));
        if (getOfferingLevelBonusCap == null)
        {
            OmegasGodTweaks.LogError(message: context + " transpiler failed: level bonus cap method lookup failed.");
            return instructions;
        }

        MethodInfo? minInt = AccessTools.Method(type: typeof(Mathf), name: nameof(Mathf.Min), parameters: new[] { typeof(int), typeof(int) });
        if (minInt == null)
        {
            OmegasGodTweaks.LogError(message: context + " transpiler failed: Mathf.Min(int,int) lookup failed.");
            return instructions;
        }

        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructions);
        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(predicate: IsVanillaOfferingLevelBonusCapLoad),
            new CodeMatch(OpCodes.Call, minInt)
        });

        if (codeMatcher.IsValid == false)
        {
            OmegasGodTweaks.LogError(message: context + " transpiler failed: offering level bonus cap pattern not matched.");
            return codeMatcher.Instructions();
        }

        CodeInstruction instruction = codeMatcher.Instruction;
        instruction.opcode = OpCodes.Call;
        instruction.operand = getOfferingLevelBonusCap;
        return codeMatcher.Instructions();
    }

    private static bool HasOfferingValueHelperSignature(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != 3)
        {
            return false;
        }

        if (parameters[0].ParameterType != typeof(SourceCategory.Row))
        {
            return false;
        }

        if (parameters[1].ParameterType != typeof(int))
        {
            return false;
        }

        if (parameters[2].ParameterType.IsByRef == false)
        {
            return false;
        }

        return true;
    }

    private static bool ContainsOfferingValueClampConstants(MethodInfo method)
    {
        MethodBody? methodBody = method.GetMethodBody();
        if (methodBody == null)
        {
            return false;
        }

        byte[]? ilBytes = methodBody.GetILAsByteArray();
        if (ilBytes == null)
        {
            return false;
        }

        bool foundWeightDivisor = false;
        bool foundCategoryMinimum = false;
        bool foundVanillaCap = false;
        for (int index = 0; index < ilBytes.Length; index++)
        {
            if (TryReadInlineIntOperand(ilBytes: ilBytes, index: index, value: out int value) == false)
            {
                continue;
            }

            if (value == 10)
            {
                foundWeightDivisor = true;
            }
            else if (value == 50)
            {
                foundCategoryMinimum = true;
            }
            else if (value == VanillaOfferingWeightValueCap)
            {
                foundVanillaCap = true;
            }
        }

        return foundWeightDivisor == true &&
               foundCategoryMinimum == true &&
               foundVanillaCap == true;
    }

    private static bool TryReadInlineIntOperand(byte[] ilBytes, int index, out int value)
    {
        value = 0;

        if (ilBytes[index] == OpCodes.Ldc_I4_S.Value)
        {
            if (index + 1 >= ilBytes.Length)
            {
                return false;
            }

            value = (sbyte)ilBytes[index + 1];
            return true;
        }

        if (ilBytes[index] == OpCodes.Ldc_I4.Value)
        {
            if (index + 4 >= ilBytes.Length)
            {
                return false;
            }

            value = System.BitConverter.ToInt32(value: ilBytes, startIndex: index + 1);
            return true;
        }

        return false;
    }

    private static bool IsOfferingWeightValueMinLoad(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldc_I4_1)
        {
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_S)
        {
            if (instruction.operand is sbyte value && value == 50)
            {
                return true;
            }
        }

        if (instruction.opcode == OpCodes.Ldc_I4)
        {
            if (instruction.operand is int value && (value == 1 || value == 50))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsVanillaOfferingWeightValueCapLoad(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldc_I4)
        {
            if (instruction.operand is int value && value == VanillaOfferingWeightValueCap)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsVanillaOfferingLevelBonusCapLoad(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldc_I4_S)
        {
            if (instruction.operand is sbyte value && value == VanillaOfferingLevelBonusCap)
            {
                return true;
            }
        }

        if (instruction.opcode == OpCodes.Ldc_I4)
        {
            if (instruction.operand is int value && value == VanillaOfferingLevelBonusCap)
            {
                return true;
            }
        }

        return false;
    }

    private static int GetOfferingWeightValueCap()
    {
        if (OmegasGodTweaksConfig.RemoveOfferingWeightValueCap.Value == false)
        {
            return VanillaOfferingWeightValueCap;
        }

        if (loggedOfferingWeightCapBypass == false)
        {
            loggedOfferingWeightCapBypass = true;
            FeatureTestLog.Log(
                feature: "Remove Offering Weight Value Cap",
                detail: "enabled; replaced vanilla per-item offering weight cap " +
                        VanillaOfferingWeightValueCap.ToString() +
                        " with int.MaxValue.");
        }

        return int.MaxValue;
    }

    private static int GetOfferingLevelBonusCap()
    {
        if (OmegasGodTweaksConfig.RemoveOfferingLevelBonusCap.Value == false)
        {
            return VanillaOfferingLevelBonusCap;
        }

        if (loggedOfferingLevelBonusCapBypass == false)
        {
            loggedOfferingLevelBonusCapBypass = true;
            FeatureTestLog.Log(
                feature: "Remove Offering Level Bonus Cap",
                detail: "enabled; replaced vanilla offering item level bonus cap " +
                        VanillaOfferingLevelBonusCap.ToString() +
                        " with " +
                        MaxOfferingLevelBonusCap.ToString() +
                        ".");
        }

        return MaxOfferingLevelBonusCap;
    }

    private static int GetFallbackOfferingCategoryValue(Thing t, SourceCategory.Row category, int num)
    {
        if (num == -1)
        {
            num = t.Num;
        }

        long value = Mathf.Clamp(value: t.SelfWeight / OfferingWeightDivisor, min: VanillaOfferingCategoryMinimum, max: GetOfferingWeightValueCap());
        value = value * category.offer / OfferingPercent;
        if (value == 0L)
        {
            return 0;
        }

        if (t.IsDecayed == true)
        {
            value /= OfferingWeightDivisor;
        }

        int levelBonus = Mathf.Min(a: t.LV * 2, b: GetOfferingLevelBonusCap());
        if (t.HasElement(ele: 757, includeNagative: false) == true)
        {
            levelBonus += BlessedOfferingBonus;
        }

        value = value * (OfferingPercent + levelBonus) / OfferingPercent;
        value = (int)Mathf.Clamp(
            value: Mathf.Max(value, 1f) * (float)num,
            min: 1f,
            max: FinalOfferingValueSafetyClamp);
        return (int)value;
    }

    private static bool IsMeatOffering(Thing t)
    {
        if (t.source == null)
        {
            return false;
        }

        return t.source._origin == "meat";
    }

    private static bool ShouldClearFreshPietyAfterJoin(bool isChangingFaith, Religion oldFaith, Religion newFaith)
    {
        if (isChangingFaith == false)
        {
            return false;
        }

        if (ShouldPreserveVanillaEythPiety(oldFaith: oldFaith) == true)
        {
            return false;
        }

        if (ShouldPreserveVanillaMoonShadowTrickeryPiety(oldFaith: oldFaith, newFaith: newFaith) == true)
        {
            return false;
        }

        return true;
    }

    private static bool ShouldPreserveVanillaEythPiety(Religion oldFaith)
    {
        if (OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == true)
        {
            return false;
        }

        if (oldFaith == null)
        {
            return false;
        }

        return oldFaith.IsEyth;
    }

    private static bool ShouldApplyConversionPunishment(bool moonShadowTrickerySwap, Religion.ConvertType type)
    {
        if (moonShadowTrickerySwap == true)
        {
            return false;
        }

        if (type == Religion.ConvertType.Campaign)
        {
            return false;
        }

        if (OmegasGodTweaksConfig.RemoveConversionPunishment.Value == true)
        {
            return false;
        }

        return true;
    }

    private static bool ShouldPreserveVanillaMoonShadowTrickeryPiety(Religion oldFaith, Religion newFaith)
    {
        if (OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == true)
        {
            return false;
        }

        return IsMoonShadowTrickerySwap(oldFaith: oldFaith, newFaith: newFaith);
    }

    private static bool IsMoonShadowTrickerySwap(Religion oldFaith, Religion newFaith)
    {
        if (oldFaith == EClass.game.religions.MoonShadow)
        {
            if (newFaith == EClass.game.religions.Trickery)
            {
                return true;
            }
        }

        if (oldFaith == EClass.game.religions.Trickery)
        {
            if (newFaith == EClass.game.religions.MoonShadow)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldReplaceLeaveFaith()
    {
        if (OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == true)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.RemoveConversionPunishment.Value == true)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.AllowOfferingsForJoinedNonCurrentGods.Value == true)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.AllowPrayerRewardChecksForJoinedGods.Value == true)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.ApplyJoinedGodBonuses.Value == true)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.UnlockGodArtifactFactionEffects.Value == true)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.EnableJoinedGodRevelationRouting.Value == true)
        {
            return true;
        }

        return false;
    }
}
