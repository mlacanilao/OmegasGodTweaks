using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace OmegasGodTweaks;

internal static class ActPrayPatch
{
    private const int DemigodFeatElementId = 1228;
    private const int RevelationTalkVariantCount = 2;

    internal static IEnumerable<CodeInstruction> TryPrayTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher codeMatcher = new CodeMatcher(instructions: instructions);
        MethodInfo? vanillaTryGetGift = AccessTools.Method(type: typeof(Religion), name: nameof(Religion.TryGetGift));
        MethodInfo? tryGetGiftIncludingJoinedGods = AccessTools.Method(
            type: typeof(ActPrayPatch),
            name: nameof(TryGetGiftIncludingJoinedGods),
            parameters: new[] { typeof(Religion), typeof(Chara) });

        if (vanillaTryGetGift == null || tryGetGiftIncludingJoinedGods == null)
        {
            OmegasGodTweaks.LogError(message: "ActPray.TryPray transpiler failed: required method lookup failed.");
            return codeMatcher.Instructions();
        }

        codeMatcher.MatchStartForward(matches: new[]
        {
            new CodeMatch(opcode: OpCodes.Callvirt, operand: vanillaTryGetGift)
        });

        if (codeMatcher.IsValid == false)
        {
            OmegasGodTweaks.LogError(message: "ActPray.TryPray transpiler failed: Religion.TryGetGift call pattern not matched; joined god reward checks remain disabled.");
            return codeMatcher.Instructions();
        }

        CodeInstruction instruction = codeMatcher.Instruction;
        instruction.opcode = OpCodes.Call;
        instruction.operand = tryGetGiftIncludingJoinedGods;
        codeMatcher.Insert(new CodeInstruction(opcode: OpCodes.Ldarg_0));
        return codeMatcher.Instructions();
    }

    internal static TryPrayState TryPrayPrefix(Chara c, bool passive)
    {
        Player? player = EClass.player;
        bool hadPrayed = player?.prayed == true;
        bool reachedVanillaRewardCheck = WouldVanillaReachRewardCheck(c: c, passive: passive);
        bool allowRepeatPrayer = reachedVanillaRewardCheck == true && ShouldAllowMultiplePrayersPerDay(c: c, passive: passive, hadPrayed: hadPrayed);
        FeatureTestLog.Log(
            feature: "Prayer Flow",
            detail: "TryPray prefix; passive=" +
                    passive.ToString() +
                    ", charaIsPc=" +
                    (c?.IsPC == true).ToString() +
                    ", faith=" +
                    FeatureTestLog.GetReligionId(religion: c?.faith) +
                    ", hadPrayed=" +
                    hadPrayed.ToString() +
                    ", reachedRewardCheck=" +
                    reachedVanillaRewardCheck.ToString() +
                    ", clearedDailyPrayerFlag=" +
                    allowRepeatPrayer.ToString());

        if (allowRepeatPrayer == true && player != null)
        {
            FeatureTestLog.Log(
                feature: "Allow Multiple Prayers Per Day",
                detail: "enabled; cleared daily prayer flag before active prayer.");
            player.prayed = false;
        }

        return new TryPrayState(
            reachedVanillaRewardCheck: reachedVanillaRewardCheck,
            hadPrayed: hadPrayed,
            clearedDailyPrayerFlag: allowRepeatPrayer);
    }

    internal static void PrayPostfix(Chara c, bool passive)
    {
        if (c == null || c.IsPC == false)
        {
            return;
        }

        bool pietyApplied = ApplyPrayerPiety(c: c, passive: passive);
        ShowPietyFaithAfterPrayerIfNeeded(c: c, pietyApplied: pietyApplied);

        if (passive == false && OmegasGodTweaksConfig.EnableJoinedGodRevelationRouting.Value == true)
        {
            RouteJoinedGodRevelations(c: c);
        }
    }

    internal static void TryPrayPostfix(Chara c, bool passive, bool __result, TryPrayState state)
    {
        bool vanillaPrayerRan = EClass.player?.prayed == true;
        FeatureTestLog.Log(
            feature: "Prayer Flow",
            detail: "TryPray postfix; result=" +
                    __result.ToString() +
                    ", passive=" +
                    passive.ToString() +
                    ", faith=" +
                    FeatureTestLog.GetReligionId(religion: c?.faith) +
                    ", reachedRewardCheck=" +
                    state.ReachedVanillaRewardCheck.ToString() +
                    ", hadPrayed=" +
                    state.HadPrayed.ToString() +
                    ", willRestoreDailyPrayerFlag=" +
                    state.ClearedDailyPrayerFlag.ToString());

        if (__result == false)
        {
            return;
        }

        if (state.ReachedVanillaRewardCheck == false)
        {
            return;
        }

        if (c == null)
        {
            return;
        }

        if (c.IsPC == false)
        {
            return;
        }

        ShowIgnoredPrayerPietyFaith(c: c, passive: passive, state: state);

        if (passive == true)
        {
            return;
        }

        ApplyRewardOnlyPrayerPiety(c: c, state: state, vanillaPrayerRan: vanillaPrayerRan);
    }

    internal static Exception? TryPrayFinalizer(TryPrayState state, Exception? exception)
    {
        RestoreDailyPrayerFlag(state: state);
        return exception;
    }

    private static bool WouldVanillaReachRewardCheck(Chara c, bool passive)
    {
        if (c == null)
        {
            return false;
        }

        if (c.IsPC == false)
        {
            return false;
        }

        if (passive == true)
        {
            return false;
        }

        if (HasPunishBallPrayer(c: c) == true)
        {
            return false;
        }

        if (c.faith == null)
        {
            return false;
        }

        if (c.faith.IsEyth == true && c.HasElement(ele: DemigodFeatElementId, includeNagative: false) == false)
        {
            FeatureTestLog.Log(
                feature: "Eyth / Demigod Edge Cases",
                detail: "current faith is Eyth without Demigod; vanilla prayer branch does not reach reward checks.");
            return false;
        }

        return true;
    }

    private static bool ShouldAllowMultiplePrayersPerDay(Chara c, bool passive, bool hadPrayed)
    {
        if (OmegasGodTweaksConfig.AllowMultiplePrayersPerDay.Value == false)
        {
            return false;
        }

        if (hadPrayed == false)
        {
            return false;
        }

        if (passive == true)
        {
            return false;
        }

        if (c == null)
        {
            return false;
        }

        if (c.IsPC == false)
        {
            return false;
        }

        if (EClass.player == null)
        {
            return false;
        }

        return true;
    }

    private static void RestoreDailyPrayerFlag(TryPrayState state)
    {
        if (state.ClearedDailyPrayerFlag == false)
        {
            return;
        }

        if (EClass.player == null)
        {
            return;
        }

        EClass.player.prayed = true;
    }

    private static void ShowIgnoredPrayerPietyFaith(Chara c, bool passive, TryPrayState state)
    {
        if (state.ClearedDailyPrayerFlag == true)
        {
            return;
        }

        if (passive == true)
        {
            return;
        }

        if (state.HadPrayed == false)
        {
            return;
        }

        if (OmegasGodTweaksConfig.ShowPietyFaithAfterPrayer.Value == false)
        {
            return;
        }

        GodFaithStateService.ShowPietyFaith(chara: c);
        FeatureTestLog.Log(feature: "Show Piety Faith After Prayer", detail: "enabled; displayed piety/faith for already-prayed active prayer.");
    }

    private static void ShowPietyFaithAfterPrayerIfNeeded(Chara c, bool pietyApplied)
    {
        if (pietyApplied == false)
        {
            return;
        }

        if (OmegasGodTweaksConfig.ShowPietyFaithAfterPrayer.Value == false)
        {
            return;
        }

        GodFaithStateService.ShowPietyFaith(chara: c);
        FeatureTestLog.Log(feature: "Show Piety Faith After Prayer", detail: "enabled; displayed piety/faith after prayer piety.");
    }

    private static bool HasPunishBallPrayer(Chara c)
    {
        if (c.HasCondition<ConWrath>() == true)
        {
            return false;
        }

        Thing punishBall = c.things.Find<TraitPunishBall>();
        return punishBall != null;
    }

    private static bool ApplyPrayerPiety(Chara c, bool passive)
    {
        if (OmegasGodTweaksConfig.AddPietyGainFromPrayer.Value == false)
        {
            return false;
        }

        if (passive == true && OmegasGodTweaksConfig.AllowPassivePrayerPietyGain.Value == false)
        {
            return false;
        }

        int amount = OmegasGodTweaksConfig.ClampNonNegative(value: OmegasGodTweaksConfig.PrayerPietyGain.Value);
        if (amount <= 0)
        {
            return false;
        }

        FeatureTestLog.Log(
            feature: "Add Piety Gain From Prayer",
            detail: "enabled; applying prayer piety rawAmount=" +
                    amount.ToString() +
                    ", passive=" +
                    passive.ToString() +
                    ", faith=" +
                    FeatureTestLog.GetReligionId(religion: c.faith));

        if (passive == true)
        {
            FeatureTestLog.Log(
                feature: "Allow Passive Prayer Piety Gain",
                detail: "enabled; passive prayer can receive piety rawAmount=" + amount.ToString());
        }

        GodFaithStateService.AddPiety(chara: c, rawAmount: amount);

        if (OmegasGodTweaksConfig.ApplyPrayerPietyToJoinedGods.Value == true)
        {
            ApplyPrayerPietyToJoinedGods(c: c, rawAmount: amount);
        }

        FaithSaveData.SnapshotCurrent();
        return true;
    }

    private static bool TryGetGiftIncludingJoinedGods(Religion currentReligion, Chara c)
    {
        bool currentGodGotGift = false;
        if (currentReligion != null)
        {
            currentGodGotGift = currentReligion.TryGetGift();
        }

        if (ShouldCheckJoinedPrayerRewards(c: c) == false)
        {
            return currentGodGotGift;
        }

        foreach (Religion religion in GetJoinedNonCurrentGods(c: c))
        {
            FeatureTestLog.Log(
                feature: "Joined Prayer Rewards",
                detail: "checking joined god reward; currentFaith=" +
                        FeatureTestLog.GetReligionId(religion: c.faith) +
                        ", joinedFaith=" +
                        FeatureTestLog.GetReligionId(religion: religion));

            bool joinedGotGift = false;
            GodFaithStateService.WithPlayerFaithState(
                religion: religion,
                action: () =>
                {
                    joinedGotGift = religion.TryGetGift();
                    FeatureTestLog.Log(
                        feature: "Joined Prayer Rewards",
                        detail: "joined god TryGetGift returned; joinedFaith=" +
                                FeatureTestLog.GetReligionId(religion: religion) +
                                ", result=" +
                                joinedGotGift.ToString() +
                                ", giftRank=" +
                                religion.giftRank.ToString());
                });

        }

        return currentGodGotGift;
    }

    private static bool ShouldCheckJoinedPrayerRewards(Chara c)
    {
        if (OmegasGodTweaksConfig.AllowPrayerRewardChecksForJoinedGods.Value == false)
        {
            FeatureTestLog.Log(feature: "Joined Prayer Rewards", detail: "skipped; toggle disabled.");
            return false;
        }

        if (c == null)
        {
            return false;
        }

        if (c.IsPC == false)
        {
            return false;
        }

        return true;
    }

    private static void ApplyRewardOnlyPrayerPiety(Chara c, TryPrayState state, bool vanillaPrayerRan)
    {
        if (state.HadPrayed == true && state.ClearedDailyPrayerFlag == false)
        {
            return;
        }

        if (vanillaPrayerRan == true)
        {
            return;
        }

        bool pietyApplied = ApplyPrayerPiety(c: c, passive: false);
        ShowPietyFaithAfterPrayerIfNeeded(c: c, pietyApplied: pietyApplied);
    }

    private static void ApplyPrayerPietyToJoinedGods(Chara c, int rawAmount)
    {
        if (c == null)
        {
            return;
        }

        foreach (Religion religion in GetJoinedNonCurrentGods(c: c))
        {
            FeatureTestLog.Log(
                feature: "Apply Prayer Piety To Joined Gods",
                detail: "enabled; applying prayer piety to joinedFaith=" +
                        FeatureTestLog.GetReligionId(religion: religion) +
                        ", rawAmount=" +
                        rawAmount.ToString());
            GodFaithStateService.WithPlayerFaithState(
                religion: religion,
                action: () =>
                {
                    GodFaithStateService.AddPiety(chara: c, rawAmount: rawAmount);
                });
        }
    }

    private static void RouteJoinedGodRevelations(Chara c)
    {
        FeatureTestLog.Log(
            feature: "Enable Joined God Revelation Routing",
            detail: "enabled; routing mode=" +
                    OmegasGodTweaksConfig.RevelationMode.Value.ToString() +
                    ", chance=" +
                    OmegasGodTweaksConfig.ClampPercent(value: OmegasGodTweaksConfig.JoinedGodRevelationChance.Value).ToString());

        switch (OmegasGodTweaksConfig.RevelationMode.Value)
        {
            case RevelationMode.Vanilla:
                return;
            case RevelationMode.SelectedJoinedGod:
                Religion? religion = GetSelectedJoinedNonCurrentGod(c: c);
                if (religion != null)
                {
                    FeatureTestLog.Log(
                        feature: "Revelation Mode",
                        detail: "SelectedJoinedGod; routing joined revelation to god=" +
                                FeatureTestLog.GetReligionId(religion: religion));
                    Reveal(religion: religion);
                }

                return;
            case RevelationMode.AllJoinedGods:
                foreach (Religion joinedReligion in GetJoinedNonCurrentGods(c: c))
                {
                    FeatureTestLog.Log(
                        feature: "Revelation Mode",
                        detail: "AllJoinedGods; routing joined revelation to god=" +
                                FeatureTestLog.GetReligionId(religion: joinedReligion));
                    Reveal(religion: joinedReligion);
                }

                return;
        }
    }

    private static Religion? GetSelectedJoinedNonCurrentGod(Chara c)
    {
        string godId = OmegasGodTweaksConfig.SelectedRevelationGod.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value: godId) == true)
        {
            return null;
        }

        if (EClass.game?.religions == null || c == null)
        {
            return null;
        }

        Religion religion = EClass.game.religions.Find(id: godId.Trim());
        if (religion == null)
        {
            return null;
        }

        if (religion == c.faith)
        {
            return null;
        }

        if (religion.IsEyth == true)
        {
            FeatureTestLog.Log(
                feature: "Eyth / Demigod Edge Cases",
                detail: "selected joined revelation god is Eyth; skipped because vanilla Revelation ignores Eyth.");
            return null;
        }

        if (GodFaithStateService.IsJoined(religion: religion) == false)
        {
            return null;
        }

        return religion;
    }

    private static void Reveal(Religion religion)
    {
        int chance = OmegasGodTweaksConfig.ClampPercent(value: OmegasGodTweaksConfig.JoinedGodRevelationChance.Value);
        if (chance <= 0)
        {
            return;
        }

        string idTalk = "chat";
        if (EClass.rnd(a: RevelationTalkVariantCount) == 0)
        {
            idTalk = "random";
        }

        int vanillaChance = GetVanillaRevelationChance(userChance: chance);
        FeatureTestLog.Log(
            feature: "Joined God Revelation Chance",
            detail: "using configured chance=" +
                    chance.ToString() +
                    ", vanillaChance=" +
                    vanillaChance.ToString() +
                    ", god=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", idTalk=" +
                    idTalk);

        religion.Revelation(
            idTalk: idTalk,
            chance: vanillaChance);
    }

    private static int GetVanillaRevelationChance(int userChance)
    {
        if (userChance < 100)
        {
            return userChance - 1;
        }

        return 100;
    }

    private static List<Religion> GetJoinedNonCurrentGods(Chara c)
    {
        List<Religion> religions = new List<Religion>();
        if (EClass.game?.religions?.list == null || c == null)
        {
            return religions;
        }

        foreach (Religion religion in EClass.game.religions.list)
        {
            if (religion == null)
            {
                continue;
            }

            if (religion == c.faith)
            {
                continue;
            }

            if (religion.IsEyth == true)
            {
                FeatureTestLog.Log(
                    feature: "Eyth / Demigod Edge Cases",
                    detail: "skipped joined Eyth in joined prayer/revelation list.");
                continue;
            }

            if (GodFaithStateService.IsJoined(religion: religion) == false)
            {
                continue;
            }

            religions.Add(item: religion);
        }

        return religions;
    }

    internal readonly struct TryPrayState
    {
        internal TryPrayState(bool reachedVanillaRewardCheck, bool hadPrayed, bool clearedDailyPrayerFlag)
        {
            ReachedVanillaRewardCheck = reachedVanillaRewardCheck;
            HadPrayed = hadPrayed;
            ClearedDailyPrayerFlag = clearedDailyPrayerFlag;
        }

        internal bool ReachedVanillaRewardCheck { get; }

        internal bool HadPrayed { get; }

        internal bool ClearedDailyPrayerFlag { get; }
    }
}
