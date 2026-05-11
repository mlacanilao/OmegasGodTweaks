using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmegasGodTweaks;

internal static class GodFaithStateService
{
    private const int PietyElementId = 85;
    private const int FaithElementId = 306;
    private const int SpeedElementId = 79;
    private const int DevoutFeatElementId = 1407;
    private const int DemigodFeatElementId = 1228;
    private const string EythReligionId = "eyth";
    private const int CappedGodBonusElementMinId = 950;
    private const int CappedGodBonusElementMaxExclusiveId = 970;
    private static bool applyingState;

    internal static bool IsApplyingState => applyingState;

    internal static void ApplyLoadedState()
    {
        if (EClass.pc == null || EClass.game?.religions == null)
        {
            return;
        }

        FaithSaveData.Current.EnsureCollections();
        SnapshotCurrentFaith(chara: EClass.pc, joined: true);

        if (OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == false)
        {
            return;
        }

        foreach (KeyValuePair<string, GodFaithState?> pair in FaithSaveData.Current.Gods)
        {
            Religion religion = EClass.game.religions.Find(id: pair.Key);
            if (religion == null || pair.Value == null)
            {
                continue;
            }

            religion.giftRank = pair.Value.GiftRank;
        }

        ApplyStateToPlayer(religion: EClass.pc.faith);
        EClass.pc.RefreshFaithElement();
        ElementContainerPatch.RefreshAppliedArtifactEffects();
    }

    internal static bool IsJoined(Religion? religion)
    {
        if (religion == null)
        {
            return false;
        }

        if (EClass.pc != null && EClass.pc.faith == religion)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.AllowJoiningMultipleReligions.Value == false)
        {
            return false;
        }

        if (FaithSaveData.HasState(godId: religion.id) == false)
        {
            return false;
        }

        return FaithSaveData.GetOrCreateState(godId: religion.id).Joined;
    }

    internal static bool IsJoinedGodId(string? godId)
    {
        if (string.IsNullOrWhiteSpace(value: godId) == true)
        {
            return false;
        }

        Religion? religion = EClass.game?.religions?.Find(id: godId!);
        return IsJoined(religion: religion);
    }

    internal static void SnapshotCurrentFaith(Chara chara, bool joined)
    {
        if (chara == null || chara.faith == null)
        {
            return;
        }

        SnapshotFaith(chara: chara, religion: chara.faith, joined: joined);
    }

    internal static void SnapshotFaith(Chara chara, Religion religion, bool joined)
    {
        if (chara == null || religion == null)
        {
            return;
        }

        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        state.Joined = joined;
        state.WorshipDays = chara.c_daysWithGod;
        state.GiftRank = religion.giftRank;

        Element piety = chara.elements.GetOrCreateElement(id: PietyElementId);
        state.PietyBase = piety.vBase;
        state.PietyExp = piety.vExp;
    }

    internal static void MarkJoined(Religion religion)
    {
        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        state.Joined = true;
        state.GiftRank = religion.giftRank;
    }

    internal static void MarkLeft(Religion religion)
    {
        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        state.Joined = false;
        state.GiftRank = religion.giftRank;
    }

    internal static void ClearPlayerPiety()
    {
        if (EClass.pc == null)
        {
            return;
        }

        Chara pc = EClass.pc;
        Element piety = pc.elements.GetOrCreateElement(id: PietyElementId);
        piety.vBase = 0;
        piety.vExp = 0;
        piety.OnChangeValue();
        pc.RefreshFaithElement();
    }

    internal static void ApplyStateToPlayer(Religion religion)
    {
        if (EClass.pc == null || religion == null || FaithSaveData.HasState(godId: religion.id) == false)
        {
            return;
        }

        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        ApplyStateToPlayer(religion: religion, state: state);
    }

    internal static void WithPlayerFaithState(Religion religion, Action action)
    {
        if (EClass.pc == null || religion == null)
        {
            FeatureTestLog.Log(
                feature: "Faith State Switch",
                detail: "fallback action without player or religion; target=" +
                        FeatureTestLog.GetReligionId(religion: religion));
            action();
            return;
        }

        Chara pc = EClass.pc;
        Religion originalFaith = pc.faith;
        SnapshotFaith(chara: pc, religion: originalFaith, joined: true);
        GodFaithState targetState = FaithSaveData.GetOrCreateState(godId: religion.id);
        FeatureTestLog.Log(
            feature: "Faith State Switch",
            detail: "enter; original=" +
                    FeatureTestLog.GetReligionId(religion: originalFaith) +
                    ", target=" +
                    FeatureTestLog.GetReligionId(religion: religion) +
                    ", targetPietyBase=" +
                    targetState.PietyBase.ToString() +
                    ", targetPietyExp=" +
                    targetState.PietyExp.ToString() +
                    ", targetGiftRank=" +
                    targetState.GiftRank.ToString() +
                    ", targetApostleCount=" +
                    targetState.ApostleRewardCount.ToString() +
                    ", targetArtifactCount=" +
                    targetState.ArtifactRewardCount.ToString());

        try
        {
            pc.faith = religion;
            ApplyStateToPlayer(religion: religion);
            FeatureTestLog.Log(
                feature: "Faith State Switch",
                detail: "applied target to player; activeFaith=" +
                        FeatureTestLog.GetReligionId(religion: pc.faith) +
                        ", piety=" +
                        pc.Evalue(ele: PietyElementId).ToString() +
                        ", giftRank=" +
                        religion.giftRank.ToString());
            action();
            SnapshotFaith(chara: pc, religion: religion, joined: true);
            GodFaithState updatedState = FaithSaveData.GetOrCreateState(godId: religion.id);
            FeatureTestLog.Log(
                feature: "Faith State Switch",
                detail: "snapshotted target after action; target=" +
                        FeatureTestLog.GetReligionId(religion: religion) +
                        ", pietyBase=" +
                        updatedState.PietyBase.ToString() +
                        ", pietyExp=" +
                        updatedState.PietyExp.ToString() +
                        ", giftRank=" +
                        updatedState.GiftRank.ToString() +
                        ", apostleCount=" +
                        updatedState.ApostleRewardCount.ToString() +
                        ", artifactCount=" +
                        updatedState.ArtifactRewardCount.ToString());
        }
        finally
        {
            pc.faith = originalFaith;
            ApplyStateToPlayer(religion: originalFaith);
            pc.RefreshFaithElement();
            FeatureTestLog.Log(
                feature: "Faith State Switch",
                detail: "restored original; activeFaith=" +
                        FeatureTestLog.GetReligionId(religion: pc.faith) +
                        ", piety=" +
                        pc.Evalue(ele: PietyElementId).ToString() +
                        ", originalGiftRank=" +
                        originalFaith.giftRank.ToString());
        }
    }

    internal static void AddPiety(Chara chara, int rawAmount)
    {
        if (chara == null)
        {
            return;
        }

        int amount = OmegasGodTweaksConfig.ClampNonNegative(value: rawAmount);
        if (amount <= 0)
        {
            return;
        }

        chara.elements.ModExp(ele: PietyElementId, a: amount, chain: false);
        SnapshotCurrentFaith(chara: chara, joined: true);
        chara.RefreshFaithElement();
    }

    internal static void ShowPietyFaith(Chara chara)
    {
        if (chara == null)
        {
            return;
        }

        int piety = chara.Evalue(ele: PietyElementId);
        int faith = chara.Evalue(ele: FaithElementId);
        string pietyName = EClass.sources.elements.map[key: PietyElementId].GetName();
        string faithName = EClass.sources.elements.map[key: FaithElementId].GetName();
        Msg.SayRaw(text: pietyName + ": " + piety.ToString() + " / " + faithName + ": " + faith.ToString());
    }

    internal static void AddJoinedFaithElements(Chara chara)
    {
        if (CanApplyJoinedFaithElements(chara: chara, game: out Game? game) == false || game == null)
        {
            return;
        }

        ReligionManager? religions = game.religions;

        if (religions?.list == null)
        {
            return;
        }

        foreach (Religion religion in religions.list)
        {
            if (religion == null)
            {
                continue;
            }

            if (religion == chara.faith)
            {
                continue;
            }

            if (religion.IsEyth == true)
            {
                FeatureTestLog.Log(
                    feature: "Eyth / Demigod Edge Cases",
                    detail: "skipped joined Eyth in joined faith bonus loop.");
                continue;
            }

            if (IsJoined(religion: religion) == false)
            {
                continue;
            }

            AddFaithElementsForReligion(chara: chara, religion: religion);
        }

        if (chara.faithElements.parent != chara.elements)
        {
            chara.faithElements.SetParent(c: chara);
        }
    }

    private static bool CanApplyJoinedFaithElements(Chara chara, out Game? game)
    {
        game = null;
        if (chara == null)
        {
            return false;
        }

        game = EClass.core?.game;
        Player? player = game?.player;
        Chara? playerChara = player?.chara;

        if (playerChara == null)
        {
            return false;
        }

        if (ReferenceEquals(objA: chara, objB: playerChara) == false)
        {
            return false;
        }

        if (OmegasGodTweaksConfig.ApplyJoinedGodBonuses.Value == false)
        {
            return false;
        }

        if (chara.faithElements == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(value: chara.idFaith) == true)
        {
            return false;
        }

        if (BlocksFaithBonusesAsEythWithoutDemigod(chara: chara) == true)
        {
            FeatureTestLog.Log(
                feature: "Eyth / Demigod Edge Cases",
                detail: "current faith is Eyth without Demigod; skipped current and joined faith bonuses.");
            return false;
        }

        if (EClass.sources.religions.map.ContainsKey(key: chara.idFaith) == false)
        {
            return false;
        }

        if (chara.HasCondition<ConExcommunication>() == true)
        {
            return false;
        }

        if (game == null)
        {
            return false;
        }

        return true;
    }

    private static bool BlocksFaithBonusesAsEythWithoutDemigod(Chara chara)
    {
        if (string.Equals(a: chara.idFaith, b: EythReligionId, comparisonType: StringComparison.OrdinalIgnoreCase) == false)
        {
            return false;
        }

        if (chara.HasElement(ele: DemigodFeatElementId, includeNagative: false) == true)
        {
            return false;
        }

        return true;
    }

    private static void AddFaithElementsForReligion(Chara chara, Religion religion)
    {
        if (FaithSaveData.HasState(godId: religion.id) == false)
        {
            return;
        }

        if (EClass.sources.religions.map.TryGetValue(key: religion.id, value: out SourceReligion.Row row) == false)
        {
            return;
        }

        GodFaithState state = FaithSaveData.GetOrCreateState(godId: religion.id);
        int pietyValue = GetSavedPietyValueForFaithBonus(chara: chara, state: state);
        if (EClass.sources.elements.alias.TryGetValue(key: "featGod_" + row.id + "1", value: out SourceElement.Row feat) == true &&
            chara.faithElements.ValueWithoutLink(ele: feat.id) <= 0)
        {
            chara.faithElements.SetBase(id: feat.id, v: 1, potential: 0);
        }

        int[] elements = row.elements;
        int num = pietyValue * (120 + chara.Evalue(ele: DevoutFeatElementId) * 15 + chara.Evalue(ele: DemigodFeatElementId) * 20) / 100;
        int appliedElementCount = 0;
        for (int i = 0; i < elements.Length; i += 2)
        {
            int value = elements[i + 1] * num / 50;
            if (elements[i] == SpeedElementId)
            {
                value = EClass.curve(_a: (long)value, start: elements[i + 1] * 2, step: 10, rate: 50);
            }

            int faithResistanceBonusCap = CharaPatch.GetFaithResistanceBonusCap();
            if (value >= faithResistanceBonusCap &&
                elements[i] >= CappedGodBonusElementMinId &&
                elements[i] < CappedGodBonusElementMaxExclusiveId)
            {
                value = faithResistanceBonusCap;
            }

            Element element = chara.faithElements.GetOrCreateElement(id: elements[i]);
            int addedValue = Mathf.Max(a: value, b: 1);
            chara.faithElements.SetBase(id: elements[i], v: element.vBase + addedValue, potential: 0);
            appliedElementCount++;
        }

        if (appliedElementCount > 0)
        {
            FeatureTestLog.Log(
                feature: "Apply Joined God Bonuses",
                detail: "enabled; added faith elements from joined god=" +
                        FeatureTestLog.GetReligionId(religion: religion) +
                        ", savedPiety=" +
                        pietyValue.ToString() +
                        ", elementCount=" +
                        appliedElementCount.ToString());
        }
    }

    private static int GetSavedPietyValueForFaithBonus(Chara chara, GodFaithState state)
    {
        if (chara._IsPC)
        {
            return 10 + (int)(Mathf.Sqrt(f: (float)state.WorshipDays) * 2f + state.PietyBase) / 2;
        }

        return chara.GetPietyValue();
    }

    private static void ApplyStateToPlayer(Religion religion, GodFaithState state)
    {
        if (EClass.pc == null)
        {
            return;
        }

        Chara pc = EClass.pc;
        applyingState = true;
        try
        {
            pc.c_daysWithGod = state.WorshipDays;
            religion.giftRank = state.GiftRank;

            Element piety = pc.elements.GetOrCreateElement(id: PietyElementId);
            piety.vBase = state.PietyBase;
            piety.vExp = state.PietyExp;
            piety.OnChangeValue();
        }
        finally
        {
            applyingState = false;
        }
    }
}
