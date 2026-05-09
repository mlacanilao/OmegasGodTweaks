using System;

namespace OmegasGodTweaks.UI;

internal static class GodAltarStatsUI
{
    internal static void Show(TraitAltar altar)
    {
        if (altar == null)
        {
            return;
        }

        Religion deity = altar.Deity;
        if (deity == null)
        {
            return;
        }

        string title = Localization.GodStatsTitle.lang(ref1: deity.Name, ref2: null, ref3: null, ref4: null, ref5: null);
        string stats = GodStatsFormatter.Format(godId: deity.id, labels: BuildLabels());
        Dialog.Ok(langDetail: title + Environment.NewLine + Environment.NewLine + stats);
    }

    private static GodStatsTextLabels BuildLabels()
    {
        return new GodStatsTextLabels
        {
            NoState = Localization.GodStatsNoState.lang(),
            Joined = Localization.GodStatsJoined.lang(),
            Yes = Localization.GodStatsYes.lang(),
            No = Localization.GodStatsNo.lang(),
            Piety = Localization.GodStatsPiety.lang(),
            PietyExp = Localization.GodStatsPietyExp.lang(),
            WorshipDays = Localization.GodStatsWorshipDays.lang(),
            GiftRank = Localization.GodStatsGiftRank.lang(),
            ApostleRewards = Localization.GodStatsApostleRewards.lang(),
            ArtifactRewards = Localization.GodStatsArtifactRewards.lang()
        };
    }
}
