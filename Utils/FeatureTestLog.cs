namespace OmegasGodTweaks;

internal static class FeatureTestLog
{
    private const int PietyElementId = 85;

    internal static void Log(string feature, string detail)
    {
        OmegasGodTweaks.LogDebug(message: "[FeatureTest] " + feature + ": " + detail);
    }

    internal static string GetReligionId(Religion religion)
    {
        if (religion == null)
        {
            return "<null>";
        }

        return religion.id ?? "<empty>";
    }

    internal static string GetThingId(Thing thing)
    {
        if (thing == null)
        {
            return "<null>";
        }

        return thing.id ?? "<empty>";
    }

    internal static string GetFaithSnapshot(Chara chara)
    {
        if (chara == null)
        {
            return "chara=<null>";
        }

        Element piety = chara.elements.GetOrCreateElement(id: PietyElementId);
        return "faith=" +
               GetReligionId(religion: chara.faith) +
               ", idFaith=" +
               (chara.idFaith ?? "<empty>") +
               ", pietyBase=" +
               piety.vBase.ToString() +
               ", pietyExp=" +
               piety.vExp.ToString() +
               ", pietyValue=" +
               chara.Evalue(ele: PietyElementId).ToString() +
               ", daysWithGod=" +
               chara.c_daysWithGod.ToString();
    }

    internal static string GetSavedState(string godId)
    {
        if (string.IsNullOrWhiteSpace(value: godId) == true)
        {
            return "savedState=<empty-god-id>";
        }

        if (FaithSaveData.HasState(godId: godId) == false)
        {
            return "savedState=<missing>";
        }

        GodFaithState state = FaithSaveData.GetOrCreateState(godId: godId);
        return "savedJoined=" +
               state.Joined.ToString() +
               ", savedPietyBase=" +
               state.PietyBase.ToString() +
               ", savedPietyExp=" +
               state.PietyExp.ToString() +
               ", savedDays=" +
               state.WorshipDays.ToString() +
               ", savedGiftRank=" +
               state.GiftRank.ToString() +
               ", savedApostleCount=" +
               state.ApostleRewardCount.ToString() +
               ", savedArtifactCount=" +
               state.ArtifactRewardCount.ToString();
    }
}
