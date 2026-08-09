using System.Collections.Generic;

namespace OmegasGodTweaks;

internal static class CardPatch
{
    private const int EythArtifactElementId = 1228;

    internal static bool PurgeDuplicateArtifactPrefix(Thing artifact)
    {
        if (artifact == null ||
            artifact.HasTag(tag: CTAG.godArtifact) == false)
        {
            return true;
        }

        if (OmegasGodTweaksConfig.AllowDuplicateGodArtifacts.Value == true)
        {
            PurgeEythArtifactOwnership(artifact: artifact);
            FeatureTestLog.Log(
                feature: "Allow Duplicate God Artifacts",
                detail: "enabled; skipped vanilla duplicate purge for artifact=" + FeatureTestLog.GetThingId(thing: artifact));
            return false;
        }

        if (ShouldSkipEythArtifactOwnership(artifact: artifact) == true)
        {
            PurgeDuplicateArtifacts(artifact: artifact);
            FeatureTestLog.Log(
                feature: "Disable Eyth Single Artifact Purge",
                detail: "enabled; skipped Eyth ownership cleanup but kept duplicate purge for artifact=" +
                        FeatureTestLog.GetThingId(thing: artifact));
            return false;
        }

        return true;
    }

    private static void PurgeEythArtifactOwnership(Thing artifact)
    {
        if (ShouldApplyEythArtifactOwnership(artifact: artifact) == false)
        {
            return;
        }

        foreach (Chara chara in GetPlayerFactionCharacters())
        {
            if (chara.IsPCFactionOrMinion == false)
            {
                continue;
            }

            List<Thing> artifacts = chara.things.List(func: IsOtherEythGodArtifact, onlyAccessible: false);
            if (artifacts.Count == 0)
            {
                continue;
            }

            foreach (Thing otherArtifact in artifacts)
            {
                Religion artifactDeity = EClass.game.religions.GetArtifactDeity(id: otherArtifact.id);
                if (otherArtifact.isEquipped == true)
                {
                    chara.body.Unequip(thing: otherArtifact, refresh: true);
                }

                otherArtifact.c_idDeity = artifactDeity?.id;
                Msg.Say(idLang: "waterCurse", c1: otherArtifact);
            }
        }

        bool IsOtherEythGodArtifact(Thing otherArtifact)
        {
            return otherArtifact.HasTag(tag: CTAG.godArtifact) == true &&
                   otherArtifact != artifact &&
                   otherArtifact.isReplica == false &&
                   otherArtifact.c_idDeity == EClass.game.religions.Eyth.id;
        }
    }

    private static void PurgeDuplicateArtifacts(Thing artifact)
    {
        if (artifact.isReplica == true)
        {
            return;
        }

        foreach (Chara chara in GetPlayerFactionCharacters())
        {
            if (chara.IsPCFactionOrMinion == false)
            {
                continue;
            }

            List<Thing> artifacts = chara.things.List(func: IsDuplicateArtifact, onlyAccessible: false);
            if (artifacts.Count == 0)
            {
                continue;
            }

            foreach (Thing duplicateArtifact in artifacts)
            {
                Msg.Say(idLang: "destroyed_inv_", c1: duplicateArtifact, c2: chara);
                duplicateArtifact.Destroy();
            }
        }

        bool IsDuplicateArtifact(Thing otherArtifact)
        {
            return otherArtifact.id == artifact.id &&
                   otherArtifact != artifact &&
                   otherArtifact.isReplica == false;
        }
    }

    private static bool ShouldSkipEythArtifactOwnership(Thing artifact)
    {
        if (OmegasGodTweaksConfig.DisableEythSingleArtifactPurge.Value == false)
        {
            return false;
        }

        return IsEythArtifactOwnershipTarget(artifact: artifact);
    }

    private static bool ShouldApplyEythArtifactOwnership(Thing artifact)
    {
        if (OmegasGodTweaksConfig.DisableEythSingleArtifactPurge.Value == true)
        {
            return false;
        }

        return IsEythArtifactOwnershipTarget(artifact: artifact);
    }

    private static bool IsEythArtifactOwnershipTarget(Thing artifact)
    {
        if (artifact.isReplica == true)
        {
            return false;
        }

        if (EClass.pc.HasElement(ele: EythArtifactElementId, includeNagative: false) == false)
        {
            return false;
        }

        return artifact.c_idDeity == EClass.game.religions.Eyth.id;
    }

    private static List<Chara> GetPlayerFactionCharacters()
    {
        List<Chara> characters = new List<Chara>();
        foreach (FactionBranch factionBranch in EClass.pc.faction.GetChildren())
        {
            foreach (Chara member in factionBranch.members)
            {
                characters.Add(item: member);
            }
        }

        foreach (Chara chara in EClass._map.charas)
        {
            characters.Add(item: chara);
        }

        return characters;
    }
}
