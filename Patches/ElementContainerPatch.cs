using System;
using System.Collections.Generic;

namespace OmegasGodTweaks;

internal static class ElementContainerPatch
{
    private static bool isForcingTrackedArtifactRemoval;
    private static bool isRefreshingAppliedArtifactEffects;

    internal static void FactionIsEffectivePostfix(Thing t, ref bool __result)
    {
        if (__result == true ||
            t == null)
        {
            return;
        }

        if (isForcingTrackedArtifactRemoval == true &&
            IsAppliedJoinedArtifact(t: t) == true)
        {
            __result = true;
            return;
        }

        if (ShouldApplyJoinedArtifact(t: t) == true)
        {
            __result = true;
            FeatureTestLog.Log(
                feature: "Unlock God Artifact Faction Effects",
                detail: "enabled; ElementContainerFaction.IsEffective allowed artifact=" +
                        FeatureTestLog.GetThingId(thing: t) +
                        ", deityId=" +
                        (t.c_idDeity ?? string.Empty));
        }
    }

    internal static void FactionOnEquipPostfix(Thing t)
    {
        if (ShouldApplyJoinedArtifact(t: t) == false)
        {
            return;
        }

        if (HasGlobalElement(t: t) == false)
        {
            return;
        }

        TrackAppliedArtifact(t: t);
        FeatureTestLog.Log(
            feature: "Unlock God Artifact Faction Effects",
            detail: "enabled; tracked equipped joined god artifact=" +
                    FeatureTestLog.GetThingId(thing: t) +
                    ", uid=" +
                    t.uid.ToString());
    }

    internal static void FactionOnUnequipPrefix(Thing t, out bool __state)
    {
        __state = t != null &&
                  IsAppliedJoinedArtifact(t: t) == true;
        isForcingTrackedArtifactRemoval = __state;
    }

    internal static void FactionOnUnequipPostfix(Thing t, bool __state)
    {
        isForcingTrackedArtifactRemoval = false;

        if (__state == false ||
            t == null)
        {
            return;
        }

        UntrackAppliedArtifact(t: t);
        FeatureTestLog.Log(
            feature: "Unlock God Artifact Faction Effects",
            detail: "removed tracked joined god artifact on unequip=" +
                    FeatureTestLog.GetThingId(thing: t) +
                    ", uid=" +
                    t.uid.ToString());
    }

    internal static Exception? FactionOnUnequipFinalizer(Exception? exception)
    {
        isForcingTrackedArtifactRemoval = false;
        return exception;
    }

    internal static void RefreshAppliedArtifactEffects()
    {
        Core? core = EClass.core;
        Game? game = core?.game;
        Chara? playerChara = game?.player?.chara;

        if (core == null ||
            core.IsGameStarted == false ||
            playerChara == null ||
            playerChara.faction?.charaElements == null)
        {
            return;
        }

        Faction playerFaction = playerChara.faction;
        ElementContainerFaction charaElements = playerFaction.charaElements;
        FaithSaveData.Current.EnsureCollections();
        bool changed = false;
        try
        {
            isRefreshingAppliedArtifactEffects = true;
            List<Thing> equippedArtifacts = ListEquippedGodArtifacts(
                game: game,
                playerChara: playerChara,
                playerFaction: playerFaction);
            foreach (Thing artifact in equippedArtifacts)
            {
                bool isApplied = IsAppliedJoinedArtifact(t: artifact);
                bool shouldApply = ShouldApplyJoinedArtifact(t: artifact);

                if (isApplied == true)
                {
                    if (shouldApply == false)
                    {
                        charaElements.OnUnequip(t: artifact);
                        changed = true;
                        FeatureTestLog.Log(
                            feature: "Unlock God Artifact Faction Effects",
                            detail: "refresh removed previously applied artifact=" +
                                    FeatureTestLog.GetThingId(thing: artifact) +
                                    ", uid=" +
                                    artifact.uid.ToString());
                    }

                    if (shouldApply == true &&
                        TrackAppliedArtifact(t: artifact) == true)
                    {
                        changed = true;
                    }

                    continue;
                }

                if (shouldApply == true &&
                    HasGlobalElement(t: artifact) == true)
                {
                    charaElements.OnEquip(t: artifact);
                    changed = true;
                    FeatureTestLog.Log(
                        feature: "Unlock God Artifact Faction Effects",
                        detail: "refresh applied already-equipped joined artifact=" +
                                FeatureTestLog.GetThingId(thing: artifact) +
                                ", uid=" +
                                artifact.uid.ToString());
                }
            }

            if (RemoveMissingAppliedArtifacts(
                    equippedArtifacts: equippedArtifacts,
                    charaElements: charaElements) == true)
            {
                changed = true;
            }
        }
        finally
        {
            isRefreshingAppliedArtifactEffects = false;
        }

        if (changed == true)
        {
            FaithSaveData.SnapshotCurrent();
        }
    }

    private static bool ShouldApplyJoinedArtifact(Thing t)
    {
        if (OmegasGodTweaksConfig.UnlockGodArtifactFactionEffects.Value == false)
        {
            return false;
        }

        if (t.c_idDeity == GetCurrentFaithId())
        {
            return false;
        }

        if (t.HasTag(tag: CTAG.godArtifact) == false)
        {
            return false;
        }

        if (GodFaithStateService.IsJoinedGodId(godId: t.c_idDeity) == true)
        {
            return true;
        }

        return false;
    }

    private static List<Thing> ListEquippedGodArtifacts(Game game, Chara playerChara, Faction playerFaction)
    {
        List<Thing> artifacts = new List<Thing>();
        HashSet<int> seenCharaIds = new HashSet<int>();
        AddEquippedGodArtifacts(
            chara: playerChara,
            artifacts: artifacts,
            seenCharaIds: seenCharaIds,
            playerFaction: playerFaction);

        foreach (FactionBranch factionBranch in playerFaction.GetChildren())
        {
            if (factionBranch.members == null)
            {
                continue;
            }

            foreach (Chara member in factionBranch.members)
            {
                AddEquippedGodArtifacts(
                    chara: member,
                    artifacts: artifacts,
                    seenCharaIds: seenCharaIds,
                    playerFaction: playerFaction);
            }
        }

        if (game.cards?.globalCharas != null)
        {
            foreach (Chara chara in game.cards.globalCharas.Values)
            {
                AddEquippedGodArtifacts(
                    chara: chara,
                    artifacts: artifacts,
                    seenCharaIds: seenCharaIds,
                    playerFaction: playerFaction);
            }
        }

        Map? map = game.activeZone?.map;
        if (map?.charas != null)
        {
            foreach (Chara chara in map.charas)
            {
                AddEquippedGodArtifacts(
                    chara: chara,
                    artifacts: artifacts,
                    seenCharaIds: seenCharaIds,
                    playerFaction: playerFaction);
            }
        }

        return artifacts;
    }

    private static void AddEquippedGodArtifacts(
        Chara? chara,
        List<Thing> artifacts,
        HashSet<int> seenCharaIds,
        Faction playerFaction)
    {
        if (chara == null ||
            seenCharaIds.Add(item: chara.uid) == false ||
            chara.faction != playerFaction ||
            chara.body?.slots == null)
        {
            return;
        }

        foreach (BodySlot bodySlot in chara.body.slots)
        {
            Thing thing = bodySlot.thing;
            if (thing == null)
            {
                continue;
            }

            if (thing.HasTag(tag: CTAG.godArtifact) == true)
            {
                artifacts.Add(item: thing);
            }
        }
    }

    private static bool RemoveMissingAppliedArtifacts(
        List<Thing> equippedArtifacts,
        ElementContainerFaction charaElements)
    {
        HashSet<int> equippedArtifactIds = new HashSet<int>();
        foreach (Thing artifact in equippedArtifacts)
        {
            equippedArtifactIds.Add(item: artifact.uid);
        }

        List<int> missingArtifactIds = new List<int>();
        foreach (int artifactId in FaithSaveData.Current.AppliedJoinedArtifactIds)
        {
            if (equippedArtifactIds.Contains(item: artifactId) == false)
            {
                missingArtifactIds.Add(item: artifactId);
            }
        }

        bool removedAny = false;
        foreach (int artifactId in missingArtifactIds)
        {
            if (RemoveAppliedArtifactBonuses(
                charaElements: charaElements,
                artifactId: artifactId) == false)
            {
                continue;
            }

            FaithSaveData.Current.AppliedJoinedArtifactIds.Remove(item: artifactId);
            FaithSaveData.Current.AppliedJoinedArtifactBonuses.Remove(key: artifactId);
            removedAny = true;
        }

        return removedAny;
    }

    private static bool TrackAppliedArtifact(Thing t)
    {
        FaithSaveData.Current.EnsureCollections();
        Dictionary<int, int> bonuses = CaptureGlobalElementBonuses(t: t);
        bool addedId = FaithSaveData.Current.AppliedJoinedArtifactIds.Add(item: t.uid);
        bool changedBonuses = SetAppliedArtifactBonuses(
            artifactId: t.uid,
            bonuses: bonuses);

        if (addedId == true ||
            changedBonuses == true)
        {
            SnapshotIfNotRefreshing();
            return true;
        }

        return false;
    }

    private static void UntrackAppliedArtifact(Thing t)
    {
        FaithSaveData.Current.EnsureCollections();
        bool removedId = FaithSaveData.Current.AppliedJoinedArtifactIds.Remove(item: t.uid);
        bool removedBonuses = FaithSaveData.Current.AppliedJoinedArtifactBonuses.Remove(key: t.uid);

        if (removedId == true ||
            removedBonuses == true)
        {
            SnapshotIfNotRefreshing();
        }
    }

    private static void SnapshotIfNotRefreshing()
    {
        if (isRefreshingAppliedArtifactEffects == true)
        {
            return;
        }

        FaithSaveData.SnapshotCurrent();
    }

    private static bool IsAppliedJoinedArtifact(Thing t)
    {
        FaithSaveData.Current.EnsureCollections();
        return FaithSaveData.Current.AppliedJoinedArtifactIds.Contains(item: t.uid);
    }

    private static Dictionary<int, int> CaptureGlobalElementBonuses(Thing t)
    {
        Dictionary<int, int> bonuses = new Dictionary<int, int>();

        if (t.elements == null)
        {
            return bonuses;
        }

        foreach (Element element in t.elements.dict.Values)
        {
            if (element.IsGlobalElement == false)
            {
                continue;
            }

            if (bonuses.TryGetValue(key: element.id, value: out int currentValue) == true)
            {
                bonuses[key: element.id] = currentValue + element.Value;
                continue;
            }

            bonuses[key: element.id] = element.Value;
        }

        return bonuses;
    }

    private static bool SetAppliedArtifactBonuses(int artifactId, Dictionary<int, int> bonuses)
    {
        if (FaithSaveData.Current.AppliedJoinedArtifactBonuses.TryGetValue(
                key: artifactId,
                value: out Dictionary<int, int>? currentBonuses) == true &&
            AreBonusesEqual(left: currentBonuses, right: bonuses) == true)
        {
            return false;
        }

        FaithSaveData.Current.AppliedJoinedArtifactBonuses[key: artifactId] = bonuses;
        return true;
    }

    private static bool AreBonusesEqual(Dictionary<int, int>? left, Dictionary<int, int> right)
    {
        if (left == null)
        {
            return false;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (KeyValuePair<int, int> pair in right)
        {
            if (left.TryGetValue(key: pair.Key, value: out int leftValue) == false)
            {
                return false;
            }

            if (leftValue != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool RemoveAppliedArtifactBonuses(
        ElementContainerFaction charaElements,
        int artifactId)
    {
        if (FaithSaveData.Current.AppliedJoinedArtifactBonuses.TryGetValue(
                key: artifactId,
                value: out Dictionary<int, int>? bonuses) == false ||
            bonuses == null)
        {
            return false;
        }

        foreach (KeyValuePair<int, int> pair in bonuses)
        {
            charaElements.ModBase(ele: pair.Key, v: -pair.Value);
            charaElements.isDirty = true;
        }

        charaElements.CheckDirty();
        return true;
    }

    private static bool HasGlobalElement(Thing t)
    {
        if (t.elements == null)
        {
            return false;
        }

        foreach (Element element in t.elements.dict.Values)
        {
            if (element.IsGlobalElement == true)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetCurrentFaithId()
    {
        Game? game = EClass.core?.game;
        Chara? playerChara = game?.player?.chara;

        if (playerChara == null)
        {
            return null;
        }

        return playerChara.idFaith;
    }
}
